import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { DialogoSiniestro } from './DialogoSiniestro';
import { validarMotivo } from './validacion';
export function DialogoAnularSiniestro({
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
    if (m) document.getElementById('siniestro-motivo')?.focus();
    else if (!ocupado) confirmar(motivo.trim());
  }
  return (
    <DialogoSiniestro
      titulo="Anular siniestro"
      id="anular-siniestro-title"
      ocupado={ocupado}
      error={error}
      cerrar={cerrar}
    >
      <p>El siniestro y sus datos se conservarán con el motivo de anulación.</p>
      <form onSubmit={enviar} noValidate>
        <CampoConError
          etiqueta="Motivo de anulación"
          htmlFor="siniestro-motivo"
          error={fallo}
          idError="siniestro-motivo-error"
        >
          <textarea
            id="siniestro-motivo"
            value={motivo}
            maxLength={500}
            disabled={ocupado}
            aria-invalid={!!fallo}
            aria-describedby={fallo ? 'siniestro-motivo-error' : undefined}
            onChange={(e) => setMotivo(e.target.value)}
          />
        </CampoConError>
        <div className="siniestros-flota-actions">
          <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
            Volver
          </button>
          <button className="danger-button" type="submit" disabled={ocupado}>
            Confirmar anulación
          </button>
        </div>
      </form>
    </DialogoSiniestro>
  );
}
