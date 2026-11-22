import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { DialogoSiniestro } from './DialogoSiniestro';
import { validarCierre } from './validacion';
import type { ValoresCierre, CerrarReclamo, ResultadoReclamo } from './tipos';
export function DialogoCierre({
  maximo,
  moneda,
  ocupado,
  error,
  cerrar,
  confirmar,
}: {
  maximo: string;
  moneda: string;
  ocupado: boolean;
  error: string;
  cerrar: () => void;
  confirmar: (d: CerrarReclamo) => void;
}) {
  const [datos, setDatos] = useState<ValoresCierre>({
    resultado: 'Aceptado',
    montoReconocido: '',
    resolucion: '',
  });
  const [errores, setErrores] = useState<Partial<Record<keyof ValoresCierre, string>>>({});
  const ref = useRef<HTMLFormElement>(null);
  function enviar(e: FormEvent) {
    e.preventDefault();
    if (ocupado) return;
    const f = validarCierre(datos, maximo);
    setErrores(f);
    if (Object.keys(f).length) {
      requestAnimationFrame(() =>
        ref.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    confirmar({
      resultado: datos.resultado,
      montoReconocido: Number(datos.montoReconocido),
      resolucion: datos.resolucion.trim(),
    });
  }
  return (
    <DialogoSiniestro
      titulo="Cerrar reclamo"
      id="cierre-title"
      ocupado={ocupado}
      error={error}
      cerrar={cerrar}
    >
      <p>
        Monto reclamado:{' '}
        <span className="siniestros-flota-number">
          {maximo} {moneda}
        </span>
        . El cierre no registra un pago.
      </p>
      <form ref={ref} onSubmit={enviar} noValidate>
        <CampoConError
          etiqueta="Resultado"
          htmlFor="cierre-resultado"
          error={errores.resultado}
          idError="cierre-resultado-error"
        >
          <select
            id="cierre-resultado"
            value={datos.resultado}
            disabled={ocupado}
            onChange={(e) =>
              setDatos((p) => ({ ...p, resultado: e.target.value as ResultadoReclamo }))
            }
            aria-invalid={!!errores.resultado}
            aria-describedby={errores.resultado ? 'cierre-resultado-error' : undefined}
          >
            <option>Aceptado</option>
            <option>Rechazado</option>
          </select>
        </CampoConError>
        <CampoConError
          etiqueta="Monto reconocido"
          htmlFor="cierre-monto"
          error={errores.montoReconocido}
          idError="cierre-monto-error"
        >
          <input
            id="cierre-monto"
            inputMode="decimal"
            value={datos.montoReconocido}
            disabled={ocupado}
            onChange={(e) => setDatos((p) => ({ ...p, montoReconocido: e.target.value }))}
            aria-invalid={!!errores.montoReconocido}
            aria-describedby={errores.montoReconocido ? 'cierre-monto-error' : undefined}
          />
        </CampoConError>
        <CampoConError
          etiqueta="Resolución"
          htmlFor="cierre-resolucion"
          error={errores.resolucion}
          idError="cierre-resolucion-error"
        >
          <textarea
            id="cierre-resolucion"
            maxLength={500}
            value={datos.resolucion}
            disabled={ocupado}
            onChange={(e) => setDatos((p) => ({ ...p, resolucion: e.target.value }))}
            aria-invalid={!!errores.resolucion}
            aria-describedby={errores.resolucion ? 'cierre-resolucion-error' : undefined}
          />
        </CampoConError>
        <div className="siniestros-flota-actions">
          <button type="button" className="secondary-button" disabled={ocupado} onClick={cerrar}>
            Volver
          </button>
          <button type="submit" className="primary-button" disabled={ocupado}>
            Confirmar cierre
          </button>
        </div>
      </form>
    </DialogoSiniestro>
  );
}
