import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import {
  actualizarCategoriaProducto,
  crearCategoriaProducto,
  desactivarCategoriaProducto,
  listarCategoriasProducto,
  obtenerCategoriaProducto,
  obtenerResumenCategoriasProducto,
} from './api';
import type {
  CategoriaProducto,
  GuardarCategoriaProductoRequest,
  ResumenCategoriasProducto,
} from './tipos';

const TAMANO_PAGINA = 10;

interface ErroresCategoriaProducto {
  nombre?: string;
  descripcion?: string;
}

function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
}

function validarCategoriaProducto(
  valores: GuardarCategoriaProductoRequest,
): ErroresCategoriaProducto {
  const errores: ErroresCategoriaProducto = {};
  const nombre = valores.nombre.trim();
  const descripcion = valores.descripcion?.trim() ?? '';

  if (nombre === '') {
    errores.nombre = 'El nombre de la categoría es obligatorio.';
  } else if (nombre.length > 100) {
    errores.nombre = 'El nombre de la categoría no puede superar 100 caracteres.';
  }

  if (descripcion.length > 250) {
    errores.descripcion = 'La descripción de la categoría no puede superar 250 caracteres.';
  }

  return errores;
}

function ResumenCategorias({
  datos,
  error,
}: {
  datos: ResumenCategoriasProducto | null;
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
    <section aria-label="Resumen de categorías de producto" className="categorias-summary">
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.totalCategorias}</span>
        <span className="cliente-stat-label">Total de categorías</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.categoriasActivas}</span>
        <span className="cliente-stat-label">Categorías activas</span>
      </article>
      <article className="cliente-stat">
        <span className="cliente-stat-value">{datos === null ? '—' : datos.categoriasInactivas}</span>
        <span className="cliente-stat-label">Categorías inactivas</span>
      </article>
    </section>
  );
}

function FormularioCategoriaProducto({
  categoria,
  cancelar,
  guardado,
}: {
  categoria: CategoriaProducto | null;
  cancelar: () => void;
  guardado: (idGuardado: number) => Promise<void>;
}) {
  const [nombre, setNombre] = useState(categoria?.nombre ?? '');
  const [descripcion, setDescripcion] = useState(categoria?.descripcion ?? '');
  const [errores, setErrores] = useState<ErroresCategoriaProducto>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const request: GuardarCategoriaProductoRequest = {
      nombre: nombre.trim(),
    };
    if (descripcion.trim() !== '') {
      request.descripcion = descripcion.trim();
    }

    const nuevosErrores = validarCategoriaProducto(request);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setGuardando(true);
    try {
      const respuesta =
        categoria === null
          ? await crearCategoriaProducto(request)
          : await actualizarCategoriaProducto(categoria.id, request);
      await guardado(respuesta.id);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar la categoría de producto.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section
      aria-labelledby="categoria-form-title"
      className="clientes-form-panel categorias-form-panel"
    >
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">{categoria === null ? 'ALTA DE CATÁLOGO' : 'EDICIÓN'}</div>
          <h2 id="categoria-form-title">
            {categoria === null ? 'Nueva categoría' : 'Editar categoría'}
          </h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">
          Cancelar
        </button>
      </div>
      <form className="categoria-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError
          etiqueta="Nombre"
          htmlFor="categoria-nombre"
          error={errores.nombre}
          idError="categoria-nombre-error"
        >
          <input
            aria-describedby={errores.nombre ? 'categoria-nombre-error' : undefined}
            aria-invalid={Boolean(errores.nombre)}
            id="categoria-nombre"
            maxLength={120}
            onChange={(event) => setNombre(event.currentTarget.value)}
            value={nombre}
          />
        </CampoConError>
        <CampoConError
          etiqueta="Descripción"
          htmlFor="categoria-descripcion"
          error={errores.descripcion}
          idError="categoria-descripcion-error"
        >
          <textarea
            aria-describedby={errores.descripcion ? 'categoria-descripcion-error' : undefined}
            aria-invalid={Boolean(errores.descripcion)}
            id="categoria-descripcion"
            maxLength={280}
            onChange={(event) => setDescripcion(event.currentTarget.value)}
            rows={3}
            value={descripcion}
          />
        </CampoConError>
        <div className="categorias-form-actions">
          <p
            aria-live="polite"
            className="clientes-form-message"
            role={mensaje ? 'alert' : 'status'}
          >
            {mensaje}
          </p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar categoría'}
          </button>
        </div>
      </form>
    </section>
  );
}

export function CategoriasProductoPage() {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [incluirInactivas, setIncluirInactivas] = useState(false);
  const [pagina, setPagina] = useState(1);
  const [categorias, setCategorias] = useState<CategoriaProducto[] | null>(null);
  const [resumen, setResumen] = useState<ResumenCategoriasProducto | null>(null);
  const [cargando, setCargando] = useState(true);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [seleccionada, setSeleccionada] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<CategoriaProducto | null>(null);
  const [cargandoDetalle, setCargandoDetalle] = useState(false);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [modoFormulario, setModoFormulario] = useState<'cerrado' | 'crear' | 'editar'>('cerrado');
  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const [eliminando, setEliminando] = useState(false);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [actualizacion, setActualizacion] = useState(0);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      setBusquedaAplicada(busqueda);
      setPagina(1);
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  useEffect(() => {
    let activo = true;
    setCargando(true);
    setErrorLista('');

    listarCategoriasProducto({
      buscar: busquedaAplicada.trim(),
      incluirInactivas,
    })
      .then((resultado) => {
        if (!activo) {
          return;
        }
        setCategorias(resultado);
        setSeleccionada((actual) => {
          if (resultado.length === 0) {
            return null;
          }
          if (actual !== null && resultado.some((item) => item.id === actual)) {
            return actual;
          }
          return resultado[0]?.id ?? null;
        });
      })
      .catch((error: unknown) => {
        if (!activo) {
          return;
        }
        setCategorias([]);
        setErrorLista(mensajeError(error, 'No se pudieron cargar las categorías de producto.'));
      })
      .finally(() => {
        if (activo) {
          setCargando(false);
        }
      });

    return () => {
      activo = false;
    };
  }, [busquedaAplicada, incluirInactivas, actualizacion]);

  useEffect(() => {
    let activo = true;
    setErrorResumen('');

    obtenerResumenCategoriasProducto()
      .then((datos) => {
        if (activo) {
          setResumen(datos);
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de categorías.'));
        }
      });

    return () => {
      activo = false;
    };
  }, [actualizacion]);

  useEffect(() => {
    if (seleccionada === null) {
      setDetalle(null);
      setErrorDetalle('');
      return;
    }

    let activo = true;
    setCargandoDetalle(true);
    setErrorDetalle('');

    obtenerCategoriaProducto(seleccionada)
      .then((datos) => {
        if (activo) {
          setDetalle(datos);
        }
      })
      .catch((error: unknown) => {
        if (activo) {
          setDetalle(null);
          setErrorDetalle(mensajeError(error, 'No se pudo cargar el detalle de la categoría.'));
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

  const cerrarDialogo = useCallback(() => {
    setDialogoAbierto(false);
    setErrorEliminar('');
    window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
  }, []);

  async function confirmarDesactivacion(): Promise<void> {
    if (detalle === null) {
      return;
    }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await desactivarCategoriaProducto(detalle.id);
      cerrarDialogo();
      if (!incluirInactivas) {
        setSeleccionada(null);
        setDetalle(null);
      }
      setActualizacion((valor) => valor + 1);
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo desactivar la categoría.'));
    } finally {
      setEliminando(false);
    }
  }

  async function manejarGuardado(idGuardado: number): Promise<void> {
    setModoFormulario('cerrado');
    setSeleccionada(idGuardado);
    setActualizacion((valor) => valor + 1);
  }

  const totalCategorias = categorias?.length ?? 0;
  const totalPaginas = Math.max(1, Math.ceil(totalCategorias / TAMANO_PAGINA));
  const paginaActual = Math.min(pagina, totalPaginas);

  const elementosPagina = useMemo(() => {
    if (categorias === null) {
      return [];
    }
    const inicio = (paginaActual - 1) * TAMANO_PAGINA;
    return categorias.slice(inicio, inicio + TAMANO_PAGINA);
  }, [categorias, paginaActual]);

  return (
    <PaginaModulo className="categorias-page" modulo="22-categorias-producto">
      <p className="module-intro">
        Administra las familias y categorías del catálogo de productos de TitanGlobal.
      </p>
      <ResumenCategorias datos={resumen} error={errorResumen} />

      {modoFormulario !== 'cerrado' && (
        <FormularioCategoriaProducto
          cancelar={() => setModoFormulario('cerrado')}
          categoria={modoFormulario === 'editar' ? detalle : null}
          guardado={manejarGuardado}
        />
      )}

      <div className="categorias-work-area">
        <section
          aria-labelledby="categorias-list-title"
          className="clientes-list-panel categorias-list-panel"
        >
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">CATÁLOGO</div>
              <h2 id="categorias-list-title">Todas las categorías</h2>
            </div>
            <button
              className="primary-button"
              onClick={() => setModoFormulario('crear')}
              type="button"
            >
              Nueva categoría
            </button>
          </div>

          <div className="categorias-filters">
            <label className="sr-only" htmlFor="categorias-buscar">
              Buscar categoría por nombre
            </label>
            <input
              aria-label="Buscar categoría por nombre"
              id="categorias-buscar"
              onChange={(event) => setBusqueda(event.currentTarget.value)}
              placeholder="Buscar por nombre…"
              type="search"
              value={busqueda}
            />
            <label className="categorias-toggle">
              <input
                checked={incluirInactivas}
                onChange={(event) => {
                  setIncluirInactivas(event.currentTarget.checked);
                  setPagina(1);
                }}
                type="checkbox"
              />
              <span>Incluir inactivas</span>
            </label>
          </div>

          {errorLista !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              <span aria-hidden="true">!</span>
              {errorLista}
            </p>
          )}

          {cargando && <p className="clientes-status">Cargando categorías…</p>}

          {!cargando && errorLista === '' && categorias !== null && categorias.length === 0 && (
            <p className="clientes-status">
              No se encontraron categorías con los filtros actuales.
            </p>
          )}

          {!cargando && elementosPagina.length > 0 && (
            <TablaPaginada
              cambiarPagina={setPagina}
              caption="Listado de categorías de producto"
              className="categorias-table"
              classNameContenedor="categorias-table-scroll"
              classNamePaginacion="clientes-pagination"
              columnas={[
                { id: 'nombre', etiqueta: 'Nombre' },
                { id: 'descripcion', etiqueta: 'Descripción' },
                { id: 'activa', etiqueta: 'Estado' },
              ]}
              elementos={elementosPagina}
              etiquetaRegion="Tabla de categorías de producto"
              obtenerClave={(categoria) => categoria.id}
              pagina={paginaActual}
              renderizarFila={(categoria) => (
                <tr className={seleccionada === categoria.id ? 'is-selected' : ''}>
                  <td>
                    <button
                      aria-current={seleccionada === categoria.id ? 'true' : undefined}
                      className="cliente-row-select"
                      onClick={() => setSeleccionada(categoria.id)}
                      type="button"
                    >
                      {categoria.nombre}
                    </button>
                  </td>
                  <td>{categoria.descripcion ?? 'Sin descripción'}</td>
                  <td>
                    <span
                      className={`categoria-status${categoria.activa ? ' is-active' : ' is-inactive'}`}
                    >
                      {categoria.activa ? 'Activa' : 'Inactiva'}
                    </span>
                  </td>
                </tr>
              )}
              totalPaginas={totalPaginas}
            />
          )}
        </section>

        <PanelDetalle
          ariaLabelledBy="categoria-detail-title"
          className="clientes-detail-panel categorias-detail-panel"
        >
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">FICHA DE CATEGORÍA</div>
              <h2 id="categoria-detail-title">
                {detalle !== null ? detalle.nombre : 'Detalle de categoría'}
              </h2>
            </div>
            {detalle !== null && (
              <div className="categorias-detail-actions">
                <button
                  className="secondary-button"
                  onClick={() => setModoFormulario('editar')}
                  type="button"
                >
                  Editar
                </button>
                {detalle.activa && (
                  <button
                    className="danger-button"
                    onClick={() => setDialogoAbierto(true)}
                    ref={botonEliminarRef}
                    type="button"
                  >
                    Desactivar
                  </button>
                )}
              </div>
            )}
          </div>

          {cargandoDetalle && <p className="clientes-status">Cargando detalle…</p>}

          {errorDetalle !== '' && (
            <p aria-live="polite" className="clientes-error" role="alert">
              <span aria-hidden="true">!</span>
              {errorDetalle}
            </p>
          )}

          {!cargandoDetalle && errorDetalle === '' && detalle === null && (
            <p className="clientes-status">
              Selecciona una categoría de la tabla para ver su detalle.
            </p>
          )}

          {!cargandoDetalle && detalle !== null && (
            <dl className="categorias-detail-grid">
              <div>
                <dt>Código interno</dt>
                <dd>#{detalle.id}</dd>
              </div>
              <div>
                <dt>Estado</dt>
                <dd>
                  <span
                    className={`categoria-status${detalle.activa ? ' is-active' : ' is-inactive'}`}
                  >
                    {detalle.activa ? 'Activa' : 'Inactiva'}
                  </span>
                </dd>
              </div>
              <div className="categorias-detail-full">
                <dt>Nombre</dt>
                <dd>{detalle.nombre}</dd>
              </div>
              <div className="categorias-detail-full">
                <dt>Descripción</dt>
                <dd>{detalle.descripcion ?? 'Sin descripción registrada.'}</dd>
              </div>
            </dl>
          )}
        </PanelDetalle>
      </div>

      {dialogoAbierto && detalle !== null && (
        <DialogoBaja
          cerrar={cerrarDialogo}
          confirmar={() => void confirmarDesactivacion()}
          eliminando={eliminando}
          entidad="categoría"
          error={errorEliminar}
          nombre={detalle.nombre}
        />
      )}
    </PaginaModulo>
  );
}
