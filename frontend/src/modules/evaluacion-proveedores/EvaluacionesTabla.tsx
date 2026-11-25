import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorPuntaje } from './puntaje';
import type { EvaluacionListado, PaginaEvaluaciones } from './tipos';

interface EvaluacionesTablaProps {
  lista: PaginaEvaluaciones | null;
  error: string;
  seleccionadaId: number | null;
  seleccionar: (evaluacion: EvaluacionListado) => void;
  pagina: number;
  cambiarPagina: (pagina: number) => void;
}

const columnas = [
  { id: 'periodo', etiqueta: 'Periodo' },
  { id: 'proveedor', etiqueta: 'Proveedor' },
  { id: 'puntaje', etiqueta: 'Puntaje final' },
  { id: 'calificacion', etiqueta: 'Calificación' },
];

export function EvaluacionesTabla({ lista, error, seleccionadaId, seleccionar, pagina, cambiarPagina }: EvaluacionesTablaProps) {
  if (error !== '') {
    return <p className="evaluaciones-error" role="alert">{error}</p>;
  }
  if (lista === null) {
    return <p className="evaluaciones-loading" role="status">Cargando evaluaciones…</p>;
  }
  if (lista.items.length === 0) {
    return <p className="evaluaciones-empty" role="status">No hay evaluaciones para mostrar.</p>;
  }
  return (
    <TablaPaginada
      caption="Evaluaciones de proveedores"
      className="evaluaciones-table"
      classNameContenedor="evaluaciones-table-scroll"
      classNamePaginacion="evaluaciones-pagination"
      columnas={columnas}
      elementos={lista.items}
      etiquetaRegion="Listado de evaluaciones de proveedores"
      obtenerClave={(evaluacion) => evaluacion.id}
      renderizarFila={(evaluacion) => (
        <tr aria-selected={seleccionadaId === evaluacion.id} className={seleccionadaId === evaluacion.id ? 'is-selected' : ''} key={evaluacion.id}>
          <th scope="row">
            <button
              aria-label={`Ver evaluación de ${evaluacion.proveedorNombre} del periodo ${evaluacion.periodo}`}
              className="evaluacion-row-select"
              onClick={() => seleccionar(evaluacion)}
              type="button"
            >
              {evaluacion.periodo}
            </button>
          </th>
          <td>{evaluacion.proveedorNombre}</td>
          <td>{formateadorPuntaje.format(evaluacion.puntajeFinal)}</td>
          <td><span className={`evaluacion-calificacion is-${evaluacion.calificacion.toLowerCase()}`}>{evaluacion.calificacion}</span></td>
        </tr>
      )}
      cambiarPagina={cambiarPagina}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(lista.total / lista.tamanoPagina))}
    />
  );
}
