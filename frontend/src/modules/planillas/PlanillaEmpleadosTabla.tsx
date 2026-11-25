import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda } from '../../lib/formato';
import type { PlanillaEmpleado } from './tipos';

interface Props {
  items: PlanillaEmpleado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  busqueda: string;
  soloIncidencias: boolean;
  cambiarBusqueda: (valor: string) => void;
  cambiarSoloIncidencias: (valor: boolean) => void;
  cambiarPagina: (pagina: number) => void;
  verBoleta: (empleado: PlanillaEmpleado) => void;
}

function Contenido({ items, error, cargando, pagina, totalPaginas, cambiarPagina, verBoleta }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando empleados de la planilla…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay empleados con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Detalle por empleado"
      etiquetaRegion="Tabla de empleados de la planilla"
      columnas={[
        { id: 'legajo', etiqueta: 'Legajo' },
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'dias', etiqueta: 'Días' },
        { id: 'ingresos', etiqueta: 'Ingresos' },
        { id: 'pension', etiqueta: 'Pensión' },
        { id: 'quinta', etiqueta: 'Quinta' },
        { id: 'prestamos', etiqueta: 'Préstamos' },
        { id: 'neto', etiqueta: 'Neto' },
        { id: 'incidencia', etiqueta: 'Incidencia' },
        { id: 'boleta', etiqueta: 'Boleta' },
      ]}
      elementos={items}
      obtenerClave={(empleado) => empleado.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="planillas-table planillas-table-empleados"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(empleado) => (
        <tr>
          <td className="planillas-numero">{empleado.empleadoLegajo}</td>
          <td>{empleado.empleadoNombre}</td>
          <td className="planillas-numero">{empleado.diasLaborados}</td>
          <td className="planillas-numero">{formateadorMoneda.format(empleado.totalIngresos)}</td>
          <td className="planillas-numero">{formateadorMoneda.format(empleado.descuentoPension)}</td>
          <td className="planillas-numero">{formateadorMoneda.format(empleado.descuentoQuinta)}</td>
          <td className="planillas-numero">{formateadorMoneda.format(empleado.descuentoPrestamos)}</td>
          <td className="planillas-numero">{formateadorMoneda.format(empleado.neto)}</td>
          <td>{empleado.incidencia === null ? '' : <span className="planillas-incidencia">{empleado.incidencia}</span>}</td>
          <td>
            {empleado.numeroBoleta !== null && (
              <button
                aria-label={`Ver boleta de ${empleado.empleadoNombre}`}
                className="secondary-button planillas-fila-boton"
                onClick={() => verBoleta(empleado)}
                type="button"
              >
                Ver boleta
              </button>
            )}
          </td>
        </tr>
      )}
    />
  );
}

export function PlanillaEmpleadosTabla(props: Props) {
  return (
    <section aria-labelledby="planilla-empleados-title" className="planillas-detail-section">
      <h3 id="planilla-empleados-title">Empleados de la planilla</h3>
      <div className="solicitudes-filters planillas-empleados-filters">
        <div>
          <label htmlFor="planilla-empleados-busqueda">Buscar por legajo o empleado</label>
          <input
            autoComplete="off"
            id="planilla-empleados-busqueda"
            onChange={(event) => props.cambiarBusqueda(event.currentTarget.value)}
            placeholder="Escribe para buscar…"
            type="search"
            value={props.busqueda}
          />
        </div>
        <label className="planillas-check">
          <input
            checked={props.soloIncidencias}
            onChange={(event) => props.cambiarSoloIncidencias(event.currentTarget.checked)}
            type="checkbox"
          />
          Solo incidencias
        </label>
      </div>
      <Contenido {...props} />
    </section>
  );
}
