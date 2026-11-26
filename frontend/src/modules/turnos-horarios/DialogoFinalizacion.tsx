import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError, useDialogoModal } from './presentacion';
import type { FinalizarAsignacionTurnoRequest } from './tipos';
import { validarFinalizacion } from './validacionTurno';

interface Props {
  descripcion: string;
  fechaInicio: string;
  cerrar: () => void;
  finalizar: (request: FinalizarAsignacionTurnoRequest) => Promise<void>;
}

// Diálogo modal propio: fija o adelanta la fecha de fin; lo ya transcurrido no se borra.
export function DialogoFinalizacion({ descripcion, fechaInicio, cerrar, finalizar }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLInputElement>(null);
  const [fechaFin, setFechaFin] = useState('');
  const [error, setError] = useState<string | undefined>(undefined);
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    primerCampoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    setMensaje('');
    const nuevoError = validarFinalizacion(fechaFin, fechaInicio);
    setError(nuevoError);
    if (nuevoError !== undefined) {
      return;
    }
    setEnviando(true);
    try {
      await finalizar({ fechaFin });
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo fijar la fecha de fin.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="asignacion-fin-dialog-title"
        aria-modal="true"
        className="clientes-dialog turnos-horarios-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">FINALIZACIÓN</div>
        <h2 id="asignacion-fin-dialog-title">Fijar fin de la asignación</h2>
        <p>Asignación: <strong>{descripcion}</strong></p>
        <form className="turnos-horarios-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Fecha de fin" htmlFor="asignacion-fin-fecha" error={error} idError="asignacion-fin-fecha-error">
            <input
              aria-describedby={error ? 'asignacion-fin-fecha-error' : undefined}
              aria-invalid={Boolean(error)}
              id="asignacion-fin-fecha"
              onChange={(event) => setFechaFin(event.currentTarget.value)}
              ref={primerCampoRef}
              type="date"
              value={fechaFin}
            />
          </CampoConError>
          {mensaje !== '' && (
            <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
              <span aria-hidden="true">!</span>
              {mensaje}
            </p>
          )}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">
              Cancelar
            </button>
            <button className="primary-button" disabled={enviando} type="submit">
              {enviando ? 'Guardando…' : 'Guardar fecha de fin'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
