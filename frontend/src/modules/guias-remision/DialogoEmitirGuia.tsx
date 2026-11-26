import { useEffect, useRef } from 'react';
export function DialogoEmitirGuia({
  ocupado,
  error,
  cerrar,
  confirmar,
}: {
  ocupado: boolean;
  error: string;
  cerrar: () => void;
  confirmar: () => void;
}) {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const origen = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    ref.current?.querySelector('button')?.focus();
    return () => {
      if (origen?.isConnected) origen.focus();
    };
  }, []);
  return (
    <div className="clientes-dialog-backdrop">
      <div
        ref={ref}
        role="dialog"
        aria-modal="true"
        aria-labelledby="emitir-guia-title"
        className="clientes-dialog guias-remision-dialog"
        onKeyDown={(e) => {
          if (e.key === 'Escape' && !ocupado) {
            e.preventDefault();
            cerrar();
          }
          if (e.key === 'Tab') {
            const botones =
              ref.current?.querySelectorAll<HTMLButtonElement>('button:not(:disabled)');
            const primero = botones?.[0];
            const ultimo = botones?.[botones.length - 1];
            if (e.shiftKey && document.activeElement === primero) {
              e.preventDefault();
              ultimo?.focus();
            } else if (!e.shiftKey && document.activeElement === ultimo) {
              e.preventDefault();
              primero?.focus();
            }
          }
        }}
      >
        <h2 id="emitir-guia-title">Emitir guía internamente</h2>
        <p>
          El documento quedará de solo lectura. Esta acción no envía información a una autoridad
          tributaria.
        </p>
        {error && (
          <p role="alert" className="field-error">
            {error}
          </p>
        )}
        <div className="guias-remision-actions">
          <button type="button" className="secondary-button" disabled={ocupado} onClick={cerrar}>
            Volver
          </button>
          <button type="button" className="primary-button" disabled={ocupado} onClick={confirmar}>
            Confirmar emisión interna
          </button>
        </div>
      </div>
    </div>
  );
}
