import { useEffect, useRef } from 'react';
export function DialogoEstadoSucursal({
  activa,
  ocupado,
  error,
  cerrar,
  confirmar,
}: {
  activa: boolean;
  ocupado: boolean;
  error: string;
  cerrar: () => void;
  confirmar: () => void;
}) {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const origen = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    ref.current?.querySelector<HTMLElement>('button')?.focus();
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
        aria-labelledby="estado-sucursal-title"
        className="clientes-dialog sucursales-dialog"
        onKeyDown={(e) => {
          if (e.key === 'Escape' && !ocupado) {
            e.preventDefault();
            cerrar();
          }
          if (e.key === 'Tab') {
            const elementos = ref.current?.querySelectorAll<HTMLElement>('button:not(:disabled)');
            if (!elementos?.length) {
              e.preventDefault();
              return;
            }
            const primero = elementos[0],
              ultimo = elementos[elementos.length - 1];
            if (
              e.shiftKey &&
              (document.activeElement === primero || document.activeElement === ref.current)
            ) {
              e.preventDefault();
              ultimo.focus();
            } else if (!e.shiftKey && document.activeElement === ultimo) {
              e.preventDefault();
              primero.focus();
            }
          }
        }}
      >
        <h2 id="estado-sucursal-title">{activa ? 'Desactivar sucursal' : 'Activar sucursal'}</h2>
        <p>
          Los datos de la sucursal se conservarán. Se registrará la fecha de este cambio de estado.
        </p>
        {error && (
          <p role="alert" className="field-error">
            {error}
          </p>
        )}
        <div className="sucursales-actions">
          <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
            Volver
          </button>
          <button className="primary-button" type="button" disabled={ocupado} onClick={confirmar}>
            {activa ? 'Confirmar desactivación' : 'Confirmar activación'}
          </button>
        </div>
      </div>
    </div>
  );
}
