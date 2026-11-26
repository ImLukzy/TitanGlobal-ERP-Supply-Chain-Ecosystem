import { useEffect, useRef, useState } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { cancelarMantenimiento } from './api';
import { formatearDia } from './presentacion';
import type { MantenimientoDetalle } from './tipos';
import { manejarTecladoDialogo } from './trampaFoco';
import { validarCancelacion } from './validacion';

interface CancelarProps {
  mantenimiento: MantenimientoDetalle;
  cerrar: () => void;
  confirmado: () => void;
}

export function CancelarMantenimientoDialog({ mantenimiento, cerrar, confirmado }: CancelarProps) {
  const dialogo = useRef<HTMLDivElement>(null);
  const campoMotivo = useRef<HTMLTextAreaElement>(null);
  const [motivo, setMotivo] = useState('');
  const [error, setError] = useState<string>();
  const [errorApi, setErrorApi] = useState('');
  const [procesando, setProcesando] = useState(false);

  useEffect(() => {
    campoMotivo.current?.focus();
  }, []);

  async function confirmar(): Promise<void> {
    if (procesando) {
      return;
    }
    const validacion = validarCancelacion(motivo);
    setError(validacion.error);
    setErrorApi('');
    if (validacion.solicitud === undefined) {
      return;
    }
    setProcesando(true);
    try {
      await cancelarMantenimiento(mantenimiento.id, validacion.solicitud);
      confirmado();
    } catch (causa: unknown) {
      setErrorApi(causa instanceof Error ? causa.message : 'No se pudo cancelar el mantenimiento.');
    } finally {
      setProcesando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-describedby="mantenimiento-preventivo-cancelar-descripcion"
        aria-labelledby="mantenimiento-preventivo-cancelar-titulo"
        aria-modal="true"
        className="clientes-dialog mantenimiento-preventivo-dialogo"
        onKeyDown={(event) => manejarTecladoDialogo(event, dialogo, cerrar, procesando)}
        ref={dialogo}
        role="dialog"
        tabIndex={-1}
      >
        <h2 id="mantenimiento-preventivo-cancelar-titulo">Cancelar {mantenimiento.numero}</h2>
        <p id="mantenimiento-preventivo-cancelar-descripcion">
          La parada del {formatearDia(mantenimiento.fechaProgramada)} en {mantenimiento.centroTrabajoCodigo} quedará cancelada y no se podrá reabrir.
        </p>
        <CampoConError error={error} etiqueta="Motivo de cancelación" htmlFor="mantenimiento-preventivo-motivo" idError="mantenimiento-preventivo-motivo-error">
          <textarea
            aria-describedby={error ? 'mantenimiento-preventivo-motivo-error' : undefined}
            aria-invalid={Boolean(error)}
            disabled={procesando}
            id="mantenimiento-preventivo-motivo"
            maxLength={270}
            onChange={(event) => setMotivo(event.currentTarget.value)}
            ref={campoMotivo}
            rows={3}
            value={motivo}
          />
        </CampoConError>
        {errorApi !== '' && <p className="field-error" role="alert">{errorApi}</p>}
        <div className="mantenimiento-preventivo-acciones">
          <button className="secondary-button" disabled={procesando} onClick={cerrar} type="button">Volver</button>
          <button className="danger-button" disabled={procesando} onClick={() => void confirmar()} type="button">
            {procesando ? 'Guardando…' : 'Confirmar cancelación'}
          </button>
        </div>
      </div>
    </div>
  );
}
