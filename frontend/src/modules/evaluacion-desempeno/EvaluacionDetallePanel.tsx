import type { ReactNode, RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { EstadoEvaluacionPastilla, textoPuntaje } from './EvaluacionesTabla';
import { TEXTO_SOLO_RRHH, textoFecha } from './presentacion';
import type { EvaluacionDetalle } from './tipos';
import { formatearDecimal } from './validacionEvaluacion';

interface Props {
  evaluacion: EvaluacionDetalle | null;
  cargando: boolean;
  error: string;
  puedeEscribir: boolean;
  cerrar: () => void;
  tituloRef: RefObject<HTMLHeadingElement | null>;
  children: ReactNode;
}

function CalificacionesLectura({ evaluacion }: { evaluacion: EvaluacionDetalle }) {
  return (
    <section aria-labelledby="evaluacion-lectura-title" className="evaluacion-desempeno-section">
      <h3 id="evaluacion-lectura-title">Calificaciones</h3>
      <div className="evaluacion-desempeno-scroll">
        <table className="evaluacion-desempeno-mini-tabla">
          <caption>Calificaciones por competencia</caption>
          <thead>
            <tr>
              <th scope="col">Competencia</th>
              <th scope="col">Peso</th>
              <th scope="col">Calificación</th>
            </tr>
          </thead>
          <tbody>
            {evaluacion.calificaciones.map((item) => (
              <tr key={item.competenciaId}>
                <td>{item.competencia}</td>
                <td className="evaluacion-desempeno-numero">{`${formatearDecimal(item.peso)} %`}</td>
                <td className="evaluacion-desempeno-numero">{item.calificacion === null ? 'Sin calificar' : item.calificacion}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="evaluacion-desempeno-comentario-lectura">{evaluacion.comentario === null || evaluacion.comentario === '' ? 'Sin comentario.' : evaluacion.comentario}</p>
    </section>
  );
}

export function EvaluacionDetallePanel({ evaluacion, cargando, error, puedeEscribir, cerrar, tituloRef, children }: Props) {
  const describedBy = puedeEscribir ? undefined : 'evaluacion-solo-rrhh';
  return (
    <PanelDetalle ariaLabelledBy="evaluacion-detail-title" className="clientes-detail-panel evaluacion-desempeno-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando evaluación…</p>}
      {error === '' && !cargando && evaluacion === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona una evaluación para ver su detalle.</p>
      )}
      {evaluacion !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{`${evaluacion.cicloCodigo} · ${evaluacion.empleadoLegajo}`}</div>
              <h2 id="evaluacion-detail-title" ref={tituloRef} tabIndex={-1}>{evaluacion.empleadoNombre}</h2>
            </div>
            <EstadoEvaluacionPastilla estado={evaluacion.estado} />
          </div>
          <dl className="evaluacion-desempeno-datos">
            <div><dt>Evaluador</dt><dd>{evaluacion.evaluadorNombre}</dd></div>
            <div><dt>Puntaje final</dt><dd>{textoPuntaje(evaluacion.puntajeFinal, evaluacion.etiqueta)}</dd></div>
            <div><dt>Enviada</dt><dd>{textoFecha(evaluacion.fechaEnvio)}</dd></div>
            <div><dt>Cerrada</dt><dd>{textoFecha(evaluacion.fechaCierre)}</dd></div>
          </dl>
          {evaluacion.estado === 'Enviada' && (
            <div className="clientes-detail-actions evaluacion-desempeno-actions">
              <button aria-describedby={describedBy} className="primary-button" disabled={!puedeEscribir} onClick={cerrar} type="button">
                Cerrar evaluación
              </button>
            </div>
          )}
          {!puedeEscribir && <p className="evaluacion-desempeno-aviso" id="evaluacion-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          {evaluacion.estado === 'Borrador' ? children : <CalificacionesLectura evaluacion={evaluacion} />}
        </>
      )}
    </PanelDetalle>
  );
}
