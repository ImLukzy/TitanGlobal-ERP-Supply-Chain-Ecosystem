import { useEffect, useMemo, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import {
  aprobarDevolucion,
  actualizarDevolucion,
  crearDevolucion,
  eliminarDevolucion,
  recibirDevolucion,
  rechazarDevolucion,
} from './api';
import { DevolucionAccionDialog, type AccionDevolucion } from './DevolucionAccionDialog';
import { DevolucionDetallePanel } from './DevolucionDetallePanel';
import { DevolucionFormulario } from './DevolucionFormulario';
import { DevolucionesFiltros } from './DevolucionesFiltros';
import { DevolucionesResumen } from './DevolucionesResumen';
import { DevolucionesTabla } from './DevolucionesTabla';
import { useDevolucionesData } from './useDevolucionesData';
import type { FiltrosDevolucion, GuardarDevolucionRequest } from './tipos';
import './devoluciones.css';

const filtrosIniciales: FiltrosDevolucion = {
  busqueda: '',
  estado: '',
  motivo: '',
  clienteId: '',
  pagina: 1,
  tamanoPagina: 10,
};

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function DevolucionesComercialesPage() {
  const [filtros, setFiltros] = useState(filtrosIniciales);
  const [busqueda, setBusqueda] = useState('');
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [formulario, setFormulario] = useState(false);
  const [accion, setAccion] = useState<AccionDevolucion | null>(null);
  const [errorAccion, setErrorAccion] = useState('');
  const [procesandoAccion, setProcesandoAccion] = useState(false);
  const botonAccionRef = useRef<HTMLButtonElement>(null);
  const filtrosConsulta = useMemo(() => filtros, [filtros]);
  const data = useDevolucionesData(filtrosConsulta, seleccionadoId);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setFiltros((actual) => (
        actual.busqueda === busqueda ? actual : { ...actual, busqueda, pagina: 1 }
      ));
    }, 300);
    return () => window.clearTimeout(timer);
  }, [busqueda]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setSeleccionadoId(null);
  }

  function cambiarFiltro(cambios: Partial<FiltrosDevolucion>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: cambios.pagina ?? 1 }));
    setSeleccionadoId(null);
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setFormulario(false);
  }

  function abrirAccion(tipo: AccionDevolucion, boton: HTMLButtonElement): void {
    botonAccionRef.current = boton;
    setErrorAccion('');
    setAccion(tipo);
  }

  function cerrarAccion(): void {
    setAccion(null);
    setErrorAccion('');
    botonAccionRef.current?.focus();
  }

  async function guardar(request: GuardarDevolucionRequest): Promise<void> {
    if (data.detalle?.estado === 'Solicitada' && formulario) {
      await actualizarDevolucion(data.detalle.id, request);
    } else {
      const creada = await crearDevolucion(request);
      setSeleccionadoId(creada.id);
    }
    setFormulario(false);
    data.recargar();
  }

  async function confirmarAccion(motivo?: string): Promise<void> {
    if (data.detalle === null || accion === null) {
      return;
    }
    setProcesandoAccion(true);
    setErrorAccion('');
    try {
      if (accion === 'aprobar') {
        await aprobarDevolucion(data.detalle.id);
      }
      if (accion === 'recibir') {
        await recibirDevolucion(data.detalle.id);
      }
      if (accion === 'rechazar') {
        await rechazarDevolucion(data.detalle.id, motivo ?? '');
      }
      if (accion === 'eliminar') {
        await eliminarDevolucion(data.detalle.id);
        setSeleccionadoId(null);
      }
      cerrarAccion();
      data.recargar();
    } catch (error: unknown) {
      setErrorAccion(mensaje(error, 'No se pudo actualizar la devolución.'));
    } finally {
      setProcesandoAccion(false);
    }
  }

  const devoluciones = data.lista?.items ?? [];
  const detalleCargando = seleccionadoId !== null && data.detalle === null && data.errorDetalle === '';
  const devolucionEditable = data.detalle?.estado === 'Solicitada' ? data.detalle : null;

  return (
    <PaginaModulo className="devoluciones-page" modulo="10-devoluciones-comerciales">
      <p className="module-intro">
        Registra y controla las devoluciones comerciales de productos vendidos.
      </p>
      <DevolucionesResumen datos={data.resumen} error={data.errorResumen} />
      {data.errorCatalogos && (
        <p className="clientes-error" role="alert">{data.errorCatalogos}</p>
      )}
      <div className="devoluciones-work-area">
        <section
          aria-labelledby="devoluciones-list-title"
          className="clientes-list-panel devoluciones-list-panel"
        >
          <div className="clientes-panel-heading">
            <div>
              <span className="page-eyebrow">COMERCIAL</span>
              <h2 id="devoluciones-list-title">Devoluciones comerciales</h2>
            </div>
            <button className="primary-button" onClick={() => setFormulario(true)} type="button">
              Nueva devolución
            </button>
          </div>
          <DevolucionesFiltros
            busqueda={busqueda}
            cambiarBusqueda={cambiarBusqueda}
            cambiarFiltro={cambiarFiltro}
            clientes={data.clientes}
            filtros={filtros}
          />
          <DevolucionesTabla
            cambiarPagina={(pagina) => cambiarFiltro({ pagina })}
            cargando={data.lista === null && data.errorLista === ''}
            error={data.errorLista}
            items={devoluciones}
            pagina={filtros.pagina}
            seleccionar={seleccionar}
            seleccionadoId={seleccionadoId}
            total={data.lista?.total ?? 0}
          />
        </section>
        <DevolucionDetallePanel
          accion={abrirAccion}
          cargando={detalleCargando}
          devolucion={data.detalle}
          editar={() => setFormulario(true)}
          error={data.errorDetalle}
        />
      </div>
      {formulario && (
        <DevolucionFormulario
          cancelar={() => setFormulario(false)}
          clientes={data.clientes}
          devolucion={devolucionEditable}
          guardar={guardar}
          key={data.detalle?.id ?? 'nueva'}
          reclamos={data.reclamos}
        />
      )}
      {accion === 'eliminar' && data.detalle && (
        <DialogoBaja
          confirmar={() => void confirmarAccion()}
          cerrar={cerrarAccion}
          eliminando={procesandoAccion}
          entidad="devolución"
          error={errorAccion}
          nombre={data.detalle.numero}
        />
      )}
      {accion && accion !== 'eliminar' && data.detalle && (
        <DevolucionAccionDialog
          accion={accion}
          cerrar={cerrarAccion}
          cerrando={procesandoAccion}
          confirmar={confirmarAccion}
          error={errorAccion}
          numero={data.detalle.numero}
        />
      )}
    </PaginaModulo>
  );
}
