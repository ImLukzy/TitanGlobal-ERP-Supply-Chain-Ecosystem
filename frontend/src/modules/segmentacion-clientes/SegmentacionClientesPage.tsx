import { useCallback, useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { useSearchParams } from 'react-router';
import { ApiError } from '../../api/client';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { SegmentoFormulario } from './SegmentoFormulario';
import { SegmentosResumen } from './SegmentosResumen';
import { VistaClasificacion } from './VistaClasificacion';
import { VistaSegmentos } from './VistaSegmentos';
import {
  eliminarSegmento,
  listarClasificacion,
  listarClientesSegmento,
  obtenerResumenSegmentacion,
  obtenerSegmento,
} from './api';
import type {
  FiltrosClasificacion,
  Pagina,
  ResumenSegmentacion,
  SegmentoDetalle as SegmentoDetalleDto,
  SegmentoListado,
  ClienteClasificado,
} from './tipos';
import { useListadoSegmentos } from './useListadoSegmentos';

const TAMANO_PAGINA = 10;
type Vista = 'segmentos' | 'clasificacion';
type FiltroClasificacion = number | '' | 'sin-segmento';

function vistaDesdeUrl(valor: string | null): Vista {
  return valor === 'clasificacion' ? 'clasificacion' : 'segmentos';
}

function mensajeApi(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function SegmentacionClientesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [vista, setVista] = useState<Vista>(() => vistaDesdeUrl(searchParams.get('vista')));
  const [resumen, setResumen] = useState<ResumenSegmentacion | null>(null);
  const [errorResumen, setErrorResumen] = useState('');
  const [busquedaSegmentos, setBusquedaSegmentos] = useState('');
  const [busquedaSegmentosAplicada, setBusquedaSegmentosAplicada] = useState('');
  const [filtrosSegmentos, setFiltrosSegmentos] = useState<{ activo: boolean | ''; pagina: number }>({ activo: '', pagina: 1 });
  const [busquedaClasificacion, setBusquedaClasificacion] = useState('');
  const [busquedaClasificacionAplicada, setBusquedaClasificacionAplicada] = useState('');
  const [filtroClasificacion, setFiltroClasificacion] = useState<FiltroClasificacion>('');
  const [paginaClasificacion, setPaginaClasificacion] = useState(1);
  const [clasificacion, setClasificacion] = useState<Pagina<ClienteClasificado> | null>(null);
  const [errorClasificacion, setErrorClasificacion] = useState('');
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<SegmentoDetalleDto | null>(null);
  const [clientesSegmento, setClientesSegmento] = useState<Pagina<ClienteClasificado> | null>(null);
  const [paginaClientesSegmento, setPaginaClientesSegmento] = useState(1);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [cargandoClientesSegmento, setCargandoClientesSegmento] = useState(false);
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [segmentoEdicion, setSegmentoEdicion] = useState<SegmentoDetalleDto | null>(null);
  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);
  const seleccionadoRef = useRef(seleccionadoId);
  seleccionadoRef.current = seleccionadoId;
  const { segmentos, errorSegmentos } = useListadoSegmentos({
    busqueda: busquedaSegmentos,
    busquedaAplicada: busquedaSegmentosAplicada,
    filtros: filtrosSegmentos,
    actualizacion,
    seleccionadoRef,
    setSeleccionadoId,
  });

  const cerrarDialogo = useCallback(() => {
    setDialogoAbierto(false);
    setErrorEliminar('');
    botonEliminarRef.current?.focus();
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setBusquedaSegmentosAplicada((actual) => actual === busquedaSegmentos ? actual : busquedaSegmentos);
    }, 300);
    return () => window.clearTimeout(timer);
  }, [busquedaSegmentos]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setBusquedaClasificacionAplicada((actual) => actual === busquedaClasificacion ? actual : busquedaClasificacion);
    }, 300);
    return () => window.clearTimeout(timer);
  }, [busquedaClasificacion]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenSegmentacion().then((datos) => {
      if (vigente) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorResumen(mensajeApi(error, 'No se pudo cargar el resumen de segmentación.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [actualizacion]);

  useEffect(() => {
    if (vista !== 'clasificacion' || busquedaClasificacion !== busquedaClasificacionAplicada) {
      return;
    }
    let vigente = true;
    setErrorClasificacion('');
    const filtros: FiltrosClasificacion = {
      busqueda: busquedaClasificacionAplicada,
      pagina: paginaClasificacion,
      tamanoPagina: TAMANO_PAGINA,
    };
    if (filtroClasificacion === 'sin-segmento') {
      filtros.sinSegmento = true;
    } else if (filtroClasificacion !== '') {
      filtros.segmentoId = filtroClasificacion;
    }
    void listarClasificacion(filtros).then((datos) => {
      if (vigente) {
        setClasificacion(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorClasificacion(mensajeApi(error, 'No se pudo cargar la clasificación de clientes.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [vista, busquedaClasificacion, busquedaClasificacionAplicada, filtroClasificacion, paginaClasificacion, actualizacion]);

  useEffect(() => {
    if (seleccionadoId === null) {
      setDetalle(null);
      setClientesSegmento(null);
      setErrorDetalle('');
      return;
    }
    let vigente = true;
    setErrorDetalle('');
    setCargandoClientesSegmento(true);
    void Promise.all([obtenerSegmento(seleccionadoId), listarClientesSegmento(seleccionadoId, paginaClientesSegmento, TAMANO_PAGINA)])
      .then(([segmento, clientes]) => {
        if (vigente) {
          setDetalle(segmento);
          setClientesSegmento(clientes);
        }
      }).catch((error: unknown) => {
        if (vigente) {
          setErrorDetalle(mensajeApi(error, 'No se pudo cargar el detalle del segmento.'));
        }
      }).finally(() => {
        if (vigente) {
          setCargandoClientesSegmento(false);
        }
      });
    return () => {
      vigente = false;
    };
  }, [seleccionadoId, paginaClientesSegmento, actualizacion]);

  function cambiarVista(siguiente: Vista): void {
    setVista(siguiente);
    setSearchParams(siguiente === 'segmentos' ? {} : { vista: 'clasificacion' });
  }

  function manejarTecladoPestanas(event: KeyboardEvent<HTMLDivElement>): void {
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') {
      return;
    }
    event.preventDefault();
    const siguiente = event.key === 'ArrowRight' ? 'clasificacion' : 'segmentos';
    cambiarVista(siguiente);
    window.requestAnimationFrame(() => document.getElementById(`tab-${siguiente}`)?.focus());
  }

  function cambiarBusquedaSegmentos(valor: string): void {
    setBusquedaSegmentos(valor);
    setFiltrosSegmentos((actual) => actual.pagina === 1 ? actual : { ...actual, pagina: 1 });
  }

  function cambiarBusquedaClasificacion(valor: string): void {
    setBusquedaClasificacion(valor);
    setPaginaClasificacion(1);
  }

  function cambiarFiltroClasificacion(valor: FiltroClasificacion): void {
    setFiltroClasificacion(valor);
    setPaginaClasificacion(1);
  }

  async function guardar(): Promise<void> {
    setMostrarFormulario(false);
    setSegmentoEdicion(null);
    setActualizacion((actual) => actual + 1);
  }

  async function confirmarEliminacion(): Promise<void> {
    if (detalle === null) {
      return;
    }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarSegmento(detalle.id);
      cerrarDialogo();
      setSeleccionadoId(null);
      setDetalle(null);
      setActualizacion((actual) => actual + 1);
    } catch (error: unknown) {
      setErrorEliminar(mensajeApi(error, 'No se pudo eliminar el segmento.'));
    } finally {
      setEliminando(false);
    }
  }

  function abrirEdicion(): void {
    if (detalle !== null) {
      setSegmentoEdicion(detalle);
      setMostrarFormulario(true);
    }
  }

  function seleccionar(segmento: SegmentoListado): void {
    setSeleccionadoId(segmento.id);
    setPaginaClientesSegmento(1);
  }

  const totalPaginasSegmentos = Math.max(1, Math.ceil((segmentos?.total ?? 0) / TAMANO_PAGINA));
  const totalPaginasClasificacion = Math.max(1, Math.ceil((clasificacion?.total ?? 0) / TAMANO_PAGINA));

  return (
    <PaginaModulo className="segmentacion-page" modulo="02-segmentacion-clientes">
      <p className="module-intro">Agrupa clientes por su monto y frecuencia de compra.</p>
      <SegmentosResumen datos={resumen} error={errorResumen} />
      <div aria-label="Vistas de segmentación" className="segment-tabs" onKeyDown={manejarTecladoPestanas} role="tablist">
        <button aria-controls="panel-segmentos" aria-selected={vista === 'segmentos'} className="segment-tab"
          id="tab-segmentos" onClick={() => cambiarVista('segmentos')} role="tab" tabIndex={vista === 'segmentos' ? 0 : -1} type="button">Segmentos</button>
        <button aria-controls="panel-clasificacion" aria-selected={vista === 'clasificacion'} className="segment-tab"
          id="tab-clasificacion" onClick={() => cambiarVista('clasificacion')} role="tab" tabIndex={vista === 'clasificacion' ? 0 : -1} type="button">Clasificación</button>
      </div>
      {vista === 'segmentos' ? (
        <VistaSegmentos
          activo={filtrosSegmentos.activo}
          botonEliminarRef={botonEliminarRef}
          cambiarActivo={(activo) => setFiltrosSegmentos((actual) => ({ ...actual, activo, pagina: 1 }))}
          cambiarBusqueda={cambiarBusquedaSegmentos}
          cambiarPagina={(pagina) => setFiltrosSegmentos((actual) => ({ ...actual, pagina }))}
          cambiarPaginaClientes={setPaginaClientesSegmento}
          busqueda={busquedaSegmentos}
          cargandoClientes={cargandoClientesSegmento}
          clientes={clientesSegmento}
          detalle={detalle}
          editar={abrirEdicion}
          eliminar={() => { setErrorEliminar(''); setDialogoAbierto(true); }}
          error={errorSegmentos}
          errorDetalle={errorDetalle}
          nuevo={() => { setSegmentoEdicion(null); setMostrarFormulario(true); }}
          pagina={filtrosSegmentos.pagina}
          segmentos={segmentos}
          seleccionado={seleccionadoId}
          seleccionar={seleccionar}
          totalPaginas={totalPaginasSegmentos}
        />
      ) : (
        <VistaClasificacion
          busqueda={busquedaClasificacion}
          cambiarBusqueda={cambiarBusquedaClasificacion}
          cambiarPagina={setPaginaClasificacion}
          cambiarSegmento={cambiarFiltroClasificacion}
          clasificacion={clasificacion}
          error={errorClasificacion}
          pagina={paginaClasificacion}
          segmentoId={filtroClasificacion}
          segmentos={segmentos?.items ?? []}
          totalPaginas={totalPaginasClasificacion}
        />
      )}
      {mostrarFormulario && <SegmentoFormulario cancelar={() => setMostrarFormulario(false)} guardado={guardar} segmento={segmentoEdicion} />}
      {dialogoAbierto && detalle !== null && <DialogoBaja cerrar={cerrarDialogo} confirmar={() => void confirmarEliminacion()}
        eliminando={eliminando} entidad="segmento" error={errorEliminar} nombre={detalle.nombre} />}
    </PaginaModulo>
  );
}
