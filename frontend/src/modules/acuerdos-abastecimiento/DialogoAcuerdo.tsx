import { useEffect, useRef, type ReactNode } from 'react';

interface DialogoAcuerdoProps {
  titulo: string;
  tituloId: string;
  cerrar: () => void;
  children: ReactNode;
}

const enfocables = 'button:not(:disabled), input:not(:disabled), textarea:not(:disabled)';

/** Diálogo modal genérico del módulo: Escape cierra y Tab queda atrapado dentro. */
export function DialogoAcuerdo({ titulo, tituloId, cerrar, children }: DialogoAcuerdoProps) {
  const dialogoRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const dialogo = dialogoRef.current;
    dialogo?.querySelector<HTMLElement>(enfocables)?.focus();
    function manejarTeclado(event: globalThis.KeyboardEvent): void {
      if (event.key === 'Escape') {
        event.preventDefault();
        cerrar();
        return;
      }
      if (event.key === 'Tab' && dialogo !== null) {
        const elementos = Array.from(dialogo.querySelectorAll<HTMLElement>(enfocables));
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
  }, [cerrar]);

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby={tituloId} aria-modal="true" className="clientes-dialog" ref={dialogoRef} role="dialog" tabIndex={-1}>
        <h2 id={tituloId}>{titulo}</h2>
        {children}
      </div>
    </div>
  );
}
