import { useEffect, useRef, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';

interface DecisionSolicitudDialogProps {
  numero: string;
  tipo: 'aprobar' | 'rechazar';
  cerrar: () => void;
  confirmarAprobacion: () => Promise<void>;
  confirmarRechazo: (motivo: string) => Promise<void>;
}

export function DecisionSolicitudDialog({
  numero,
  tipo,
  cerrar,
  confirmarAprobacion,
  confirmarRechazo,
}: DecisionSolicitudDialogProps) {
  const [motivo, setMotivo] = useState('');
  const [error, setError] = useState('');
  const [procesando, setProcesando] = useState(false);
  const dialogRef = useRef<HTMLDivElement>(null);
  const motivoId = 'motivo-rechazo';
  const errorId = 'motivo-rechazo-error';

  useEffect(() => {
    dialogRef.current?.querySelector<HTMLElement>('button:not(:disabled), textarea')?.focus();
    function manejarTeclado(event: KeyboardEvent): void {
      if (event.key === 'Escape' && !procesando) {
        event.preventDefault();
        cerrar();
      }
      if (event.key === 'Tab' && dialogRef.current !== null) {
        const elementos = Array.from(dialogRef.current.querySelectorAll<HTMLElement>('button:not(:disabled), textarea'));
        const primero = elementos[0];
        const ultimo = elementos[elementos.length - 1];
        if (event.shiftKey && document.activeElement === primero && ultimo !== undefined) {
          event.preventDefault();
          ultimo.focus();
        } else if (!event.shiftKey && document.activeElement === ultimo && primero !== undefined) {
          event.preventDefault();
          primero.focus();
        }
      }
    }
    document.addEventListener('keydown', manejarTeclado);
    return () => document.removeEventListener('keydown', manejarTeclado);
  }, [cerrar, procesando]);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (tipo === 'rechazar' && motivo.trim() === '') {
      setError('El motivo de rechazo es obligatorio.');
      return;
    }
    if (tipo === 'rechazar' && motivo.trim().length > 500) {
      setError('El motivo de rechazo no puede superar 500 caracteres.');
      return;
    }
    setError('');
    setProcesando(true);
    try {
      if (tipo === 'aprobar') {
        await confirmarAprobacion();
      } else {
        await confirmarRechazo(motivo.trim());
      }
    } catch (cause: unknown) {
      setError(cause instanceof ApiError ? cause.message : 'No se pudo actualizar la solicitud.');
    } finally {
      setProcesando(false);
    }
  }

  const aprobar = tipo === 'aprobar';
  const titulo = aprobar ? `Aprobar ${numero}` : `Rechazar ${numero}`;
  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="solicitud-decision-title"
        aria-modal="true"
        className="clientes-dialog solicitud-decision-dialog"
        ref={dialogRef}
        role="dialog"
        tabIndex={-1}
      >
        <span className="page-eyebrow">CONFIRMAR DECISIÓN</span>
        <h2 id="solicitud-decision-title">{titulo}</h2>
        <p>{aprobar ? 'La solicitud quedará aprobada y ya no podrá editarse.' : 'Indica el motivo para registrar el rechazo.'}</p>
        <form noValidate onSubmit={(event) => void enviar(event)}>
          {!aprobar && (
            <label className="solicitud-reject-field" htmlFor={motivoId}>
              Motivo de rechazo
              <textarea
                id={motivoId}
                aria-describedby={error ? errorId : undefined}
                aria-invalid={Boolean(error)}
                maxLength={520}
                rows={4}
                value={motivo}
                onChange={(event) => setMotivo(event.currentTarget.value)}
              />
              {error.startsWith('El motivo de rechazo') && <span className="field-error" id={errorId}>{error}</span>}
            </label>
          )}
          {error && !error.startsWith('El motivo de rechazo') && (
            <p className="solicitudes-error" role="alert">{error}</p>
          )}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" disabled={procesando} onClick={cerrar} type="button">Cancelar</button>
            <button className={aprobar ? 'primary-button' : 'danger-button'} disabled={procesando} type="submit">
              {procesando ? 'Guardando…' : aprobar ? 'Confirmar aprobación' : 'Confirmar rechazo'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
