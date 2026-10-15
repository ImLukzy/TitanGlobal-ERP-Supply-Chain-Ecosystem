import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { PaginaModulo } from '../../components/PaginaModulo';
import {
  actualizarSolicitud,
  aprobarSolicitud,
  crearSolicitud,
  eliminarSolicitud,
  rechazarSolicitud,
} from './api';
import { SolicitudDetallePanel } from './SolicitudDetallePanel';
import { SolicitudesFiltros } from './SolicitudesFiltros';
import { SolicitudesOverlays } from './SolicitudesOverlays';
import { SolicitudesResumen } from './SolicitudesResumen';
import { SolicitudesTabla } from './SolicitudesTabla';
import { useSolicitudesData } from './useSolicitudesData';
import type {
  FiltrosSolicitud,
  GuardarSolicitudRequest,
  SolicitudListado,
} from './tipos';

const filtrosIniciales: FiltrosSolicitud = { pagina: 1, tamanoPagina: 10 };

function obtenerMensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function SolicitudesCompraPage() {
  const [filtros, setFiltros] = useState<FiltrosSolicitud>(filtrosIniciales);
  const [busqueda, setBusqueda] = useState('');
  const [errorFecha, setErrorFecha] = useState('');
  const [seleccionadaId, setSeleccionadaId] = useState<number | null>(null);
  const [formulario, setFormulario] = useState<'crear' | 'editar' | null>(null);
  const [decision, setDecision] = useState<'aprobar' | 'rechazar' | null>(null);
  const [dialogoBaja, setDialogoBaja] = useState(false);
  const [errorBaja, setErrorBaja] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const botonBajaRef = useRef<HTMLButtonElement>(null);
  const {
    lista,
    resumen,
    proveedores,
    productos,
    errorLista,
    errorResumen,
    errorCatalogos,
    solicitud,
    errorDetalle,
    cargandoDetalle,
    recargar,
  } = useSolicitudesData(filtros, seleccionadaId);

  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      setFiltros((actuales) => ({ ...actuales, busqueda, pagina: 1 }));
      setSeleccionadaId(null);
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarFiltros(cambios: Partial<FiltrosSolicitud>): void {
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
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actuales) => ({ ...actuales, pagina }));
  }

  function seleccionar(item: SolicitudListado): void {
    setSeleccionadaId(item.id);
    setFormulario(null);
  }

  function abrirBaja(): void {
    setErrorBaja('');
    setDialogoBaja(true);
  }

  function cerrarBaja(): void {
    setDialogoBaja(false);
    setErrorBaja('');
    window.requestAnimationFrame(() => botonBajaRef.current?.focus());
  }

  function abrirAprobacion(boton: HTMLButtonElement): void {
    botonBajaRef.current = boton;
    setDecision('aprobar');
  }

  function abrirRechazo(boton: HTMLButtonElement): void {
    botonBajaRef.current = boton;
    setDecision('rechazar');
  }

  function cerrarDecision(): void {
    setDecision(null);
    window.requestAnimationFrame(() => botonBajaRef.current?.focus());
  }

  function abrirDialogoBaja(boton: HTMLButtonElement): void {
    botonBajaRef.current = boton;
    abrirBaja();
  }

  async function guardar(request: GuardarSolicitudRequest): Promise<void> {
    if (formulario === 'editar' && solicitud !== null) {
      await actualizarSolicitud(solicitud.id, request);
    } else {
      const creada = await crearSolicitud(request);
      setSeleccionadaId(creada.id);
    }
    setFormulario(null);
    recargar();
  }

  async function aprobar(): Promise<void> {
    if (solicitud === null) {
      return;
    }
    await aprobarSolicitud(solicitud.id);
    setDecision(null);
    recargar();
  }

  async function rechazar(motivo: string): Promise<void> {
    if (solicitud === null) {
      return;
    }
    await rechazarSolicitud(solicitud.id, { motivo });
    setDecision(null);
    recargar();
  }

  async function eliminar(): Promise<void> {
    if (solicitud === null) {
      return;
    }
    setEliminando(true);
    try {
      await eliminarSolicitud(solicitud.id);
      cerrarBaja();
      setSeleccionadaId(null);
      recargar();
    } catch (error: unknown) {
      setErrorBaja(obtenerMensaje(error, 'No se pudo eliminar la solicitud.'));
    } finally {
      setEliminando(false);
    }
  }

  const pagina = filtros.pagina ?? 1;
  return (
    <PaginaModulo className="solicitudes-page" modulo="13-solicitudes-compra">
      <p className="module-intro">Registra solicitudes, revisa sus productos y gestiona las decisiones de compra.</p>
      <SolicitudesResumen datos={resumen} error={errorResumen} />
      {errorCatalogos && <p className="solicitudes-error" role="alert">{errorCatalogos}</p>}
      <div className="solicitudes-work-area">
        <section aria-labelledby="solicitudes-list-title" className="solicitudes-panel solicitudes-list-panel">
          <div className="solicitudes-panel-heading">
            <div>
              <span className="page-eyebrow">COMPRAS</span>
              <h2 id="solicitudes-list-title">Solicitudes de compra</h2>
            </div>
            <button className="primary-button" onClick={() => setFormulario('crear')} type="button">Nueva solicitud</button>
          </div>
          <SolicitudesFiltros
            busqueda={busqueda}
            filtros={filtros}
            proveedores={proveedores}
            errorFecha={errorFecha}
            cambiarBusqueda={setBusqueda}
            cambiarFiltros={cambiarFiltros}
          />
          <SolicitudesTabla
            lista={lista}
            error={errorLista}
            seleccionadaId={seleccionadaId}
            seleccionar={seleccionar}
            pagina={pagina}
            cambiarPagina={cambiarPagina}
          />
        </section>
        <SolicitudDetallePanel
          solicitud={solicitud}
          cargando={cargandoDetalle}
          editar={() => setFormulario('editar')}
          aprobar={abrirAprobacion}
          rechazar={abrirRechazo}
          eliminar={abrirDialogoBaja}
        />
      </div>
      {errorDetalle && <p className="solicitudes-error" role="alert">{errorDetalle}</p>}
      <SolicitudesOverlays
        formulario={formulario}
        solicitud={solicitud}
        proveedores={proveedores}
        productos={productos}
        cancelarFormulario={() => setFormulario(null)}
        guardar={guardar}
        decision={decision}
        cerrarDecision={cerrarDecision}
        confirmarAprobacion={aprobar}
        confirmarRechazo={rechazar}
        dialogoBaja={dialogoBaja}
        cerrarBaja={cerrarBaja}
        confirmarBaja={() => void eliminar()}
        eliminando={eliminando}
        errorBaja={errorBaja}
      />
    </PaginaModulo>
  );
}
