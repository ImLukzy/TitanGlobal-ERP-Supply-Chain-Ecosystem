import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { DialogoEntrega } from './DialogoEntrega';
import { validarMotivo } from './validacion';
export function DialogoCancelarEntrega({
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
    if (m) document.getElementById('entrega-motivo')?.focus();
    else if (!ocupado) confirmar(motivo.trim());
  }
  return (
    <DialogoEntrega
      titulo="Cancelar entrega"
      id="cancelar-entrega-title"
      ocupado={ocupado}
      error={error}
      cerrar={cerrar}
    >
      <p>El despacho y su historial se conservarán con el motivo de cancelación.</p>
      <form onSubmit={enviar} noValidate>
        <CampoConError
          etiqueta="Motivo de cancelación"
          htmlFor="entrega-motivo"
          error={fallo}
          idError="entrega-motivo-error"
        >
          <textarea
            id="entrega-motivo"
            value={motivo}
            maxLength={500}
            disabled={ocupado}
            aria-invalid={!!fallo}
            aria-describedby={fallo ? 'entrega-motivo-error' : undefined}
            onChange={(e) => setMotivo(e.target.value)}
          />
        </CampoConError>
        <div className="monitoreo-entregas-actions">
          <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
            Volver
          </button>
          <button className="danger-button" type="submit" disabled={ocupado}>
            Confirmar cancelación
          </button>
        </div>
      </form>
    </DialogoEntrega>
  );
}
