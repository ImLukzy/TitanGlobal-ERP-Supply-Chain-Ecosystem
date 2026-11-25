import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha } from '../../lib/formato';
import { formateadorPuntaje } from './puntaje';
import type { EvaluacionDetalle } from './tipos';

interface EvaluacionDetallePanelProps {
  evaluacion: EvaluacionDetalle | null;
  cargando: boolean;
  editar: () => void;
  eliminar: (boton: HTMLButtonElement) => void;
}

export function EvaluacionDetallePanel({ evaluacion, cargando, editar, eliminar }: EvaluacionDetallePanelProps) {
  return (
    <PanelDetalle ariaLabelledBy="evaluacion-detail-title" className="evaluaciones-panel evaluaciones-detail">
      {cargando && <p role="status">Cargando evaluación…</p>}
      {!cargando && evaluacion === null && <p role="status">Selecciona una evaluación para ver el detalle.</p>}
      {!cargando && evaluacion !== null && (
        <>
          <div className="evaluaciones-panel-heading">
            <div>
              <span className="page-eyebrow">DETALLE DE EVALUACIÓN</span>
              <h2 id="evaluacion-detail-title">{evaluacion.proveedorNombre} · {evaluacion.periodo}</h2>
            </div>
            <span className={`evaluacion-calificacion is-${evaluacion.calificacion.toLowerCase()}`}>{evaluacion.calificacion}</span>
          </div>
          <dl className="evaluacion-detail-grid">
            <div><dt>Proveedor</dt><dd>{evaluacion.proveedorNombre}</dd></div>
            <div><dt>Periodo</dt><dd>{evaluacion.periodo}</dd></div>
            <div><dt>Cumplimiento</dt><dd>{evaluacion.puntajeCumplimiento}</dd></div>
            <div><dt>Calidad</dt><dd>{evaluacion.puntajeCalidad}</dd></div>
            <div><dt>Plazos</dt><dd>{evaluacion.puntajePlazos}</dd></div>
            <div><dt>Puntaje final</dt><dd>{formateadorPuntaje.format(evaluacion.puntajeFinal)}</dd></div>
            <div><dt>Calificación</dt><dd>{evaluacion.calificacion}</dd></div>
            <div><dt>Fecha de registro</dt><dd>{formatearFecha(evaluacion.fechaRegistro)}</dd></div>
          </dl>
          <section className="evaluacion-detail-note">
            <h3>Observaciones</h3>
            <p>{evaluacion.observaciones === null || evaluacion.observaciones === '' ? 'Sin observaciones' : evaluacion.observaciones}</p>
          </section>
          <div className="evaluacion-actions">
            <button className="secondary-button" onClick={editar} type="button">Editar</button>
            <button className="danger-button" onClick={(event) => eliminar(event.currentTarget)} type="button">Eliminar</button>
          </div>
        </>
      )}
    </PanelDetalle>
  );
}
