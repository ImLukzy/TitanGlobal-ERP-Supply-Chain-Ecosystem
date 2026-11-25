import { TablaPaginada } from '../../components/TablaPaginada';
import type { EstadoEvaluacion, EvaluacionListado } from './tipos';
import { formatearDecimal } from './validacionEvaluacion';

interface Props {
  items: EvaluacionListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

// El estado se muestra con texto, nunca solo con color.
export function EstadoEvaluacionPastilla({ estado }: { estado: EstadoEvaluacion }) {
  return <span className={`evaluacion-desempeno-estado is-${estado.toLowerCase()}`}>{estado}</span>;
}

export function textoPuntaje(puntaje: number | null, etiqueta: string | null): string {
  if (puntaje === null) {
    return 'Sin puntaje';
  }
  return etiqueta === null ? formatearDecimal(puntaje) : `${formatearDecimal(puntaje)} · ${etiqueta}`;
}

export function EvaluacionesTabla({ items, error, cargando, pagina, totalPaginas, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando evaluaciones…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay evaluaciones con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de evaluaciones"
      etiquetaRegion="Tabla de evaluaciones"
      columnas={[
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'evaluador', etiqueta: 'Evaluador' },
        { id: 'ciclo', etiqueta: 'Ciclo' },
        { id: 'estado', etiqueta: 'Estado' },
        { id: 'puntaje', etiqueta: 'Puntaje y etiqueta' },
      ]}
      elementos={items}
      obtenerClave={(evaluacion) => evaluacion.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="evaluacion-desempeno-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(evaluacion) => (
        <tr className={evaluacion.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={evaluacion.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(evaluacion.id)}
              type="button"
            >
              {evaluacion.empleadoNombre}
            </button>
          </td>
          <td>{evaluacion.evaluadorNombre}</td>
          <td>{evaluacion.cicloCodigo}</td>
          <td><EstadoEvaluacionPastilla estado={evaluacion.estado} /></td>
          <td className="evaluacion-desempeno-numero">{textoPuntaje(evaluacion.puntajeFinal, evaluacion.etiqueta)}</td>
        </tr>
      )}
    />
  );
}
