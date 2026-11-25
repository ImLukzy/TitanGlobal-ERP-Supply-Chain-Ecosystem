import { useEffect, useRef, useState } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { dictaminarInspeccionInsumo } from './api';
import { formatearPorcentaje } from './presentacion';
import { resultadosDictamen, type InspeccionInsumoDetalle, type ResultadoDictamen } from './tipos';
import { manejarTecladoDialogo } from './trampaFoco';
import { leerDefectuosa, porcentajeDefectos, validarDictamen, type FormularioDictamen } from './validacion';

interface DictamenProps {
  inspeccion: InspeccionInsumoDetalle;
  cerrar: () => void;
  confirmado: () => void;
}

export function DictamenInsumoDialog({ inspeccion, cerrar, confirmado }: DictamenProps) {
  const dialogo = useRef<HTMLDivElement>(null);
  const primerCampo = useRef<HTMLInputElement>(null);
  const [valores, setValores] = useState<FormularioDictamen>({ resultado: '', cantidadDefectuosa: '0', observaciones: '' });
  const [errores, setErrores] = useState<Partial<Record<string, string>>>({});
  const [errorApi, setErrorApi] = useState('');
  const [procesando, setProcesando] = useState(false);
  const defectuosa = leerDefectuosa(valores.cantidadDefectuosa);
  const porcentaje = defectuosa === null ? null : porcentajeDefectos(defectuosa, inspeccion.cantidadMuestra);
  const supera = porcentaje !== null && porcentaje > inspeccion.toleranciaDefectosPorcentaje;
  // Sobre la tolerancia solo se puede rechazar: «Aprobada» se deshabilita y deja de estar elegida.
  const resultado = supera && valores.resultado === 'Aprobada' ? '' : valores.resultado;

  useEffect(() => {
    primerCampo.current?.focus();
  }, []);

  async function confirmar(): Promise<void> {
    if (procesando) {
      return;
    }
    const validacion = validarDictamen({ ...valores, resultado }, inspeccion.cantidadMuestra);
    setErrores(validacion.errores);
    setErrorApi('');
    if (validacion.solicitud === undefined) {
      return;
    }
    setProcesando(true);
    try {
      await dictaminarInspeccionInsumo(inspeccion.id, validacion.solicitud);
      confirmado();
    } catch (causa: unknown) {
      setErrorApi(causa instanceof Error ? causa.message : 'No se pudo dictaminar la inspección.');
    } finally {
      setProcesando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="control-calidad-insumos-dictamen-titulo"
        aria-modal="true"
        className="clientes-dialog control-calidad-insumos-dialogo"
        onKeyDown={(event) => manejarTecladoDialogo(event, dialogo, cerrar, procesando)}
        ref={dialogo}
        role="dialog"
        tabIndex={-1}
      >
        <h2 id="control-calidad-insumos-dictamen-titulo">Dictaminar {inspeccion.numero}</h2>
        <p>
          {inspeccion.productoNombre} · lote {inspeccion.lote}. Muestra de {inspeccion.cantidadMuestra} {inspeccion.unidadMedida}, tolerancia{' '}
          {formatearPorcentaje(inspeccion.toleranciaDefectosPorcentaje)}.
        </p>
        <CampoConError
          error={errores.cantidadDefectuosa}
          etiqueta="Cantidad defectuosa"
          htmlFor="control-calidad-insumos-defectuosa"
          idError="control-calidad-insumos-defectuosa-error"
        >
          <input
            aria-describedby={['control-calidad-insumos-porcentaje', errores.cantidadDefectuosa ? 'control-calidad-insumos-defectuosa-error' : '']
              .join(' ')
              .trim()}
            aria-invalid={Boolean(errores.cantidadDefectuosa)}
            disabled={procesando}
            id="control-calidad-insumos-defectuosa"
            inputMode="decimal"
            onChange={(event) => setValores({ ...valores, cantidadDefectuosa: event.currentTarget.value })}
            ref={primerCampo}
            value={valores.cantidadDefectuosa}
          />
        </CampoConError>
        <p aria-live="polite" className={`control-calidad-insumos-tolerancia${supera ? ' is-supera' : ''}`} id="control-calidad-insumos-porcentaje">
          {porcentaje === null
            ? 'Escribe la cantidad defectuosa para calcular el porcentaje.'
            : `Defectos: ${formatearPorcentaje(porcentaje)}. ${supera ? 'Supera la tolerancia: solo puede rechazarse.' : 'Dentro de la tolerancia.'}`}
        </p>
        <fieldset
          aria-describedby={errores.resultado ? 'control-calidad-insumos-resultado-error' : undefined}
          className="control-calidad-insumos-resultado"
          disabled={procesando}
        >
          <legend>Resultado</legend>
          {resultadosDictamen.map((opcion: ResultadoDictamen) => (
            <label key={opcion}>
              <input
                checked={resultado === opcion}
                disabled={opcion === 'Aprobada' && supera}
                name="control-calidad-insumos-resultado"
                onChange={() => setValores({ ...valores, resultado: opcion })}
                type="radio"
                value={opcion}
              />
              {opcion}
            </label>
          ))}
          {errores.resultado && <span className="field-error" id="control-calidad-insumos-resultado-error">{errores.resultado}</span>}
        </fieldset>
        <CampoConError
          error={errores.observaciones}
          etiqueta="Observaciones (opcional)"
          htmlFor="control-calidad-insumos-observaciones"
          idError="control-calidad-insumos-observaciones-error"
        >
          <textarea
            aria-describedby={errores.observaciones ? 'control-calidad-insumos-observaciones-error' : undefined}
            aria-invalid={Boolean(errores.observaciones)}
            disabled={procesando}
            id="control-calidad-insumos-observaciones"
            maxLength={520}
            onChange={(event) => setValores({ ...valores, observaciones: event.currentTarget.value })}
            rows={3}
            value={valores.observaciones}
          />
        </CampoConError>
        {errorApi !== '' && <p className="field-error" role="alert">{errorApi}</p>}
        <div className="control-calidad-insumos-acciones">
          <button className="secondary-button" disabled={procesando} onClick={cerrar} type="button">Cancelar</button>
          <button className="primary-button" disabled={procesando} onClick={() => void confirmar()} type="button">
            {procesando ? 'Guardando…' : 'Confirmar dictamen'}
          </button>
        </div>
      </div>
    </div>
  );
}
