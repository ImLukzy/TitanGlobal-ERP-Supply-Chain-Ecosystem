import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFecha } from '../../lib/formato';
import { EmpleadoEstadoPastilla } from './EmpleadoEstadoPastilla';
import type { EmpleadoListado } from './tipos';

interface Props {
  items: EmpleadoListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

export function EmpleadosTabla({
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
    return <p aria-live="polite" className="clientes-status" role="status">Cargando empleados…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay empleados con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de empleados"
      etiquetaRegion="Tabla de empleados"
      columnas={[
        { id: 'legajo', etiqueta: 'Legajo' },
        { id: 'nombre', etiqueta: 'Apellidos y nombres' },
        { id: 'documento', etiqueta: 'Documento' },
        { id: 'cargo', etiqueta: 'Cargo' },
        { id: 'area', etiqueta: 'Área' },
        { id: 'ingreso', etiqueta: 'Ingreso' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(empleado) => empleado.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="empleados-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(empleado) => (
        <tr className={empleado.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={empleado.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(empleado.id)}
              type="button"
            >
              {empleado.legajo}
            </button>
          </td>
          <td>{`${empleado.apellidos}, ${empleado.nombres}`}</td>
          <td>{`${empleado.tipoDocumento} ${empleado.numeroDocumento}`}</td>
          <td>{empleado.cargo}</td>
          <td>{empleado.area}</td>
          <td>{formatearFecha(empleado.fechaIngreso)}</td>
          <td><EmpleadoEstadoPastilla estado={empleado.estado} /></td>
        </tr>
      )}
    />
  );
}
