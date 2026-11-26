import { useEffect, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import {
  actualizarTomaInventario,
  anularTomaInventario,
  cerrarTomaInventario,
  crearTomaInventario,
  listarTomasInventario,
  obtenerResumenTomasInventario,
  obtenerTomaInventario,
} from './api';
import type {
  ActualizarTomaInventarioRequest,
  CrearTomaInventarioRequest,
  EstadoTomaInventario,
  LineaTomaInventarioRequest,
  PaginaTomasInventario,
  ResumenTomasInventario,
  TomaInventarioDetalle,
} from './tipos';
import {
  validarEdicionToma,
  validarNuevaToma,
  type ErroresTomaInventario,
} from './validacion';

const TAMANO_PAGINA = 10;

interface LineaBorrador {
  clave: number;
  productoId: string;
  cantidadContada: string;
  observacion: string;
}

function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
}

function formatearFecha(fecha: string): string {
  return fecha.slice(0, 10);
}

function claseEstadoToma(estado: EstadoTomaInventario): string {
  if (estado === 'Abierta') {
    return 'is-abierta';
  }
  if (estado === 'Cerrada') {
    return 'is-cerrada';
  }
  return 'is-anulada';
}

function ResumenTomasPanel({
  datos,
  error,
}: {
  datos: ResumenTomasInventario | null;
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
    <section aria-label="Resumen de conteo físico" className="conteo-summary">
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.totalTomas}</span>
        <span className="cliente-stat-label">Total de tomas</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.abiertas}</span>
        <span className="cliente-stat-label">Abiertas</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.cerradas}</span>
        <span className="cliente-stat-label">Cerradas</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.anuladas}</span>
        <span className="cliente-stat-label">Anuladas</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.lineasConDiferencia}</span>
        <span className="cliente-stat-label">Líneas con diferencia</span>
      </article>
    </section>
  );
}

function FormularioTomaInventario({
  toma,
  cancelar,
  guardado,
}: {
  toma: TomaInventarioDetalle | null;
  cancelar: () => void;
  guardado: (detalleGuardado: TomaInventarioDetalle) => void;
}) {
  const [codigo, setCodigo] = useState(toma?.codigo ?? '');
  const [almacenId, setAlmacenId] = useState(toma !== null ? String(toma.almacenId) : '');
  const [fechaToma, setFechaToma] = useState('');
  const [responsable, setResponsable] = useState(toma?.responsable ?? '');
  const [observaciones, setObservaciones] = useState(toma?.observaciones ?? '');
  const [lineas, setLineas] = useState<LineaBorrador[]>(() => {
    if (toma !== null && toma.lineas.length > 0) {
      return toma.lineas.map((linea, indice) => ({
        clave: indice + 1,
        productoId: String(linea.productoId),
        cantidadContada: String(linea.cantidadContada),
        observacion: linea.observacion ?? '',
      }));
    }
    return [{ clave: 1, productoId: '', cantidadContada: '0', observacion: '' }];
  });
  const [errores, setErrores] = useState<ErroresTomaInventario>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  function agregarLinea(): void {
    setLineas((actuales) => {
      const maxClave = actuales.reduce((max, item) => Math.max(max, item.clave), 0);
      return [
        ...actuales,
        { clave: maxClave + 1, productoId: '', cantidadContada: '0', observacion: '' },
      ];
    });
  }

  function quitarLinea(clave: number): void {
    setLineas((actuales) =>
      actuales.length <= 1 ? actuales : actuales.filter((item) => item.clave !== clave),
    );
  }

  function actualizarLinea(clave: number, campo: keyof Omit<LineaBorrador, 'clave'>, valor: string): void {
    setLineas((actuales) =>
      actuales.map((item) => (item.clave === clave ? { ...item, [campo]: valor } : item)),
    );
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');

    const lineasPayload: LineaTomaInventarioRequest[] = lineas.map((item) => {
      const linea: LineaTomaInventarioRequest = {
        productoId: Number(item.productoId),
        cantidadContada: Number(item.cantidadContada),
      };
      if (item.observacion.trim() !== '') {
        linea.observacion = item.observacion.trim();
      }
      return linea;
    });

    if (toma === null) {
      const request: CrearTomaInventarioRequest = {
        codigo: codigo.trim().toUpperCase(),
        almacenId: Number(almacenId),
        responsable: responsable.trim(),
        lineas: lineasPayload,
      };
      if (fechaToma !== '') {
        request.fechaToma = fechaToma;
      }
      if (observaciones.trim() !== '') {
        request.observaciones = observaciones.trim();
      }

      const nuevosErrores = validarNuevaToma(request);
      setErrores(nuevosErrores);
      if (Object.keys(nuevosErrores).length > 0) {
        return;
      }

      setGuardando(true);
      try {
        const creada = await crearTomaInventario(request);
        guardado(creada);
      } catch (error: unknown) {
        setMensaje(mensajeError(error, 'No se pudo crear la toma de inventario.'));
      } finally {
        setGuardando(false);
      }
      return;
    }

    const requestActualizar: ActualizarTomaInventarioRequest = {
      responsable: responsable.trim(),
      lineas: lineasPayload,
    };
    if (observaciones.trim() !== '') {
      requestActualizar.observaciones = observaciones.trim();
    }

    const nuevosErrores = validarEdicionToma(requestActualizar);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setGuardando(true);
    try {
      const actualizada = await actualizarTomaInventario(toma.id, requestActualizar);
      guardado(actualizada);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo actualizar la toma de inventario.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="conteo-form-title" className="clientes-form-panel conteo-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">{toma === null ? 'NUEVO CONTEO' : 'EDICIÓN DE CONTEO'}</div>
          <h2 id="conteo-form-title">
            {toma === null ? 'Nueva toma de inventario físico' : `Editar toma ${toma.codigo}`}
          </h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">
          Cancelar
        </button>
      </div>
      <form className="conteo-form" noValidate onSubmit={(event) => void enviar(event)}>
        {toma === null && (
          <>
            <CampoConError
              etiqueta="Código de toma"
              htmlFor="conteo-codigo"
              error={errores.codigo}
              idError="conteo-codigo-error"
            >
              <input
                aria-describedby={errores.codigo ? 'conteo-codigo-error' : undefined}
                aria-invalid={Boolean(errores.codigo)}
                id="conteo-codigo"
                maxLength={25}
                onChange={(event) => setCodigo(event.currentTarget.value)}
                placeholder="Ej. TF-2026-001"
                value={codigo}
              />
            </CampoConError>

            <CampoConError
              etiqueta="ID de almacén"
              htmlFor="conteo-almacen-id"
              error={errores.almacenId}
              idError="conteo-almacen-id-error"
            >
              <input
                aria-describedby={errores.almacenId ? 'conteo-almacen-id-error' : undefined}
                aria-invalid={Boolean(errores.almacenId)}
                id="conteo-almacen-id"
                min={1}
                onChange={(event) => setAlmacenId(event.currentTarget.value)}
                type="number"
                value={almacenId}
              />
            </CampoConError>

            <div className="form-field">
              <label htmlFor="conteo-fecha">Fecha de toma (opcional)</label>
              <input
                id="conteo-fecha"
                onChange={(event) => setFechaToma(event.currentTarget.value)}
                type="date"
                value={fechaToma}
              />
            </div>
          </>
        )}

        <CampoConError
          etiqueta="Responsable"
          htmlFor="conteo-responsable"
          error={errores.responsable}
          idError="conteo-responsable-error"
        >
          <input
            aria-describedby={errores.responsable ? 'conteo-responsable-error' : undefined}
            aria-invalid={Boolean(errores.responsable)}
            id="conteo-responsable"
            maxLength={105}
            onChange={(event) => setResponsable(event.currentTarget.value)}
            placeholder="Ej. Carlos Mendoza"
            value={responsable}
          />
        </CampoConError>

        <div className="conteo-span-full">
          <CampoConError
            etiqueta="Observaciones generales (opcional)"
            htmlFor="conteo-observaciones"
            error={errores.observaciones}
            idError="conteo-observaciones-error"
          >
            <input
              aria-describedby={errores.observaciones ? 'conteo-observaciones-error' : undefined}
              aria-invalid={Boolean(errores.observaciones)}
              id="conteo-observaciones"
              maxLength={520}
              onChange={(event) => setObservaciones(event.currentTarget.value)}
              placeholder="Ej. Inventario cíclico pasillo B"
              value={observaciones}
            />
          </CampoConError>
        </div>

        <div className="conteo-lineas-box conteo-span-full">
          <div className="conteo-lineas-header">
            <strong>Líneas de conteo físico</strong>
            <button className="secondary-button" onClick={agregarLinea} type="button">
              Agregar producto
            </button>
          </div>
          {errores.lineas !== undefined && (
            <p className="field-error" id="conteo-lineas-error">
              {errores.lineas}
            </p>
          )}
          {lineas.map((linea, indice) => (
            <div className="conteo-linea-row" key={linea.clave}>
              <div className="form-field">
                <label htmlFor={`conteo-linea-prod-${linea.clave}`}>
                  Producto ID #{indice + 1}
                </label>
                <input
                  id={`conteo-linea-prod-${linea.clave}`}
                  min={1}
                  onChange={(event) =>
                    actualizarLinea(linea.clave, 'productoId', event.currentTarget.value)
                  }
                  type="number"
                  value={linea.productoId}
                />
              </div>
              <div className="form-field">
                <label htmlFor={`conteo-linea-cant-${linea.clave}`}>
                  Cantidad contada #{indice + 1}
                </label>
                <input
                  id={`conteo-linea-cant-${linea.clave}`}
                  min={0}
                  onChange={(event) =>
                    actualizarLinea(linea.clave, 'cantidadContada', event.currentTarget.value)
                  }
                  type="number"
                  value={linea.cantidadContada}
                />
              </div>
              <div className="form-field">
                <label htmlFor={`conteo-linea-obs-${linea.clave}`}>
                  Observación #{indice + 1}
                </label>
                <input
                  id={`conteo-linea-obs-${linea.clave}`}
                  maxLength={260}
                  onChange={(event) =>
                    actualizarLinea(linea.clave, 'observacion', event.currentTarget.value)
                  }
                  value={linea.observacion}
                />
              </div>
              <button
                className="secondary-button"
                disabled={lineas.length <= 1}
                onClick={() => quitarLinea(linea.clave)}
                type="button"
              >
                Quitar
              </button>
            </div>
          ))}
        </div>

        <div className="conteo-form-actions conteo-span-full">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>
            {mensaje}
          </p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar toma'}
          </button>
        </div>
      </form>
    </section>
  );
}

export function ConteoFisicoPage() {
  const [resumen, setResumen] = useState<ResumenTomasInventario | null>(null);
  const [errorResumen, setErrorResumen] = useState('');
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [estadoFiltro, setEstadoFiltro] = useState<EstadoTomaInventario | ''>('');
  const [pagina, setPagina] = useState(1);
  const [paginaDatos, setPaginaDatos] = useState<PaginaTomasInventario | null>(null);
  const [cargando, setCargando] = useState(true);
  const [errorLista, setErrorLista] = useState('');
  const [seleccionada, setSeleccionada] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<TomaInventarioDetalle | null>(null);
  const [cargandoDetalle, setCargandoDetalle] = useState(false);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [errorAccion, setErrorAccion] = useState('');
  const [procesandoEstado, setProcesandoEstado] = useState(false);
  const [formModo, setFormModo] = useState<'cerrado' | 'crear' | 'editar'>('cerrado');
  const [actualizacion, setActualizacion] = useState(0);

  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      setCargando(true);
      setBusquedaAplicada(busqueda);
      setPagina(1);
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  useEffect(() => {
    let activo = true;
    obtenerResumenTomasInventario()
      .then((datos) => {
        if (activo) {
          setResumen(datos);
          setErrorResumen('');
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de conteo físico.'));
        }
      });
    return () => {
      activo = false;
    };
  }, [actualizacion]);

  useEffect(() => {
    let activo = true;
    listarTomasInventario({
      busqueda: busquedaAplicada.trim(),
      estado: estadoFiltro,
      pagina,
      tamanoPagina: TAMANO_PAGINA,
    })
      .then((datos) => {
        if (!activo) {
          return;
        }
        setPaginaDatos(datos);
        setErrorLista('');
        setSeleccionada((actual) => {
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
        setPaginaDatos({ items: [], total: 0, pagina: 1, tamanoPagina: TAMANO_PAGINA });
        setErrorLista(mensajeError(error, 'No se pudieron cargar las tomas de inventario.'));
      })
      .finally(() => {
        if (activo) {
          setCargando(false);
        }
      });

    return () => {
      activo = false;
    };
  }, [busquedaAplicada, estadoFiltro, pagina, actualizacion]);

  useEffect(() => {
    if (seleccionada === null) {
      return;
    }

    let activo = true;
    obtenerTomaInventario(seleccionada)
      .then((datos) => {
        if (activo) {
          setDetalle(datos);
          setErrorDetalle('');
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setDetalle(null);
          setErrorDetalle(mensajeError(error, 'No se pudo cargar el detalle de la toma.'));
        }
      })
      .finally(() => {
        if (activo) {
          setCargandoDetalle(false);
        }
      });

    return () => {
      activo = false;
    };
  }, [seleccionada, actualizacion]);

  async function manejarCambioEstado(accion: 'cerrar' | 'anular'): Promise<void> {
    if (detalle === null) {
      return;
    }
    setErrorAccion('');
    setProcesandoEstado(true);
    try {
      const resultado =
        accion === 'cerrar'
          ? await cerrarTomaInventario(detalle.id)
          : await anularTomaInventario(detalle.id);
      setDetalle(resultado);
      setFormModo('cerrado');
      setActualizacion((valor) => valor + 1);
    } catch (error: unknown) {
      setErrorAccion(
        mensajeError(
          error,
          accion === 'cerrar'
            ? 'No se pudo cerrar la toma de inventario.'
            : 'No se pudo anular la toma de inventario.',
        ),
      );
    } finally {
      setProcesandoEstado(false);
    }
  }

  const detalleVisible = seleccionada === null ? null : detalle;
  const errorDetalleVisible = seleccionada === null ? '' : errorDetalle;
  const totalPaginas = Math.max(1, Math.ceil((paginaDatos?.total ?? 0) / TAMANO_PAGINA));

  return (
    <PaginaModulo className="conteo-page" modulo="28-toma-inventario">
      <p className="module-intro">
        Planifica tomas de inventario físico, contrasta stock del sistema contra el conteo real y cierra o anula auditorías.
      </p>

      <ResumenTomasPanel datos={resumen} error={errorResumen} />

      {formModo !== 'cerrado' && (
        <FormularioTomaInventario
          cancelar={() => setFormModo('cerrado')}
          guardado={(detalleGuardado) => {
            setFormModo('cerrado');
            setDetalle(detalleGuardado);
            setSeleccionada(detalleGuardado.id);
            setActualizacion((valor) => valor + 1);
          }}
          toma={formModo === 'editar' ? detalleVisible : null}
        />
      )}

      <div className="conteo-work-area">
        <section
          aria-labelledby="conteo-list-title"
          className="clientes-list-panel conteo-list-panel"
        >
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">AUDITORÍA DE EXISTENCIAS</div>
              <h2 id="conteo-list-title">Tomas de inventario</h2>
            </div>
            <button
              className="primary-button"
              onClick={() => setFormModo('crear')}
              type="button"
            >
              Nueva toma
            </button>
          </div>

          <div className="conteo-filters">
            <label className="sr-only" htmlFor="conteo-buscar">
              Buscar toma de inventario
            </label>
            <input
              aria-label="Buscar toma de inventario"
              id="conteo-buscar"
              onChange={(event) => setBusqueda(event.currentTarget.value)}
              placeholder="Buscar por código o responsable…"
              type="search"
              value={busqueda}
            />
            <select
              aria-label="Filtrar tomas por estado"
              onChange={(event) => {
                setCargando(true);
                setEstadoFiltro(event.currentTarget.value as EstadoTomaInventario | '');
                setPagina(1);
              }}
              value={estadoFiltro}
            >
              <option value="">Todos los estados</option>
              <option value="Abierta">Abierta</option>
              <option value="Cerrada">Cerrada</option>
              <option value="Anulada">Anulada</option>
            </select>
          </div>

          {errorLista !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              <span aria-hidden="true">!</span>
              {errorLista}
            </p>
          )}

          {cargando && <p className="clientes-status">Cargando tomas de inventario…</p>}

          {!cargando &&
            errorLista === '' &&
            paginaDatos !== null &&
            paginaDatos.items.length === 0 && (
              <p className="clientes-status">No se encontraron tomas de inventario con los filtros actuales.</p>
            )}

          {!cargando && paginaDatos !== null && paginaDatos.items.length > 0 && (
            <TablaPaginada
              cambiarPagina={(nuevaPagina) => {
                setCargando(true);
                setPagina(nuevaPagina);
              }}
              caption="Tomas de inventario físico registradas"
              className="conteo-table"
              classNameContenedor="conteo-table-scroll"
              classNamePaginacion="clientes-pagination"
              columnas={[
                { id: 'codigo', etiqueta: 'Código' },
                { id: 'almacen', etiqueta: 'Almacén' },
                { id: 'fecha', etiqueta: 'Fecha' },
                { id: 'responsable', etiqueta: 'Responsable' },
                { id: 'diferencias', etiqueta: 'Diferencias' },
                { id: 'estado', etiqueta: 'Estado' },
              ]}
              elementos={paginaDatos.items}
              etiquetaRegion="Tabla de tomas de inventario"
              obtenerClave={(item) => item.id}
              pagina={pagina}
              renderizarFila={(item) => (
                <tr className={seleccionada === item.id ? 'is-selected' : ''}>
                  <td>
                    <button
                      aria-current={seleccionada === item.id ? 'true' : undefined}
                      className="cliente-row-select"
                      onClick={() => {
                        setErrorAccion('');
                        setCargandoDetalle(true);
                        setSeleccionada(item.id);
                      }}
                      type="button"
                    >
                      {item.codigo}
                    </button>
                  </td>
                  <td className="conteo-nowrap">{item.almacenCodigo}</td>
                  <td className="conteo-nowrap">{formatearFecha(item.fechaToma)}</td>
                  <td>{item.responsable}</td>
                  <td className="conteo-numeric">
                    {item.lineasConDiferencia} / {item.cantidadProductos}
                  </td>
                  <td>
                    <span className={`conteo-status ${claseEstadoToma(item.estado)}`}>
                      {item.estado}
                    </span>
                  </td>
                </tr>
              )}
              totalPaginas={totalPaginas}
            />
          )}
        </section>

        <PanelDetalle
          ariaLabelledBy="conteo-detail-title"
          className="clientes-detail-panel conteo-detail-panel"
        >
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">HOJA DE CONTEO</div>
              <h2 id="conteo-detail-title">
                {detalleVisible !== null ? detalleVisible.codigo : 'Detalle de toma'}
              </h2>
            </div>
            {detalleVisible !== null && detalleVisible.estado === 'Abierta' && (
              <div className="conteo-detail-actions">
                <button
                  className="secondary-button"
                  disabled={procesandoEstado}
                  onClick={() => setFormModo('editar')}
                  type="button"
                >
                  Editar
                </button>
                <button
                  className="primary-button"
                  disabled={procesandoEstado}
                  onClick={() => void manejarCambioEstado('cerrar')}
                  type="button"
                >
                  Cerrar toma
                </button>
                <button
                  className="danger-button"
                  disabled={procesandoEstado}
                  onClick={() => void manejarCambioEstado('anular')}
                  type="button"
                >
                  Anular
                </button>
              </div>
            )}
          </div>

          {errorAccion !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              <span aria-hidden="true">!</span>
              {errorAccion}
            </p>
          )}

          {cargandoDetalle && <p className="clientes-status">Cargando detalle…</p>}

          {errorDetalleVisible !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              <span aria-hidden="true">!</span>
              {errorDetalleVisible}
            </p>
          )}

          {!cargandoDetalle && errorDetalleVisible === '' && detalleVisible === null && (
            <p className="clientes-status">Selecciona una toma de inventario para revisar sus conteos.</p>
          )}

          {!cargandoDetalle && detalleVisible !== null && (
            <>
              <dl className="conteo-detail-grid">
                <div>
                  <dt>Almacén</dt>
                  <dd>
                    {detalleVisible.almacenCodigo} · {detalleVisible.almacenNombre}
                  </dd>
                </div>
                <div>
                  <dt>Estado</dt>
                  <dd>
                    <span className={`conteo-status ${claseEstadoToma(detalleVisible.estado)}`}>
                      {detalleVisible.estado}
                    </span>
                  </dd>
                </div>
                <div>
                  <dt>Responsable</dt>
                  <dd>{detalleVisible.responsable}</dd>
                </div>
                <div>
                  <dt>Fecha de toma</dt>
                  <dd className="conteo-nowrap">{formatearFecha(detalleVisible.fechaToma)}</dd>
                </div>
                <div className="conteo-span-full">
                  <dt>Observaciones</dt>
                  <dd>{detalleVisible.observaciones ?? 'Sin observaciones.'}</dd>
                </div>
              </dl>

              <div
                aria-label="Líneas contadas en la toma"
                className="conteo-table-scroll"
                role="region"
                tabIndex={0}
              >
                <table className="tabla-paginada conteo-table">
                  <caption>Comparativo entre stock del sistema y conteo físico</caption>
                  <thead>
                    <tr>
                      <th scope="col">Producto</th>
                      <th scope="col">Sistema</th>
                      <th scope="col">Contado</th>
                      <th scope="col">Diferencia</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detalleVisible.lineas.map((linea) => (
                      <tr key={linea.id}>
                        <td>{linea.productoNombre}</td>
                        <td className="conteo-numeric">{linea.stockSistema}</td>
                        <td className="conteo-numeric">{linea.cantidadContada}</td>
                        <td className="conteo-numeric">
                          {linea.diferencia > 0 ? `+${linea.diferencia}` : linea.diferencia}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </PanelDetalle>
      </div>
    </PaginaModulo>
  );
}
