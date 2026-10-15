import { useEffect, useRef, useState } from 'react';

interface Props { accion: 'activar' | 'finalizar' | 'cancelar' | 'eliminar'; numero: string; error: string; cerrar: () => void; confirmar: (motivo?: string) => Promise<void>; }

export function ContratoAccionDialog({ accion, numero, error, cerrar, confirmar }: Props) {
  const [motivo, setMotivo] = useState('');
  const [errorLocal, setErrorLocal] = useState('');
  const [ocupado, setOcupado] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const exigeMotivo = accion === 'cancelar';
  const titulo = `${accion[0]?.toUpperCase()}${accion.slice(1)} ${numero}`;
  useEffect(() => {
    ref.current?.querySelector<HTMLElement>('button')?.focus();
    function manejarEscape(event: KeyboardEvent): void {
      if (event.key === 'Escape' && !ocupado) { event.preventDefault(); cerrar(); }
    }
    document.addEventListener('keydown', manejarEscape);
    return () => document.removeEventListener('keydown', manejarEscape);
  }, [cerrar, ocupado]);

  async function enviar(): Promise<void> {
    if (exigeMotivo && !motivo.trim()) { setErrorLocal('El motivo de cancelación es obligatorio.'); return; }
    if (exigeMotivo && motivo.trim().length > 500) { setErrorLocal('El motivo de cancelación no puede superar 500 caracteres.'); return; }
    setErrorLocal('');
    setOcupado(true);
    try { await confirmar(exigeMotivo ? motivo.trim() : undefined); } finally { setOcupado(false); }
  }

  return (
    <div className="clientes-dialog-backdrop"><div aria-labelledby="contrato-action-title" aria-modal="true" className="clientes-dialog" ref={ref} role="dialog">
      <h2 id="contrato-action-title">{titulo}</h2><p>{exigeMotivo ? 'Indica el motivo de cancelación.' : 'Confirma esta acción sobre el contrato.'}</p>
      {exigeMotivo && <label>Motivo de cancelación<textarea onChange={(event) => setMotivo(event.currentTarget.value)} value={motivo} /></label>}
      {errorLocal && <p className="field-error" role="alert">{errorLocal}</p>}{error && <p className="solicitudes-error" role="alert">{error}</p>}
      <div className="clientes-dialog-actions"><button className="secondary-button" disabled={ocupado} onClick={cerrar} type="button">Volver</button><button className={accion === 'eliminar' || exigeMotivo ? 'danger-button' : 'primary-button'} disabled={ocupado} onClick={() => void enviar()} type="button">{ocupado ? 'Procesando…' : `Confirmar ${accion}`}</button></div>
    </div></div>
  );
}
