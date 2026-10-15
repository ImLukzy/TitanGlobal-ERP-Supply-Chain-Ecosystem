import { useEffect, useRef, useState } from 'react';

export type AccionDevolucion = 'aprobar' | 'recibir' | 'rechazar' | 'eliminar';
interface Props {
  accion: AccionDevolucion;
  numero: string;
  error: string;
  cerrando: boolean;
  cerrar: () => void;
  confirmar: (motivo?: string) => Promise<void>;
}

export function DevolucionAccionDialog({ accion, numero, error, cerrando, cerrar, confirmar }: Props) {
  const [motivo, setMotivo] = useState('');
  const [errorLocal, setErrorLocal] = useState('');
  const ref = useRef<HTMLDivElement>(null);
  const titulos: Record<AccionDevolucion, string> = {
    aprobar: 'Aprobar devolución',
    recibir: 'Marcar devolución recibida',
    rechazar: 'Rechazar devolución',
    eliminar: 'Eliminar devolución',
  };
  const titulo = titulos[accion];

  useEffect(() => {
    ref.current?.querySelector<HTMLElement>('button')?.focus();
    function teclado(event: KeyboardEvent): void {
      if (event.key === 'Escape') {
        event.preventDefault();
        cerrar();
      }
    }
    document.addEventListener('keydown', teclado);
    return () => document.removeEventListener('keydown', teclado);
  }, [cerrar]);

  async function enviar(): Promise<void> {
    if (accion === 'rechazar' && !motivo.trim()) { setErrorLocal('El motivo de rechazo es obligatorio.'); return; }
    if (accion === 'rechazar' && motivo.trim().length > 500) { setErrorLocal('El motivo de rechazo no puede superar 500 caracteres.'); return; }
    setErrorLocal('');
    await confirmar(accion === 'rechazar' ? motivo.trim() : undefined);
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby="devolucion-action-title" aria-modal="true" className="clientes-dialog devolucion-action-dialog" ref={ref} role="dialog" tabIndex={-1}>
        <span className="page-eyebrow">ACCIÓN DE DEVOLUCIÓN</span>
        <h2 id="devolucion-action-title">{titulo}</h2>
        <p>{numero}</p>
        {accion === 'rechazar' && (
          <div className="form-field">
            <label htmlFor="devolucion-rechazo-motivo">Motivo del rechazo</label>
            <textarea
              id="devolucion-rechazo-motivo"
              maxLength={500}
              onChange={(event) => setMotivo(event.currentTarget.value)}
              value={motivo}
            />
          </div>
        )}
        {(errorLocal || error) && <p className="clientes-error" role="alert">{errorLocal || error}</p>}
        <div className="clientes-dialog-actions">
          <button className="secondary-button" disabled={cerrando} onClick={cerrar} type="button">
            Cancelar
          </button>
          <button
            className={accion === 'rechazar' || accion === 'eliminar' ? 'danger-button' : 'primary-button'}
            disabled={cerrando}
            onClick={() => void enviar()}
            type="button"
          >
            {cerrando ? 'Procesando…' : `Confirmar ${accion}`}
          </button>
        </div>
      </div>
    </div>
  );
}
