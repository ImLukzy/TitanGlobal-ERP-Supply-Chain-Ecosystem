import { TablaPaginada } from '../../components/TablaPaginada';
import { EstadoPastilla, RolesPastillas } from './UsuarioPastillas';
import type { UsuarioListado } from './tipos';

interface Props {
  items: UsuarioListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

export function UsuariosTabla({
  items,
  error,
  cargando,
  pagina,
  totalPaginas,
  seleccionadoId,
  seleccionar,
  cambiarPagina,
}: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando usuarios…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay usuarios que coincidan con los filtros</p>;
  }
  return (
    <TablaPaginada
      caption="Directorio de usuarios"
      etiquetaRegion="Tabla de usuarios"
      columnas={[
        { id: 'usuario', etiqueta: 'Usuario' },
        { id: 'nombre', etiqueta: 'Nombre completo' },
        { id: 'email', etiqueta: 'Email' },
        { id: 'roles', etiqueta: 'Roles' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(usuario) => usuario.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="usuarios-table"
      classNameContenedor="usuarios-table-scroll"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(usuario) => (
        <tr className={usuario.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={usuario.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(usuario.id)}
              type="button"
            >
              {usuario.nombreUsuario}
            </button>
          </td>
          <td>{usuario.nombreCompleto}</td>
          <td>{usuario.email}</td>
          <td><RolesPastillas codigos={usuario.roles} /></td>
          <td><EstadoPastilla activo={usuario.activo} /></td>
        </tr>
      )}
    />
  );
}
