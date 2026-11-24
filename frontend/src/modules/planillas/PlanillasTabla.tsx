import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda } from '../../lib/formato';
import { textoPeriodo } from './presentacion';
import type { EstadoPlanilla, PlanillaListado } from './tipos';

interface Props {
  items: PlanillaListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

// El estado se muestra con texto, nunca solo con color.
export function EstadoPastilla({ estado }: { estado: EstadoPlanilla }) {
  return <span className={`planillas-estado is-${estado.toLowerCase()}`}>{estado}</span>;
}

export function PlanillasTabla({
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
    return <p aria-live="polite" className="clientes-status" role="status">Cargando planillas…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay planillas con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de planillas"
      etiquetaRegion="Tabla de planillas"
      columnas={[
        { id: 'numero', etiqueta: 'Número' },
        { id: 'periodo', etiqueta: 'Periodo' },
        { id: 'tipo', etiqueta: 'Tipo' },
        { id: 'estado', etiqueta: 'Estado' },
        { id: 'empleados', etiqueta: 'Empleados' },
        { id: 'neto', etiqueta: 'Neto' },
      ]}
      elementos={items}
      obtenerClave={(planilla) => planilla.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="planillas-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(planilla) => (
        <tr className={planilla.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={planilla.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(planilla.id)}
              type="button"
            >
              {planilla.numero}
            </button>
          </td>
          <td className="planillas-numero">{textoPeriodo(planilla.mes, planilla.anio)}</td>
          <td>{planilla.tipo}</td>
          <td><EstadoPastilla estado={planilla.estado} /></td>
          <td className="planillas-numero">{planilla.empleados}</td>
          <td className="planillas-numero">{formateadorMoneda.format(planilla.totalNeto)}</td>
        </tr>
      )}
    />
  );
}
