import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router';
import { ApiError } from '../../api/client';
import { PaginaModulo } from '../../components/PaginaModulo';
import { actualizarReclamo, cerrarReclamo, crearReclamo, iniciarReclamo, rechazarReclamo, resolverReclamo } from './api';
import { ReclamoAccionDialog, type TipoAccionReclamo } from './ReclamoAccionDialog';
import { ReclamoDetallePanel } from './ReclamoDetallePanel';
import { ReclamoFormulario } from './ReclamoFormulario';
import { ReclamosFiltros } from './ReclamosFiltros';
import { ReclamosResumen } from './ReclamosResumen';
import { ReclamosTabla } from './ReclamosTabla';
import { useReclamosData } from './useReclamosData';
import type { FiltrosReclamos, ReclamoListado } from './tipos';

const filtrosIniciales: FiltrosReclamos = { pagina: 1, tamanoPagina: 10 };

function idQuery(valor: string | null): number | null {
  if (valor === null || !/^\d+$/.test(valor)) return null;
  const id = Number(valor);
  return id > 0 ? id : null;
}

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function ReclamosPostventaPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [filtros, setFiltros] = useState(filtrosIniciales);
  const [busqueda, setBusqueda] = useState('');
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(() => idQuery(searchParams.get('reclamo')));
  const [formulario, setFormulario] = useState<'crear' | 'editar' | null>(null);
  const [accion, setAccion] = useState<TipoAccionReclamo | null>(null);
  const [errorAccion, setErrorAccion] = useState('');
  const [ocupado, setOcupado] = useState(false);
  const primeraBusqueda = useRef(true);
  const filtrosRef = useRef(filtros);
  const botonAccionRef = useRef<HTMLButtonElement | null>(null);
  filtrosRef.current = filtros;
  const data = useReclamosData(filtros, seleccionadoId);

  useEffect(() => {
    if (primeraBusqueda.current) {
      primeraBusqueda.current = false;
      return;
    }
    const temporizador = window.setTimeout(() => {
      if ((filtrosRef.current.busqueda ?? '') === busqueda) return;
      setFiltros((actual) => actual.busqueda === busqueda ? actual : { ...actual, busqueda, pagina: 1 });
      setSeleccionadoId(null);
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
  }

  function cambiarFiltro(cambio: Partial<FiltrosReclamos>): void {
    setFiltros((actual) => ({ ...actual, ...cambio, pagina: 1 }));
    setSeleccionadoId(null);
    setSearchParams({});
  }

  function seleccionar(item: ReclamoListado): void {
    setSeleccionadoId(item.id);
    setFormulario(null);
    setSearchParams({ reclamo: String(item.id) });
  }

  function abrirAccion(tipo: TipoAccionReclamo, boton: HTMLButtonElement): void {
    botonAccionRef.current = boton;
    setErrorAccion('');
    setAccion(tipo);
  }

  function cerrarAccion(): void {
    setAccion(null);
    setErrorAccion('');
    window.requestAnimationFrame(() => botonAccionRef.current?.focus());
  }

  async function guardar(request: Parameters<typeof crearReclamo>[0]): Promise<void> {
    const respuesta = formulario === 'editar' && data.reclamo !== null
      ? await actualizarReclamo(data.reclamo.id, request)
      : await crearReclamo(request);
    setFormulario(null);
    setSeleccionadoId(respuesta.id);
    setSearchParams({ reclamo: String(respuesta.id) });
    data.recargar();
  }

  async function confirmarAccion(texto: string): Promise<void> {
    if (data.reclamo === null || accion === null) return;
    setOcupado(true);
    setErrorAccion('');
    try {
      if (accion === 'iniciar') await iniciarReclamo(data.reclamo.id);
      if (accion === 'resolver') await resolverReclamo(data.reclamo.id, { resolucion: texto });
      if (accion === 'cerrar') await cerrarReclamo(data.reclamo.id);
      if (accion === 'rechazar') await rechazarReclamo(data.reclamo.id, { motivo: texto });
      setAccion(null);
      data.recargar();
    } catch (error: unknown) {
      setErrorAccion(mensaje(error, 'No se pudo actualizar el reclamo.'));
    } finally {
      setOcupado(false);
    }
  }

  return (
    <PaginaModulo className="reclamos-page" modulo="09-reclamos-postventa">
      <p className="module-intro">Registra reclamos de clientes y sigue su atención hasta el cierre.</p>
      <ReclamosResumen datos={data.resumen} error={data.errorResumen} />
      {data.errorCatalogos && <p className="clientes-error" role="alert">{data.errorCatalogos}</p>}
      <div className="reclamos-work-area">
        <section aria-labelledby="reclamos-list-title" className="reclamos-panel reclamos-list-panel">
          <div className="reclamos-panel-heading"><div><span className="page-eyebrow">POSTVENTA</span><h2 id="reclamos-list-title">Bandeja de reclamos</h2></div><button className="primary-button" onClick={() => setFormulario('crear')} type="button">Nuevo reclamo</button></div>
          <ReclamosFiltros busqueda={busqueda} filtros={filtros} clientes={data.clientes} cambiarBusqueda={cambiarBusqueda} cambiarFiltro={cambiarFiltro} />
          <ReclamosTabla lista={data.lista} error={data.errorLista} seleccionadoId={seleccionadoId} seleccionar={seleccionar} pagina={filtros.pagina ?? 1} cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))} />
        </section>
        <ReclamoDetallePanel reclamo={data.reclamo} cargando={seleccionadoId !== null && data.reclamo === null && data.errorDetalle === ''} abrirFormulario={() => setFormulario('editar')} accion={abrirAccion} />
      </div>
      {data.errorDetalle && <p className="clientes-error" role="alert">{data.errorDetalle}</p>}
      {formulario && <ReclamoFormulario key={formulario === 'editar' ? `editar-${data.reclamo?.id ?? 0}` : 'crear'} reclamo={formulario === 'editar' ? data.reclamo : null} clientes={data.clientes} cargarOrdenes={data.cargarOrdenes} cancelar={() => setFormulario(null)} guardar={guardar} />}
      {accion && data.reclamo && <ReclamoAccionDialog tipo={accion} numero={data.reclamo.numero} error={errorAccion} ocupado={ocupado} cerrar={cerrarAccion} confirmar={(texto) => void confirmarAccion(texto)} />}
    </PaginaModulo>
  );
}
