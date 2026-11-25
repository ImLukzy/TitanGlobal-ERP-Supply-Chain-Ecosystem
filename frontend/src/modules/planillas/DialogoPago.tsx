import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { hoyUtc, mensajeError, useDialogoModal } from './presentacion';
import { validarFechaPago } from './validacionPlanilla';

interface Props {
  numero: string;
  cerrar: () => void;
  pagar: (fechaPago: string) => Promise<void>;
}

// Diálogo modal propio: registra el pago de una planilla aprobada con su fecha (mensajes 7 y 12).
export function DialogoPago({ numero, cerrar, pagar }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLInputElement>(null);
  const [fecha, setFecha] = useState(hoyUtc);
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
    const nuevoError = validarFechaPago(fecha);
    setError(nuevoError);
    if (nuevoError !== undefined) {
      return;
    }
    setEnviando(true);
    try {
      await pagar(fecha);
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo registrar el pago.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="planilla-pago-dialog-title"
        aria-modal="true"
        className="clientes-dialog planillas-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">PLANILLAS</div>
        <h2 id="planilla-pago-dialog-title">Registrar pago</h2>
        <p>Planilla: <strong>{numero}</strong></p>
        <form className="planillas-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Fecha de pago" htmlFor="planilla-pago-fecha" error={error} idError="planilla-pago-fecha-error">
            <input
              aria-describedby={error ? 'planilla-pago-fecha-error' : undefined}
              aria-invalid={Boolean(error)}
              id="planilla-pago-fecha"
              onChange={(event) => setFecha(event.currentTarget.value)}
              ref={primerCampoRef}
              type="date"
              value={fecha}
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
              {enviando ? 'Registrando…' : 'Registrar pago'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
