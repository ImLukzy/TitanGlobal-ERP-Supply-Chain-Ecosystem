import { useEffect, useRef } from 'react';
import type { Alerta } from './tipos';

interface Props {
  alerta: Alerta;
  error: string;
  procesando: boolean;
  confirmar: () => void;
  cerrar: () => void;
}

export function AlertaConfirmacionDialog({ alerta, error, procesando, confirmar, cerrar }: Props) {
  const cancelarRef = useRef<HTMLButtonElement>(null);
  const verbo = alerta.descartada ? 'restaurar' : 'descartar';
  useEffect(() => {
    cancelarRef.current?.focus();
    function manejarEscape(evento: KeyboardEvent): void {
      if (evento.key === 'Escape' && !procesando) {
        evento.preventDefault();
        cerrar();
      }
    }
    document.addEventListener('keydown', manejarEscape);
    return () => document.removeEventListener('keydown', manejarEscape);
  }, [cerrar, procesando]);

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby="alerta-confirm-title" aria-modal="true" className="clientes-dialog alertas-dialog" role="dialog">
        <div className="page-eyebrow">CONFIRMAR ACCIÓN</div>
        <h2 id="alerta-confirm-title">{alerta.descartada ? 'Restaurar alerta' : 'Descartar alerta'}</h2>
        <p>{alerta.descartada ? '¿Quieres restaurar esta alerta?' : '¿Quieres descartar esta alerta?'}</p>
        <p className="alertas-dialog-message">{alerta.mensaje}</p>
        {error !== '' && <p className="clientes-error clientes-dialog-error" role="alert">{error}</p>}
        <div className="clientes-dialog-actions">
          <button className="secondary-button" disabled={procesando} onClick={cerrar} ref={cancelarRef} type="button">Cancelar</button>
          <button className={alerta.descartada ? 'primary-button' : 'danger-button'} disabled={procesando} onClick={confirmar} type="button">
            {procesando ? 'Procesando…' : `Confirmar ${verbo}`}
          </button>
        </div>
      </div>
    </div>
  );
}
