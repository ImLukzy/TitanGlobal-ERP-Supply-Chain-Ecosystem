import { useEffect, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import {
  listarTransferencias,
  obtenerResumenTransferencias,
  obtenerTransferencia,
  registrarTransferencia,
} from './api';
import type {
  GuardarTransferenciaRequest,
  PaginaTransferencias,
  ResumenTransferencias,
  TransferenciaDetalle,
} from './tipos';
import { validarTransferencia, type ErroresTransferencia } from './validacion';

const TAMANO_PAGINA = 10;

function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
}

function formatearFecha(fecha: string): string {
  return fecha.slice(0, 10);
}

function formatearMoneda(valor: number): string {
  return `S/ ${valor.toFixed(2)}`;
}

function ResumenTransferenciasPanel({
  datos,
  error,
}: {
  datos: ResumenTransferencias | null;
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
    <section aria-label="Resumen de transferencias entre almacenes" className="transf-summary">
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.totalTransferencias}</span>
        <span className="cliente-stat-label">Total de transferencias</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.unidadesTransferidas}</span>
        <span className="cliente-stat-label">Unidades transferidas</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">
          {datos === null ? '—' : formatearMoneda(datos.valorTransferido)}
        </span>
        <span className="cliente-stat-label">Valor transferido</span>
      </article>
    </section>
  );
}

function FormularioTransferencia({
  cancelar,
  guardado,
}: {
  cancelar: () => void;
  guardado: (idCreado: number) => void;
}) {
  const [productoId, setProductoId] = useState('');
  const [almacenOrigenId, setAlmacenOrigenId] = useState('');
  const [almacenDestinoId, setAlmacenDestinoId] = useState('');
  const [cantidad, setCantidad] = useState('1');
  const [observaciones, setObservaciones] = useState('');
  const [errores, setErrores] = useState<ErroresTransferencia>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');

    const request: GuardarTransferenciaRequest = {
      productoId: Number(productoId),
      almacenOrigenId: Number(almacenOrigenId),
      almacenDestinoId: Number(almacenDestinoId),
      cantidad: Number(cantidad),
    };
    if (observaciones.trim() !== '') {
      request.observaciones = observaciones.trim();
    }

    const nuevosErrores = validarTransferencia(request);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setGuardando(true);
    try {
      const creada = await registrarTransferencia(request);
      guardado(creada.id);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo registrar la transferencia.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="transf-form-title" className="clientes-form-panel transf-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">NUEVA TRANSFERENCIA</div>
          <h2 id="transf-form-title">Registrar transferencia entre almacenes</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">
          Cancelar
        </button>
      </div>
      <form className="transf-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError
          etiqueta="ID de producto"
          htmlFor="transf-producto-id"
          error={errores.productoId}
          idError="transf-producto-id-error"
        >
          <input
            aria-describedby={errores.productoId ? 'transf-producto-id-error' : undefined}
            aria-invalid={Boolean(errores.productoId)}
            id="transf-producto-id"
            min={1}
            onChange={(event) => setProductoId(event.currentTarget.value)}
            type="number"
            value={productoId}
          />
        </CampoConError>

        <CampoConError
          etiqueta="ID de almacén origen"
          htmlFor="transf-origen-id"
          error={errores.almacenOrigenId}
          idError="transf-origen-id-error"
        >
          <input
            aria-describedby={errores.almacenOrigenId ? 'transf-origen-id-error' : undefined}
            aria-invalid={Boolean(errores.almacenOrigenId)}
            id="transf-origen-id"
            min={1}
            onChange={(event) => setAlmacenOrigenId(event.currentTarget.value)}
            type="number"
            value={almacenOrigenId}
          />
        </CampoConError>

        <CampoConError
          etiqueta="ID de almacén destino"
          htmlFor="transf-destino-id"
          error={errores.almacenDestinoId}
          idError="transf-destino-id-error"
        >
          <input
            aria-describedby={errores.almacenDestinoId ? 'transf-destino-id-error' : undefined}
            aria-invalid={Boolean(errores.almacenDestinoId)}
            id="transf-destino-id"
            min={1}
            onChange={(event) => setAlmacenDestinoId(event.currentTarget.value)}
            type="number"
            value={almacenDestinoId}
          />
        </CampoConError>

        <CampoConError
          etiqueta="Cantidad"
          htmlFor="transf-cantidad"
          error={errores.cantidad}
          idError="transf-cantidad-error"
        >
          <input
            aria-describedby={errores.cantidad ? 'transf-cantidad-error' : undefined}
            aria-invalid={Boolean(errores.cantidad)}
            id="transf-cantidad"
            min={1}
            onChange={(event) => setCantidad(event.currentTarget.value)}
            type="number"
            value={cantidad}
          />
        </CampoConError>

        <div className="transf-observaciones-field">
          <CampoConError
            etiqueta="Observaciones (opcional)"
            htmlFor="transf-observaciones"
            error={errores.observaciones}
            idError="transf-observaciones-error"
          >
            <input
              aria-describedby={errores.observaciones ? 'transf-observaciones-error' : undefined}
              aria-invalid={Boolean(errores.observaciones)}
              id="transf-observaciones"
              maxLength={520}
              onChange={(event) => setObservaciones(event.currentTarget.value)}
              placeholder="Ej. Reposición semanal sucursal sur"
              value={observaciones}
            />
          </CampoConError>
        </div>

        <div className="transf-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>
            {mensaje}
          </p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Transfiriendo…' : 'Confirmar transferencia'}
          </button>
        </div>
      </form>
    </section>
  );
}

export function TransferenciasAlmacenPage() {
  const [resumen, setResumen] = useState<ResumenTransferencias | null>(null);
  const [errorResumen, setErrorResumen] = useState('');
  const [fechaDesde, setFechaDesde] = useState('');
  const [fechaHasta, setFechaHasta] = useState('');
  const [pagina, setPagina] = useState(1);
  const [paginaDatos, setPaginaDatos] = useState<PaginaTransferencias | null>(null);
  const [cargando, setCargando] = useState(true);
  const [errorLista, setErrorLista] = useState('');
  const [seleccionada, setSeleccionada] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<TransferenciaDetalle | null>(null);
  const [cargandoDetalle, setCargandoDetalle] = useState(false);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [formAbierto, setFormAbierto] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);

  const rangoFechasInvalido =
    fechaDesde !== '' && fechaHasta !== '' && fechaDesde > fechaHasta;

  useEffect(() => {
    let activo = true;
    obtenerResumenTransferencias()
      .then((datos) => {
        if (activo) {
          setResumen(datos);
          setErrorResumen('');
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de transferencias.'));
        }
      });
    return () => {
      activo = false;
    };
  }, [actualizacion]);

  useEffect(() => {
    if (rangoFechasInvalido) {
      return;
    }

    let activo = true;
    listarTransferencias({
      fechaDesde,
      fechaHasta,
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
        setErrorLista(mensajeError(error, 'No se pudieron cargar las transferencias.'));
      })
      .finally(() => {
        if (activo) {
          setCargando(false);
        }
      });

    return () => {
      activo = false;
    };
  }, [fechaDesde, fechaHasta, pagina, actualizacion, rangoFechasInvalido]);

  useEffect(() => {
    if (seleccionada === null) {
      return;
    }

    let activo = true;
    obtenerTransferencia(seleccionada)
      .then((datos) => {
        if (activo) {
          setDetalle(datos);
          setErrorDetalle('');
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setDetalle(null);
          setErrorDetalle(mensajeError(error, 'No se pudo cargar el detalle de la transferencia.'));
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

  const errorListadoVisible = rangoFechasInvalido
    ? 'La fecha inicial no puede ser posterior a la final.'
    : errorLista;
  const detalleVisible = seleccionada === null ? null : detalle;
  const errorDetalleVisible = seleccionada === null ? '' : errorDetalle;
  const totalPaginas = Math.max(1, Math.ceil((paginaDatos?.total ?? 0) / TAMANO_PAGINA));

  return (
    <PaginaModulo className="transf-page" modulo="27-transferencias-almacen">
      <p className="module-intro">
        Gestiona traslados de mercadería entre almacenes con valorización automática en kárdex.
      </p>

      <ResumenTransferenciasPanel datos={resumen} error={errorResumen} />

      {formAbierto && (
        <FormularioTransferencia
          cancelar={() => setFormAbierto(false)}
          guardado={(idCreado) => {
            setFormAbierto(false);
            setCargando(true);
            setCargandoDetalle(true);
            setSeleccionada(idCreado);
            setActualizacion((valor) => valor + 1);
          }}
        />
      )}

      <div className="transf-work-area">
        <section
          aria-labelledby="transf-list-title"
          className="clientes-list-panel transf-list-panel"
        >
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">MOVIMIENTOS ENTRE SEDES</div>
              <h2 id="transf-list-title">Transferencias registradas</h2>
            </div>
            <button
              className="primary-button"
              onClick={() => setFormAbierto(true)}
              type="button"
            >
              Nueva transferencia
            </button>
          </div>

          <div className="transf-filters">
            <label className="kardex-date-field">
              <span>Desde</span>
              <input
                aria-label="Fecha desde"
                onChange={(event) => {
                  setCargando(true);
                  setFechaDesde(event.currentTarget.value);
                  setPagina(1);
                }}
                type="date"
                value={fechaDesde}
              />
            </label>
            <label className="kardex-date-field">
              <span>Hasta</span>
              <input
                aria-label="Fecha hasta"
                onChange={(event) => {
                  setCargando(true);
                  setFechaHasta(event.currentTarget.value);
                  setPagina(1);
                }}
                type="date"
                value={fechaHasta}
              />
            </label>
          </div>

          {errorListadoVisible !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              <span aria-hidden="true">!</span>
              {errorListadoVisible}
            </p>
          )}

          {!rangoFechasInvalido && cargando && (
            <p className="clientes-status">Cargando transferencias…</p>
          )}

          {!rangoFechasInvalido &&
            !cargando &&
            errorListadoVisible === '' &&
            paginaDatos !== null &&
            paginaDatos.items.length === 0 && (
              <p className="clientes-status">No hay transferencias registradas en el rango indicado.</p>
            )}

          {!rangoFechasInvalido &&
            !cargando &&
            paginaDatos !== null &&
            paginaDatos.items.length > 0 && (
              <TablaPaginada
                cambiarPagina={(nuevaPagina) => {
                  setCargando(true);
                  setPagina(nuevaPagina);
                }}
                caption="Transferencias entre almacenes"
                className="transf-table"
                classNameContenedor="transf-table-scroll"
                classNamePaginacion="clientes-pagination"
                columnas={[
                  { id: 'id', etiqueta: 'ID' },
                  { id: 'fecha', etiqueta: 'Fecha' },
                  { id: 'producto', etiqueta: 'Producto' },
                  { id: 'ruta', etiqueta: 'Origen → Destino' },
                  { id: 'cantidad', etiqueta: 'Cant.' },
                  { id: 'valor', etiqueta: 'Valor total' },
                ]}
                elementos={paginaDatos.items}
                etiquetaRegion="Tabla de transferencias entre almacenes"
                obtenerClave={(item) => item.id}
                pagina={pagina}
                renderizarFila={(item) => (
                  <tr className={seleccionada === item.id ? 'is-selected' : ''}>
                    <td>
                      <button
                        aria-current={seleccionada === item.id ? 'true' : undefined}
                        className="cliente-row-select"
                        onClick={() => {
                          setCargandoDetalle(true);
                          setSeleccionada(item.id);
                        }}
                        type="button"
                      >
                        #{item.id}
                      </button>
                    </td>
                    <td className="transf-nowrap">{formatearFecha(item.fecha)}</td>
                    <td>{item.productoNombre}</td>
                    <td className="transf-nowrap">
                      {item.almacenOrigenCodigo} → {item.almacenDestinoCodigo}
                    </td>
                    <td className="transf-numeric">{item.cantidad}</td>
                    <td className="transf-numeric">{formatearMoneda(item.valorTotal)}</td>
                  </tr>
                )}
                totalPaginas={totalPaginas}
              />
            )}
        </section>

        <PanelDetalle
          ariaLabelledBy="transf-detail-title"
          className="clientes-detail-panel transf-detail-panel"
        >
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">FICHA DE TRANSFERENCIA</div>
              <h2 id="transf-detail-title">
                {detalleVisible !== null
                  ? `Transferencia #${detalleVisible.id}`
                  : 'Detalle de transferencia'}
              </h2>
            </div>
          </div>

          {cargandoDetalle && <p className="clientes-status">Cargando detalle…</p>}

          {errorDetalleVisible !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              <span aria-hidden="true">!</span>
              {errorDetalleVisible}
            </p>
          )}

          {!cargandoDetalle && errorDetalleVisible === '' && detalleVisible === null && (
            <p className="clientes-status">Selecciona una transferencia para ver su trazabilidad.</p>
          )}

          {!cargandoDetalle && detalleVisible !== null && (
            <dl className="transf-detail-grid">
              <div>
                <dt>Producto</dt>
                <dd>
                  #{detalleVisible.productoId} · {detalleVisible.productoNombre}
                </dd>
              </div>
              <div>
                <dt>Fecha</dt>
                <dd className="transf-nowrap">{formatearFecha(detalleVisible.fecha)}</dd>
              </div>
              <div>
                <dt>Almacén origen</dt>
                <dd>{detalleVisible.almacenOrigenCodigo}</dd>
              </div>
              <div>
                <dt>Almacén destino</dt>
                <dd>{detalleVisible.almacenDestinoCodigo}</dd>
              </div>
              <div>
                <dt>Cantidad</dt>
                <dd className="transf-numeric">{detalleVisible.cantidad}</dd>
              </div>
              <div>
                <dt>Costo unitario</dt>
                <dd className="transf-numeric">{formatearMoneda(detalleVisible.costoUnitario)}</dd>
              </div>
              <div>
                <dt>Valor total</dt>
                <dd className="transf-numeric">{formatearMoneda(detalleVisible.valorTotal)}</dd>
              </div>
              <div>
                <dt>Movimientos Kárdex</dt>
                <dd className="transf-nowrap">
                  Salida #{detalleVisible.movimientoSalidaId} · Entrada #{detalleVisible.movimientoEntradaId}
                </dd>
              </div>
              <div className="transf-detail-notes">
                <dt>Observaciones</dt>
                <dd>{detalleVisible.observaciones ?? 'Sin observaciones.'}</dd>
              </div>
            </dl>
          )}
        </PanelDetalle>
      </div>
    </PaginaModulo>
  );
}
