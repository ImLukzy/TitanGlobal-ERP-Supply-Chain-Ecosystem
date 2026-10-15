import { useEffect, useRef, type FocusEvent } from 'react';

interface DialogoBajaProps {
  entidad: string;
  nombre: string;
  cerrar: () => void;
  confirmar: () => void;
  eliminando: boolean;
  error: string;
}

export function DialogoBaja({ entidad, nombre, cerrar, confirmar, eliminando, error }: DialogoBajaProps) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const tituloId = `eliminar-${entidad}-title`;

  useEffect(() => {
    const dialogo = dialogoRef.current;
    const primerBoton = dialogo?.querySelector<HTMLElement>('button:not(:disabled)');
    primerBoton?.focus();
    function manejarTeclado(event: globalThis.KeyboardEvent): void {
      if (event.key === 'Escape') {
        event.preventDefault();
        cerrar();
      }
      if (event.key === 'Tab' && dialogo !== null) {
        const elementos = Array.from(dialogo.querySelectorAll<HTMLElement>('button:not(:disabled)'));
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

  function manejarFoco(event: FocusEvent<HTMLDivElement>): void {
    if (event.target === event.currentTarget) {
      dialogoRef.current?.querySelector<HTMLElement>('button:not(:disabled)')?.focus();
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby={tituloId}
        aria-modal="true"
        className="clientes-dialog"
        onFocus={manejarFoco}
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">CONFIRMAR BAJA</div>
        <h2 id={tituloId}>Eliminar {entidad}</h2>
        <p>¿Seguro que deseas eliminar a <strong>{nombre}</strong>?</p>
        {error !== '' && (
          <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
            <span aria-hidden="true">!</span>
            {error}
          </p>
        )}
        <div className="clientes-dialog-actions">
          <button className="secondary-button" disabled={eliminando} onClick={cerrar} type="button">
            Cancelar
          </button>
          <button className="danger-button" disabled={eliminando} onClick={confirmar} type="button">
            {eliminando ? 'Eliminando…' : `Eliminar ${entidad}`}
          </button>
        </div>
      </div>
    </div>
  );
}
