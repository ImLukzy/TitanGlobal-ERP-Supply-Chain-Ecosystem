import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router';
import { ApiError } from '../../api/client';
import { PaginaModulo } from '../../components/PaginaModulo';
import { anularOrdenAbastecimiento, actualizarOrdenAbastecimiento, crearOrdenAbastecimiento, recibirOrdenAbastecimiento } from './api';
import { OrdenDetallePanel } from './OrdenDetallePanel';
import { OrdenesFiltros } from './OrdenesFiltros';
import { OrdenesOverlays } from './OrdenesOverlays';
import { OrdenesResumen } from './OrdenesResumen';
import { OrdenesTabla } from './OrdenesTabla';
import { useOrdenesData } from './useOrdenesData';
import type { FiltrosOrdenAbastecimiento, GuardarOrdenAbastecimientoRequest, OrdenAbastecimientoListado } from './tipos';

const filtrosIniciales: FiltrosOrdenAbastecimiento = { pagina: 1, tamanoPagina: 10 };

function obtenerMensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

function obtenerIdQuery(valor: string | null): number | null {
  if (valor === null || !/^\d+$/.test(valor)) {
    return null;
  }
  const id = Number(valor);
  return id > 0 ? id : null;
}

export function OrdenesAbastecimientoPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const omitirBusquedaInicialRef = useRef(searchParams.has('orden'));
  const [filtros, setFiltros] = useState<FiltrosOrdenAbastecimiento>(filtrosIniciales);
  const [busqueda, setBusqueda] = useState('');
  const [errorFecha, setErrorFecha] = useState('');
  const [seleccionadaId, setSeleccionadaId] = useState<number | null>(() => obtenerIdQuery(searchParams.get('orden')));
  const [formulario, setFormulario] = useState<'crear' | 'editar' | null>(null);
  const [accion, setAccion] = useState<'recibir' | 'anular' | null>(null);
  const [errorAccion, setErrorAccion] = useState('');
  const [ocupada, setOcupada] = useState(false);
  const primeraBusquedaRef = useRef(true);
  const botonAccionRef = useRef<HTMLButtonElement>(null);
  const { lista, resumen, proveedores, productos, errorLista, errorResumen, errorCatalogos, orden, errorDetalle, cargandoDetalle, recargar } = useOrdenesData(filtros, seleccionadaId);

  useEffect(() => {
    if (primeraBusquedaRef.current) {
      primeraBusquedaRef.current = false;
      return;
    }
    if (busqueda === '' && omitirBusquedaInicialRef.current) {
      return;
    }
    const temporizador = window.setTimeout(() => {
      setFiltros((actuales) => ({ ...actuales, busqueda, pagina: 1 }));
      setSeleccionadaId(null);
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarBusqueda(valor: string): void {
    omitirBusquedaInicialRef.current = false;
    setBusqueda(valor);
    if (valor !== '') {
      setSearchParams({});
    }
  }

  function cambiarFiltros(cambios: Partial<FiltrosOrdenAbastecimiento>): void {
    const siguientes = { ...filtros, ...cambios, pagina: 1 };
    const desde = siguientes.fechaDesde ?? '';
    const hasta = siguientes.fechaHasta ?? '';
    if (desde !== '' && hasta !== '' && desde > hasta) {
      setErrorFecha('La fecha desde no puede ser posterior a la fecha hasta.');
      return;
    }
    setErrorFecha('');
    setFiltros(siguientes);
    setSeleccionadaId(null);
    setSearchParams({});
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actuales) => ({ ...actuales, pagina }));
  }

  function seleccionar(item: OrdenAbastecimientoListado): void {
    setSeleccionadaId(item.id);
    setFormulario(null);
    setSearchParams({ orden: String(item.id) });
  }

  function abrirAccion(tipo: 'recibir' | 'anular', boton: HTMLButtonElement): void {
    botonAccionRef.current = boton;
    setErrorAccion('');
    setAccion(tipo);
  }

  function cerrarAccion(): void {
    setAccion(null);
    setErrorAccion('');
    window.requestAnimationFrame(() => botonAccionRef.current?.focus());
  }

  async function guardar(request: GuardarOrdenAbastecimientoRequest): Promise<void> {
    const respuesta = formulario === 'editar' && orden !== null
      ? await actualizarOrdenAbastecimiento(orden.id, request)
      : await crearOrdenAbastecimiento(request);
    setFormulario(null);
    setSeleccionadaId(respuesta.id);
    setSearchParams({ orden: String(respuesta.id) });
    recargar();
  }

  async function confirmarAccion(motivo?: string): Promise<void> {
    if (orden === null || accion === null) {
      return;
    }
    setErrorAccion('');
    setOcupada(true);
    try {
      if (accion === 'recibir') {
        await recibirOrdenAbastecimiento(orden.id);
      } else {
        await anularOrdenAbastecimiento(orden.id, { motivo: motivo ?? '' });
      }
      setAccion(null);
      recargar();
    } catch (error: unknown) {
      setErrorAccion(obtenerMensaje(error, 'No se pudo actualizar la orden.'));
    } finally {
      setOcupada(false);
    }
  }

  return (
    <PaginaModulo className="ordenes-abastecimiento-page" modulo="15-ordenes-abastecimiento">
      <p className="module-intro">Emite órdenes a proveedores, controla entregas y registra su recepción.</p>
      <OrdenesResumen datos={resumen} error={errorResumen} />
      {errorCatalogos && <p className="orden-error" role="alert">{errorCatalogos}</p>}
      <div className="ordenes-work-area">
        <section aria-labelledby="ordenes-list-title" className="ordenes-panel ordenes-list-panel">
          <div className="ordenes-panel-heading">
            <div>
              <span className="page-eyebrow">ABASTECIMIENTO</span>
              <h2 id="ordenes-list-title">Órdenes de abastecimiento</h2>
            </div>
            <button className="primary-button" onClick={() => setFormulario('crear')} type="button">Nueva orden</button>
          </div>
          <OrdenesFiltros
            busqueda={busqueda}
            cambiarBusqueda={cambiarBusqueda}
            cambiarFiltros={cambiarFiltros}
            errorFecha={errorFecha}
            filtros={filtros}
            proveedores={proveedores}
          />
          <OrdenesTabla
            cambiarPagina={cambiarPagina}
            error={errorLista}
            lista={lista}
            pagina={filtros.pagina ?? 1}
            seleccionadaId={seleccionadaId}
            seleccionar={seleccionar}
          />
        </section>
        <OrdenDetallePanel
          anular={(boton) => abrirAccion('anular', boton)}
          cargando={cargandoDetalle}
          editar={() => setFormulario('editar')}
          recibir={(boton) => abrirAccion('recibir', boton)}
          orden={orden}
        />
      </div>
      {errorDetalle && <p className="orden-error" role="alert">{errorDetalle}</p>}
      <OrdenesOverlays
        accion={accion}
        cancelarFormulario={() => setFormulario(null)}
        cerrarAccion={cerrarAccion}
        confirmarAccion={confirmarAccion}
        errorAccion={errorAccion}
        formulario={formulario}
        guardar={guardar}
        ocupada={ocupada}
        orden={orden}
        productos={productos}
        proveedores={proveedores}
      />
    </PaginaModulo>
  );
}
