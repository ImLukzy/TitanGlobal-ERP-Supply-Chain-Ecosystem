import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import {
  actualizarCategoriaProducto,
  crearCategoriaProducto,
  desactivarCategoriaProducto,
  listarCategoriasProducto,
  obtenerCategoriaProducto,
  obtenerResumenCategoriasProducto,
  reactivarCategoriaProducto,
} from './api';
import { mensajeError } from './presentacion';
import type { CategoriaProducto, ResumenCategoriasProducto } from './tipos';
import { validarCategoriaProducto, type ErroresCategoriaProducto } from './validacion';

export function CategoriasProductoPage() {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [incluirInactivas, setIncluirInactivas] = useState(true);
  const [categorias, setCategorias] = useState<CategoriaProducto[] | null>(null);
  const [resumen, setResumen] = useState<ResumenCategoriasProducto | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [mensajeExito, setMensajeExito] = useState('');
  const [seleccionadaId, setSeleccionadaId] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<CategoriaProducto | null>(null);
  const [cargandoDetalle, setCargandoDetalle] = useState(false);
  const [errorDetalle, setErrorDetalle] = useState('');

  const [formularioAbierto, setFormularioAbierto] = useState(false);
  const [editando, setEditando] = useState<CategoriaProducto | null>(null);
  const [nombreForm, setNombreForm] = useState('');
  const [descripcionForm, setDescripcionForm] = useState('');
  const [erroresForm, setErroresForm] = useState<ErroresCategoriaProducto>({});
  const [errorFormApi, setErrorFormApi] = useState('');
  const [guardando, setGuardando] = useState(false);

  const [baja, setBaja] = useState<CategoriaProducto | null>(null);
  const [eliminando, setEliminando] = useState(false);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [reactivando, setReactivando] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);

  const botonNuevaRef = useRef<HTMLButtonElement>(null);
  const seleccionadaRef = useRef(seleccionadaId);
  seleccionadaRef.current = seleccionadaId;

  useEffect(() => {
    const timer = window.setTimeout(() => setBusquedaAplicada(busqueda), 250);
    return () => window.clearTimeout(timer);
  }, [busqueda]);

  useEffect(() => {
    if (busqueda !== busquedaAplicada) {
      return;
    }
    let vigente = true;
    setCategorias(null);
    setErrorLista('');

    void Promise.all([
      listarCategoriasProducto({ buscar: busquedaAplicada, incluirInactivas }),
      obtenerResumenCategoriasProducto(),
    ])
      .then(([listaDatos, resumenDatos]) => {
        if (!vigente) {
          return;
        }
        setCategorias(listaDatos);
        setResumen(resumenDatos);
        if (!listaDatos.some((item) => item.id === seleccionadaRef.current)) {
          setSeleccionadaId(listaDatos[0]?.id ?? null);
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          setErrorLista(mensajeError(error, 'No se pudo cargar el catálogo de categorías.'));
        }
      });

    return () => {
      vigente = false;
    };
  }, [busqueda, busquedaAplicada, incluirInactivas, actualizacion]);

  useEffect(() => {
    setDetalle(null);
    setErrorDetalle('');
    if (seleccionadaId === null) {
      setCargandoDetalle(false);
      return;
    }
    let vigente = true;
    setCargandoDetalle(true);

    void obtenerCategoriaProducto(seleccionadaId)
      .then((datos) => {
        if (vigente) {
          setDetalle(datos);
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          setErrorDetalle(mensajeError(error, 'No se pudo cargar el detalle de la categoría.'));
        }
      })
      .finally(() => {
        if (vigente) {
          setCargandoDetalle(false);
        }
      });

    return () => {
      vigente = false;
    };
  }, [seleccionadaId, actualizacion]);

  function abrirCrear(): void {
    setEditando(null);
    setNombreForm('');
    setDescripcionForm('');
    setErroresForm({});
    setErrorFormApi('');
    setFormularioAbierto(true);
  }

  function abrirEditar(categoria: CategoriaProducto): void {
    setEditando(categoria);
    setNombreForm(categoria.nombre);
    setDescripcionForm(categoria.descripcion ?? '');
    setErroresForm({});
    setErrorFormApi('');
    setFormularioAbierto(true);
  }

  function cerrarFormulario(): void {
    if (guardando) {
      return;
    }
    setFormularioAbierto(false);
    setEditando(null);
    window.requestAnimationFrame(() => botonNuevaRef.current?.focus());
  }

  async function manejarGuardar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const payload = {
      nombre: nombreForm.trim(),
      descripcion: descripcionForm.trim() === '' ? undefined : descripcionForm.trim(),
    };
    const errores = validarCategoriaProducto(payload);
    setErroresForm(errores);
    if (Object.keys(errores).length > 0) {
      return;
    }

    setGuardando(true);
    setErrorFormApi('');
    try {
      if (editando === null) {
        const creada = await crearCategoriaProducto(payload);
        setMensajeExito(`Categoría "${payload.nombre}" registrada correctamente.`);
        setSeleccionadaId(creada.id);
      } else {
        await actualizarCategoriaProducto(editando.id, payload);
        setMensajeExito(`Categoría "${payload.nombre}" actualizada correctamente.`);
        setSeleccionadaId(editando.id);
      }
      setFormularioAbierto(false);
      setEditando(null);
      setActualizacion((valor) => valor + 1);
    } catch (error: unknown) {
      setErrorFormApi(mensajeError(error, 'No se pudo guardar la categoría.'));
    } finally {
      setGuardando(false);
    }
  }

  const cerrarBaja = useCallback(() => {
    if (eliminando) {
      return;
    }
    setBaja(null);
    setErrorEliminar('');
  }, [eliminando]);

  async function confirmarDesactivar(): Promise<void> {
    if (baja === null) {
      return;
    }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await desactivarCategoriaProducto(baja.id);
      setMensajeExito(`Categoría "${baja.nombre}" desactivada correctamente.`);
      setBaja(null);
      setActualizacion((valor) => valor + 1);
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo desactivar la categoría.'));
    } finally {
      setEliminando(false);
    }
  }

  async function manejarReactivar(categoria: CategoriaProducto): Promise<void> {
    setReactivando(true);
    setErrorDetalle('');
    try {
      await reactivarCategoriaProducto(categoria.id);
      setMensajeExito(`Categoría "${categoria.nombre}" reactivada correctamente.`);
      setActualizacion((valor) => valor + 1);
    } catch (error: unknown) {
      setErrorDetalle(mensajeError(error, 'No se pudo reactivar la categoría.'));
    } finally {
      setReactivando(false);
    }
  }

  return (
    <PaginaModulo modulo={22}>
      {resumen !== null && (
        <section aria-label="Resumen de categorías" className="module-kpi-grid">
          <article className="module-kpi-card">
            <span className="module-kpi-label">Total categorías</span>
            <strong className="module-kpi-value">{resumen.totalCategorias}</strong>
          </article>
          <article className="module-kpi-card">
            <span className="module-kpi-label">Activas</span>
            <strong className="module-kpi-value">{resumen.categoriasActivas}</strong>
          </article>
          <article className="module-kpi-card">
            <span className="module-kpi-label">Inactivas</span>
            <strong className="module-kpi-value">{resumen.categoriasInactivas}</strong>
          </article>
        </section>
      )}

      <section className="module-toolbar">
        <div className="module-toolbar-filters">
          <label className="field-inline">
            <span>Buscar</span>
            <input
              aria-label="Buscar categoría"
              onChange={(event) => setBusqueda(event.target.value)}
              placeholder="Nombre o descripción…"
              type="search"
              value={busqueda}
            />
          </label>
          <label className="checkbox-inline">
            <input
              checked={incluirInactivas}
              onChange={(event) => setIncluirInactivas(event.target.checked)}
              type="checkbox"
            />
            <span>Mostrar inactivas</span>
          </label>
        </div>
        <button className="primary-button" onClick={abrirCrear} ref={botonNuevaRef} type="button">
          Nueva categoría
        </button>
      </section>

      {mensajeExito !== '' && (
        <p aria-live="polite" className="module-feedback-success" role="status">
          {mensajeExito}
        </p>
      )}

      {errorLista !== '' && (
        <p aria-live="polite" className="clientes-error" role="alert">
          <span aria-hidden="true">!</span>
          {errorLista}
        </p>
      )}

      <div className="module-master-detail">
        <section aria-label="Listado de categorías" className="module-list-card">
          {categorias === null && errorLista === '' && (
            <p className="module-empty-state">Cargando categorías…</p>
          )}

          {categorias !== null && categorias.length === 0 && (
            <p className="module-empty-state">No hay categorías registradas con los filtros actuales.</p>
          )}

          {categorias !== null && categorias.length > 0 && (
            <div className="table-scroll">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Nombre</th>
                    <th>Descripción</th>
                    <th>Estado</th>
                    <th>Acciones</th>
                  </tr>
                </thead>
                <tbody>
                  {categorias.map((categoria) => (
                    <tr
                      aria-selected={categoria.id === seleccionadaId}
                      className={categoria.id === seleccionadaId ? 'is-selected' : undefined}
                      key={categoria.id}
                      onClick={() => setSeleccionadaId(categoria.id)}
                    >
                      <td><strong>{categoria.nombre}</strong></td>
                      <td>{categoria.descripcion ?? '—'}</td>
                      <td>
                        <span className={`status-badge ${categoria.activa ? 'status-active' : 'status-inactive'}`}>
                          {categoria.activa ? 'Activa' : 'Inactiva'}
                        </span>
                      </td>
                      <td>
                        <div className="table-actions">
                          <button
                            className="secondary-button"
                            onClick={(event) => {
                              event.stopPropagation();
                              abrirEditar(categoria);
                            }}
                            type="button"
                          >
                            Editar
                          </button>
                          {categoria.activa ? (
                            <button
                              className="danger-button"
                              onClick={(event) => {
                                event.stopPropagation();
                                setErrorEliminar('');
                                setBaja(categoria);
                              }}
                              type="button"
                            >
                              Desactivar
                            </button>
                          ) : (
                            <button
                              className="secondary-button"
                              disabled={reactivando}
                              onClick={(event) => {
                                event.stopPropagation();
                                void manejarReactivar(categoria);
                              }}
                              type="button"
                            >
                              Reactivar
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <PanelDetalle ariaLabelledBy="categoria-detalle-titulo" className="module-detail-card">
          {cargandoDetalle && <p className="module-empty-state">Cargando detalle…</p>}
          {errorDetalle !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              <span aria-hidden="true">!</span>
              {errorDetalle}
            </p>
          )}
          {!cargandoDetalle && detalle === null && errorDetalle === '' && (
            <p className="module-empty-state">Selecciona una categoría para ver su detalle.</p>
          )}
          {detalle !== null && (
            <div className="detail-card-content">
              <div className="page-eyebrow">DETALLE DE CATEGORÍA #{detalle.id}</div>
              <h2 id="categoria-detalle-titulo">{detalle.nombre}</h2>
              <dl className="detail-grid">
                <div>
                  <dt>Estado</dt>
                  <dd>{detalle.activa ? 'Activa' : 'Inactiva'}</dd>
                </div>
                <div>
                  <dt>Descripción</dt>
                  <dd>{detalle.descripcion ?? 'Sin descripción registrada.'}</dd>
                </div>
              </dl>
              <div className="detail-actions">
                <button className="secondary-button" onClick={() => abrirEditar(detalle)} type="button">
                  Editar categoría
                </button>
                {detalle.activa ? (
                  <button
                    className="danger-button"
                    onClick={() => {
                      setErrorEliminar('');
                      setBaja(detalle);
                    }}
                    type="button"
                  >
                    Desactivar categoría
                  </button>
                ) : (
                  <button
                    className="primary-button"
                    disabled={reactivando}
                    onClick={() => void manejarReactivar(detalle)}
                    type="button"
                  >
                    {reactivando ? 'Reactivando…' : 'Reactivar categoría'}
                  </button>
                )}
              </div>
            </div>
          )}
        </PanelDetalle>
      </div>

      {formularioAbierto && (
        <div className="clientes-dialog-backdrop">
          <div
            aria-labelledby="categoria-form-titulo"
            aria-modal="true"
            className="clientes-dialog"
            role="dialog"
          >
            <div className="page-eyebrow">
              {editando === null ? 'NUEVA CATEGORÍA' : 'EDITAR CATEGORÍA'}
            </div>
            <h2 id="categoria-form-titulo">
              {editando === null ? 'Registrar categoría' : `Editar ${editando.nombre}`}
            </h2>
            <form noValidate onSubmit={(event) => void manejarGuardar(event)}>
              <CampoConError error={erroresForm.nombre} htmlFor="categoria-nombre" idError="categoria-nombre-error" etiqueta="Nombre">
                <input
                  id="categoria-nombre"
                  maxLength={100}
                  onChange={(event) => setNombreForm(event.target.value)}
                  required
                  type="text"
                  value={nombreForm}
                />
              </CampoConError>

              <CampoConError error={erroresForm.descripcion} htmlFor="categoria-descripcion" idError="categoria-descripcion-error" etiqueta="Descripción">
                <textarea
                  id="categoria-descripcion"
                  maxLength={250}
                  onChange={(event) => setDescripcionForm(event.target.value)}
                  rows={3}
                  value={descripcionForm}
                />
              </CampoConError>

              {errorFormApi !== '' && (
                <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
                  <span aria-hidden="true">!</span>
                  {errorFormApi}
                </p>
              )}

              <div className="clientes-dialog-actions">
                <button className="secondary-button" disabled={guardando} onClick={cerrarFormulario} type="button">
                  Cancelar
                </button>
                <button className="primary-button" disabled={guardando} type="submit">
                  {guardando ? 'Guardando…' : 'Guardar categoría'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {baja !== null && (
        <DialogoBaja
          cerrar={cerrarBaja}
          confirmar={() => void confirmarDesactivar()}
          eliminando={eliminando}
          entidad="categoría"
          error={errorEliminar}
          nombre={baja.nombre}
        />
      )}
    </PaginaModulo>
  );
}
