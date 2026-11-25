import { useEffect, useRef, useState } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { anularInspeccionInsumo } from './api';
import type { InspeccionInsumoDetalle } from './tipos';
import { manejarTecladoDialogo } from './trampaFoco';
import { validarAnulacion } from './validacion';

interface AnularProps {
  inspeccion: InspeccionInsumoDetalle;
  cerrar: () => void;
  confirmado: () => void;
}

export function AnularInspeccionInsumoDialog({ inspeccion, cerrar, confirmado }: AnularProps) {
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
    const validacion = validarAnulacion(motivo);
    setError(validacion.error);
    setErrorApi('');
    if (validacion.solicitud === undefined) {
      return;
    }
    setProcesando(true);
    try {
      await anularInspeccionInsumo(inspeccion.id, validacion.solicitud);
      confirmado();
    } catch (causa: unknown) {
      setErrorApi(causa instanceof Error ? causa.message : 'No se pudo anular la inspección.');
    } finally {
      setProcesando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-describedby="control-calidad-insumos-anular-descripcion"
        aria-labelledby="control-calidad-insumos-anular-titulo"
        aria-modal="true"
        className="clientes-dialog control-calidad-insumos-dialogo"
        onKeyDown={(event) => manejarTecladoDialogo(event, dialogo, cerrar, procesando)}
        ref={dialogo}
        role="dialog"
        tabIndex={-1}
      >
        <h2 id="control-calidad-insumos-anular-titulo">Anular {inspeccion.numero}</h2>
        <p id="control-calidad-insumos-anular-descripcion">
          La inspección del lote {inspeccion.lote} quedará anulada y el lote se podrá volver a inspeccionar.
        </p>
        <CampoConError error={error} etiqueta="Motivo de anulación" htmlFor="control-calidad-insumos-motivo" idError="control-calidad-insumos-motivo-error">
          <textarea
            aria-describedby={error ? 'control-calidad-insumos-motivo-error' : undefined}
            aria-invalid={Boolean(error)}
            disabled={procesando}
            id="control-calidad-insumos-motivo"
            maxLength={270}
            onChange={(event) => setMotivo(event.currentTarget.value)}
            ref={campoMotivo}
            rows={3}
            value={motivo}
          />
        </CampoConError>
        {errorApi !== '' && <p className="field-error" role="alert">{errorApi}</p>}
        <div className="control-calidad-insumos-acciones">
          <button className="secondary-button" disabled={procesando} onClick={cerrar} type="button">Cancelar</button>
          <button className="danger-button" disabled={procesando} onClick={() => void confirmar()} type="button">
            {procesando ? 'Guardando…' : 'Confirmar anulación'}
          </button>
        </div>
      </div>
    </div>
  );
}
