import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { ApiError } from '../../api/client';
import { cambiarEstadoRutaProduccion } from './api';
import type { DecisionRuta, RutaProduccionDetalle } from './tipos';

interface DecisionProps {
  accion: DecisionRuta;
  ruta: RutaProduccionDetalle;
  cerrar: () => void;
  confirmado: () => void;
  consultar: () => void;
}

export function RutaProduccionDecisionDialog({ accion, ruta, cerrar, confirmado, consultar }: DecisionProps) {
  const dialogo = useRef<HTMLDivElement>(null);
  const cancelar = useRef<HTMLButtonElement>(null);
  const [procesando, setProcesando] = useState(false);
  const [error, setError] = useState<{ mensaje: string; consultar: boolean }>();
  const activar = accion === 'activar';

  useEffect(() => {
    cancelar.current?.focus();
  }, []);

  async function confirmar(): Promise<void> {
    if (procesando) {
      return;
    }
    setProcesando(true);
    setError(undefined);
    dialogo.current?.focus();
    try {
      await cambiarEstadoRutaProduccion(ruta.id, accion);
      confirmado();
    } catch (causa: unknown) {
      setError({
        mensaje: causa instanceof Error ? causa.message : 'No se pudo actualizar el estado.',
        consultar: causa instanceof ApiError && [404, 409].includes(causa.statusCode),
      });
    } finally {
      setProcesando(false);
    }
  }

  function manejarTeclado(event: KeyboardEvent<HTMLDivElement>): void {
    if (event.key === 'Escape' && !procesando) {
      event.preventDefault();
      cerrar();
      return;
    }
    if (event.key !== 'Tab') {
      return;
    }
    // Trampa de foco: Tab y Shift+Tab recorren solo los botones habilitados del diálogo.
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

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-describedby="rutas-produccion-decision-descripcion"
        aria-labelledby="rutas-produccion-decision-titulo"
        aria-modal="true"
        className="clientes-dialog rutas-produccion-dialogo"
        onKeyDown={manejarTeclado}
        ref={dialogo}
        role="dialog"
        tabIndex={-1}
      >
        <h2 id="rutas-produccion-decision-titulo">{activar ? 'Activar' : 'Retirar'} versión {ruta.version}</h2>
        <p id="rutas-produccion-decision-descripcion">
          {ruta.productoNombre}.{' '}
          {activar
            ? 'La versión quedará activa y sus operaciones quedarán en modo de consulta. Todos sus centros de trabajo deben estar activos.'
            : 'La versión quedará disponible en el historial.'}
        </p>
        {error && <p className="field-error" role="alert">{error.mensaje}</p>}
        <div className="rutas-produccion-acciones">
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
