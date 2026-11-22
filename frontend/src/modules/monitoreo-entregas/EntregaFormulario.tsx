import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { camposEntrega, validarEntrega, type Errores } from './validacion';
import { fechaParaApi, fechaParaInput } from './presentacion';
import type { EntregaDetalle, GuardarEntrega, ValoresEntrega } from './tipos';
export function EntregaFormulario({
  inicial,
  ocupado,
  error,
  guardar,
  cerrar,
}: {
  inicial: EntregaDetalle | null;
  ocupado: boolean;
  error: string;
  guardar: (d: GuardarEntrega) => void;
  cerrar: () => void;
}) {
  const [datos, setDatos] = useState<ValoresEntrega>(
    inicial
      ? {
          codigo: inicial.codigo,
          referenciaGuia: inicial.referenciaGuia,
          destinatario: inicial.destinatario,
          direccionDestino: inicial.direccionDestino,
          fechaProgramada: fechaParaInput(inicial.fechaProgramada),
          observaciones: inicial.observaciones ?? '',
        }
      : {
          codigo: '',
          referenciaGuia: '',
          destinatario: '',
          direccionDestino: '',
          fechaProgramada: '',
          observaciones: '',
        },
  );
  const [errores, setErrores] = useState<Errores>({});
  const ref = useRef<HTMLFormElement>(null);
  function enviar(e: FormEvent) {
    e.preventDefault();
    if (ocupado) return;
    const fallos = validarEntrega(datos);
    setErrores(fallos);
    if (Object.keys(fallos).length) {
      requestAnimationFrame(() =>
        ref.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    guardar({
      codigo: datos.codigo.trim(),
      referenciaGuia: datos.referenciaGuia.trim(),
      destinatario: datos.destinatario.trim(),
      direccionDestino: datos.direccionDestino.trim(),
      fechaProgramada: fechaParaApi(datos.fechaProgramada),
      observaciones: datos.observaciones.trim() || undefined,
    });
  }
  const campos = [
    ...camposEntrega.map(([c, l, m]) => [c, l, m] as const),
    ['fechaProgramada', 'Fecha programada (UTC)', undefined],
    ['observaciones', 'Observaciones', 500],
  ] as const;
  return (
    <section className="monitoreo-entregas-card" aria-labelledby="entrega-form-title">
      <h2 id="entrega-form-title">{inicial ? 'Editar entrega' : 'Nueva entrega'}</h2>
      <form ref={ref} onSubmit={enviar} noValidate>
        <fieldset disabled={ocupado} className="monitoreo-entregas-form">
          <legend className="sr-only">Programación del despacho</legend>
          {campos.map(([campo, label, maximo], i) => (
            <CampoConError
              key={campo}
              etiqueta={label}
              htmlFor={`entrega-${campo}`}
              error={errores[campo]}
              idError={`entrega-${campo}-error`}
            >
              <input
                autoFocus={i === 0}
                id={`entrega-${campo}`}
                type={campo === 'fechaProgramada' ? 'datetime-local' : 'text'}
                step={campo === 'fechaProgramada' ? '0.001' : undefined}
                value={datos[campo]}
                maxLength={maximo}
                aria-invalid={!!errores[campo]}
                aria-describedby={errores[campo] ? `entrega-${campo}-error` : undefined}
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
        <div className="monitoreo-entregas-actions">
          <button className="primary-button" disabled={ocupado} type="submit">
            {ocupado ? 'Guardando…' : 'Guardar programación'}
          </button>
          <button className="secondary-button" disabled={ocupado} type="button" onClick={cerrar}>
            Volver al detalle
          </button>
        </div>
      </form>
    </section>
  );
}
