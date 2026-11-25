import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearHoras } from './presentacion';
import type { CursoListado } from './tipos';

interface Props {
  items: CursoListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

// El estado se muestra con texto, nunca solo con color.
export function ActivoPastilla({ activo }: { activo: boolean }) {
  return <span className={`capacitaciones-estado ${activo ? 'is-activo' : 'is-inactivo'}`}>{activo ? 'Activo' : 'Inactivo'}</span>;
}

export function CursosTabla({ items, error, cargando, pagina, totalPaginas, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando cursos…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay cursos con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de cursos"
      etiquetaRegion="Tabla de cursos"
      columnas={[
        { id: 'codigo', etiqueta: 'Código' },
        { id: 'nombre', etiqueta: 'Nombre' },
        { id: 'horas', etiqueta: 'Horas' },
        { id: 'tipo', etiqueta: 'Tipo' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(curso) => curso.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="capacitaciones-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(curso) => (
        <tr className={curso.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={curso.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(curso.id)}
              type="button"
            >
              {curso.codigo}
            </button>
          </td>
          <td>{curso.nombre}</td>
          <td className="capacitaciones-numero">{formatearHoras(curso.horas)}</td>
          <td>{curso.tipo}</td>
          <td><ActivoPastilla activo={curso.activo} /></td>
        </tr>
      )}
    />
  );
}
