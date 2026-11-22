import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { camposTexto, validarRetorno, type Errores } from './validacion';
import { fechaParaInput, fechaParaApi } from './presentacion';
import type { RetornoDetalle, GuardarRetorno, ValoresRetorno, TipoRetorno } from './tipos';
export function RetornoFormulario({
  inicial,
  ocupado,
  error,
  guardar,
  cerrar,
}: {
  inicial: RetornoDetalle | null;
  ocupado: boolean;
  error: string;
  guardar: (d: GuardarRetorno) => void;
  cerrar: () => void;
}) {
  const [datos, setDatos] = useState<ValoresRetorno>(
    inicial
      ? {
          codigo: inicial.codigo,
          referenciaOrigen: inicial.referenciaOrigen,
          contraparte: inicial.contraparte,
          puntoRecogida: inicial.puntoRecogida,
          puntoRecepcion: inicial.puntoRecepcion,
          descripcion: inicial.descripcion,
          unidad: inicial.unidad,
          tipo: inicial.tipo,
          fechaProgramada: fechaParaInput(inicial.fechaProgramada),
          cantidadProgramada: inicial.cantidadProgramada,
          observaciones: inicial.observaciones || '',
        }
      : {
          codigo: '',
          referenciaOrigen: '',
          contraparte: '',
          puntoRecogida: '',
          puntoRecepcion: '',
          descripcion: '',
          unidad: '',
          tipo: 'Pallet',
          fechaProgramada: '',
          cantidadProgramada: '',
          observaciones: '',
        },
  );
  const [errores, setErrores] = useState<Errores>({});
  const ref = useRef<HTMLFormElement>(null);
  function enviar(e: FormEvent) {
    e.preventDefault();
    if (ocupado) return;
    const f = validarRetorno(datos);
    setErrores(f);
    if (Object.keys(f).length) {
      requestAnimationFrame(() =>
        ref.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    guardar({
      ...datos,
      codigo: datos.codigo.trim(),
      referenciaOrigen: datos.referenciaOrigen.trim(),
      contraparte: datos.contraparte.trim(),
      puntoRecogida: datos.puntoRecogida.trim(),
      puntoRecepcion: datos.puntoRecepcion.trim(),
      descripcion: datos.descripcion.trim(),
      unidad: datos.unidad.trim(),
      fechaProgramada: fechaParaApi(datos.fechaProgramada),
      cantidadProgramada: Number(datos.cantidadProgramada),
      observaciones: datos.observaciones.trim() || undefined,
    });
  }
  const campos = [
    ...camposTexto.map(([c, l, m]) => [c, l, m] as const),
    ['fechaProgramada', 'Fecha programada (UTC)', undefined],
    ['cantidadProgramada', 'Cantidad programada', undefined],
    ['observaciones', 'Observaciones', 500],
  ] as const;
  return (
    <section className="logistica-inversa-card" aria-labelledby="retorno-form-title">
      <h2 id="retorno-form-title">{inicial ? 'Editar retorno' : 'Nuevo retorno'}</h2>
      <form ref={ref} onSubmit={enviar} noValidate>
        <fieldset disabled={ocupado} className="logistica-inversa-form">
          <legend className="sr-only">Datos del retorno</legend>
          {campos.map(([campo, l, max], i) => (
            <CampoConError
              key={campo}
              etiqueta={l}
              htmlFor={`retorno-${campo}`}
              error={errores[campo]}
              idError={`retorno-${campo}-error`}
            >
              <input
                autoFocus={i === 0}
                id={`retorno-${campo}`}
                type={campo === 'fechaProgramada' ? 'datetime-local' : 'text'}
                step={campo === 'fechaProgramada' ? '0.001' : undefined}
                inputMode={campo === 'cantidadProgramada' ? 'decimal' : undefined}
                maxLength={max}
                value={datos[campo]}
                onChange={(e) => setDatos((p) => ({ ...p, [campo]: e.target.value }))}
                aria-invalid={!!errores[campo]}
                aria-describedby={errores[campo] ? `retorno-${campo}-error` : undefined}
              />
            </CampoConError>
          ))}
          <CampoConError
            etiqueta="Tipo"
            htmlFor="retorno-tipo"
            error={errores.tipo}
            idError="retorno-tipo-error"
          >
            <select
              id="retorno-tipo"
              value={datos.tipo}
              onChange={(e) => setDatos((p) => ({ ...p, tipo: e.target.value as TipoRetorno }))}
              aria-invalid={!!errores.tipo}
              aria-describedby={errores.tipo ? 'retorno-tipo-error' : undefined}
            >
              {['Pallet', 'Retornable', 'DevolucionRuta'].map((t) => (
                <option key={t}>{t}</option>
              ))}
            </select>
          </CampoConError>
          <p>Un material y una unidad por retorno. Las referencias se registran manualmente.</p>
          {error && (
            <p role="alert" className="field-error">
              {error}
            </p>
          )}
          <div className="logistica-inversa-actions">
            <button className="primary-button" type="submit">
              Guardar programación
            </button>
            <button className="secondary-button" type="button" onClick={cerrar}>
              Volver al detalle
            </button>
          </div>
        </fieldset>
      </form>
    </section>
  );
}
