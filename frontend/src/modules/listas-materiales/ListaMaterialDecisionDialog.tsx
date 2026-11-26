import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { cambiarEstadoListaMaterial } from './api';
import type { DecisionLista, ListaMaterialDetalle } from './tipos';

interface DecisionProps {
  accion: DecisionLista;
  lista: ListaMaterialDetalle;
  cerrar: () => void;
  confirmado: () => void;
  consultar: () => void;
}

export function ListaMaterialDecisionDialog({ accion, lista, cerrar, confirmado, consultar }: DecisionProps) {
  const dialogo = useRef<HTMLDivElement>(null);
  const cancelar = useRef<HTMLButtonElement>(null);
  const [procesando, setProcesando] = useState(false);
  const [error, setError] = useState<{ mensaje: string; consultar: boolean }>();
  const activar = accion === 'activar';

  useEffect(() => { cancelar.current?.focus(); }, []);

  async function confirmar(): Promise<void> {
    if (procesando) return;
    setProcesando(true);
    setError(undefined);
    dialogo.current?.focus();
    try {
      await cambiarEstadoListaMaterial(lista.id, accion);
      confirmado();
    } catch (cause: unknown) {
      setError({ mensaje: cause instanceof Error ? cause.message : 'No se pudo actualizar el estado.', consultar: cause instanceof ApiError && [404, 409].includes(cause.statusCode) });
    } finally {
      setProcesando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-describedby="listas-materiales-decision-descripcion"
        aria-labelledby="listas-materiales-decision-titulo"
        aria-modal="true"
        className="clientes-dialog listas-materiales-dialogo"
        onKeyDown={(event) => {
          if (event.key === 'Escape' && !procesando) {
            event.preventDefault();
            cerrar();
          }
          if (event.key === 'Tab') {
            const botones = dialogo.current?.querySelectorAll<HTMLButtonElement>('button:enabled');
            const primero = botones?.[0];
            const ultimo = botones?.[botones.length - 1];
            if (primero === undefined || document.activeElement === dialogo.current) {
              event.preventDefault();
              (event.shiftKey ? ultimo : primero)?.focus();
            } else if (event.shiftKey && document.activeElement === primero) {
              event.preventDefault();
              ultimo?.focus();
            } else if (!event.shiftKey && document.activeElement === ultimo) {
              event.preventDefault();
              primero.focus();
            }
          }
        }}
        ref={dialogo}
        role="dialog"
        tabIndex={-1}
      >
        <h2 id="listas-materiales-decision-titulo">{activar ? 'Activar' : 'Retirar'} versión {lista.version}</h2>
        <p id="listas-materiales-decision-descripcion">
          {lista.productoNombre}. {activar ? 'La versión quedará activa y sus componentes quedarán en modo de consulta.' : 'La versión quedará disponible en el historial.'}
        </p>
        {error && <p className="field-error" role="alert">{error.mensaje}</p>}
        <div className="listas-materiales-acciones">
          <button className="secondary-button" disabled={procesando} onClick={cerrar} ref={cancelar} type="button">Cancelar</button>
          {error?.consultar && <button className="secondary-button" onClick={consultar} type="button">Consultar estado actual</button>}
          <button className={activar ? 'primary-button' : 'danger-button'} disabled={procesando} onClick={() => void confirmar()} type="button">
            {procesando ? 'Guardando…' : activar ? 'Confirmar activación' : 'Confirmar retiro'}
          </button>
        </div>
      </div>
    </div>
  );
}
