import { useEffect, useRef, type ReactNode } from 'react';
export function DialogoRetorno({
  titulo,
  id,
  ocupado,
  error,
  cerrar,
  children,
}: {
  titulo: string;
  id: string;
  ocupado: boolean;
  error: string;
  cerrar: () => void;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const origen = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    ref.current?.querySelector<HTMLElement>('input,textarea,button')?.focus();
    return () => {
      if (origen?.isConnected) origen.focus();
    };
  }, []);
  useEffect(() => {
    if (ocupado) ref.current?.focus();
  }, [ocupado]);
  return (
    <div className="clientes-dialog-backdrop">
      <div
        ref={ref}
        role="dialog"
        tabIndex={-1}
        aria-modal="true"
        aria-labelledby={id}
        className="clientes-dialog logistica-inversa-dialog"
        onKeyDown={(e) => {
          if (e.key === 'Escape' && !ocupado) {
            e.preventDefault();
            cerrar();
          }
          if (e.key === 'Tab') {
            const elementos = ref.current?.querySelectorAll<HTMLElement>(
              'input:not(:disabled),textarea:not(:disabled),button:not(:disabled)',
            );
            const primero = elementos?.[0],
              ultimo = elementos?.[elementos.length - 1];
            if (
              e.shiftKey &&
              (document.activeElement === primero || document.activeElement === ref.current)
            ) {
              e.preventDefault();
              ultimo?.focus();
            } else if (!e.shiftKey && document.activeElement === ultimo) {
              e.preventDefault();
              primero?.focus();
            }
          }
        }}
      >
        <h2 id={id}>{titulo}</h2>
        {error && (
          <p role="alert" className="field-error">
            {error}
          </p>
        )}
        {children}
      </div>
    </div>
  );
}
