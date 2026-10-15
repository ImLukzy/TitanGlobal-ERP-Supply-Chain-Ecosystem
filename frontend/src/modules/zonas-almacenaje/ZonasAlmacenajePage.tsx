import { useCallback, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router';
import { ApiError } from '../../api/client';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { listarAlmacenes } from '../almacenes/api';
import type { AlmacenListado } from '../almacenes/tipos';
import { ZonaDetallePanel } from './ZonaDetallePanel';
import { ZonaFormulario } from './ZonaFormulario';
import { SelectorAlmacen } from './SelectorAlmacen';
import { ZonasFiltros } from './ZonasFiltros';
import { ZonasResumen } from './ZonasResumen';
import { ZonasTabla } from './ZonasTabla';
import { eliminarZona, listarZonas, obtenerResumenZonas, obtenerZona } from './api';
import type {
  FiltrosZona,
  PaginaZonas,
  ResumenZonas,
  TemperaturaZona,
  TipoZona,
  ZonaDetalle as ZonaDetalleDto,
  ZonaListado,
} from './tipos';

const TAMANO_PAGINA = 10;

function leerAlmacenId(valor: string | null): number | '' {
  if (valor === null || !/^\d+$/.test(valor)) {
    return '';
  }
  const id = Number(valor);
  return id > 0 ? id : '';
}

function obtenerMensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function ZonasAlmacenajePage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [almacenId, setAlmacenId] = useState<number | ''>(() => leerAlmacenId(searchParams.get('almacen')));
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [filtros, setFiltros] = useState<{ tipo: TipoZona | ''; temperatura: TemperaturaZona | ''; activa: boolean | ''; pagina: number }>({
    tipo: '', temperatura: '', activa: '', pagina: 1,
  });
  const [almacenes, setAlmacenes] = useState<AlmacenListado[]>([]);
  const [errorAlmacenes, setErrorAlmacenes] = useState('');
  const [lista, setLista] = useState<PaginaZonas | null>(null);
  const [resumen, setResumen] = useState<ResumenZonas | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [seleccionadaId, setSeleccionadaId] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<ZonaDetalleDto | null>(null);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [zonaEdicion, setZonaEdicion] = useState<ZonaDetalleDto | null>(null);
  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);
  const seleccionadaRef = useRef(seleccionadaId);
  seleccionadaRef.current = seleccionadaId;

  const cerrarDialogo = useCallback(() => {
    setDialogoAbierto(false);
    setErrorEliminar('');
    window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setBusquedaAplicada((actual) => actual === busqueda ? actual : busqueda);
    }, 300);
    return () => window.clearTimeout(timer);
  }, [busqueda]);

  useEffect(() => {
    let vigente = true;
    void listarAlmacenes({ activo: true, pagina: 1, tamanoPagina: 100 }).then((datos) => {
      if (vigente) {
        setAlmacenes(datos.items);
        setErrorAlmacenes('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorAlmacenes(obtenerMensaje(error, 'No se pudieron cargar los almacenes.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, []);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenZonas(almacenId).then((datos) => {
      if (vigente) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorResumen(obtenerMensaje(error, 'No se pudo cargar el resumen de zonas.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [almacenId, actualizacion]);

  useEffect(() => {
    if (busqueda !== busquedaAplicada) {
      return;
    }
    let vigente = true;
    setErrorLista('');
    const consulta: FiltrosZona = {
      almacenId,
      busqueda: busquedaAplicada,
      tipo: filtros.tipo,
      temperatura: filtros.temperatura,
      activa: filtros.activa,
      pagina: filtros.pagina,
      tamanoPagina: TAMANO_PAGINA,
    };
    void listarZonas(consulta).then((datos) => {
      if (!vigente) {
        return;
      }
      setLista(datos);
      const idActual = seleccionadaRef.current;
      if (datos.items.length > 0 && (idActual === null || !datos.items.some((item) => item.id === idActual))) {
        setSeleccionadaId(datos.items[0]?.id ?? null);
      }
      if (datos.items.length === 0) {
        setSeleccionadaId(null);
        setDetalle(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(obtenerMensaje(error, 'No se pudo cargar el listado de zonas.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [almacenId, busqueda, busquedaAplicada, filtros, actualizacion]);

  useEffect(() => {
    if (seleccionadaId === null) {
      setDetalle(null);
      setErrorDetalle('');
      return;
    }
    let vigente = true;
    setErrorDetalle('');
    void obtenerZona(seleccionadaId).then((datos) => {
      if (vigente) {
        setDetalle(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorDetalle(obtenerMensaje(error, 'No se pudo cargar el detalle de la zona.'));
        setDetalle(null);
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadaId, actualizacion]);

  function cambiarFiltro(cambios: Partial<typeof filtros>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
  }

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setFiltros((actual) => actual.pagina === 1 ? actual : { ...actual, pagina: 1 });
  }

  function cambiarAlmacen(id: number | ''): void {
    setAlmacenId(id);
    setFiltros((actual) => ({ ...actual, pagina: 1 }));
    setSearchParams(id === '' ? {} : { almacen: String(id) });
  }

  function seleccionar(zona: ZonaListado): void {
    setSeleccionadaId(zona.id);
    setErrorDetalle('');
  }

  async function guardar(): Promise<void> {
    setMostrarFormulario(false);
    setZonaEdicion(null);
    setActualizacion((valor) => valor + 1);
  }

  async function confirmarEliminacion(): Promise<void> {
    if (detalle === null) {
      return;
    }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarZona(detalle.id);
      cerrarDialogo();
      setSeleccionadaId(null);
      setDetalle(null);
      setActualizacion((valor) => valor + 1);
    } catch (error: unknown) {
      setErrorEliminar(obtenerMensaje(error, 'No se pudo eliminar la zona.'));
    } finally {
      setEliminando(false);
    }
  }

  function abrirEdicion(): void {
    if (detalle !== null) {
      setZonaEdicion(detalle);
      setMostrarFormulario(true);
    }
  }

  const totalPaginas = Math.max(1, Math.ceil((lista?.total ?? 0) / TAMANO_PAGINA));

  return (
    <PaginaModulo className="zonas-page" modulo="24-zonas-almacenaje">
      <p className="module-intro">Organiza las áreas de almacenaje y monitorea su capacidad asignada.</p>
      <SelectorAlmacen almacenes={almacenes} almacenId={almacenId} cambiar={cambiarAlmacen} />
      {errorAlmacenes !== '' && <p className="clientes-error" role="alert">{errorAlmacenes}</p>}
      <ZonasResumen error={errorResumen} resumen={resumen} />
      <div className="zonas-work-area">
        <section aria-labelledby="zonas-list-title" className="clientes-list-panel zonas-list-panel">
          <div className="clientes-panel-heading">
            <div><div className="page-eyebrow">DIRECTORIO</div><h2 id="zonas-list-title">Zonas de almacenaje</h2></div>
            <button className="primary-button" onClick={() => { setZonaEdicion(null); setMostrarFormulario(true); }} type="button">
              Nueva zona
            </button>
          </div>
          <ZonasFiltros
            activa={filtros.activa}
            busqueda={busqueda}
            buscar={cambiarBusqueda}
            cambiarActiva={(activa) => cambiarFiltro({ activa })}
            cambiarTemperatura={(temperatura) => cambiarFiltro({ temperatura })}
            cambiarTipo={(tipo) => cambiarFiltro({ tipo })}
            temperatura={filtros.temperatura}
            tipo={filtros.tipo}
          />
          {errorLista !== '' ? <p className="clientes-error" role="alert">{errorLista}</p>
            : lista === null ? <p className="clientes-status" role="status">Cargando zonas…</p>
              : lista.items.length === 0 ? <p className="clientes-status" role="status">No hay zonas que coincidan con los filtros.</p>
                : <ZonasTabla
                  cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
                  mostrarAlmacen={almacenId === ''}
                  pagina={filtros.pagina}
                  seleccionar={seleccionar}
                  seleccionada={seleccionadaId}
                  totalPaginas={totalPaginas}
                  zonas={lista.items}
                />}
        </section>
        <ZonaDetallePanel
          botonEliminarRef={botonEliminarRef}
          editar={abrirEdicion}
          eliminar={() => { setErrorEliminar(''); setDialogoAbierto(true); }}
          error={errorDetalle}
          zona={detalle}
        />
      </div>
      {mostrarFormulario && <ZonaFormulario almacenIdInicial={almacenId} almacenes={almacenes} cancelar={() => setMostrarFormulario(false)}
        guardado={guardar} zona={zonaEdicion} />}
      {dialogoAbierto && detalle !== null && <DialogoBaja cerrar={cerrarDialogo} confirmar={() => void confirmarEliminacion()}
        eliminando={eliminando} entidad="zona" error={errorEliminar} nombre={detalle.nombre} />}
    </PaginaModulo>
  );
}
