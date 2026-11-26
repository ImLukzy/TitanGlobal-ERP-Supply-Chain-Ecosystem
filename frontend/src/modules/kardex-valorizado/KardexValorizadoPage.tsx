import { useEffect, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { PaginaModulo } from '../../components/PaginaModulo';
import { TablaPaginada } from '../../components/TablaPaginada';
import {
  consultarKardex,
  listarMovimientos,
  listarSaldosKardex,
  obtenerResumenKardex,
  registrarMovimiento,
} from './api';
import type {
  GuardarMovimientoRequest,
  KardexValorizado,
  MetodoValorizacion,
  PaginaMovimientos,
  ResumenKardex,
  SaldoKardex,
  TipoMovimientoKardex,
} from './tipos';
import { validarMovimiento, type ErroresMovimiento } from './validacion';

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

function formatearMoneda(valor: number | null): string {
  if (valor === null) {
    return '—';
  }
  return `S/ ${valor.toFixed(2)}`;
}

function ResumenKardexPanel({
  datos,
  error,
}: {
  datos: ResumenKardex | null;
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
    <section aria-label="Resumen de kárdex valorizado" className="kardex-summary">
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.totalMovimientos}</span>
        <span className="cliente-stat-label">Total movimientos</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.totalEntradas}</span>
        <span className="cliente-stat-label">Movimientos entrada</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.totalSalidas}</span>
        <span className="cliente-stat-label">Movimientos salida</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.unidadesEntradas}</span>
        <span className="cliente-stat-label">Unidades ingresadas</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.unidadesSalidas}</span>
        <span className="cliente-stat-label">Unidades retiradas</span>
      </article>
    </section>
  );
}

function FormularioMovimiento({
  cancelar,
  guardado,
}: {
  cancelar: () => void;
  guardado: () => void;
}) {
  const [productoId, setProductoId] = useState('');
  const [almacenId, setAlmacenId] = useState('');
  const [loteId, setLoteId] = useState('');
  const [tipo, setTipo] = useState<TipoMovimientoKardex>('Entrada');
  const [cantidad, setCantidad] = useState('1');
  const [costoUnitario, setCostoUnitario] = useState('');
  const [fecha, setFecha] = useState('');
  const [referencia, setReferencia] = useState('');
  const [errores, setErrores] = useState<ErroresMovimiento>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');

    const request: GuardarMovimientoRequest = {
      productoId: Number(productoId),
      almacenId: Number(almacenId),
      tipo,
      cantidad: Number(cantidad),
    };
    if (loteId.trim() !== '') {
      request.loteId = Number(loteId);
    }
    if (tipo === 'Entrada' && costoUnitario.trim() !== '') {
      request.costoUnitario = Number(costoUnitario);
    }
    if (fecha !== '') {
      request.fecha = fecha;
    }
    if (referencia.trim() !== '') {
      request.referencia = referencia.trim();
    }

    const nuevosErrores = validarMovimiento(request);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setGuardando(true);
    try {
      await registrarMovimiento(request);
      guardado();
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo registrar el movimiento en kárdex.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="kardex-form-title" className="clientes-form-panel kardex-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">REGISTRO DE MOVIMIENTO</div>
          <h2 id="kardex-form-title">Nuevo movimiento de inventario</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">
          Cancelar
        </button>
      </div>
      <form className="kardex-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError
          etiqueta="ID de producto"
          htmlFor="mov-producto-id"
          error={errores.productoId}
          idError="mov-producto-id-error"
        >
          <input
            aria-describedby={errores.productoId ? 'mov-producto-id-error' : undefined}
            aria-invalid={Boolean(errores.productoId)}
            id="mov-producto-id"
            min={1}
            onChange={(event) => setProductoId(event.currentTarget.value)}
            type="number"
            value={productoId}
          />
        </CampoConError>

        <CampoConError
          etiqueta="ID de almacén"
          htmlFor="mov-almacen-id"
          error={errores.almacenId}
          idError="mov-almacen-id-error"
        >
          <input
            aria-describedby={errores.almacenId ? 'mov-almacen-id-error' : undefined}
            aria-invalid={Boolean(errores.almacenId)}
            id="mov-almacen-id"
            min={1}
            onChange={(event) => setAlmacenId(event.currentTarget.value)}
            type="number"
            value={almacenId}
          />
        </CampoConError>

        <div className="form-field">
          <label htmlFor="mov-tipo">Tipo de movimiento</label>
          <select
            id="mov-tipo"
            onChange={(event) => setTipo(event.currentTarget.value as TipoMovimientoKardex)}
            value={tipo}
          >
            <option value="Entrada">Entrada</option>
            <option value="Salida">Salida</option>
          </select>
        </div>

        <CampoConError
          etiqueta="Cantidad"
          htmlFor="mov-cantidad"
          error={errores.cantidad}
          idError="mov-cantidad-error"
        >
          <input
            aria-describedby={errores.cantidad ? 'mov-cantidad-error' : undefined}
            aria-invalid={Boolean(errores.cantidad)}
            id="mov-cantidad"
            min={1}
            onChange={(event) => setCantidad(event.currentTarget.value)}
            type="number"
            value={cantidad}
          />
        </CampoConError>

        {tipo === 'Entrada' && (
          <CampoConError
            etiqueta="Costo unitario (S/)"
            htmlFor="mov-costo"
            error={errores.costoUnitario}
            idError="mov-costo-error"
          >
            <input
              aria-describedby={errores.costoUnitario ? 'mov-costo-error' : undefined}
              aria-invalid={Boolean(errores.costoUnitario)}
              id="mov-costo"
              min={0.01}
              onChange={(event) => setCostoUnitario(event.currentTarget.value)}
              step="0.01"
              type="number"
              value={costoUnitario}
            />
          </CampoConError>
        )}

        <div className="form-field">
          <label htmlFor="mov-lote-id">ID de lote (opcional)</label>
          <input
            id="mov-lote-id"
            min={1}
            onChange={(event) => setLoteId(event.currentTarget.value)}
            type="number"
            value={loteId}
          />
        </div>

        <div className="form-field">
          <label htmlFor="mov-fecha">Fecha (opcional)</label>
          <input
            id="mov-fecha"
            onChange={(event) => setFecha(event.currentTarget.value)}
            type="date"
            value={fecha}
          />
        </div>

        <CampoConError
          etiqueta="Referencia / Documento"
          htmlFor="mov-referencia"
          error={errores.referencia}
          idError="mov-referencia-error"
        >
          <input
            aria-describedby={errores.referencia ? 'mov-referencia-error' : undefined}
            aria-invalid={Boolean(errores.referencia)}
            id="mov-referencia"
            maxLength={110}
            onChange={(event) => setReferencia(event.currentTarget.value)}
            placeholder="Ej. OC-2026-0045"
            value={referencia}
          />
        </CampoConError>

        <div className="kardex-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>
            {mensaje}
          </p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Registrando…' : 'Registrar movimiento'}
          </button>
        </div>
      </form>
    </section>
  );
}

export function KardexValorizadoPage() {
  const [vista, setVista] = useState<'movimientos' | 'kardex' | 'saldos'>('movimientos');
  const [resumen, setResumen] = useState<ResumenKardex | null>(null);
  const [errorResumen, setErrorResumen] = useState('');
  const [actualizacion, setActualizacion] = useState(0);

  // Movimientos
  const [tipoFiltro, setTipoFiltro] = useState<TipoMovimientoKardex | ''>('');
  const [fechaDesde, setFechaDesde] = useState('');
  const [fechaHasta, setFechaHasta] = useState('');
  const [pagina, setPagina] = useState(1);
  const [paginaMovimientos, setPaginaMovimientos] = useState<PaginaMovimientos | null>(null);
  const [cargandoMovimientos, setCargandoMovimientos] = useState(true);
  const [errorMovimientos, setErrorMovimientos] = useState('');
  const [formAbierto, setFormAbierto] = useState(false);

  // Consulta Kárdex PEPS / Promedio
  const [kardexProductoId, setKardexProductoId] = useState('');
  const [kardexAlmacenId, setKardexAlmacenId] = useState('');
  const [metodo, setMetodo] = useState<MetodoValorizacion>('Promedio');
  const [kardexDatos, setKardexDatos] = useState<KardexValorizado | null>(null);
  const [cargandoKardex, setCargandoKardex] = useState(false);
  const [errorKardex, setErrorKardex] = useState('');

  // Saldos
  const [saldos, setSaldos] = useState<SaldoKardex[]>([]);
  const [cargandoSaldos, setCargandoSaldos] = useState(false);
  const [errorSaldos, setErrorSaldos] = useState('');

  useEffect(() => {
    let activo = true;
    setErrorResumen('');
    obtenerResumenKardex()
      .then((datos) => {
        if (activo) {
          setResumen(datos);
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de kárdex.'));
        }
      });
    return () => {
      activo = false;
    };
  }, [actualizacion]);

  useEffect(() => {
    if (vista !== 'movimientos') {
      return;
    }
    if (fechaDesde !== '' && fechaHasta !== '' && fechaDesde > fechaHasta) {
      setErrorMovimientos('La fecha inicial no puede ser posterior a la final.');
      setCargandoMovimientos(false);
      return;
    }

    let activo = true;
    setCargandoMovimientos(true);
    setErrorMovimientos('');

    listarMovimientos({
      tipo: tipoFiltro,
      fechaDesde,
      fechaHasta,
      pagina,
      tamanoPagina: TAMANO_PAGINA,
    })
      .then((datos) => {
        if (activo) {
          setPaginaMovimientos(datos);
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setPaginaMovimientos({ items: [], total: 0, pagina: 1, tamanoPagina: TAMANO_PAGINA });
          setErrorMovimientos(mensajeError(error, 'No se pudieron cargar los movimientos.'));
        }
      })
      .finally(() => {
        if (activo) {
          setCargandoMovimientos(false);
        }
      });

    return () => {
      activo = false;
    };
  }, [vista, tipoFiltro, fechaDesde, fechaHasta, pagina, actualizacion]);

  useEffect(() => {
    if (vista !== 'saldos') {
      return;
    }
    let activo = true;
    setCargandoSaldos(true);
    setErrorSaldos('');

    listarSaldosKardex()
      .then((datos) => {
        if (activo) {
          setSaldos(datos);
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setSaldos([]);
          setErrorSaldos(mensajeError(error, 'No se pudieron cargar los saldos.'));
        }
      })
      .finally(() => {
        if (activo) {
          setCargandoSaldos(false);
        }
      });

    return () => {
      activo = false;
    };
  }, [vista, actualizacion]);

  async function ejecutarConsultaKardex(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setErrorKardex('');
    const prod = Number(kardexProductoId);
    const alm = Number(kardexAlmacenId);
    if (!Number.isInteger(prod) || prod < 1 || !Number.isInteger(alm) || alm < 1) {
      setErrorKardex('Ingresa un ID de producto y un ID de almacén válidos.');
      return;
    }

    setCargandoKardex(true);
    try {
      const resultado = await consultarKardex(prod, alm, metodo);
      setKardexDatos(resultado);
    } catch (error: unknown) {
      setKardexDatos(null);
      setErrorKardex(mensajeError(error, 'No se pudo calcular el kárdex valorizado.'));
    } finally {
      setCargandoKardex(false);
    }
  }

  const totalPaginas = Math.max(
    1,
    Math.ceil((paginaMovimientos?.total ?? 0) / TAMANO_PAGINA),
  );

  return (
    <PaginaModulo className="kardex-page" modulo="26-kardex-valorizado">
      <p className="module-intro">
        Registra movimientos de entrada y salida, consulta saldos por almacén y valoriza existencias por Promedio Ponderado o PEPS.
      </p>

      <ResumenKardexPanel datos={resumen} error={errorResumen} />

      <div aria-label="Vistas de kárdex valorizado" className="kardex-tabs" role="tablist">
        <button
          aria-selected={vista === 'movimientos'}
          className={`kardex-tab${vista === 'movimientos' ? ' is-active' : ''}`}
          onClick={() => setVista('movimientos')}
          role="tab"
          type="button"
        >
          Movimientos
        </button>
        <button
          aria-selected={vista === 'kardex'}
          className={`kardex-tab${vista === 'kardex' ? ' is-active' : ''}`}
          onClick={() => setVista('kardex')}
          role="tab"
          type="button"
        >
          Kárdex valorizado (PEPS / Promedio)
        </button>
        <button
          aria-selected={vista === 'saldos'}
          className={`kardex-tab${vista === 'saldos' ? ' is-active' : ''}`}
          onClick={() => setVista('saldos')}
          role="tab"
          type="button"
        >
          Saldos consolidados
        </button>
      </div>

      {vista === 'movimientos' && (
        <>
          {formAbierto && (
            <FormularioMovimiento
              cancelar={() => setFormAbierto(false)}
              guardado={() => {
                setFormAbierto(false);
                setActualizacion((valor) => valor + 1);
              }}
            />
          )}

          <section aria-labelledby="kardex-movimientos-title" className="clientes-list-panel">
            <div className="clientes-panel-heading">
              <div>
                <div className="page-eyebrow">HISTORIAL OPERATIVO</div>
                <h2 id="kardex-movimientos-title">Movimientos de inventario</h2>
              </div>
              <button
                className="primary-button"
                onClick={() => setFormAbierto(true)}
                type="button"
              >
                Registrar movimiento
              </button>
            </div>

            <div className="kardex-filters">
              <select
                aria-label="Filtrar por tipo de movimiento"
                onChange={(event) => {
                  setTipoFiltro(event.currentTarget.value as TipoMovimientoKardex | '');
                  setPagina(1);
                }}
                value={tipoFiltro}
              >
                <option value="">Todos los tipos</option>
                <option value="Entrada">Entrada</option>
                <option value="Salida">Salida</option>
              </select>
              <label className="kardex-date-field">
                <span>Desde</span>
                <input
                  aria-label="Fecha desde"
                  onChange={(event) => {
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
                    setFechaHasta(event.currentTarget.value);
                    setPagina(1);
                  }}
                  type="date"
                  value={fechaHasta}
                />
              </label>
            </div>

            {errorMovimientos !== '' && (
              <p aria-live="polite" className="clientes-error" role="alert">
                <span aria-hidden="true">!</span>
                {errorMovimientos}
              </p>
            )}

            {cargandoMovimientos && <p className="clientes-status">Cargando movimientos…</p>}

            {!cargandoMovimientos &&
              errorMovimientos === '' &&
              paginaMovimientos !== null &&
              paginaMovimientos.items.length === 0 && (
                <p className="clientes-status">No hay movimientos registrados con los filtros actuales.</p>
              )}

            {!cargandoMovimientos &&
              paginaMovimientos !== null &&
              paginaMovimientos.items.length > 0 && (
                <TablaPaginada
                  cambiarPagina={setPagina}
                  caption="Movimientos de kárdex registrados"
                  className="kardex-table"
                  classNameContenedor="kardex-table-scroll"
                  classNamePaginacion="clientes-pagination"
                  columnas={[
                    { id: 'fecha', etiqueta: 'Fecha' },
                    { id: 'tipo', etiqueta: 'Tipo' },
                    { id: 'producto', etiqueta: 'Producto' },
                    { id: 'almacen', etiqueta: 'Almacén' },
                    { id: 'cantidad', etiqueta: 'Cantidad' },
                    { id: 'costo', etiqueta: 'Costo unit.' },
                    { id: 'referencia', etiqueta: 'Referencia' },
                  ]}
                  elementos={paginaMovimientos.items}
                  etiquetaRegion="Tabla de movimientos de kárdex"
                  obtenerClave={(mov) => mov.id}
                  pagina={pagina}
                  renderizarFila={(mov) => (
                    <tr>
                      <td className="kardex-nowrap">{formatearFecha(mov.fecha)}</td>
                      <td>
                        <span
                          className={`kardex-badge ${mov.tipo === 'Entrada' ? 'is-entrada' : 'is-salida'}`}
                        >
                          {mov.tipo}
                        </span>
                      </td>
                      <td>{mov.productoNombre}</td>
                      <td className="kardex-nowrap">{mov.almacenCodigo}</td>
                      <td className="kardex-numeric">{mov.cantidad}</td>
                      <td className="kardex-numeric">{formatearMoneda(mov.costoUnitario)}</td>
                      <td>{mov.referencia ?? '—'}</td>
                    </tr>
                  )}
                  totalPaginas={totalPaginas}
                />
              )}
          </section>
        </>
      )}

      {vista === 'kardex' && (
        <section aria-labelledby="kardex-hoja-title" className="clientes-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">HOJA DE VALORIZACIÓN</div>
              <h2 id="kardex-hoja-title">Consulta de kárdex por producto y almacén</h2>
            </div>
          </div>

          <form className="kardex-consulta-form" onSubmit={(event) => void ejecutarConsultaKardex(event)}>
            <div className="form-field">
              <label htmlFor="consulta-producto-id">ID de producto</label>
              <input
                id="consulta-producto-id"
                min={1}
                onChange={(event) => setKardexProductoId(event.currentTarget.value)}
                type="number"
                value={kardexProductoId}
              />
            </div>
            <div className="form-field">
              <label htmlFor="consulta-almacen-id">ID de almacén</label>
              <input
                id="consulta-almacen-id"
                min={1}
                onChange={(event) => setKardexAlmacenId(event.currentTarget.value)}
                type="number"
                value={kardexAlmacenId}
              />
            </div>
            <div className="form-field">
              <label htmlFor="consulta-metodo">Método de valuación</label>
              <select
                id="consulta-metodo"
                onChange={(event) => setMetodo(event.currentTarget.value as MetodoValorizacion)}
                value={metodo}
              >
                <option value="Promedio">Promedio Ponderado</option>
                <option value="PEPS">PEPS (FIFO)</option>
              </select>
            </div>
            <button className="primary-button" disabled={cargandoKardex} type="submit">
              {cargandoKardex ? 'Calculando…' : 'Calcular kárdex'}
            </button>
          </form>

          {errorKardex !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              <span aria-hidden="true">!</span>
              {errorKardex}
            </p>
          )}

          {kardexDatos !== null && (
            <div className="kardex-resultado">
              <div className="kardex-resultado-header">
                <strong>
                  {kardexDatos.productoNombre} · Almacén {kardexDatos.almacenCodigo} ({kardexDatos.metodo})
                </strong>
                <span className="kardex-numeric">
                  Saldo físico: {kardexDatos.saldoCantidad} und · Saldo valorizado:{' '}
                  {formatearMoneda(kardexDatos.saldoValor)}
                </span>
              </div>
              <div className="kardex-table-scroll" role="region" tabIndex={0} aria-label="Detalle valorizado del kárdex">
                <table className="tabla-paginada kardex-table">
                  <caption>Movimientos valorizados ({kardexDatos.metodo})</caption>
                  <thead>
                    <tr>
                      <th scope="col">Fecha</th>
                      <th scope="col">Tipo</th>
                      <th scope="col">Cant.</th>
                      <th scope="col">Costo unit.</th>
                      <th scope="col">Valor mov.</th>
                      <th scope="col">Saldo cant.</th>
                      <th scope="col">Saldo valor</th>
                      <th scope="col">Costo prom.</th>
                    </tr>
                  </thead>
                  <tbody>
                    {kardexDatos.lineas.map((linea) => (
                      <tr key={linea.movimientoId}>
                        <td className="kardex-nowrap">{formatearFecha(linea.fecha)}</td>
                        <td>{linea.tipo}</td>
                        <td className="kardex-numeric">{linea.cantidad}</td>
                        <td className="kardex-numeric">{formatearMoneda(linea.costoUnitario)}</td>
                        <td className="kardex-numeric">{formatearMoneda(linea.valorMovimiento)}</td>
                        <td className="kardex-numeric">{linea.saldoCantidad}</td>
                        <td className="kardex-numeric">{formatearMoneda(linea.saldoValor)}</td>
                        <td className="kardex-numeric">{formatearMoneda(linea.costoPromedio)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </section>
      )}

      {vista === 'saldos' && (
        <section aria-labelledby="kardex-saldos-title" className="clientes-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">EXISTENCIAS CONSOLIDADAS</div>
              <h2 id="kardex-saldos-title">Saldos por producto y almacén</h2>
            </div>
          </div>

          {errorSaldos !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              <span aria-hidden="true">!</span>
              {errorSaldos}
            </p>
          )}

          {cargandoSaldos && <p className="clientes-status">Cargando saldos…</p>}

          {!cargandoSaldos && errorSaldos === '' && saldos.length === 0 && (
            <p className="clientes-status">No existen saldos registrados en el kárdex.</p>
          )}

          {!cargandoSaldos && saldos.length > 0 && (
            <div className="kardex-table-scroll" role="region" tabIndex={0} aria-label="Tabla de saldos de kárdex">
              <table className="tabla-paginada kardex-table">
                <caption>Saldos acumulados por almacén</caption>
                <thead>
                  <tr>
                    <th scope="col">Almacén</th>
                    <th scope="col">Producto</th>
                    <th scope="col">Entradas</th>
                    <th scope="col">Salidas</th>
                    <th scope="col">Saldo actual</th>
                  </tr>
                </thead>
                <tbody>
                  {saldos.map((saldo) => (
                    <tr key={`${saldo.almacenId}-${saldo.productoId}`}>
                      <td className="kardex-nowrap">{saldo.almacenCodigo}</td>
                      <td>{saldo.productoNombre}</td>
                      <td className="kardex-numeric">{saldo.entradas}</td>
                      <td className="kardex-numeric">{saldo.salidas}</td>
                      <td className="kardex-numeric">{saldo.saldo}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </PaginaModulo>
  );
}
