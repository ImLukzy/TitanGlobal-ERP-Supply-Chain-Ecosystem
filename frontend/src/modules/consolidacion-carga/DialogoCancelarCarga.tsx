import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { validarMotivo } from './validacion';

export function DialogoCancelarCarga({
  referencia,
  ocupado,
  error,
  cerrar,
  confirmar,
}: {
  referencia: string;
  ocupado: boolean;
  error: string;
  cerrar: () => void;
  confirmar: (motivo: string) => void;
}) {
  const dialogo = useRef<HTMLDivElement>(null);
  const [motivo, setMotivo] = useState('');
  const [fallo, setFallo] = useState('');
  useEffect(() => {
    const origen = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    dialogo.current?.querySelector('textarea')?.focus();
    return () => {
      if (origen?.isConnected) origen.focus();
    };
  }, []);
  function enviar(evento: FormEvent) {
    evento.preventDefault();
    const mensaje = validarMotivo(motivo);
    setFallo(mensaje);
    if (mensaje) dialogo.current?.querySelector('textarea')?.focus();
    else if (!ocupado) confirmar(motivo.trim());
  }
  return (
    <div className="clientes-dialog-backdrop">
      <div
        ref={dialogo}
        role="dialog"
        aria-modal="true"
        aria-labelledby="cancelar-carga-title"
        className="clientes-dialog consolidacion-carga-dialog"
        onKeyDown={(evento) => {
          if (evento.key === 'Escape' && !ocupado) {
            evento.preventDefault();
            cerrar();
          }
          if (evento.key === 'Tab') {
            const elementos = Array.from(
              dialogo.current?.querySelectorAll<HTMLElement>(
                'textarea:not(:disabled), button:not(:disabled)',
              ) ?? [],
            );
            const primero = elementos[0];
            const ultimo = elementos.at(-1);
            if (evento.shiftKey && document.activeElement === primero) {
              evento.preventDefault();
              ultimo?.focus();
            } else if (!evento.shiftKey && document.activeElement === ultimo) {
              evento.preventDefault();
              primero?.focus();
            }
          }
        }}
      >
        <h2 id="cancelar-carga-title">Cancelar carga {referencia}</h2>
        <p>La carga se conservará en el historial con el motivo de cancelación.</p>
        <form onSubmit={enviar} noValidate>
          <CampoConError
            etiqueta="Motivo de cancelación"
            htmlFor="carga-motivo"
            error={fallo}
            idError="carga-motivo-error"
          >
            <textarea
              id="carga-motivo"
              value={motivo}
              maxLength={500}
              disabled={ocupado}
              aria-invalid={!!fallo}
              aria-describedby={fallo ? 'carga-motivo-error' : undefined}
              onChange={(e) => setMotivo(e.target.value)}
            />
          </CampoConError>
          {error && (
            <p role="alert" className="field-error">
              {error}
            </p>
          )}
          <div className="consolidacion-carga-actions">
            <button className="secondary-button" disabled={ocupado} type="button" onClick={cerrar}>
              Volver
            </button>
            <button className="danger-button" type="submit" disabled={ocupado}>
              {ocupado ? 'Cancelando…' : 'Cancelar carga'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
