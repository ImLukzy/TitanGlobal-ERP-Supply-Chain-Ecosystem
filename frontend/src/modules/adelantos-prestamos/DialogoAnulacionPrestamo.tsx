import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError, useDialogoModal } from './presentacion';
import { LIMITE_MOTIVO, validarMotivoAnulacion } from './validacionPrestamo';

interface Props {
  numero: string;
  cerrar: () => void;
  anular: (motivo: string) => Promise<void>;
}

// Diálogo modal propio: anular un préstamo exige un motivo (no lo borra: lo deja en estado Anulado).
export function DialogoAnulacionPrestamo({ numero, cerrar, anular }: Props) {
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
    const nuevoError = validarMotivoAnulacion(motivo);
    setError(nuevoError);
    if (nuevoError !== undefined) {
      return;
    }
    setEnviando(true);
    try {
      await anular(motivo.trim());
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo anular el préstamo.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="prestamo-anulacion-dialog-title"
        aria-modal="true"
        className="clientes-dialog adelantos-prestamos-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">PRÉSTAMOS</div>
        <h2 id="prestamo-anulacion-dialog-title">Anular préstamo</h2>
        <p>Préstamo: <strong>{numero}</strong></p>
        <form className="adelantos-prestamos-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Motivo de la anulación" htmlFor="prestamo-anulacion-motivo" error={error} idError="prestamo-anulacion-motivo-error">
            <textarea
              aria-describedby={error ? 'prestamo-anulacion-contador prestamo-anulacion-motivo-error' : 'prestamo-anulacion-contador'}
              aria-invalid={Boolean(error)}
              id="prestamo-anulacion-motivo"
              onChange={(event) => setMotivo(event.currentTarget.value)}
              ref={primerCampoRef}
              rows={3}
              value={motivo}
            />
          </CampoConError>
          <p className="adelantos-prestamos-contador" id="prestamo-anulacion-contador">{`${motivo.length}/${LIMITE_MOTIVO}`}</p>
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
            <button className="danger-button" disabled={enviando} type="submit">
              {enviando ? 'Anulando…' : 'Anular préstamo'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
