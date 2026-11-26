import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { hoyIso, mensajeError, useDialogoModal } from './presentacion';
import type { AccionCorrectiva } from './tipos';
import { fechaValida, MENSAJE_FECHA } from './validacionSst';

interface Props {
  accion: AccionCorrectiva;
  cerrar: () => void;
  completar: (fechaCompletada: string) => Promise<void>;
}

// Diálogo modal propio: la fecha de completado no puede ser posterior a hoy (mensaje 11 de la spec backend).
export function DialogoCompletarAccion({ accion, cerrar, completar }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const campoRef = useRef<HTMLInputElement>(null);
  const hoy = hoyIso();
  const [fecha, setFecha] = useState(hoy);
  const [error, setError] = useState('');
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    campoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    setMensaje('');
    if (!fechaValida(fecha, hoy)) {
      setError(MENSAJE_FECHA);
      return;
    }
    setError('');
    setEnviando(true);
    try {
      await completar(fecha);
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo completar la acción correctiva.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby="completar-dialog-title" aria-modal="true" className="clientes-dialog sst-dialog" ref={dialogoRef} role="dialog" tabIndex={-1}>
        <div className="page-eyebrow">ACCIÓN CORRECTIVA</div>
        <h2 id="completar-dialog-title">Completar acción</h2>
        <p>{accion.descripcion}</p>
        <form className="sst-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Fecha de completado" htmlFor="completar-fecha" error={error} idError="completar-fecha-error">
            <input aria-describedby={error === '' ? undefined : 'completar-fecha-error'} aria-invalid={error !== ''} id="completar-fecha" max={hoy} onChange={(event) => setFecha(event.currentTarget.value)} ref={campoRef} type="date" value={fecha} />
          </CampoConError>
          {mensaje !== '' && (
            <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
              <span aria-hidden="true">!</span>
              {mensaje}
            </p>
          )}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">Cancelar</button>
            <button className="primary-button" disabled={enviando} type="submit">{enviando ? 'Guardando…' : 'Completar acción'}</button>
          </div>
        </form>
      </div>
    </div>
  );
}
