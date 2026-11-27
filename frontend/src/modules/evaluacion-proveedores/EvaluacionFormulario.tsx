import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { calcularPuntajeFinal, calificarPuntaje, formateadorPuntaje } from './puntaje';
import type { CrearEvaluacionRequest, EvaluacionDetalle, ProveedorOpcion } from './tipos';
import { mesActual, puntajeValido, validarEvaluacion, type CampoEvaluacion, type ErroresEvaluacion } from './validacion';

interface EvaluacionFormularioProps {
  evaluacion: EvaluacionDetalle | null;
  proveedores: ProveedorOpcion[];
  cancelar: () => void;
  guardar: (datos: CrearEvaluacionRequest) => Promise<void>;
}

const campos: CampoEvaluacion[] = ['proveedor', 'periodo', 'cumplimiento', 'calidad', 'plazos', 'observaciones'];

function describir(errores: ErroresEvaluacion, campo: CampoEvaluacion): string | undefined {
  return errores[campo] ? `evaluacion-${campo}-error` : undefined;
}

export function EvaluacionFormulario({ evaluacion, proveedores, cancelar, guardar }: EvaluacionFormularioProps) {
  const edicion = evaluacion !== null;
  const [proveedorId, setProveedorId] = useState<number | ''>(evaluacion?.proveedorId ?? '');
  const [periodo, setPeriodo] = useState(evaluacion?.periodo ?? '');
  const [cumplimiento, setCumplimiento] = useState(evaluacion === null ? '' : String(evaluacion.puntajeCumplimiento));
  const [calidad, setCalidad] = useState(evaluacion === null ? '' : String(evaluacion.puntajeCalidad));
  const [plazos, setPlazos] = useState(evaluacion === null ? '' : String(evaluacion.puntajePlazos));
  const [observaciones, setObservaciones] = useState(evaluacion?.observaciones ?? '');
  const [errores, setErrores] = useState<ErroresEvaluacion>({});
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);

  const completos = puntajeValido(cumplimiento) && puntajeValido(calidad) && puntajeValido(plazos);
  const final = completos ? calcularPuntajeFinal(Number(cumplimiento), Number(calidad), Number(plazos)) : null;

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const nuevos = validarEvaluacion({ proveedorId, periodo, cumplimiento, calidad, plazos, observaciones }, edicion);
    setErrores(nuevos);
    if (campos.some((campo) => nuevos[campo] !== undefined)) {
      return;
    }
    const datos: CrearEvaluacionRequest = {
      proveedorId: Number(proveedorId),
      periodo,
      puntajeCumplimiento: Number(cumplimiento),
      puntajeCalidad: Number(calidad),
      puntajePlazos: Number(plazos),
    };
    if (observaciones.trim() !== '') {
      datos.observaciones = observaciones.trim();
    }
    setErrorApi('');
    setGuardando(true);
    try {
      await guardar(datos);
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo guardar la evaluación.');
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="evaluacion-form-title" className="evaluaciones-panel evaluacion-form">
      <div className="evaluaciones-panel-heading">
        <div>
          <span className="page-eyebrow">EVALUACIÓN DE PROVEEDOR</span>
          <h2 id="evaluacion-form-title">{edicion ? `Editar evaluación de ${evaluacion.proveedorNombre}` : 'Nueva evaluación'}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <div className="evaluacion-form-fields">
          {edicion ? (
            <dl className="evaluacion-form-readonly">
              <div><dt>Proveedor</dt><dd>{evaluacion.proveedorNombre}</dd></div>
              <div><dt>Periodo</dt><dd>{evaluacion.periodo}</dd></div>
            </dl>
          ) : (
            <>
              <CampoConError etiqueta="Proveedor" htmlFor="evaluacion-proveedor" error={errores.proveedor} idError="evaluacion-proveedor-error">
                <select
                  id="evaluacion-proveedor"
                  aria-describedby={describir(errores, 'proveedor')}
                  aria-invalid={Boolean(errores.proveedor)}
                  onChange={(event) => setProveedorId(event.currentTarget.value === '' ? '' : Number(event.currentTarget.value))}
                  value={proveedorId}
                >
                  <option value="">Selecciona proveedor</option>
                  {proveedores.map((proveedor) => <option key={proveedor.id} value={proveedor.id}>{proveedor.razonSocial}</option>)}
                </select>
              </CampoConError>
              <CampoConError etiqueta="Periodo" htmlFor="evaluacion-periodo" error={errores.periodo} idError="evaluacion-periodo-error">
                <input
                  id="evaluacion-periodo"
                  aria-describedby={describir(errores, 'periodo')}
                  aria-invalid={Boolean(errores.periodo)}
                  max={mesActual()}
                  onChange={(event) => setPeriodo(event.currentTarget.value)}
                  type="month"
                  value={periodo}
                />
              </CampoConError>
            </>
          )}
          <CampoConError etiqueta="Puntaje de cumplimiento" htmlFor="evaluacion-cumplimiento" error={errores.cumplimiento} idError="evaluacion-cumplimiento-error">
            <input
              id="evaluacion-cumplimiento"
              aria-describedby={describir(errores, 'cumplimiento')}
              aria-invalid={Boolean(errores.cumplimiento)}
              inputMode="numeric"
              onChange={(event) => setCumplimiento(event.currentTarget.value)}
              type="number"
              value={cumplimiento}
            />
          </CampoConError>
          <CampoConError etiqueta="Puntaje de calidad" htmlFor="evaluacion-calidad" error={errores.calidad} idError="evaluacion-calidad-error">
            <input
              id="evaluacion-calidad"
              aria-describedby={describir(errores, 'calidad')}
              aria-invalid={Boolean(errores.calidad)}
              inputMode="numeric"
              onChange={(event) => setCalidad(event.currentTarget.value)}
              type="number"
              value={calidad}
            />
          </CampoConError>
          <CampoConError etiqueta="Puntaje de plazos" htmlFor="evaluacion-plazos" error={errores.plazos} idError="evaluacion-plazos-error">
            <input
              id="evaluacion-plazos"
              aria-describedby={describir(errores, 'plazos')}
              aria-invalid={Boolean(errores.plazos)}
              inputMode="numeric"
              onChange={(event) => setPlazos(event.currentTarget.value)}
              type="number"
              value={plazos}
            />
          </CampoConError>
          <CampoConError etiqueta="Observaciones" htmlFor="evaluacion-observaciones" error={errores.observaciones} idError="evaluacion-observaciones-error">
            <textarea
              id="evaluacion-observaciones"
              aria-describedby={describir(errores, 'observaciones')}
              aria-invalid={Boolean(errores.observaciones)}
              maxLength={520}
              onChange={(event) => setObservaciones(event.currentTarget.value)}
              value={observaciones}
            />
            <span className="evaluacion-contador">{observaciones.length}/500</span>
          </CampoConError>
        </div>
        <p aria-live="polite" className="evaluacion-preview">
          {final === null
            ? 'Vista previa: completa los tres puntajes entre 0 y 100.'
            : `Vista previa: ${formateadorPuntaje.format(final)} · ${calificarPuntaje(final)}`}
        </p>
        {errorApi && <p className="evaluaciones-error" role="alert">{errorApi}</p>}
        <div className="evaluacion-form-actions">
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar evaluación'}</button>
        </div>
      </form>
    </section>
  );
}
