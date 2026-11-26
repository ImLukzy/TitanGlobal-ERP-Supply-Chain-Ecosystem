import { useEffect, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import {
  actualizarLote,
  cambiarEstadoSerie,
  crearLote,
  crearSerie,
  listarLotes,
  listarSeries,
  obtenerLote,
  obtenerResumenLotes,
} from './api';
import type {
  ActualizarLoteRequest,
  EstadoLote,
  EstadoSerie,
  GuardarLoteRequest,
  GuardarSerieRequest,
  LoteDetalle,
  PaginaLotes,
  PaginaSeries,
  ResumenLotes,
  SerieListado,
} from './tipos';
import {
  validarEdicionLote,
  validarNuevaSerie,
  validarNuevoLote,
  type ErroresLote,
  type ErroresSerie,
} from './validacion';

const TAMANO_PAGINA = 10;

function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
}

function formatearFecha(fecha: string | null): string {
  if (fecha === null || fecha === '') {
    return 'Sin fecha';
  }
  return fecha.slice(0, 10);
}

function claseEstadoLote(estado: EstadoLote): string {
  if (estado === 'Disponible') {
    return 'is-disponible';
  }
  if (estado === 'Cuarentena') {
    return 'is-cuarentena';
  }
  return 'is-bloqueado';
}

function claseEstadoSerie(estado: EstadoSerie): string {
  if (estado === 'Disponible') {
    return 'is-disponible';
  }
  if (estado === 'Asignada') {
    return 'is-asignada';
  }
  return 'is-baja';
}

function ResumenLotesPanel({
  datos,
  error,
}: {
  datos: ResumenLotes | null;
  error: string;
}) {
  if (error !== '') {
    return (
      <p aria-live="polite" className="clientes-error" role="alert">
        <span aria-hidden="true">!</span>
        {error}
      </p>
    );
  }

  return (
    <section aria-label="Resumen de lotes y series" className="lotes-summary">
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.totalLotes}</span>
        <span className="cliente-stat-label">Total de lotes</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.disponibles}</span>
        <span className="cliente-stat-label">Disponibles</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.enCuarentena}</span>
        <span className="cliente-stat-label">En cuarentena</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.bloqueados}</span>
        <span className="cliente-stat-label">Bloqueados</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.porVencer}</span>
        <span className="cliente-stat-label">Por vencer (30 días)</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.vencidos}</span>
        <span className="cliente-stat-label">Vencidos</span>
      </article>
    </section>
  );
}

function FormularioLote({
  lote,
  cancelar,
  guardado,
}: {
  lote: LoteDetalle | null;
  cancelar: () => void;
  guardado: (idGuardado: number) => void;
}) {
  const [productoId, setProductoId] = useState(lote !== null ? String(lote.productoId) : '');
  const [codigo, setCodigo] = useState(lote?.codigo ?? '');
  const [fechaFabricacion, setFechaFabricacion] = useState(
    lote?.fechaFabricacion ? lote.fechaFabricacion.slice(0, 10) : '',
  );
  const [fechaVencimiento, setFechaVencimiento] = useState(
    lote?.fechaVencimiento ? lote.fechaVencimiento.slice(0, 10) : '',
  );
  const [cantidad, setCantidad] = useState(lote !== null ? String(lote.cantidad) : '1');
  const [estado, setEstado] = useState<EstadoLote>(lote?.estado ?? 'Disponible');
  const [errores, setErrores] = useState<ErroresLote>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const cantidadNum = Number(cantidad);

    if (lote === null) {
      const request: GuardarLoteRequest = {
        productoId: Number(productoId),
        codigo: codigo.trim().toUpperCase(),
        cantidad: cantidadNum,
        estado,
      };
      if (fechaFabricacion !== '') {
        request.fechaFabricacion = fechaFabricacion;
      }
      if (fechaVencimiento !== '') {
        request.fechaVencimiento = fechaVencimiento;
      }
      const nuevosErrores = validarNuevoLote(request);
      setErrores(nuevosErrores);
      if (Object.keys(nuevosErrores).length > 0) {
        return;
      }

      setGuardando(true);
      try {
        const respuesta = await crearLote(request);
        guardado(respuesta.id);
      } catch (error: unknown) {
        setMensaje(mensajeError(error, 'No se pudo registrar el lote.'));
      } finally {
        setGuardando(false);
      }
      return;
    }

    const requestActualizar: ActualizarLoteRequest = {
      cantidad: cantidadNum,
      estado,
    };
    if (fechaFabricacion !== '') {
      requestActualizar.fechaFabricacion = fechaFabricacion;
    }
    if (fechaVencimiento !== '') {
      requestActualizar.fechaVencimiento = fechaVencimiento;
    }
    const nuevosErrores = validarEdicionLote(requestActualizar);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setGuardando(true);
    try {
      const respuesta = await actualizarLote(lote.id, requestActualizar);
      guardado(respuesta.id);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo actualizar el lote.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="lote-form-title" className="clientes-form-panel lotes-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">{lote === null ? 'ALTA DE LOTE' : 'EDICIÓN DE LOTE'}</div>
          <h2 id="lote-form-title">{lote === null ? 'Nuevo lote' : `Editar lote ${lote.codigo}`}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">
          Cancelar
        </button>
      </div>
      <form className="lotes-form" noValidate onSubmit={(event) => void enviar(event)}>
        {lote === null && (
          <>
            <CampoConError
              etiqueta="ID de producto"
              htmlFor="lote-producto-id"
              error={errores.productoId}
              idError="lote-producto-id-error"
            >
              <input
                aria-describedby={errores.productoId ? 'lote-producto-id-error' : undefined}
                aria-invalid={Boolean(errores.productoId)}
                id="lote-producto-id"
                min={1}
                onChange={(event) => setProductoId(event.currentTarget.value)}
                type="number"
                value={productoId}
              />
            </CampoConError>
            <CampoConError
              etiqueta="Código de lote"
              htmlFor="lote-codigo"
              error={errores.codigo}
              idError="lote-codigo-error"
            >
              <input
                aria-describedby={errores.codigo ? 'lote-codigo-error' : undefined}
                aria-invalid={Boolean(errores.codigo)}
                id="lote-codigo"
                maxLength={35}
                onChange={(event) => setCodigo(event.currentTarget.value)}
                placeholder="Ej. LOT-2026-001"
                value={codigo}
              />
            </CampoConError>
          </>
        )}

        <CampoConError
          etiqueta="Cantidad"
          htmlFor="lote-cantidad"
          error={errores.cantidad}
          idError="lote-cantidad-error"
        >
          <input
            aria-describedby={errores.cantidad ? 'lote-cantidad-error' : undefined}
            aria-invalid={Boolean(errores.cantidad)}
            id="lote-cantidad"
            min={1}
            onChange={(event) => setCantidad(event.currentTarget.value)}
            type="number"
            value={cantidad}
          />
        </CampoConError>

        <div className="form-field">
          <label htmlFor="lote-estado">Estado</label>
          <select
            id="lote-estado"
            onChange={(event) => setEstado(event.currentTarget.value as EstadoLote)}
            value={estado}
          >
            <option value="Disponible">Disponible</option>
            <option value="Cuarentena">Cuarentena</option>
            <option value="Bloqueado">Bloqueado</option>
          </select>
        </div>

        <div className="form-field">
          <label htmlFor="lote-fabricacion">Fecha de fabricación</label>
          <input
            id="lote-fabricacion"
            onChange={(event) => setFechaFabricacion(event.currentTarget.value)}
            type="date"
            value={fechaFabricacion}
          />
        </div>

        <CampoConError
          etiqueta="Fecha de vencimiento"
          htmlFor="lote-vencimiento"
          error={errores.fechas}
          idError="lote-fechas-error"
        >
          <input
            aria-describedby={errores.fechas ? 'lote-fechas-error' : undefined}
            aria-invalid={Boolean(errores.fechas)}
            id="lote-vencimiento"
            onChange={(event) => setFechaVencimiento(event.currentTarget.value)}
            type="date"
            value={fechaVencimiento}
          />
        </CampoConError>

        <div className="lotes-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>
            {mensaje}
          </p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar lote'}
          </button>
        </div>
      </form>
    </section>
  );
}

function FormularioSerie({
  cancelar,
  guardado,
}: {
  cancelar: () => void;
  guardado: () => void;
}) {
  const [productoId, setProductoId] = useState('');
  const [loteId, setLoteId] = useState('');
  const [numeroSerie, setNumeroSerie] = useState('');
  const [errores, setErrores] = useState<ErroresSerie>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const request: GuardarSerieRequest = {
      productoId: Number(productoId),
      numeroSerie: numeroSerie.trim().toUpperCase(),
    };
    if (loteId.trim() !== '') {
      request.loteId = Number(loteId);
    }

    const nuevosErrores = validarNuevaSerie(request);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setGuardando(true);
    try {
      await crearSerie(request);
      guardado();
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo registrar la serie.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="serie-form-title" className="clientes-form-panel lotes-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">ALTA DE UNIDAD SERIALIZADA</div>
          <h2 id="serie-form-title">Nueva serie</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">
          Cancelar
        </button>
      </div>
      <form className="lotes-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError
          etiqueta="ID de producto"
          htmlFor="serie-producto-id"
          error={errores.productoId}
          idError="serie-producto-id-error"
        >
          <input
            aria-describedby={errores.productoId ? 'serie-producto-id-error' : undefined}
            aria-invalid={Boolean(errores.productoId)}
            id="serie-producto-id"
            min={1}
            onChange={(event) => setProductoId(event.currentTarget.value)}
            type="number"
            value={productoId}
          />
        </CampoConError>

        <CampoConError
          etiqueta="ID de lote (opcional)"
          htmlFor="serie-lote-id"
          error={errores.loteId}
          idError="serie-lote-id-error"
        >
          <input
            aria-describedby={errores.loteId ? 'serie-lote-id-error' : undefined}
            aria-invalid={Boolean(errores.loteId)}
            id="serie-lote-id"
            min={1}
            onChange={(event) => setLoteId(event.currentTarget.value)}
            type="number"
            value={loteId}
          />
        </CampoConError>

        <CampoConError
          etiqueta="Número de serie"
          htmlFor="serie-numero"
          error={errores.numeroSerie}
          idError="serie-numero-error"
        >
          <input
            aria-describedby={errores.numeroSerie ? 'serie-numero-error' : undefined}
            aria-invalid={Boolean(errores.numeroSerie)}
            id="serie-numero"
            maxLength={55}
            onChange={(event) => setNumeroSerie(event.currentTarget.value)}
            placeholder="Ej. SN-2026-0001"
            value={numeroSerie}
          />
        </CampoConError>

        <div className="lotes-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>
            {mensaje}
          </p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar serie'}
          </button>
        </div>
      </form>
    </section>
  );
}

export function LotesSeriesPage() {
  const [vista, setVista] = useState<'lotes' | 'series'>('lotes');
  const [resumen, setResumen] = useState<ResumenLotes | null>(null);
  const [errorResumen, setErrorResumen] = useState('');
  const [actualizacion, setActualizacion] = useState(0);

  // Estados de Lotes
  const [busquedaLote, setBusquedaLote] = useState('');
  const [busquedaLoteAplicada, setBusquedaLoteAplicada] = useState('');
  const [estadoLoteFiltro, setEstadoLoteFiltro] = useState<EstadoLote | ''>('');
  const [vencidosFiltro, setVencidosFiltro] = useState<'' | 'true' | 'false'>('');
  const [paginaLotes, setPaginaLotes] = useState(1);
  const [paginaLotesDatos, setPaginaLotesDatos] = useState<PaginaLotes | null>(null);
  const [cargandoLotes, setCargandoLotes] = useState(true);
  const [errorLotes, setErrorLotes] = useState('');
  const [loteSeleccionado, setLoteSeleccionado] = useState<number | null>(null);
  const [detalleLote, setDetalleLote] = useState<LoteDetalle | null>(null);
  const [cargandoDetalleLote, setCargandoDetalleLote] = useState(false);
  const [errorDetalleLote, setErrorDetalleLote] = useState('');
  const [formLote, setFormLote] = useState<'cerrado' | 'crear' | 'editar'>('cerrado');

  // Estados de Series
  const [busquedaSerie, setBusquedaSerie] = useState('');
  const [busquedaSerieAplicada, setBusquedaSerieAplicada] = useState('');
  const [estadoSerieFiltro, setEstadoSerieFiltro] = useState<EstadoSerie | ''>('');
  const [paginaSeries, setPaginaSeries] = useState(1);
  const [paginaSeriesDatos, setPaginaSeriesDatos] = useState<PaginaSeries | null>(null);
  const [cargandoSeries, setCargandoSeries] = useState(false);
  const [errorSeries, setErrorSeries] = useState('');
  const [formSerieAbierto, setFormSerieAbierto] = useState(false);
  const [errorCambioEstadoSerie, setErrorCambioEstadoSerie] = useState('');
  const [actualizandoSerieId, setActualizandoSerieId] = useState<number | null>(null);

  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      setBusquedaLoteAplicada(busquedaLote);
      setPaginaLotes(1);
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busquedaLote]);

  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      setBusquedaSerieAplicada(busquedaSerie);
      setPaginaSeries(1);
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busquedaSerie]);

  useEffect(() => {
    let activo = true;
    setErrorResumen('');
    obtenerResumenLotes()
      .then((datos) => {
        if (activo) {
          setResumen(datos);
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de lotes.'));
        }
      });
    return () => {
      activo = false;
    };
  }, [actualizacion]);

  useEffect(() => {
    let activo = true;
    setCargandoLotes(true);
    setErrorLotes('');

    listarLotes({
      busqueda: busquedaLoteAplicada.trim(),
      estado: estadoLoteFiltro,
      vencidos: vencidosFiltro === '' ? '' : vencidosFiltro === 'true',
      pagina: paginaLotes,
      tamanoPagina: TAMANO_PAGINA,
    })
      .then((datos) => {
        if (!activo) {
          return;
        }
        setPaginaLotesDatos(datos);
        setLoteSeleccionado((actual) => {
          if (datos.items.length === 0) {
            return null;
          }
          if (actual !== null && datos.items.some((item) => item.id === actual)) {
            return actual;
          }
          return datos.items[0]?.id ?? null;
        });
      })
      .catch((error: unknown) => {
        if (!activo) {
          return;
        }
        setPaginaLotesDatos({ items: [], total: 0, pagina: 1, tamanoPagina: TAMANO_PAGINA });
        setErrorLotes(mensajeError(error, 'No se pudieron cargar los lotes.'));
      })
      .finally(() => {
        if (activo) {
          setCargandoLotes(false);
        }
      });

    return () => {
      activo = false;
    };
  }, [busquedaLoteAplicada, estadoLoteFiltro, vencidosFiltro, paginaLotes, actualizacion]);

  useEffect(() => {
    if (loteSeleccionado === null) {
      setDetalleLote(null);
      setErrorDetalleLote('');
      return;
    }

    let activo = true;
    setCargandoDetalleLote(true);
    setErrorDetalleLote('');

    obtenerLote(loteSeleccionado)
      .then((datos) => {
        if (activo) {
          setDetalleLote(datos);
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setDetalleLote(null);
          setErrorDetalleLote(mensajeError(error, 'No se pudo cargar el detalle del lote.'));
        }
      })
      .finally(() => {
        if (activo) {
          setCargandoDetalleLote(false);
        }
      });

    return () => {
      activo = false;
    };
  }, [loteSeleccionado, actualizacion]);

  useEffect(() => {
    if (vista !== 'series') {
      return;
    }
    let activo = true;
    setCargandoSeries(true);
    setErrorSeries('');

    listarSeries({
      busqueda: busquedaSerieAplicada.trim(),
      estado: estadoSerieFiltro,
      pagina: paginaSeries,
      tamanoPagina: TAMANO_PAGINA,
    })
      .then((datos) => {
        if (activo) {
          setPaginaSeriesDatos(datos);
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setPaginaSeriesDatos({ items: [], total: 0, pagina: 1, tamanoPagina: TAMANO_PAGINA });
          setErrorSeries(mensajeError(error, 'No se pudieron cargar las series.'));
        }
      })
      .finally(() => {
        if (activo) {
          setCargandoSeries(false);
        }
      });

    return () => {
      activo = false;
    };
  }, [vista, busquedaSerieAplicada, estadoSerieFiltro, paginaSeries, actualizacion]);

  async function manejarCambioEstadoSerie(serie: SerieListado, nuevoEstado: EstadoSerie): Promise<void> {
    setErrorCambioEstadoSerie('');
    setActualizandoSerieId(serie.id);
    try {
      await cambiarEstadoSerie(serie.id, { estado: nuevoEstado });
      setActualizacion((valor) => valor + 1);
    } catch (error: unknown) {
      setErrorCambioEstadoSerie(mensajeError(error, 'No se pudo cambiar el estado de la serie.'));
    } finally {
      setActualizandoSerieId(null);
    }
  }

  const totalPaginasLotes = Math.max(
    1,
    Math.ceil((paginaLotesDatos?.total ?? 0) / TAMANO_PAGINA),
  );
  const totalPaginasSeries = Math.max(
    1,
    Math.ceil((paginaSeriesDatos?.total ?? 0) / TAMANO_PAGINA),
  );

  return (
    <PaginaModulo className="lotes-page" modulo="25-lotes-series">
      <p className="module-intro">
        Controla la trazabilidad por lotes con vencimiento y unidades individualizadas por número de serie.
      </p>

      <ResumenLotesPanel datos={resumen} error={errorResumen} />

      <div aria-label="Vistas de trazabilidad" className="lotes-tabs" role="tablist">
        <button
          aria-selected={vista === 'lotes'}
          className={`lotes-tab${vista === 'lotes' ? ' is-active' : ''}`}
          onClick={() => setVista('lotes')}
          role="tab"
          type="button"
        >
          Lotes
        </button>
        <button
          aria-selected={vista === 'series'}
          className={`lotes-tab${vista === 'series' ? ' is-active' : ''}`}
          onClick={() => setVista('series')}
          role="tab"
          type="button"
        >
          Números de serie
        </button>
      </div>

      {vista === 'lotes' && (
        <>
          {formLote !== 'cerrado' && (
            <FormularioLote
              cancelar={() => setFormLote('cerrado')}
              guardado={(idGuardado) => {
                setFormLote('cerrado');
                setLoteSeleccionado(idGuardado);
                setActualizacion((valor) => valor + 1);
              }}
              lote={formLote === 'editar' ? detalleLote : null}
            />
          )}

          <div className="lotes-work-area">
            <section
              aria-labelledby="lotes-list-title"
              className="clientes-list-panel lotes-list-panel"
            >
              <div className="clientes-panel-heading">
                <div>
                  <div className="page-eyebrow">TRAZABILIDAD POR LOTE</div>
                  <h2 id="lotes-list-title">Lotes registrados</h2>
                </div>
                <button
                  className="primary-button"
                  onClick={() => setFormLote('crear')}
                  type="button"
                >
                  Nuevo lote
                </button>
              </div>

              <div className="lotes-filters">
                <label className="sr-only" htmlFor="lotes-buscar">
                  Buscar lote por código
                </label>
                <input
                  aria-label="Buscar lote por código"
                  id="lotes-buscar"
                  onChange={(event) => setBusquedaLote(event.currentTarget.value)}
                  placeholder="Buscar por código de lote…"
                  type="search"
                  value={busquedaLote}
                />
                <select
                  aria-label="Filtrar lotes por estado"
                  onChange={(event) => {
                    setEstadoLoteFiltro(event.currentTarget.value as EstadoLote | '');
                    setPaginaLotes(1);
                  }}
                  value={estadoLoteFiltro}
                >
                  <option value="">Todos los estados</option>
                  <option value="Disponible">Disponible</option>
                  <option value="Cuarentena">Cuarentena</option>
                  <option value="Bloqueado">Bloqueado</option>
                </select>
                <select
                  aria-label="Filtrar lotes por vigencia"
                  onChange={(event) => {
                    setVencidosFiltro(event.currentTarget.value as '' | 'true' | 'false');
                    setPaginaLotes(1);
                  }}
                  value={vencidosFiltro}
                >
                  <option value="">Vigentes y vencidos</option>
                  <option value="false">Solo vigentes</option>
                  <option value="true">Solo vencidos</option>
                </select>
              </div>

              {errorLotes !== '' && (
                <p aria-live="polite" className="clientes-error" role="alert">
                  <span aria-hidden="true">!</span>
                  {errorLotes}
                </p>
              )}

              {cargandoLotes && <p className="clientes-status">Cargando lotes…</p>}

              {!cargandoLotes &&
                errorLotes === '' &&
                paginaLotesDatos !== null &&
                paginaLotesDatos.items.length === 0 && (
                  <p className="clientes-status">No se encontraron lotes con los filtros actuales.</p>
                )}

              {!cargandoLotes &&
                paginaLotesDatos !== null &&
                paginaLotesDatos.items.length > 0 && (
                  <TablaPaginada
                    cambiarPagina={setPaginaLotes}
                    caption="Listado de lotes por producto"
                    className="lotes-table"
                    classNameContenedor="lotes-table-scroll"
                    classNamePaginacion="clientes-pagination"
                    columnas={[
                      { id: 'codigo', etiqueta: 'Código' },
                      { id: 'producto', etiqueta: 'Producto' },
                      { id: 'vencimiento', etiqueta: 'Vencimiento' },
                      { id: 'cantidad', etiqueta: 'Cantidad' },
                      { id: 'estado', etiqueta: 'Estado' },
                    ]}
                    elementos={paginaLotesDatos.items}
                    etiquetaRegion="Tabla de lotes"
                    obtenerClave={(lote) => lote.id}
                    pagina={paginaLotes}
                    renderizarFila={(lote) => (
                      <tr className={loteSeleccionado === lote.id ? 'is-selected' : ''}>
                        <td>
                          <button
                            aria-current={loteSeleccionado === lote.id ? 'true' : undefined}
                            className="cliente-row-select"
                            onClick={() => setLoteSeleccionado(lote.id)}
                            type="button"
                          >
                            {lote.codigo}
                          </button>
                        </td>
                        <td>{lote.productoNombre}</td>
                        <td className="lotes-nowrap">
                          {formatearFecha(lote.fechaVencimiento)}
                          {lote.vencido ? ' (Vencido)' : ''}
                        </td>
                        <td className="lotes-numeric">{lote.cantidad}</td>
                        <td>
                          <span className={`lote-status ${claseEstadoLote(lote.estado)}`}>
                            {lote.estado}
                          </span>
                        </td>
                      </tr>
                    )}
                    totalPaginas={totalPaginasLotes}
                  />
                )}
            </section>

            <PanelDetalle
              ariaLabelledBy="lote-detail-title"
              className="clientes-detail-panel lotes-detail-panel"
            >
              <div className="clientes-panel-heading">
                <div>
                  <div className="page-eyebrow">FICHA DE LOTE</div>
                  <h2 id="lote-detail-title">
                    {detalleLote !== null ? detalleLote.codigo : 'Detalle de lote'}
                  </h2>
                </div>
                {detalleLote !== null && (
                  <button
                    className="secondary-button"
                    onClick={() => setFormLote('editar')}
                    type="button"
                  >
                    Editar
                  </button>
                )}
              </div>

              {cargandoDetalleLote && <p className="clientes-status">Cargando detalle…</p>}

              {errorDetalleLote !== '' && (
                <p aria-live="polite" className="clientes-error" role="alert">
                  <span aria-hidden="true">!</span>
                  {errorDetalleLote}
                </p>
              )}

              {!cargandoDetalleLote && errorDetalleLote === '' && detalleLote === null && (
                <p className="clientes-status">Selecciona un lote para consultar su ficha.</p>
              )}

              {!cargandoDetalleLote && detalleLote !== null && (
                <dl className="lotes-detail-grid">
                  <div>
                    <dt>Producto</dt>
                    <dd>
                      #{detalleLote.productoId} · {detalleLote.productoNombre}
                    </dd>
                  </div>
                  <div>
                    <dt>Estado</dt>
                    <dd>
                      <span className={`lote-status ${claseEstadoLote(detalleLote.estado)}`}>
                        {detalleLote.estado}
                      </span>
                    </dd>
                  </div>
                  <div>
                    <dt>Fabricación</dt>
                    <dd className="lotes-nowrap">{formatearFecha(detalleLote.fechaFabricacion)}</dd>
                  </div>
                  <div>
                    <dt>Vencimiento</dt>
                    <dd className="lotes-nowrap">{formatearFecha(detalleLote.fechaVencimiento)}</dd>
                  </div>
                  <div>
                    <dt>Cantidad del lote</dt>
                    <dd className="lotes-numeric">{detalleLote.cantidad}</dd>
                  </div>
                  <div>
                    <dt>Series vinculadas</dt>
                    <dd className="lotes-numeric">{detalleLote.totalSeries}</dd>
                  </div>
                </dl>
              )}
            </PanelDetalle>
          </div>
        </>
      )}

      {vista === 'series' && (
        <>
          {formSerieAbierto && (
            <FormularioSerie
              cancelar={() => setFormSerieAbierto(false)}
              guardado={() => {
                setFormSerieAbierto(false);
                setActualizacion((valor) => valor + 1);
              }}
            />
          )}

          <section
            aria-labelledby="series-list-title"
            className="clientes-list-panel lotes-list-panel"
          >
            <div className="clientes-panel-heading">
              <div>
                <div className="page-eyebrow">UNIDADES SERIALIZADAS</div>
                <h2 id="series-list-title">Números de serie</h2>
              </div>
              <button
                className="primary-button"
                onClick={() => setFormSerieAbierto(true)}
                type="button"
              >
                Nueva serie
              </button>
            </div>

            <div className="lotes-filters">
              <label className="sr-only" htmlFor="series-buscar">
                Buscar por número de serie
              </label>
              <input
                aria-label="Buscar por número de serie"
                id="series-buscar"
                onChange={(event) => setBusquedaSerie(event.currentTarget.value)}
                placeholder="Buscar por número de serie…"
                type="search"
                value={busquedaSerie}
              />
              <select
                aria-label="Filtrar series por estado"
                onChange={(event) => {
                  setEstadoSerieFiltro(event.currentTarget.value as EstadoSerie | '');
                  setPaginaSeries(1);
                }}
                value={estadoSerieFiltro}
              >
                <option value="">Todos los estados</option>
                <option value="Disponible">Disponible</option>
                <option value="Asignada">Asignada</option>
                <option value="Baja">Baja</option>
              </select>
            </div>

            {errorCambioEstadoSerie !== '' && (
              <p aria-live="polite" className="clientes-error" role="alert">
                <span aria-hidden="true">!</span>
                {errorCambioEstadoSerie}
              </p>
            )}

            {errorSeries !== '' && (
              <p aria-live="polite" className="clientes-error" role="alert">
                <span aria-hidden="true">!</span>
                {errorSeries}
              </p>
            )}

            {cargandoSeries && <p className="clientes-status">Cargando series…</p>}

            {!cargandoSeries &&
              errorSeries === '' &&
              paginaSeriesDatos !== null &&
              paginaSeriesDatos.items.length === 0 && (
                <p className="clientes-status">No se encontraron series con los filtros actuales.</p>
              )}

            {!cargandoSeries &&
              paginaSeriesDatos !== null &&
              paginaSeriesDatos.items.length > 0 && (
                <TablaPaginada
                  cambiarPagina={setPaginaSeries}
                  caption="Listado de unidades por número de serie"
                  className="lotes-table"
                  classNameContenedor="lotes-table-scroll"
                  classNamePaginacion="clientes-pagination"
                  columnas={[
                    { id: 'serie', etiqueta: 'Número de serie' },
                    { id: 'producto', etiqueta: 'Producto ID' },
                    { id: 'lote', etiqueta: 'Lote ID' },
                    { id: 'estado', etiqueta: 'Estado' },
                    { id: 'acciones', etiqueta: 'Acciones' },
                  ]}
                  elementos={paginaSeriesDatos.items}
                  etiquetaRegion="Tabla de números de serie"
                  obtenerClave={(serie) => serie.id}
                  pagina={paginaSeries}
                  renderizarFila={(serie) => (
                    <tr>
                      <td>{serie.numeroSerie}</td>
                      <td className="lotes-numeric">#{serie.productoId}</td>
                      <td className="lotes-numeric">
                        {serie.loteId !== null ? `#${serie.loteId}` : 'Sin lote'}
                      </td>
                      <td>
                        <span className={`lote-status ${claseEstadoSerie(serie.estado)}`}>
                          {serie.estado}
                        </span>
                      </td>
                      <td>
                        {serie.estado === 'Baja' ? (
                          <span className="lotes-readonly">Estado terminal</span>
                        ) : (
                          <div className="series-row-actions">
                            {serie.estado === 'Disponible' && (
                              <button
                                className="secondary-button"
                                disabled={actualizandoSerieId === serie.id}
                                onClick={() => void manejarCambioEstadoSerie(serie, 'Asignada')}
                                type="button"
                              >
                                Asignar
                              </button>
                            )}
                            {serie.estado === 'Asignada' && (
                              <button
                                className="secondary-button"
                                disabled={actualizandoSerieId === serie.id}
                                onClick={() => void manejarCambioEstadoSerie(serie, 'Disponible')}
                                type="button"
                              >
                                Liberar
                              </button>
                            )}
                            <button
                              className="danger-button"
                              disabled={actualizandoSerieId === serie.id}
                              onClick={() => void manejarCambioEstadoSerie(serie, 'Baja')}
                              type="button"
                            >
                              Dar de baja
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  )}
                  totalPaginas={totalPaginasSeries}
                />
              )}
          </section>
        </>
      )}
    </PaginaModulo>
  );
}
