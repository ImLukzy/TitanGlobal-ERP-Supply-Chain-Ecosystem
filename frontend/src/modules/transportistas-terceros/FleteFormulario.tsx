import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { SelectorTransportista } from './SelectorTransportista';
import type { RegistrarFlete } from './tipos';
import { validarFlete, type Errores, type ValoresFlete } from './validacion';

export function FleteFormulario({
  ocupado,
  error,
  guardar,
  cerrar,
}: {
  ocupado: boolean;
  error: string;
  guardar: (datos: RegistrarFlete) => void;
  cerrar: () => void;
}) {
  const [datos, setDatos] = useState<ValoresFlete>({
    transportistaId: 0,
    referencia: '',
    fecha: new Date().toISOString().slice(0, 10),
    origen: '',
    destino: '',
    importe: '',
    moneda: 'PEN',
    observaciones: '',
  });
  const [errores, setErrores] = useState<Errores>({});
  const formulario = useRef<HTMLFormElement>(null);
  function enviar(evento: FormEvent) {
    evento.preventDefault();
    const fallos = validarFlete(datos);
    setErrores(fallos);
    if (Object.keys(fallos).length) {
      requestAnimationFrame(() =>
        formulario.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    guardar({
      ...datos,
      referencia: datos.referencia.trim(),
      fecha: `${datos.fecha}T00:00:00Z`,
      origen: datos.origen.trim(),
      destino: datos.destino.trim(),
      importe: Number(datos.importe),
      moneda: datos.moneda.trim().toUpperCase(),
      observaciones: datos.observaciones.trim() || undefined,
    });
  }
  return (
    <section className="transportistas-terceros-card" aria-labelledby="flete-form-title">
      <h2 id="flete-form-title">Registrar flete</h2>
      <p>
        El importe conserva la moneda declarada. El registro solo puede cancelarse con un motivo.
      </p>
      <form ref={formulario} onSubmit={enviar} noValidate>
        <fieldset disabled={ocupado} className="transportistas-terceros-form">
          <legend className="sr-only">Datos del flete</legend>
          <SelectorTransportista
            soloActivos
            valor={datos.transportistaId}
            cambiar={(transportistaId) => setDatos({ ...datos, transportistaId })}
            error={errores.transportistaId}
          />
          {(
            [
              ['referencia', 'Referencia', 'text', 30],
              ['fecha', 'Fecha UTC', 'date', undefined],
              ['origen', 'Origen', 'text', 120],
              ['destino', 'Destino', 'text', 120],
              ['importe', 'Importe', 'text', undefined],
              ['moneda', 'Moneda', 'text', 3],
              ['observaciones', 'Observaciones del flete', 'text', 500],
            ] as const
          ).map(([campo, etiqueta, tipo, maximo], indice) => (
            <CampoConError
              key={campo}
              etiqueta={etiqueta}
              htmlFor={`flete-${campo}`}
              error={errores[campo]}
              idError={`flete-${campo}-error`}
            >
              <input
                autoFocus={indice === 0}
                id={`flete-${campo}`}
                type={tipo}
                inputMode={campo === 'importe' ? 'decimal' : undefined}
                maxLength={maximo}
                value={datos[campo]}
                aria-invalid={!!errores[campo]}
                aria-describedby={errores[campo] ? `flete-${campo}-error` : undefined}
                onChange={(e) => setDatos({ ...datos, [campo]: e.target.value })}
              />
            </CampoConError>
          ))}
        </fieldset>
        {error && (
          <p role="alert" className="field-error">
            {error}
          </p>
        )}
        <div className="transportistas-terceros-actions">
          <button className="primary-button" type="submit" disabled={ocupado}>
            {ocupado ? 'Registrando…' : 'Guardar flete'}
          </button>
          <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
            Volver a fletes
          </button>
        </div>
      </form>
    </section>
  );
}
