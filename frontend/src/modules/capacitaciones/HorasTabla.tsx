import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearHoras } from './presentacion';
import type { HorasEmpleado } from './tipos';

interface Props {
  items: HorasEmpleado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

export function HorasTabla({ items, error, cargando, pagina, totalPaginas, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando horas por empleado…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay empleados con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Horas de capacitación por empleado"
      etiquetaRegion="Tabla de horas por empleado"
      columnas={[
        { id: 'legajo', etiqueta: 'Legajo' },
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'cursos', etiqueta: 'Cursos aprobados' },
        { id: 'horas', etiqueta: 'Horas del año' },
      ]}
      elementos={items}
      obtenerClave={(fila) => fila.empleadoId}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="capacitaciones-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(fila) => (
        <tr className={fila.empleadoId === seleccionadoId ? 'is-selected' : ''}>
          <td className="capacitaciones-numero">{fila.empleadoLegajo}</td>
          <td>
            <button
              aria-current={fila.empleadoId === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(fila.empleadoId)}
              type="button"
            >
              {fila.empleadoNombre}
            </button>
          </td>
          <td className="capacitaciones-numero">{fila.cursos}</td>
          <td className="capacitaciones-numero">{formatearHoras(fila.horas)}</td>
        </tr>
      )}
    />
  );
}
