import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError, useDialogoModal } from './presentacion';
import type { AnularContratoLaboralRequest } from './tipos';
import { LIMITE_MOTIVO_ANULACION, validarAnulacion, type ErroresAnulacion } from './validacionContratoLaboral';

interface Props {
  numero: string;
  cerrar: () => void;
  anular: (request: AnularContratoLaboralRequest) => Promise<void>;
}

// Diálogo modal propio: la anulación no borra el contrato, lo deja en estado Anulado.
export function DialogoAnulacion({ numero, cerrar, anular }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLTextAreaElement>(null);
  const [motivo, setMotivo] = useState('');
  const [errores, setErrores] = useState<ErroresAnulacion>({});
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    primerCampoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarAnulacion(motivo);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setEnviando(true);
    try {
      await anular({ motivo: motivo.trim() });
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo anular el contrato.'));
    } finally {
      setEnviando(false);
    }
  }

  const descripcionMotivo = errores.motivo
    ? 'anulacion-motivo-contador anulacion-motivo-error'
    : 'anulacion-motivo-contador';

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="contrato-anulacion-dialog-title"
        aria-modal="true"
        className="clientes-dialog contratos-laborales-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">ANULACIÓN</div>
        <h2 id="contrato-anulacion-dialog-title">Anular contrato</h2>
        <p>Contrato: <strong>{numero}</strong></p>
        <p className="contratos-laborales-aviso">La anulación no se puede deshacer.</p>
        <form className="contratos-laborales-dialog-form contratos-laborales-dialog-form-simple" noValidate onSubmit={(event) => void enviar(event)}>
          <CampoConError etiqueta="Motivo de anulación" htmlFor="anulacion-motivo" error={errores.motivo} idError="anulacion-motivo-error">
            <textarea
              aria-describedby={descripcionMotivo}
              aria-invalid={Boolean(errores.motivo)}
              id="anulacion-motivo"
              maxLength={LIMITE_MOTIVO_ANULACION}
              onChange={(event) => setMotivo(event.currentTarget.value)}
              ref={primerCampoRef}
              rows={4}
              value={motivo}
            />
          </CampoConError>
          <p className="contratos-laborales-contador" id="anulacion-motivo-contador">{`${motivo.length}/${LIMITE_MOTIVO_ANULACION}`}</p>
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
              {enviando ? 'Anulando…' : 'Confirmar anulación'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
