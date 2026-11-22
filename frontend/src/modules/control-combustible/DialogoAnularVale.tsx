import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { DialogoVale } from './DialogoVale';
import { validarMotivo } from './validacion';
export function DialogoAnularVale({
  ocupado,
  error,
  cerrar,
  confirmar,
}: {
  ocupado: boolean;
  error: string;
  cerrar: () => void;
  confirmar: (m: string) => void;
}) {
  const [motivo, setMotivo] = useState('');
  const [fallo, setFallo] = useState('');
  function enviar(e: FormEvent) {
    e.preventDefault();
    const m = validarMotivo(motivo);
    setFallo(m);
    if (m) document.getElementById('vale-motivo')?.focus();
    else if (!ocupado) confirmar(motivo.trim());
  }
  return (
    <DialogoVale
      titulo="Anular vale"
      id="anular-vale-title"
      ocupado={ocupado}
      error={error}
      cerrar={cerrar}
    >
      <p>El vale y sus datos se conservarán con el motivo de anulación.</p>
      <form onSubmit={enviar} noValidate>
        <CampoConError
          etiqueta="Motivo de anulación"
          htmlFor="vale-motivo"
          error={fallo}
          idError="vale-motivo-error"
        >
          <textarea
            id="vale-motivo"
            value={motivo}
            maxLength={500}
            disabled={ocupado}
            aria-invalid={!!fallo}
            aria-describedby={fallo ? 'vale-motivo-error' : undefined}
            onChange={(e) => setMotivo(e.target.value)}
          />
        </CampoConError>
        <div className="control-combustible-actions">
          <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
            Volver
          </button>
          <button className="danger-button" type="submit" disabled={ocupado}>
            Confirmar anulación
          </button>
        </div>
      </form>
    </DialogoVale>
  );
}
