import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearCantidad } from '../../lib/formato';
import {
  actualizarProveedor,
  crearProveedor,
  eliminarProveedor,
  listarPaisesProveedor,
  listarProveedores,
  obtenerProveedor,
  obtenerResumenProveedores,
} from './api';
import type {
  FiltrosProveedor,
  GuardarProveedorRequest,
  PaginaProveedores,
  ProveedorDetalle,
  ResumenProveedores,
} from './tipos';

const TAMANO_PAGINA = 10;

interface ErroresProveedor {
  razonSocial?: string;
  ruc?: string;
  pais?: string;
  email?: string;
  telefono?: string;
}

function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
}

function validarProveedor(valores: GuardarProveedorRequest): ErroresProveedor {
  const errores: ErroresProveedor = {};
  const razonSocial = valores.razonSocial.trim();
  const ruc = valores.ruc.trim();
  const pais = valores.pais.trim();
  const email = valores.email?.trim() ?? '';
  const telefono = valores.telefono?.trim() ?? '';

  if (razonSocial === '') {
    errores.razonSocial = 'La razón social es obligatoria.';
  } else if (razonSocial.length > 200) {
    errores.razonSocial = 'La razón social no puede superar 200 caracteres.';
  }
  if (!/^\d{11}$/.test(ruc)) {
    errores.ruc = 'El RUC debe tener 11 dígitos numéricos.';
  }
  if (pais === '') {
    errores.pais = 'El país es obligatorio.';
  } else if (pais.length > 100) {
    errores.pais = 'El país no puede superar 100 caracteres.';
  }
  if (email !== '' && (email.length > 150 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))) {
    errores.email = 'El email no tiene un formato válido.';
  }
  if (telefono !== '' && !/^[\d +-]{6,20}$/.test(telefono)) {
    errores.telefono = 'El teléfono no tiene un formato válido.';
  }
  return errores;
}

function Resumen({ datos, error }: { datos: ResumenProveedores | null; error: string }) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen…</p>;
  }
  return (
    <section aria-label="Resumen de proveedores" className="proveedores-summary">
      <article className="cliente-stat">
        <span>Total de proveedores</span>
        <strong>{datos.totalProveedores}</strong>
      </article>
      <article className="cliente-stat">
        <span>Activos</span>
        <strong>{datos.activos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Inactivos</span>
        <strong>{datos.inactivos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Sin email</span>
        <strong>{datos.sinEmail}</strong>
      </article>
      <section aria-labelledby="proveedores-pais-title" className="proveedores-country-panel">
        <h2 id="proveedores-pais-title">Proveedores por país</h2>
        {datos.porPais.length === 0 ? (
          <p role="status">Sin proveedores registrados por país</p>
        ) : (
          <table className="proveedores-country-table">
            <caption>Distribución de proveedores por país</caption>
            <thead>
              <tr>
                <th scope="col">País</th>
                <th scope="col">Proveedores</th>
                <th scope="col">Activos</th>
              </tr>
            </thead>
            <tbody>
              {datos.porPais.map((pais) => (
                <tr key={pais.pais}>
                  <th scope="row">{pais.pais}</th>
                  <td>{formatearCantidad(pais.totalProveedores, 'proveedor', 'proveedores')}</td>
                  <td>{pais.activos}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </section>
  );
}

function FormularioProveedor({ proveedor, paises, cancelar, guardado }: {
  proveedor: ProveedorDetalle | null;
  paises: string[];
  cancelar: () => void;
  guardado: () => Promise<void>;
}) {
  const [razonSocial, setRazonSocial] = useState(proveedor?.razonSocial ?? '');
  const [ruc, setRuc] = useState(proveedor?.ruc ?? '');
  const [pais, setPais] = useState(proveedor?.pais ?? '');
  const [email, setEmail] = useState(proveedor?.email ?? '');
  const [telefono, setTelefono] = useState(proveedor?.telefono ?? '');
  const [activo, setActivo] = useState(proveedor?.activo ?? true);
  const [errores, setErrores] = useState<ErroresProveedor>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const request: GuardarProveedorRequest = {
      razonSocial: razonSocial.trim(),
      ruc: ruc.trim(),
      pais: pais.trim(),
      activo,
    };
    if (email.trim() !== '') {
      request.email = email.trim();
    }
    if (telefono.trim() !== '') {
      request.telefono = telefono.trim();
    }
    const nuevosErrores = validarProveedor(request);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setGuardando(true);
    try {
      if (proveedor === null) {
        await crearProveedor(request);
      } else {
        await actualizarProveedor(proveedor.id, request);
      }
      await guardado();
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el proveedor.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="proveedor-form-title" className="clientes-form-panel proveedores-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">PROVEEDORES</div>
          <h2 id="proveedor-form-title">{proveedor === null ? 'Nuevo proveedor' : 'Editar proveedor'}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="proveedor-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Razón social" htmlFor="proveedor-razon-social" error={errores.razonSocial} idError="proveedor-razon-social-error">
          <input
            aria-describedby={errores.razonSocial ? 'proveedor-razon-social-error' : undefined}
            aria-invalid={Boolean(errores.razonSocial)}
            autoComplete="organization"
            id="proveedor-razon-social"
            maxLength={220}
            onChange={(event) => setRazonSocial(event.currentTarget.value)}
            value={razonSocial}
          />
        </CampoConError>
        <CampoConError etiqueta="RUC" htmlFor="proveedor-ruc" error={errores.ruc} idError="proveedor-ruc-error">
          <input
            aria-describedby={errores.ruc ? 'proveedor-ruc-error' : undefined}
            aria-invalid={Boolean(errores.ruc)}
            id="proveedor-ruc"
            inputMode="numeric"
            maxLength={11}
            onChange={(event) => setRuc(event.currentTarget.value)}
            value={ruc}
          />
        </CampoConError>
        <CampoConError etiqueta="País" htmlFor="proveedor-pais" error={errores.pais} idError="proveedor-pais-error">
          <input
            aria-describedby={errores.pais ? 'proveedor-pais-error' : undefined}
            aria-invalid={Boolean(errores.pais)}
            id="proveedor-pais"
            list="proveedor-paises"
            maxLength={110}
            onChange={(event) => setPais(event.currentTarget.value)}
            value={pais}
          />
          <datalist id="proveedor-paises">
            {paises.map((nombrePais) => <option key={nombrePais} value={nombrePais} />)}
          </datalist>
        </CampoConError>
        <CampoConError etiqueta="Email" htmlFor="proveedor-email" error={errores.email} idError="proveedor-email-error">
          <input
            aria-describedby={errores.email ? 'proveedor-email-error' : undefined}
            aria-invalid={Boolean(errores.email)}
            autoComplete="email"
            id="proveedor-email"
            maxLength={160}
            onChange={(event) => setEmail(event.currentTarget.value)}
            type="email"
            value={email}
          />
        </CampoConError>
        <CampoConError etiqueta="Teléfono" htmlFor="proveedor-telefono" error={errores.telefono} idError="proveedor-telefono-error">
          <input
            aria-describedby={errores.telefono ? 'proveedor-telefono-error' : undefined}
            aria-invalid={Boolean(errores.telefono)}
            autoComplete="tel"
            id="proveedor-telefono"
            maxLength={22}
            onChange={(event) => setTelefono(event.currentTarget.value)}
            value={telefono}
          />
        </CampoConError>
        <label className="proveedor-active-field">
          <input checked={activo} onChange={(event) => setActivo(event.currentTarget.checked)} type="checkbox" />
          Activo
        </label>
        <div className="proveedores-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar proveedor'}
          </button>
        </div>
      </form>
    </section>
  );
}

export function ProveedoresPage() {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [filtros, setFiltros] = useState<{ pais: string; activo: boolean | ''; pagina: number }>({
    pais: '',
    activo: '',
    pagina: 1,
  });
  const [lista, setLista] = useState<PaginaProveedores | null>(null);
  const [resumen, setResumen] = useState<ResumenProveedores | null>(null);
  const [paises, setPaises] = useState<string[]>([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorPaises, setErrorPaises] = useState('');
  const [seleccionado, setSeleccionado] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<ProveedorDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [proveedorEdicion, setProveedorEdicion] = useState<ProveedorDetalle | null>(null);
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
    let activo = true;
    void obtenerResumenProveedores().then((datos) => {
      if (activo) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (activo) {
        setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de proveedores.'));
      }
    });
    return () => {
      activo = false;
    };
  }, [actualizacion]);

  useEffect(() => {
    let activo = true;
    void listarPaisesProveedor().then((datos) => {
      if (activo) {
        setPaises(datos);
        setErrorPaises('');
      }
    }).catch((error: unknown) => {
      if (activo) {
        setErrorPaises(mensajeError(error, 'No se pudieron cargar los países.'));
      }
    });
    return () => {
      activo = false;
    };
  }, []);

  useEffect(() => {
    if (busqueda !== busquedaAplicada) {
      return;
    }
    let activo = true;
    setErrorLista('');
    const consulta: FiltrosProveedor = {
      busqueda: busquedaAplicada,
      pais: filtros.pais,
      activo: filtros.activo,
      pagina: filtros.pagina,
      tamanoPagina: TAMANO_PAGINA,
    };
    void listarProveedores(consulta).then((datos) => {
      if (activo) {
        setLista(datos);
        const proveedorActual = seleccionadoRef.current;
        if (datos.items.length > 0 && (proveedorActual === null || !datos.items.some((item) => item.id === proveedorActual))) {
          setSeleccionado(datos.items[0]?.id ?? null);
        }
        if (datos.items.length === 0) {
          setSeleccionado(null);
          setDetalle(null);
        }
      }
    }).catch((error: unknown) => {
      if (activo) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de proveedores.'));
      }
    });
    return () => {
      activo = false;
    };
  }, [busqueda, busquedaAplicada, filtros, actualizacion]);

  useEffect(() => {
    if (seleccionado === null) {
      setDetalle(null);
      setErrorDetalle('');
      return;
    }
    let activo = true;
    setErrorDetalle('');
    void obtenerProveedor(seleccionado).then((datos) => {
      if (activo) {
        setDetalle(datos);
      }
    }).catch((error: unknown) => {
      if (activo) {
        setErrorDetalle(mensajeError(error, 'No se pudo cargar el detalle del proveedor.'));
        setDetalle(null);
      }
    });
    return () => {
      activo = false;
    };
  }, [seleccionado, actualizacion]);

  function cambiarFiltro(cambios: Partial<typeof filtros>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
  }

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setFiltros((actual) => ({ ...actual, pagina: 1 }));
  }

  async function guardarProveedor(): Promise<void> {
    setMostrarFormulario(false);
    setProveedorEdicion(null);
    setActualizacion((valor) => valor + 1);
  }

  async function confirmarEliminacion(): Promise<void> {
    if (detalle === null) {
      return;
    }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarProveedor(detalle.id);
      cerrarDialogo();
      setSeleccionado(null);
      setDetalle(null);
      setActualizacion((valor) => valor + 1);
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo eliminar el proveedor.'));
    } finally {
      setEliminando(false);
    }
  }

  function abrirEdicion(): void {
    if (detalle !== null) {
      setProveedorEdicion(detalle);
      setMostrarFormulario(true);
    }
  }

  const paginas = Math.max(1, Math.ceil((lista?.total ?? 0) / TAMANO_PAGINA));

  return (
    <PaginaModulo className="proveedores-page" modulo="11-proveedores">
      <p className="module-intro">Administra los datos y el estado de tus proveedores.</p>
      <Resumen datos={resumen} error={errorResumen} />
      <div className="proveedores-work-area">
        <section aria-labelledby="proveedores-list-title" className="clientes-list-panel proveedores-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">DIRECTORIO</div>
              <h2 id="proveedores-list-title">Todos los proveedores</h2>
            </div>
            <button
              className="primary-button"
              onClick={() => {
                setProveedorEdicion(null);
                setMostrarFormulario(true);
              }}
              type="button"
            >
              Nuevo proveedor
            </button>
          </div>
          <div className="proveedores-filters">
            <div>
              <label htmlFor="proveedores-busqueda">Buscar por razón social o RUC</label>
              <input
                autoComplete="off"
                id="proveedores-busqueda"
                onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
                placeholder="Escribe para buscar…"
                type="search"
                value={busqueda}
              />
            </div>
            <div>
              <label htmlFor="proveedores-pais">País</label>
              <select
                id="proveedores-pais"
                onChange={(event) => cambiarFiltro({ pais: event.currentTarget.value })}
                value={filtros.pais}
              >
                <option value="">Todos</option>
                {paises.map((nombrePais) => <option key={nombrePais} value={nombrePais}>{nombrePais}</option>)}
              </select>
            </div>
            <div>
              <label htmlFor="proveedores-estado">Estado</label>
              <select
                id="proveedores-estado"
                onChange={(event) => cambiarFiltro({ activo: event.currentTarget.value === '' ? '' : event.currentTarget.value === 'true' })}
                value={filtros.activo === '' ? '' : String(filtros.activo)}
              >
                <option value="">Todos</option>
                <option value="true">Activos</option>
                <option value="false">Inactivos</option>
              </select>
            </div>
          </div>
          {errorPaises !== '' && <p className="clientes-error" role="alert">{errorPaises}</p>}
          {errorLista !== '' && <p aria-live="polite" className="clientes-error" role="alert">{errorLista}</p>}
          {lista === null && errorLista === '' && <p aria-live="polite" className="clientes-status" role="status">Cargando proveedores…</p>}
          {lista !== null && errorLista === '' && lista.items.length === 0 && (
            <p aria-live="polite" className="clientes-status" role="status">No hay proveedores que coincidan</p>
          )}
          {lista !== null && errorLista === '' && lista.items.length > 0 && (
            <TablaPaginada
              caption="Directorio de proveedores"
              etiquetaRegion="Tabla de proveedores"
              columnas={[
                { id: 'razonSocial', etiqueta: 'Razón social' },
                { id: 'ruc', etiqueta: 'RUC' },
                { id: 'pais', etiqueta: 'País' },
                { id: 'activo', etiqueta: 'Estado' },
              ]}
              elementos={lista.items}
              obtenerClave={(proveedor) => proveedor.id}
              pagina={filtros.pagina}
              totalPaginas={paginas}
              cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
              className="proveedores-table"
              classNameContenedor="proveedores-table-scroll"
              classNamePaginacion="clientes-pagination"
              renderizarFila={(proveedor) => (
                <tr className={seleccionado === proveedor.id ? 'is-selected' : ''}>
                  <td>
                    <button
                      aria-current={seleccionado === proveedor.id ? 'true' : undefined}
                      className="cliente-row-select"
                      onClick={() => setSeleccionado(proveedor.id)}
                      type="button"
                    >
                      {proveedor.razonSocial}
                    </button>
                  </td>
                  <td>{proveedor.ruc}</td>
                  <td>{proveedor.pais}</td>
                  <td>
                    <span className={`proveedor-status${proveedor.activo ? ' is-active' : ' is-inactive'}`}>
                      {proveedor.activo ? 'Activo' : 'Inactivo'}
                    </span>
                  </td>
                </tr>
              )}
            />
          )}
        </section>
        <PanelDetalle ariaLabelledBy="proveedor-detail-title" className="clientes-detail-panel proveedores-detail-panel">
          {errorDetalle !== '' && <p className="clientes-error" role="alert">{errorDetalle}</p>}
          {detalle === null && errorDetalle === '' && (
            <p aria-live="polite" className="clientes-status" role="status">Selecciona un proveedor para ver su detalle.</p>
          )}
          {detalle !== null && (
            <>
              <div className="clientes-panel-heading">
                <div>
                  <div className="page-eyebrow">FICHA DEL PROVEEDOR</div>
                  <h2 id="proveedor-detail-title">{detalle.razonSocial}</h2>
                </div>
                <span aria-hidden="true" className="cliente-initials">
                  {detalle.razonSocial.slice(0, 1).toLocaleUpperCase('es-PE')}
                </span>
              </div>
              <dl className="proveedores-detail-grid">
                <div>
                  <dt>RUC</dt>
                  <dd>{detalle.ruc}</dd>
                </div>
                <div>
                  <dt>País</dt>
                  <dd>{detalle.pais}</dd>
                </div>
                <div>
                  <dt>Email</dt>
                  <dd>{detalle.email || 'Sin email'}</dd>
                </div>
                <div>
                  <dt>Teléfono</dt>
                  <dd>{detalle.telefono || 'Sin teléfono'}</dd>
                </div>
                <div>
                  <dt>Estado</dt>
                  <dd>{detalle.activo ? 'Activo' : 'Inactivo'}</dd>
                </div>
              </dl>
              <div className="clientes-detail-actions">
                <button className="secondary-button" onClick={abrirEdicion} type="button">Editar</button>
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
        <FormularioProveedor
          cancelar={() => {
            setMostrarFormulario(false);
            setProveedorEdicion(null);
          }}
          guardado={guardarProveedor}
          key={proveedorEdicion?.id ?? 'nuevo'}
          paises={paises}
          proveedor={proveedorEdicion}
        />
      )}
      {dialogoAbierto && detalle !== null && (
        <DialogoBaja
          entidad="proveedor"
          nombre={detalle.razonSocial}
          cerrar={cerrarDialogo}
          confirmar={() => void confirmarEliminacion()}
          eliminando={eliminando}
          error={errorEliminar}
        />
      )}
    </PaginaModulo>
  );
}
