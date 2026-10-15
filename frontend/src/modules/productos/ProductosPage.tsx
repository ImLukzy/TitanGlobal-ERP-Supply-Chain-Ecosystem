import { useEffect, useRef, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda } from '../../lib/formato';
import { actualizarProducto, crearProducto, eliminarProducto, listarCategoriasProducto, listarProductos, obtenerProducto, obtenerResumenProductos } from './api';
import type { FiltrosProducto, PaginaProductos, ProductoDetalle, ProductoRequest, ResumenProductos } from './tipos';

const resumenVacio: ResumenProductos = {
  totalProductos: 0,
  productosSinDescripcion: 0,
  precioPromedio: 0,
  porCategoria: [],
  masVendidos: [],
};
const filtrosIniciales: FiltrosProducto = {
  busqueda: '',
  categoria: '',
  precioMin: '',
  precioMax: '',
  pagina: 1,
  tamanoPagina: 10,
};
const mensaje = (error: unknown, fallback: string) => (
  error instanceof ApiError ? error.message : fallback
);

interface FormularioProps {
  producto: ProductoDetalle | null;
  categorias: string[];
  cerrar: () => void;
  guardar: (request: ProductoRequest) => Promise<void>;
}

function Formulario({ producto, categorias, cerrar, guardar }: FormularioProps) {
  const [nombre, setNombre] = useState(producto?.nombre ?? '');
  const [categoria, setCategoria] = useState(producto?.categoria ?? '');
  const [precio, setPrecio] = useState(producto ? String(producto.precio) : '');
  const [descripcion, setDescripcion] = useState(producto?.descripcion ?? '');
  const [errores, setErrores] = useState<{ nombre?: string; categoria?: string; precio?: string }>({});
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);
  async function enviar(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nuevosErrores: { nombre?: string; categoria?: string; precio?: string } = {};
    if (!nombre.trim()) {
      nuevosErrores.nombre = 'El nombre del producto es obligatorio.';
    } else if (nombre.trim().length > 200) {
      nuevosErrores.nombre = 'El nombre del producto no puede superar 200 caracteres.';
    }
    if (!categoria.trim()) {
      nuevosErrores.categoria = 'La categoría es obligatoria.';
    } else if (categoria.trim().length > 100) {
      nuevosErrores.categoria = 'La categoría no puede superar 100 caracteres.';
    }
    if (!Number.isFinite(Number(precio)) || Number(precio) <= 0) {
      nuevosErrores.precio = 'El precio debe ser mayor que 0.';
    }
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setErrorApi('');
    setGuardando(true);
    try {
      const request: ProductoRequest = { nombre: nombre.trim(), categoria: categoria.trim(), precio: Number(precio) };
      if (descripcion.trim()) request.descripcion = descripcion.trim();
      await guardar(request);
    } catch (cause: unknown) {
      setErrorApi(mensaje(cause, 'No se pudo guardar el producto.'));
    } finally {
      setGuardando(false);
    }
  }
  return (
    <section className="productos-panel productos-form" aria-labelledby="producto-form-title">
      <h2 id="producto-form-title">{producto ? 'Editar producto' : 'Nuevo producto'}</h2>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError
          etiqueta="Nombre"
          htmlFor="producto-nombre"
          error={errores.nombre}
          idError="producto-nombre-error"
        >
          <input
            id="producto-nombre"
            aria-describedby={errores.nombre ? 'producto-nombre-error' : undefined}
            aria-invalid={Boolean(errores.nombre)}
            maxLength={220}
            value={nombre}
            onChange={(event) => setNombre(event.currentTarget.value)}
          />
        </CampoConError>
        <CampoConError
          etiqueta="Categoría"
          htmlFor="producto-categoria"
          error={errores.categoria}
          idError="producto-categoria-error"
        >
          <input
            id="producto-categoria"
            aria-describedby={errores.categoria ? 'producto-categoria-error' : undefined}
            aria-invalid={Boolean(errores.categoria)}
            list="categorias-producto-form"
            maxLength={120}
            value={categoria}
            onChange={(event) => setCategoria(event.currentTarget.value)}
          />
          <datalist id="categorias-producto-form">
            {categorias.map((item) => (
              <option key={item} value={item} />
            ))}
          </datalist>
        </CampoConError>
        <CampoConError
          etiqueta="Precio"
          htmlFor="producto-precio"
          error={errores.precio}
          idError="producto-precio-error"
        >
          <input
            id="producto-precio"
            aria-describedby={errores.precio ? 'producto-precio-error' : undefined}
            aria-invalid={Boolean(errores.precio)}
            min="0.01"
            step="0.01"
            type="number"
            value={precio}
            onChange={(event) => setPrecio(event.currentTarget.value)}
          />
        </CampoConError>
        <CampoConError
          etiqueta="Descripción"
          htmlFor="producto-descripcion"
          idError="producto-descripcion-error"
        >
          <textarea
            id="producto-descripcion"
            rows={3}
            value={descripcion}
            onChange={(event) => setDescripcion(event.currentTarget.value)}
          />
        </CampoConError>
        {errorApi && <p className="productos-error" role="alert">{errorApi}</p>}
        <div className="productos-actions">
          <button className="secondary-button" onClick={cerrar} type="button">
            Cancelar
          </button>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar producto'}
          </button>
        </div>
      </form>
    </section>
  );
}

export function ProductosPage() {
  const [filtros, setFiltros] = useState(filtrosIniciales);
  const [busqueda, setBusqueda] = useState('');
  const [lista, setLista] = useState<PaginaProductos | null>(null);
  const [detalle, setDetalle] = useState<ProductoDetalle | null>(null);
  const [resumen, setResumen] = useState<ResumenProductos>(resumenVacio);
  const [categorias, setCategorias] = useState<string[]>([]);
  const [error, setError] = useState('');
  const [erroresFiltro, setErroresFiltro] = useState<{ precioMin?: string; precioMax?: string }>({});
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [dialogo, setDialogo] = useState(false);
  const [errorBaja, setErrorBaja] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);
  const cerrarDialogo = () => {
    setDialogo(false);
    setErrorBaja('');
    window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
  };
  useEffect(() => {
    const id = window.setTimeout(() => setFiltros((actual) => ({ ...actual, busqueda, pagina: 1 })), 300);
    return () => window.clearTimeout(id);
  }, [busqueda]);
  useEffect(() => {
    let vivo = true;
    void Promise.all([
      listarProductos(filtros),
      obtenerResumenProductos(),
      listarCategoriasProducto(),
    ]).then(([productos, stats, cats]) => {
      if (!vivo) return;
      setLista(productos);
      setResumen(stats);
      setCategorias(cats);
      setError('');
    }).catch((cause: unknown) => {
      if (vivo) setError(mensaje(cause, 'No se pudieron cargar los productos.'));
    });
    return () => { vivo = false; };
  }, [filtros, actualizacion]);
  async function seleccionar(id: number) {
    try {
      setDetalle(await obtenerProducto(id));
    } catch (cause: unknown) {
      setError(mensaje(cause, 'No se pudo cargar el detalle.'));
    }
  }
  function cambiarFiltro(cambios: Partial<FiltrosProducto>) {
    const siguientes = { ...filtros, ...cambios, pagina: 1 };
    if (siguientes.precioMin && Number(siguientes.precioMin) < 0) {
      setErroresFiltro({ precioMin: 'Los precios del filtro no pueden ser negativos.' });
      return;
    }
    if (siguientes.precioMax && Number(siguientes.precioMax) < 0) {
      setErroresFiltro({ precioMax: 'Los precios del filtro no pueden ser negativos.' });
      return;
    }
    if (siguientes.precioMin && siguientes.precioMax && Number(siguientes.precioMin) > Number(siguientes.precioMax)) {
      setErroresFiltro({ precioMax: 'El precio mínimo no puede ser mayor que el máximo.' });
      return;
    }
    setErroresFiltro({});
    setFiltros(siguientes);
  }
  async function guardar(request: ProductoRequest) {
    if (detalle) {
      await actualizarProducto(detalle.id, request);
    } else {
      await crearProducto(request);
    }
    setMostrarFormulario(false);
    setActualizacion((value) => value + 1);
  }
  async function baja() {
    if (!detalle) return;
    setEliminando(true);
    try {
      await eliminarProducto(detalle.id);
      cerrarDialogo();
      setDetalle(null);
      setActualizacion((value) => value + 1);
    } catch (cause: unknown) {
      setErrorBaja(mensaje(cause, 'No se pudo eliminar el producto.'));
    } finally {
      setEliminando(false);
    }
  }
  const paginas = Math.max(1, Math.ceil((lista?.total ?? 0) / 10));
  return (
    <PaginaModulo modulo="21-productos" className="productos-page">
      <p className="module-intro">Consulta el catálogo, administra precios y revisa el movimiento de cada producto.</p>
      <section aria-label="Resumen de productos" className="productos-summary">
        <article>
          <span>Total productos</span>
          <strong>{resumen.totalProductos}</strong>
        </article>
        <article>
          <span>Sin descripción</span>
          <strong>{resumen.productosSinDescripcion}</strong>
        </article>
        <article>
          <span>Precio promedio</span>
          <strong>{formateadorMoneda.format(resumen.precioPromedio)}</strong>
        </article>
        <article>
          <span>Categorías</span>
          <strong>{resumen.porCategoria.length}</strong>
        </article>
      </section>
      <section className="productos-summary-tables" aria-label="Análisis de productos">
        <div className="productos-panel">
          <h2>Productos por categoría</h2>
          <table className="productos-analysis-table">
            <caption>Resumen de precio y cantidad por categoría</caption>
            <thead>
              <tr>
                <th scope="col">Categoría</th>
                <th scope="col">Productos</th>
                <th scope="col">Precio promedio</th>
                <th scope="col">Precio máximo</th>
              </tr>
            </thead>
            <tbody>
              {resumen.porCategoria.map((item) => (
                <tr key={item.categoria}>
                  <th scope="row">{item.categoria}</th>
                  <td>{item.totalProductos}</td>
                  <td>{formateadorMoneda.format(item.precioPromedio)}</td>
                  <td>{formateadorMoneda.format(item.precioMaximo)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="productos-panel">
          <h2>Más vendidos</h2>
          {resumen.masVendidos.length === 0 ? (
            <p role="status">Sin ventas registradas</p>
          ) : (
            <ol className="productos-top-list">
              {resumen.masVendidos.slice(0, 5).map((item) => (
                <li key={item.productoId}>
                  <span>{item.nombre}</span>
                  <span>{item.unidadesVendidas} unidades</span>
                  <strong>{formateadorMoneda.format(item.montoVendido)}</strong>
                </li>
              ))}
            </ol>
          )}
        </div>
      </section>
      <div className="productos-work-area">
        <section className="productos-panel productos-list" aria-labelledby="productos-title">
          <div className="productos-heading">
            <h2 id="productos-title">Catálogo de productos</h2>
            <button
              className="primary-button"
              onClick={() => {
                setDetalle(null);
                setMostrarFormulario(true);
              }}
              type="button"
            >
              Nuevo producto
            </button>
          </div>
          <div className="productos-filters">
            <label>
              Buscar
              <input
                type="search"
                value={busqueda}
                placeholder="Nombre del producto"
                onChange={(event) => setBusqueda(event.currentTarget.value)}
              />
            </label>
            <label>
              Categoría
              <select
                value={filtros.categoria}
                onChange={(event) => cambiarFiltro({ categoria: event.currentTarget.value })}
              >
                <option value="">Todas</option>
                {categorias.map((cat) => <option key={cat}>{cat}</option>)}
              </select>
            </label>
            <label>
              Precio mínimo
              <input
                type="number"
                value={filtros.precioMin}
                onChange={(event) => cambiarFiltro({ precioMin: event.currentTarget.value })}
                aria-label="Precio mínimo"
                aria-describedby={erroresFiltro.precioMin ? 'precio-min-error' : undefined}
                aria-invalid={Boolean(erroresFiltro.precioMin)}
              />
              {erroresFiltro.precioMin && (
                <span className="field-error" id="precio-min-error">{erroresFiltro.precioMin}</span>
              )}
            </label>
            <label>
              Precio máximo
              <input
                type="number"
                value={filtros.precioMax}
                onChange={(event) => cambiarFiltro({ precioMax: event.currentTarget.value })}
                aria-label="Precio máximo"
                aria-describedby={erroresFiltro.precioMax ? 'precio-max-error' : undefined}
                aria-invalid={Boolean(erroresFiltro.precioMax)}
              />
              {erroresFiltro.precioMax && (
                <span className="field-error" id="precio-max-error">{erroresFiltro.precioMax}</span>
              )}
            </label>
          </div>
          {error && <p className="productos-error" role="alert">{error}</p>}
          {!lista && !error && <p role="status">Cargando productos…</p>}
          {lista && lista.items.length === 0 && (
            <p role="status">No hay productos que coincidan con los filtros.</p>
          )}
          {lista && lista.items.length > 0 && (
            <TablaPaginada
              caption="Productos disponibles"
              etiquetaRegion="Tabla de productos"
              columnas={[
                { id: 'nombre', etiqueta: 'Producto' },
                { id: 'categoria', etiqueta: 'Categoría' },
                { id: 'precio', etiqueta: 'Precio' },
                { id: 'descripcion', etiqueta: 'Descripción' },
              ]}
              elementos={lista.items}
              obtenerClave={(item) => item.id}
              pagina={filtros.pagina}
              totalPaginas={paginas}
              cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
              className="productos-table"
              classNameContenedor="productos-table-scroll"
              classNamePaginacion="productos-pagination"
              renderizarFila={(item) => (
                <tr className={detalle?.id === item.id ? 'is-selected' : ''}>
                  <td>
                    <button
                      className="producto-row-select"
                      onClick={() => void seleccionar(item.id)}
                      type="button"
                    >
                      {item.nombre}
                    </button>
                  </td>
                  <td>{item.categoria}</td>
                  <td>{formateadorMoneda.format(item.precio)}</td>
                  <td>{item.tieneDescripcion ? 'Registrada' : 'Sin descripción'}</td>
                </tr>
              )}
            />
          )}
        </section>
        <PanelDetalle ariaLabelledBy="producto-detail-title" className="productos-panel productos-detail">
          {detalle ? (
            <>
              <div className="productos-heading">
                <div>
                  <span className="page-eyebrow">DETALLE DEL PRODUCTO</span>
                  <h2 id="producto-detail-title">{detalle.nombre}</h2>
                </div>
                <span className="product-category">{detalle.categoria}</span>
              </div>
              <dl className="productos-detail-grid">
                <div><dt>Precio</dt><dd>{formateadorMoneda.format(detalle.precio)}</dd></div>
                <div>
                  <dt>Unidades vendidas</dt>
                  <dd>{detalle.unidadesVendidas}</dd>
                </div>
                <div>
                  <dt>Monto vendido</dt>
                  <dd>{formateadorMoneda.format(detalle.montoVendido)}</dd>
                </div>
                <div>
                  <dt>Clientes distintos</dt>
                  <dd>{detalle.clientesDistintos}</dd>
                </div>
                {detalle.unidadesVendidas === 0 && (
                  <div className="productos-no-sales">Sin ventas registradas</div>
                )}
                <div>
                  <dt>Descripción</dt>
                  <dd>{detalle.descripcion || 'Sin descripción registrada'}</dd>
                </div>
              </dl>
              <div className="productos-actions">
                <button
                  className="secondary-button"
                  onClick={() => setMostrarFormulario(true)}
                  type="button"
                >
                  Editar
                </button>
                <button
                  className="danger-button"
                  onClick={() => {
                    setErrorBaja('');
                    setDialogo(true);
                  }}
                  ref={botonEliminarRef}
                  type="button"
                >
                  Eliminar
                </button>
              </div>
            </>
          ) : (
            <p role="status">Selecciona un producto para ver su detalle.</p>
          )}
        </PanelDetalle>
      </div>
      {mostrarFormulario && (
        <Formulario
          categorias={categorias}
          producto={detalle}
          cerrar={() => setMostrarFormulario(false)}
          guardar={guardar}
        />
      )}
      {dialogo && detalle && (
        <DialogoBaja
          entidad="producto"
          nombre={detalle.nombre}
          cerrar={cerrarDialogo}
          confirmar={() => void baja()}
          eliminando={eliminando}
          error={errorBaja}
        />
      )}
    </PaginaModulo>
  );
}
