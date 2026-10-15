import { useEffect, useRef, useState } from 'react';

interface OrdenAccionesDialogProps {
  tipo: 'recibir' | 'anular';
  numero: string;
  error: string;
  ocupado: boolean;
  cerrar: () => void;
  confirmar: (motivo?: string) => Promise<void>;
}

export function OrdenAccionesDialog({ tipo, numero, error, ocupado, cerrar, confirmar }: OrdenAccionesDialogProps) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const [motivo, setMotivo] = useState('');
  const [errorMotivo, setErrorMotivo] = useState('');
  const esAnulacion = tipo === 'anular';
  const tituloId = `orden-${tipo}-title`;

  useEffect(() => {
    const dialogo = dialogoRef.current;
    const primerBoton = dialogo?.querySelector<HTMLElement>('button:not(:disabled)');
    primerBoton?.focus();
    function manejarTeclado(event: KeyboardEvent): void {
      if (event.key === 'Escape') {
        event.preventDefault();
        cerrar();
      }
      if (event.key === 'Tab' && dialogo !== null) {
        const elementos = Array.from(dialogo.querySelectorAll<HTMLElement>('button:not(:disabled), textarea:not(:disabled)'));
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

  async function enviar(): Promise<void> {
    if (esAnulacion && motivo.trim() === '') {
      setErrorMotivo('El motivo de anulación es obligatorio.');
      return;
    }
    if (esAnulacion && motivo.trim().length > 500) {
      setErrorMotivo('El motivo de anulación no puede superar 500 caracteres.');
      return;
    }
    setErrorMotivo('');
    await confirmar(esAnulacion ? motivo.trim() : undefined);
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby={tituloId} aria-modal="true" className="clientes-dialog orden-action-dialog" ref={dialogoRef} role="dialog" tabIndex={-1}>
        <span className="page-eyebrow">ORDEN DE ABASTECIMIENTO</span>
        <h2 id={tituloId}>{esAnulacion ? `Anular ${numero}` : `Recibir ${numero}`}</h2>
        <p>{esAnulacion ? 'Indica el motivo de anulación de esta orden.' : 'Confirma que recibiste los productos de esta orden.'}</p>
        {esAnulacion && (
          <div className="orden-dialog-field">
            <label htmlFor="orden-motivo">Motivo de anulación</label>
            <textarea
              aria-describedby={errorMotivo ? 'orden-motivo-error' : undefined}
              aria-invalid={Boolean(errorMotivo)}
              id="orden-motivo"
              maxLength={520}
              onChange={(event) => setMotivo(event.currentTarget.value)}
              value={motivo}
            />
            {errorMotivo && <span className="orden-dialog-error" id="orden-motivo-error" role="alert">{errorMotivo}</span>}
          </div>
        )}
        {error && <p className="orden-dialog-error" role="alert">{error}</p>}
        <div className="clientes-dialog-actions">
          <button className="secondary-button" disabled={ocupado} onClick={cerrar} type="button">Cancelar</button>
          <button
            className={esAnulacion ? 'danger-button' : 'primary-button'}
            disabled={ocupado}
            onClick={() => void enviar()}
            type="button"
          >
            {ocupado ? 'Procesando…' : esAnulacion ? 'Confirmar anulación' : 'Confirmar recepción'}
          </button>
        </div>
      </div>
    </div>
  );
}
