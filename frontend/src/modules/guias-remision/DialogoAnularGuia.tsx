import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { validarMotivo } from './validacion';

export function DialogoAnularGuia({
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
        aria-labelledby="anular-guia-title"
        className="clientes-dialog guias-remision-dialog"
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
        <h2 id="anular-guia-title">Anular guía {referencia}</h2>
        <p>La guía se conservará en el historial con el motivo de anulación.</p>
        <form onSubmit={enviar} noValidate>
          <CampoConError
            etiqueta="Motivo de anulación"
            htmlFor="guia-motivo"
            error={fallo}
            idError="guia-motivo-error"
          >
            <textarea
              id="guia-motivo"
              value={motivo}
              maxLength={500}
              disabled={ocupado}
              aria-invalid={!!fallo}
              aria-describedby={fallo ? 'guia-motivo-error' : undefined}
              onChange={(e) => setMotivo(e.target.value)}
            />
          </CampoConError>
          {error && (
            <p role="alert" className="field-error">
              {error}
            </p>
          )}
          <div className="guias-remision-actions">
            <button className="secondary-button" disabled={ocupado} type="button" onClick={cerrar}>
              Volver
            </button>
            <button className="danger-button" type="submit" disabled={ocupado}>
              {ocupado ? 'Anulando…' : 'Anular guía'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
