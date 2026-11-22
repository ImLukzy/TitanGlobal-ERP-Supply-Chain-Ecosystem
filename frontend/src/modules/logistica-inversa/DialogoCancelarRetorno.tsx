import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { DialogoRetorno } from './DialogoRetorno';
import { validarMotivo } from './validacion';
export function DialogoCancelarRetorno({
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
    if (m) document.getElementById('retorno-motivo')?.focus();
    else if (!ocupado) confirmar(motivo.trim());
  }
  return (
    <DialogoRetorno
      titulo="Cancelar retorno"
      id="anular-retorno-title"
      ocupado={ocupado}
      error={error}
      cerrar={cerrar}
    >
      <p>El retorno y sus datos se conservarán con el motivo de cancelación.</p>
      <form onSubmit={enviar} noValidate>
        <CampoConError
          etiqueta="Motivo de cancelación"
          htmlFor="retorno-motivo"
          error={fallo}
          idError="retorno-motivo-error"
        >
          <textarea
            id="retorno-motivo"
            value={motivo}
            maxLength={500}
            disabled={ocupado}
            aria-invalid={!!fallo}
            aria-describedby={fallo ? 'retorno-motivo-error' : undefined}
            onChange={(e) => setMotivo(e.target.value)}
          />
        </CampoConError>
        <div className="logistica-inversa-actions">
          <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
            Volver
          </button>
          <button className="danger-button" type="submit" disabled={ocupado}>
            Confirmar cancelación
          </button>
        </div>
      </form>
    </DialogoRetorno>
  );
}
