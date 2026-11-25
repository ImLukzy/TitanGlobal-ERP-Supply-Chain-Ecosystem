import { useState } from 'react';
import { ETIQUETAS_CALIFICACION, mensajeError } from './presentacion';
import type { EvaluacionDetalle, GuardarCalificacionesRequest } from './tipos';
import {
  calcularPuntajeCentesimas,
  construirRequestCalificaciones,
  etiquetaPuntaje,
  formatearCentesimas,
  formatearDecimal,
  LIMITE_COMENTARIO,
  validarComentario,
} from './validacionEvaluacion';

interface Props {
  evaluacion: EvaluacionDetalle;
  puedeEscribir: boolean;
  guardar: (request: GuardarCalificacionesRequest) => Promise<void>;
  enviar: (request: GuardarCalificacionesRequest) => Promise<void>;
}

function calificacionesIniciales(evaluacion: EvaluacionDetalle): Map<number, number> {
  const mapa = new Map<number, number>();
  for (const item of evaluacion.calificaciones) {
    if (item.calificacion !== null) {
      mapa.set(item.competenciaId, item.calificacion);
    }
  }
  return mapa;
}

function textoSinPuntaje(total: number, faltan: number): string {
  if (total === 0) {
    return 'El ciclo no tiene competencias que calificar.';
  }
  return `Faltan calificar ${faltan} competencias para estimar el puntaje.`;
}

// Calificación de una evaluación en borrador: cinco opciones con texto por competencia y el puntaje estimado en vivo.
export function CalificacionesEditor({ evaluacion, puedeEscribir, guardar, enviar }: Props) {
  const [calificaciones, setCalificaciones] = useState<Map<number, number>>(() => calificacionesIniciales(evaluacion));
  const [comentario, setComentario] = useState(evaluacion.comentario ?? '');
  const [errorComentario, setErrorComentario] = useState('');
  const [mensaje, setMensaje] = useState('');
  const [guardado, setGuardado] = useState(false);
  const [enCurso, setEnCurso] = useState(false);
  const competencias = evaluacion.calificaciones;
  const faltan = competencias.filter((item) => !calificaciones.has(item.competenciaId)).length;
  const items = competencias.map((item) => ({ peso: item.peso, calificacion: calificaciones.get(item.competenciaId) ?? 0 }));
  const puntaje = faltan === 0 && competencias.length > 0 ? calcularPuntajeCentesimas(items) : null;

  function calificar(competenciaId: number, valor: number): void {
    setGuardado(false);
    setCalificaciones((actual) => new Map(actual).set(competenciaId, valor));
  }

  async function ejecutar(accion: (request: GuardarCalificacionesRequest) => Promise<void>, fallo: string): Promise<void> {
    setMensaje('');
    setGuardado(false);
    const error = validarComentario(comentario);
    setErrorComentario(error);
    if (error !== '') {
      return;
    }
    setEnCurso(true);
    try {
      await accion(construirRequestCalificaciones(calificaciones, comentario));
      setGuardado(true);
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, fallo));
    } finally {
      setEnCurso(false);
    }
  }

  return (
    <section aria-labelledby="evaluacion-calificar-title" className="evaluacion-desempeno-section">
      <h3 id="evaluacion-calificar-title">Calificación</h3>
      {competencias.map((item) => (
        <fieldset className="evaluacion-desempeno-calificacion" disabled={!puedeEscribir || enCurso} key={item.competenciaId}>
          <legend>{`${item.competencia} · peso ${formatearDecimal(item.peso)} %`}</legend>
          <div className="evaluacion-desempeno-opciones">
            {ETIQUETAS_CALIFICACION.map((etiqueta, indice) => {
              const valor = indice + 1;
              const id = `calificacion-${item.competenciaId}-${valor}`;
              return (
                <label className="evaluacion-desempeno-opcion" htmlFor={id} key={valor}>
                  <input
                    checked={calificaciones.get(item.competenciaId) === valor}
                    id={id}
                    name={`calificacion-${item.competenciaId}`}
                    onChange={() => calificar(item.competenciaId, valor)}
                    type="radio"
                    value={valor}
                  />
                  <span>{etiqueta}</span>
                </label>
              );
            })}
          </div>
        </fieldset>
      ))}
      <div className="form-field evaluacion-desempeno-comentario">
        <label htmlFor="evaluacion-comentario">Comentario</label>
        <textarea
          aria-describedby={errorComentario === '' ? undefined : 'evaluacion-comentario-error'}
          aria-invalid={errorComentario !== ''}
          disabled={!puedeEscribir || enCurso}
          id="evaluacion-comentario"
          maxLength={LIMITE_COMENTARIO + 100}
          onChange={(event) => {
            setComentario(event.currentTarget.value);
            setGuardado(false);
          }}
          rows={3}
          value={comentario}
        />
        {errorComentario !== '' && <span className="field-error" id="evaluacion-comentario-error">{errorComentario}</span>}
      </div>
      <p aria-live="polite" className="evaluacion-desempeno-puntaje" role="status">
        {puntaje === null
          ? textoSinPuntaje(competencias.length, faltan)
          : `Puntaje estimado: ${formatearCentesimas(puntaje)} · ${etiquetaPuntaje(puntaje)}`}
      </p>
      <div className="clientes-detail-actions evaluacion-desempeno-actions">
        <button className="secondary-button" disabled={!puedeEscribir || enCurso} onClick={() => void ejecutar(guardar, 'No se pudieron guardar las calificaciones.')} type="button">
          {enCurso ? 'Procesando…' : 'Guardar'}
        </button>
        <button className="primary-button" disabled={!puedeEscribir || enCurso} onClick={() => void ejecutar(enviar, 'No se pudo enviar la evaluación.')} type="button">
          Enviar
        </button>
      </div>
      {guardado && <p aria-live="polite" className="evaluacion-desempeno-ok" role="status">Cambios guardados.</p>}
      {mensaje !== '' && <p aria-live="polite" className="clientes-error" role="alert">{mensaje}</p>}
    </section>
  );
}
