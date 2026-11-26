import { useEffect, useRef, useState } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { completarMantenimiento } from './api';
import { formatearHoras } from './presentacion';
import type { MantenimientoDetalle } from './tipos';
import { manejarTecladoDialogo } from './trampaFoco';
import { validarCierre } from './validacion';

interface CompletarProps {
  mantenimiento: MantenimientoDetalle;
  cerrar: () => void;
  confirmado: () => void;
}

export function CompletarMantenimientoDialog({ mantenimiento, cerrar, confirmado }: CompletarProps) {
  const dialogo = useRef<HTMLDivElement>(null);
  const campoHoras = useRef<HTMLInputElement>(null);
  const [horas, setHoras] = useState('');
  const [observaciones, setObservaciones] = useState('');
  const [errores, setErrores] = useState<Partial<Record<string, string>>>({});
  const [errorApi, setErrorApi] = useState('');
  const [procesando, setProcesando] = useState(false);

  useEffect(() => {
    campoHoras.current?.focus();
  }, []);

  async function confirmar(): Promise<void> {
    if (procesando) {
      return;
    }
    const validacion = validarCierre(horas, observaciones);
    setErrores(validacion.errores);
    setErrorApi('');
    if (validacion.solicitud === undefined) {
      return;
    }
    setProcesando(true);
    try {
      await completarMantenimiento(mantenimiento.id, validacion.solicitud);
      confirmado();
    } catch (causa: unknown) {
      setErrorApi(causa instanceof Error ? causa.message : 'No se pudo completar el mantenimiento.');
    } finally {
      setProcesando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-describedby="mantenimiento-preventivo-completar-descripcion"
        aria-labelledby="mantenimiento-preventivo-completar-titulo"
        aria-modal="true"
        className="clientes-dialog mantenimiento-preventivo-dialogo"
        onKeyDown={(event) => manejarTecladoDialogo(event, dialogo, cerrar, procesando)}
        ref={dialogo}
        role="dialog"
        tabIndex={-1}
      >
        <h2 id="mantenimiento-preventivo-completar-titulo">Completar {mantenimiento.numero}</h2>
        <p id="mantenimiento-preventivo-completar-descripcion">
          Registra las horas que tomó la parada. Estaba planificada en {formatearHoras(mantenimiento.duracionHorasPlanificada)}.
        </p>
        <CampoConError
          error={errores.horasReales}
          etiqueta="Horas reales"
          htmlFor="mantenimiento-preventivo-horas-reales"
          idError="mantenimiento-preventivo-horas-reales-error"
        >
          <input
            aria-describedby={errores.horasReales ? 'mantenimiento-preventivo-horas-reales-error' : undefined}
            aria-invalid={Boolean(errores.horasReales)}
            disabled={procesando}
            id="mantenimiento-preventivo-horas-reales"
            inputMode="decimal"
            onChange={(event) => setHoras(event.currentTarget.value)}
            ref={campoHoras}
            value={horas}
          />
        </CampoConError>
        <CampoConError
          error={errores.observaciones}
          etiqueta="Observaciones (opcional)"
          htmlFor="mantenimiento-preventivo-observaciones"
          idError="mantenimiento-preventivo-observaciones-error"
        >
          <textarea
            aria-describedby={errores.observaciones ? 'mantenimiento-preventivo-observaciones-error' : undefined}
            aria-invalid={Boolean(errores.observaciones)}
            disabled={procesando}
            id="mantenimiento-preventivo-observaciones"
            maxLength={520}
            onChange={(event) => setObservaciones(event.currentTarget.value)}
            rows={3}
            value={observaciones}
          />
        </CampoConError>
        {errorApi !== '' && <p className="field-error" role="alert">{errorApi}</p>}
        <div className="mantenimiento-preventivo-acciones">
          <button className="secondary-button" disabled={procesando} onClick={cerrar} type="button">Volver</button>
          <button className="primary-button" disabled={procesando} onClick={() => void confirmar()} type="button">
            {procesando ? 'Guardando…' : 'Confirmar cierre'}
          </button>
        </div>
      </div>
    </div>
  );
}
