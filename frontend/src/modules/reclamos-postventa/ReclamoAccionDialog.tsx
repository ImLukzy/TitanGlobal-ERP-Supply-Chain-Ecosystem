import { useEffect, useRef, useState } from 'react';

export type TipoAccionReclamo = 'iniciar' | 'resolver' | 'cerrar' | 'rechazar';
const textos: Record<TipoAccionReclamo, { titulo: string; confirmar: string; campo: string }> = {
  iniciar: { titulo: 'Iniciar atención', confirmar: 'Iniciar', campo: '' },
  resolver: { titulo: 'Resolver reclamo', confirmar: 'Resolver', campo: 'Resolución' },
  cerrar: { titulo: 'Cerrar reclamo', confirmar: 'Cerrar', campo: '' },
  rechazar: { titulo: 'Rechazar reclamo', confirmar: 'Rechazar', campo: 'Motivo de rechazo' },
};

export function ReclamoAccionDialog({ tipo, numero, error, ocupado, cerrar, confirmar }: {
  tipo: TipoAccionReclamo;
  numero: string;
  error: string;
  ocupado: boolean;
  cerrar: () => void;
  confirmar: (texto: string) => void;
}) {
  const [texto, setTexto] = useState('');
  const [errorLocal, setErrorLocal] = useState('');
  const botonRef = useRef<HTMLButtonElement>(null);
  const datos = textos[tipo];
  useEffect(() => { botonRef.current?.focus(); }, []);
  function enviar(): void {
    if (datos.campo && texto.trim() === '') {
      setErrorLocal(tipo === 'resolver' ? 'La resolución es obligatoria.' : 'El motivo de rechazo es obligatorio.');
      return;
    }
    confirmar(texto.trim());
  }
  return (
    <div className="clientes-dialog-backdrop"><section aria-labelledby="reclamo-action-title" aria-modal="true" className="clientes-dialog" onKeyDown={(event) => { if (event.key === 'Escape') cerrar(); }} role="dialog">
      <span className="page-eyebrow">ACCIÓN DE RECLAMO</span><h2 id="reclamo-action-title">{datos.titulo}</h2><p>{numero}</p>
      {datos.campo && <label className="reclamo-action-field">{datos.campo}<textarea aria-label={datos.campo} onChange={(event) => { setTexto(event.currentTarget.value); setErrorLocal(''); }} value={texto} /></label>}
      {(errorLocal || error) && <p className="clientes-error" role="alert">{errorLocal || error}</p>}
      <div className="clientes-dialog-actions"><button className="secondary-button" disabled={ocupado} onClick={cerrar} type="button">Cancelar</button><button className="primary-button" disabled={ocupado} onClick={enviar} ref={botonRef} type="button">{ocupado ? 'Procesando…' : datos.confirmar}</button></div>
    </section></div>
  );
}
