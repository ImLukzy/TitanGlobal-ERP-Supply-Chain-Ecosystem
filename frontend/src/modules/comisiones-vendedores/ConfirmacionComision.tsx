import { useEffect, useRef } from 'react';

interface Props {
  titulo: string;
  texto: string;
  confirmarEtiqueta: string;
  error: string;
  ocupado: boolean;
  cerrar: () => void;
  confirmar: () => void;
}

export function ConfirmacionComision({ titulo, texto, confirmarEtiqueta, error, ocupado, cerrar, confirmar }: Props) {
  const botonCerrarRef = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    botonCerrarRef.current?.focus();
    function manejarEscape(evento: KeyboardEvent): void {
      if (evento.key === 'Escape' && !ocupado) {
        cerrar();
      }
    }
    document.addEventListener('keydown', manejarEscape);
    return () => document.removeEventListener('keydown', manejarEscape);
  }, [cerrar, ocupado]);

  return (
    <div className="clientes-dialog-backdrop">
      <section aria-labelledby="comision-confirm-title" aria-modal="true" className="clientes-dialog comision-dialog" role="dialog">
        <div className="page-eyebrow">CONFIRMACIÓN</div>
        <h2 id="comision-confirm-title">{titulo}</h2>
        <p>{texto}</p>
        {error !== '' && <p className="clientes-error clientes-dialog-error" role="alert">{error}</p>}
        <div className="clientes-dialog-actions">
          <button className="secondary-button" disabled={ocupado} onClick={cerrar} ref={botonCerrarRef} type="button">Cancelar</button>
          <button className="primary-button" disabled={ocupado} onClick={confirmar} type="button">{ocupado ? 'Procesando…' : confirmarEtiqueta}</button>
        </div>
      </section>
    </div>
  );
}
