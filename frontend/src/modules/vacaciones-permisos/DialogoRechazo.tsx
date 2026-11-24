import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError, useDialogoModal } from './presentacion';
import { LIMITE_MOTIVO, validarMotivoRechazo } from './validacionAusencia';

interface PropsRechazo {
  numero: string;
  cerrar: () => void;
  rechazar: (motivo: string) => Promise<void>;
}

// Diálogo modal propio: el rechazo de una ausencia exige un motivo.
export function DialogoRechazo({ numero, cerrar, rechazar }: PropsRechazo) {
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
      await rechazar(motivo.trim());
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo rechazar la ausencia.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="ausencia-rechazo-dialog-title"
        aria-modal="true"
        className="clientes-dialog vacaciones-permisos-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">AUSENCIAS</div>
        <h2 id="ausencia-rechazo-dialog-title">Rechazar ausencia</h2>
        <p>Ausencia: <strong>{numero}</strong></p>
        <form className="vacaciones-permisos-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Motivo del rechazo" htmlFor="ausencia-rechazo-motivo" error={error} idError="ausencia-rechazo-motivo-error">
            <textarea
              aria-describedby={error ? 'ausencia-rechazo-contador ausencia-rechazo-motivo-error' : 'ausencia-rechazo-contador'}
              aria-invalid={Boolean(error)}
              id="ausencia-rechazo-motivo"
              onChange={(event) => setMotivo(event.currentTarget.value)}
              ref={primerCampoRef}
              rows={3}
              value={motivo}
            />
          </CampoConError>
          <p className="vacaciones-permisos-contador" id="ausencia-rechazo-contador">{`${motivo.length}/${LIMITE_MOTIVO}`}</p>
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
              {enviando ? 'Rechazando…' : 'Rechazar ausencia'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

interface PropsCancelacion {
  numero: string;
  cerrar: () => void;
  cancelar: () => Promise<void>;
}

// Confirmación de cancelación (no borra la ausencia: la deja en estado Cancelada).
export function DialogoCancelacion({ numero, cerrar, cancelar }: PropsCancelacion) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    dialogoRef.current?.querySelector<HTMLElement>('button:not(:disabled)')?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  async function confirmar(): Promise<void> {
    setMensaje('');
    setEnviando(true);
    try {
      await cancelar();
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo cancelar la solicitud.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="ausencia-cancelacion-dialog-title"
        aria-modal="true"
        className="clientes-dialog vacaciones-permisos-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">CONFIRMAR CANCELACIÓN</div>
        <h2 id="ausencia-cancelacion-dialog-title">Cancelar solicitud</h2>
        <p>¿Seguro que deseas cancelar la solicitud <strong>{numero}</strong>? La ausencia queda registrada como cancelada.</p>
        {mensaje !== '' && (
          <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
            <span aria-hidden="true">!</span>
            {mensaje}
          </p>
        )}
        <div className="clientes-dialog-actions">
          <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">
            Volver
          </button>
          <button className="danger-button" disabled={enviando} onClick={() => void confirmar()} type="button">
            {enviando ? 'Cancelando…' : 'Confirmar cancelación'}
          </button>
        </div>
      </div>
    </div>
  );
}
