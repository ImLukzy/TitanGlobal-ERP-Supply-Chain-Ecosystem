import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearCantidad } from '../../lib/formato';
import type { PaginaRoles } from './tipos';
import { TAMANO_PAGINA } from './useRolesData';

interface RolesListaProps {
  busqueda: string;
  cambiarBusqueda: (valor: string) => void;
  lista: PaginaRoles | null;
  errorLista: string;
  pagina: number;
  cambiarPagina: (pagina: number) => void;
  seleccionado: number | null;
  seleccionar: (id: number) => void;
  nuevo: () => void;
}

export function RolesLista({
  busqueda,
  cambiarBusqueda,
  lista,
  errorLista,
  pagina,
  cambiarPagina,
  seleccionado,
  seleccionar,
  nuevo,
}: RolesListaProps) {
  const paginas = Math.max(1, Math.ceil((lista?.total ?? 0) / TAMANO_PAGINA));

  return (
    <section aria-labelledby="roles-list-title" className="clientes-list-panel roles-list-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">PERFILES</div>
          <h2 id="roles-list-title">Todos los roles</h2>
        </div>
        <button className="primary-button" onClick={nuevo} type="button">Nuevo rol</button>
      </div>
      <div className="roles-filters">
        <div>
          <label htmlFor="roles-busqueda">Buscar por código o nombre</label>
          <input
            autoComplete="off"
            id="roles-busqueda"
            onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
            placeholder="Escribe para buscar…"
            type="search"
            value={busqueda}
          />
        </div>
      </div>
      {errorLista !== '' && <p aria-live="polite" className="clientes-error" role="alert">{errorLista}</p>}
      {lista === null && errorLista === '' && <p aria-live="polite" className="clientes-status" role="status">Cargando roles…</p>}
      {lista !== null && errorLista === '' && lista.items.length === 0 && (
        <p aria-live="polite" className="clientes-status" role="status">No hay roles que coincidan</p>
      )}
      {lista !== null && errorLista === '' && lista.items.length > 0 && (
        <TablaPaginada
          caption="Listado de roles"
          etiquetaRegion="Tabla de roles"
          columnas={[
            { id: 'codigo', etiqueta: 'Código' },
            { id: 'nombre', etiqueta: 'Nombre' },
            { id: 'tipo', etiqueta: 'Tipo' },
            { id: 'lectura', etiqueta: 'Lectura' },
            { id: 'escritura', etiqueta: 'Escritura' },
          ]}
          elementos={lista.items}
          obtenerClave={(rol) => rol.id}
          pagina={pagina}
          totalPaginas={paginas}
          cambiarPagina={cambiarPagina}
          className="roles-table"
          classNameContenedor="roles-table-scroll"
          classNamePaginacion="clientes-pagination"
          renderizarFila={(rol) => (
            <tr className={seleccionado === rol.id ? 'is-selected' : ''}>
              <td>
                <button
                  aria-current={seleccionado === rol.id ? 'true' : undefined}
                  className="cliente-row-select"
                  onClick={() => seleccionar(rol.id)}
                  type="button"
                >
                  {rol.codigo}
                </button>
              </td>
              <td>{rol.nombre}</td>
              <td>
                <span className={`rol-tipo${rol.esSistema ? ' is-sistema' : ''}`}>
                  {rol.esSistema ? 'De sistema' : 'Personalizado'}
                </span>
              </td>
              <td>{formatearCantidad(rol.modulosConLectura, 'módulo', 'módulos')}</td>
              <td>{formatearCantidad(rol.modulosConEscritura, 'módulo', 'módulos')}</td>
            </tr>
          )}
        />
      )}
    </section>
  );
}
