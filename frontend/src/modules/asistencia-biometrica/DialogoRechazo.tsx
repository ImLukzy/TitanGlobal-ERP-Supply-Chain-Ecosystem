import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError, useDialogoModal } from './presentacion';
import { validarMotivoRechazo } from './validacionMarcacion';

interface Props {
  descripcion: string;
  cerrar: () => void;
  rechazar: (motivo: string) => Promise<void>;
}

// Diálogo modal propio: el rechazo de horas extra exige un motivo.
export function DialogoRechazo({ descripcion, cerrar, rechazar }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLTextAreaElement>(null);
  const [motivo, setMotivo] = useState('');
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
    const nuevoError = validarMotivoRechazo(motivo);
    setError(nuevoError);
    if (nuevoError !== undefined) {
      return;
    }
    setEnviando(true);
    try {
      await rechazar(motivo);
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo rechazar las horas extra.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="rechazo-dialog-title"
        aria-modal="true"
        className="clientes-dialog asistencia-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">HORAS EXTRA</div>
        <h2 id="rechazo-dialog-title">Rechazar horas extra</h2>
        <p>Jornada: <strong>{descripcion}</strong></p>
        <form className="asistencia-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Motivo del rechazo" htmlFor="rechazo-motivo" error={error} idError="rechazo-motivo-error">
            <textarea
              aria-describedby={error ? 'rechazo-motivo-error' : undefined}
              aria-invalid={Boolean(error)}
              id="rechazo-motivo"
              onChange={(event) => setMotivo(event.currentTarget.value)}
              ref={primerCampoRef}
              rows={3}
              value={motivo}
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
            <button className="danger-button" disabled={enviando} type="submit">
              {enviando ? 'Rechazando…' : 'Rechazar horas extra'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
