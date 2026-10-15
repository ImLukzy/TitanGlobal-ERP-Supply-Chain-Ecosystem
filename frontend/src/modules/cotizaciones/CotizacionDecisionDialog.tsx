import { useEffect, useRef, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';

interface Props {
  numero: string;
  tipo: 'aceptar' | 'rechazar';
  cerrar: () => void;
  confirmarAceptacion: () => Promise<void>;
  confirmarRechazo: (motivo: string) => Promise<void>;
}

export function CotizacionDecisionDialog({
  numero,
  tipo,
  cerrar,
  confirmarAceptacion,
  confirmarRechazo,
}: Props) {
  const [motivo, setMotivo] = useState('');
  const [error, setError] = useState('');
  const [procesando, setProcesando] = useState(false);
  const dialogRef = useRef<HTMLDivElement>(null);
  const aceptar = tipo === 'aceptar';

  useEffect(() => {
    dialogRef.current
      ?.querySelector<HTMLElement>('button:not(:disabled), textarea')
      ?.focus();
    function manejarTeclado(event: KeyboardEvent): void {
      if (event.key === 'Escape' && !procesando) {
        event.preventDefault();
        cerrar();
      }
      if (event.key === 'Tab' && dialogRef.current !== null) {
        const elementos = Array.from(
          dialogRef.current.querySelectorAll<HTMLElement>(
            'button:not(:disabled), textarea',
          ),
        );
        const primero = elementos[0];
        const ultimo = elementos[elementos.length - 1];
        if (
          event.shiftKey &&
          document.activeElement === primero &&
          ultimo !== undefined
        ) {
          event.preventDefault();
          ultimo.focus();
        } else if (
          !event.shiftKey &&
          document.activeElement === ultimo &&
          primero !== undefined
        ) {
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
    if (!aceptar && motivo.trim() === '') {
      setError('El motivo de rechazo es obligatorio.');
      return;
    }
    if (!aceptar && motivo.trim().length > 500) {
      setError('El motivo de rechazo no puede superar 500 caracteres.');
      return;
    }
    setProcesando(true);
    setError('');
    try {
      if (aceptar) {
        await confirmarAceptacion();
      } else {
        await confirmarRechazo(motivo.trim());
      }
    } catch (cause: unknown) {
      setError(
        cause instanceof ApiError
          ? cause.message
          : 'No se pudo actualizar la cotización.',
      );
    } finally {
      setProcesando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="cotizacion-decision-title"
        aria-modal="true"
        className="clientes-dialog solicitud-decision-dialog"
        ref={dialogRef}
        role="dialog"
        tabIndex={-1}
      >
        <span className="page-eyebrow">CONFIRMAR DECISIÓN</span>
        <h2 id="cotizacion-decision-title">
          {aceptar ? `Aceptar ${numero}` : `Rechazar ${numero}`}
        </h2>
        <p>
          {aceptar
            ? `Se generará la orden de compra OC-${numero}.`
            : 'Indica el motivo para registrar el rechazo.'}
        </p>
        <form noValidate onSubmit={(event) => void enviar(event)}>
          {!aceptar && (
            <label
              className="solicitud-reject-field"
              htmlFor="cotizacion-motivo"
            >
              Motivo de rechazo
              <textarea
                id="cotizacion-motivo"
                aria-describedby={error ? 'cotizacion-motivo-error' : undefined}
                aria-invalid={Boolean(error)}
                maxLength={520}
                rows={4}
                value={motivo}
                onChange={(event) => setMotivo(event.currentTarget.value)}
              />
              {error && (
                <span
                  className="field-error"
                  id="cotizacion-motivo-error"
                  role="alert"
                >
                  {error}
                </span>
              )}
            </label>
          )}
          {aceptar && error && (
            <p className="solicitudes-error" role="alert">
              {error}
            </p>
          )}
          <div className="clientes-dialog-actions">
            <button
              className="secondary-button"
              disabled={procesando}
              onClick={cerrar}
              type="button"
            >
              Cancelar
            </button>
            <button
              className={aceptar ? 'primary-button' : 'danger-button'}
              disabled={procesando}
              type="submit"
            >
              {procesando
                ? 'Guardando…'
                : aceptar
                  ? 'Confirmar aceptación'
                  : 'Confirmar rechazo'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
