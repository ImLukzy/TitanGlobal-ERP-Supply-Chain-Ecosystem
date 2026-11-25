import { useEffect, useRef, useState } from 'react';
import { mensajeError, useDialogoModal } from './presentacion';

interface Props {
  eyebrow: string;
  titulo: string;
  descripcion: string;
  advertencia: string;
  textoConfirmar: string;
  textoEnviando: string;
  mensajeFallo: string;
  cerrar: () => void;
  confirmar: () => Promise<void>;
}

// Diálogo modal propio para confirmar una acción que no se puede deshacer (cancelar una programación).
export function DialogoConfirmacion({
  eyebrow,
  titulo,
  descripcion,
  advertencia,
  textoConfirmar,
  textoEnviando,
  mensajeFallo,
  cerrar,
  confirmar,
}: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const volverRef = useRef<HTMLButtonElement>(null);
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    volverRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  async function aceptar(): Promise<void> {
    setMensaje('');
    setEnviando(true);
    try {
      await confirmar();
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, mensajeFallo));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby="capacitaciones-confirmar-title" aria-modal="true" className="clientes-dialog capacitaciones-dialog" ref={dialogoRef} role="dialog" tabIndex={-1}>
        <div className="page-eyebrow">{eyebrow}</div>
        <h2 id="capacitaciones-confirmar-title">{titulo}</h2>
        <p>{descripcion}</p>
        <p><strong>{advertencia}</strong></p>
        {mensaje !== '' && (
          <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
            <span aria-hidden="true">!</span>
            {mensaje}
          </p>
        )}
        <div className="clientes-dialog-actions">
          <button className="secondary-button" disabled={enviando} onClick={cerrar} ref={volverRef} type="button">Volver</button>
          <button className="primary-button" disabled={enviando} onClick={() => void aceptar()} type="button">
            {enviando ? textoEnviando : textoConfirmar}
          </button>
        </div>
      </div>
    </div>
  );
}
