import { useCallback, useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda, formatearCantidad, formatearFecha } from '../../lib/formato';
import { actualizarCliente, crearCliente, eliminarCliente, listarClientes, obtenerCliente, obtenerResumenClientes } from './api';
import type { ClienteDetalle, GuardarClienteRequest, PaginaClientes, ResumenClientes } from './tipos';

const TAMANO_PAGINA = 10;

function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
}

function Resumen({ datos, error }: { datos: ResumenClientes | null; error: string }) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen…</p>;
  }
  return (
    <section aria-label="Resumen de clientes" className="clientes-resumen">
      <article className="cliente-stat"><span>Total de clientes</span><strong>{datos.totalClientes}</strong></article>
      <article className="cliente-stat"><span>Con órdenes</span><strong>{datos.clientesConOrdenes}</strong></article>
      <article className="cliente-stat"><span>Sin descripción</span><strong>{datos.clientesSinDescripcion}</strong></article>
      <article className="cliente-top">
        <h2>Mayores compradores</h2>
        {datos.topClientesPorMonto.length === 0 ? <p>Sin compras registradas</p> : (
          <ol>
            {datos.topClientesPorMonto.map((cliente) => (
              <li key={cliente.clienteId}>
                <strong>{cliente.nombre}</strong>
                <span>{formatearCantidad(cliente.totalOrdenes, 'orden', 'órdenes')}</span>
                <b>{formateadorMoneda.format(cliente.montoTotal)}</b>
              </li>
            ))}
          </ol>
        )}
      </article>
    </section>
  );
}

function FormularioCliente({ cliente, cancelar, guardado }: {
  cliente: ClienteDetalle | null;
  cancelar: () => void;
  guardado: () => Promise<void>;
}) {
  const [nombre, setNombre] = useState(cliente?.nombre ?? '');
  const [ruc, setRuc] = useState(cliente?.ruc ?? '');
  const [descripcion, setDescripcion] = useState(cliente?.descripcion ?? '');
  const [errores, setErrores] = useState<{ nombre?: string; ruc?: string }>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores: { nombre?: string; ruc?: string } = {};
    if (nombre.trim() === '') {
      nuevosErrores.nombre = 'El nombre del cliente es obligatorio.';
    } else if (nombre.trim().length > 200) {
      nuevosErrores.nombre = 'El nombre del cliente no puede superar 200 caracteres.';
    }
    if (!/^\d{11}$/.test(ruc.trim())) {
      nuevosErrores.ruc = 'El RUC debe tener 11 dígitos numéricos.';
    }
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    const request: GuardarClienteRequest = { nombre: nombre.trim(), ruc: ruc.trim() };
    if (descripcion.trim() !== '') {
      request.descripcion = descripcion.trim();
    }
    setGuardando(true);
    try {
      if (cliente === null) {
        await crearCliente(request);
      } else {
        await actualizarCliente(cliente.id, request);
      }
      await guardado();
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el cliente.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="cliente-form-title" className="clientes-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">CLIENTES</div>
          <h2 id="cliente-form-title">{cliente === null ? 'Nuevo cliente' : 'Editar cliente'}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">
          Cancelar
        </button>
      </div>
      <form className="cliente-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Nombre" htmlFor="cliente-nombre" error={errores.nombre} idError="cliente-nombre-error">
          <input
            aria-describedby={errores.nombre ? 'cliente-nombre-error' : undefined}
            aria-invalid={Boolean(errores.nombre)}
            id="cliente-nombre"
            maxLength={220}
            onChange={(event) => setNombre(event.currentTarget.value)}
            value={nombre}
          />
        </CampoConError>
        <CampoConError etiqueta="RUC" htmlFor="cliente-ruc" error={errores.ruc} idError="cliente-ruc-error">
          <input
            aria-describedby={errores.ruc ? 'cliente-ruc-error' : undefined}
            aria-invalid={Boolean(errores.ruc)}
            id="cliente-ruc"
            inputMode="numeric"
            maxLength={11}
            onChange={(event) => setRuc(event.currentTarget.value)}
            value={ruc}
          />
        </CampoConError>
        <div className="form-field cliente-descripcion-field">
          <label htmlFor="cliente-descripcion">Descripción <span className="optional-label">Opcional</span></label>
          <textarea
            id="cliente-descripcion"
            onChange={(event) => setDescripcion(event.currentTarget.value)}
            rows={3}
            value={descripcion}
          />
        </div>
        <div className="clientes-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar cliente'}
          </button>
        </div>
      </form>
    </section>
  );
}

export function ClientesPage() {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [pagina, setPagina] = useState(1);
  const [lista, setLista] = useState<PaginaClientes | null>(null);
  const [resumen, setResumen] = useState<ResumenClientes | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [seleccionado, setSeleccionado] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<ClienteDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [clienteEdicion, setClienteEdicion] = useState<ClienteDetalle | null>(null);
  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);
  const seleccionadoRef = useRef(seleccionado);
  seleccionadoRef.current = seleccionado;
  const cerrarDialogo = useCallback(() => {
    setDialogoAbierto(false);
    setErrorEliminar('');
    window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
  }, []);

  useEffect(() => {
    const temporizador = window.setTimeout(() => setBusquedaAplicada(busqueda), 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  useEffect(() => {
    if (busqueda !== busquedaAplicada) {
      return;
    }
    let activo = true;
    setErrorLista('');
    void listarClientes(busquedaAplicada, pagina, TAMANO_PAGINA).then((datos) => {
      if (activo) {
        setLista(datos);
        const clienteActual = seleccionadoRef.current;
        if (datos.items.length > 0 && (clienteActual === null || !datos.items.some((cliente) => cliente.id === clienteActual))) {
          setSeleccionado(datos.items[0]?.id ?? null);
        }
        if (datos.items.length === 0) {
          setSeleccionado(null);
          setDetalle(null);
        }
      }
    }).catch((error: unknown) => {
      if (activo) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de clientes.'));
      }
    });
    return () => { activo = false; };
  }, [busqueda, busquedaAplicada, pagina, actualizacion]);

  useEffect(() => {
    let activo = true;
    void obtenerResumenClientes().then((datos) => {
      if (activo) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (activo) {
        setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de clientes.'));
      }
    });
    return () => { activo = false; };
  }, [actualizacion]);

  useEffect(() => {
    if (seleccionado === null) {
      setDetalle(null);
      setErrorDetalle('');
      return;
    }
    let activo = true;
    setErrorDetalle('');
    void obtenerCliente(seleccionado).then((datos) => {
      if (activo) {
        setDetalle(datos);
      }
    }).catch((error: unknown) => {
      if (activo) {
        setErrorDetalle(mensajeError(error, 'No se pudo cargar el detalle del cliente.'));
        setDetalle(null);
      }
    });
    return () => { activo = false; };
  }, [seleccionado, actualizacion]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setPagina(1);
  }

  async function refrescarTrasGuardar(): Promise<void> {
    setMostrarFormulario(false);
    setClienteEdicion(null);
    setActualizacion((valor) => valor + 1);
  }

  async function confirmarEliminacion(): Promise<void> {
    if (seleccionado === null) {
      return;
    }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarCliente(seleccionado);
      cerrarDialogo();
      setSeleccionado(null);
      setDetalle(null);
      setActualizacion((valor) => valor + 1);
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo eliminar el cliente.'));
    } finally {
      setEliminando(false);
    }
  }

  function abrirEdicion(): void {
    if (detalle !== null) {
      setClienteEdicion(detalle);
      setMostrarFormulario(true);
    }
  }

  function manejarTecladoFila(event: KeyboardEvent<HTMLButtonElement>, id: number): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      setSeleccionado(id);
    }
  }

  const paginas = Math.max(1, Math.ceil((lista?.total ?? 0) / TAMANO_PAGINA));

  return (
    <PaginaModulo className="clientes-page" modulo="01-clientes">
      <p className="module-intro">Administra datos, actividad y relaciones comerciales.</p>
      <Resumen datos={resumen} error={errorResumen} />
      <div className="clientes-work-area">
        <section aria-labelledby="clientes-list-title" className="clientes-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">DIRECTORIO</div>
              <h2 id="clientes-list-title">Todos los clientes</h2>
            </div>
            <button
              className="primary-button"
              onClick={() => {
                setClienteEdicion(null);
                setMostrarFormulario(true);
              }}
              type="button"
            >
              Nuevo cliente
            </button>
          </div>
          <div className="clientes-search">
            <label htmlFor="busqueda-clientes">Buscar por nombre o RUC</label>
            <input
              autoComplete="off"
              id="busqueda-clientes"
              onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
              placeholder="Escribe para buscar…"
              type="search"
              value={busqueda}
            />
          </div>
          {errorLista !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              {errorLista}
            </p>
          )}
          {lista === null && errorLista === '' && (
            <p aria-live="polite" className="clientes-status" role="status">
              Cargando clientes…
            </p>
          )}
          {lista !== null && errorLista === '' && lista.items.length === 0 && (
            <p aria-live="polite" className="clientes-status" role="status">
              No hay clientes que coincidan
            </p>
          )}
          {lista !== null && errorLista === '' && lista.items.length > 0 && (
            <>
              <TablaPaginada
                caption="Directorio de clientes"
                etiquetaRegion="Tabla de clientes"
                columnas={[{ id: 'nombre', etiqueta: 'Nombre' }, { id: 'ruc', etiqueta: 'RUC' }, { id: 'ordenes', etiqueta: 'Órdenes' }]}
                elementos={lista.items}
                obtenerClave={(cliente) => cliente.id}
                pagina={pagina}
                totalPaginas={paginas}
                cambiarPagina={setPagina}
                className="clientes-table"
                classNameContenedor="clientes-table-scroll"
                classNamePaginacion="clientes-pagination"
                renderizarFila={(cliente) => (
                  <tr className={seleccionado === cliente.id ? 'is-selected' : ''}>
                    <td>
                      <button
                        aria-current={seleccionado === cliente.id ? 'true' : undefined}
                        className="cliente-row-select"
                        onClick={() => setSeleccionado(cliente.id)}
                        onKeyDown={(event) => manejarTecladoFila(event, cliente.id)}
                        type="button"
                      >
                        {cliente.nombre}
                      </button>
                    </td>
                    <td>{cliente.ruc}</td>
                    <td>{cliente.totalOrdenes}</td>
                  </tr>
                )}
              />
            </>
          )}
        </section>
        <PanelDetalle ariaLabelledBy="cliente-detail-title" className="clientes-detail-panel">
          {errorDetalle !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              {errorDetalle}
            </p>
          )}
          {detalle === null && errorDetalle === '' && (
            <p aria-live="polite" className="clientes-status" role="status">
              Selecciona un cliente para ver su detalle.
            </p>
          )}
          {detalle !== null && (
            <>
              <div className="clientes-panel-heading">
                <div>
                  <div className="page-eyebrow">FICHA DEL CLIENTE</div>
                  <h2 id="cliente-detail-title">{detalle.nombre}</h2>
                </div>
                <span className="cliente-initials" aria-hidden="true">
                  {detalle.nombre.slice(0, 1).toLocaleUpperCase('es-PE')}
                </span>
              </div>
              <dl className="cliente-detail-grid">
                <div>
                  <dt>RUC</dt>
                  <dd>{detalle.ruc}</dd>
                </div>
                <div>
                  <dt>Órdenes de compra</dt>
                  <dd>{detalle.totalOrdenes}</dd>
                </div>
                <div>
                  <dt>Monto total</dt>
                  <dd>{formateadorMoneda.format(detalle.montoTotal)}</dd>
                </div>
                <div>
                  <dt>Última orden</dt>
                  <dd>{detalle.ultimaOrdenFecha ? formatearFecha(detalle.ultimaOrdenFecha) : 'Sin órdenes'}</dd>
                </div>
                <div className="cliente-description">
                  <dt>Descripción</dt>
                  <dd>{detalle.descripcion || 'Sin descripción registrada'}</dd>
                </div>
              </dl>
              <div className="clientes-detail-actions">
                <button className="secondary-button" onClick={abrirEdicion} type="button">
                  Editar
                </button>
                <button
                  className="danger-button"
                  onClick={() => {
                    setErrorEliminar('');
                    setDialogoAbierto(true);
                  }}
                  ref={botonEliminarRef}
                  type="button"
                >
                  Eliminar
                </button>
              </div>
            </>
          )}
        </PanelDetalle>
      </div>
      {mostrarFormulario && (
        <FormularioCliente
          cliente={clienteEdicion}
          cancelar={() => {
            setMostrarFormulario(false);
            setClienteEdicion(null);
          }}
          guardado={refrescarTrasGuardar}
          key={clienteEdicion?.id ?? 'nuevo'}
        />
      )}
      {dialogoAbierto && detalle !== null && (
        <DialogoBaja
          entidad="cliente"
          nombre={detalle.nombre}
          cerrar={cerrarDialogo}
          confirmar={() => void confirmarEliminacion()}
          eliminando={eliminando}
          error={errorEliminar}
        />
      )}
    </PaginaModulo>
  );
}
