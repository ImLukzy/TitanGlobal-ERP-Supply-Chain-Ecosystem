import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { camposTexto, validarVale, type Errores } from './validacion';
import { previsualizar } from './calculos';
import { fechaParaInput, fechaParaApi } from './presentacion';
import type { ValeDetalle, GuardarVale, ValoresVale } from './tipos';
export function ValeFormulario({
  inicial,
  ocupado,
  error,
  guardar,
  cerrar,
}: {
  inicial: ValeDetalle | null;
  ocupado: boolean;
  error: string;
  guardar: (d: GuardarVale) => void;
  cerrar: () => void;
}) {
  const [datos, setDatos] = useState<ValoresVale>(
    inicial
      ? {
          codigo: inicial.codigo,
          placa: inicial.placa,
          estacion: inicial.estacion,
          fechaCarga: fechaParaInput(inicial.fechaCarga),
          tipoCombustible: inicial.tipoCombustible,
          litros: inicial.litros,
          precioLitro: inicial.precioLitro,
          moneda: inicial.moneda,
          odometroInicial: String(inicial.odometroInicial),
          odometroFinal: inicial.odometroFinal === null ? '' : String(inicial.odometroFinal),
          observaciones: inicial.observaciones ?? '',
        }
      : {
          codigo: '',
          placa: '',
          estacion: '',
          fechaCarga: '',
          tipoCombustible: 'Diesel',
          litros: '',
          precioLitro: '',
          moneda: '',
          odometroInicial: '',
          odometroFinal: '',
          observaciones: '',
        },
  );
  const [errores, setErrores] = useState<Errores>({});
  const ref = useRef<HTMLFormElement>(null);
  const total = previsualizar(datos);
  function enviar(e: FormEvent) {
    e.preventDefault();
    if (ocupado) return;
    const f = validarVale(datos);
    setErrores(f);
    if (Object.keys(f).length) {
      requestAnimationFrame(() =>
        ref.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    guardar({
      codigo: datos.codigo.trim(),
      placa: datos.placa.trim().toUpperCase(),
      estacion: datos.estacion.trim(),
      fechaCarga: fechaParaApi(datos.fechaCarga),
      tipoCombustible: datos.tipoCombustible,
      litros: Number(datos.litros),
      precioLitro: Number(datos.precioLitro),
      moneda: datos.moneda.trim().toUpperCase(),
      odometroInicial: Number(datos.odometroInicial),
      odometroFinal: datos.odometroFinal === '' ? null : Number(datos.odometroFinal),
      observaciones: datos.observaciones.trim() || undefined,
    });
  }
  const campos = [
    ...camposTexto.map(([c, l, m]) => [c, l, m] as const),
    ['fechaCarga', 'Fecha de carga (UTC)', undefined],
    ['litros', 'Litros (L)', undefined],
    ['precioLitro', 'Precio por litro', undefined],
    ['moneda', 'Moneda (3 letras)', 3],
    ['odometroInicial', 'Odómetro inicial (km)', undefined],
    ['odometroFinal', 'Odómetro final (km, opcional)', undefined],
    ['observaciones', 'Observaciones', 500],
  ] as const;
  return (
    <section className="control-combustible-card" aria-labelledby="vale-form-title">
      <h2 id="vale-form-title">{inicial ? 'Editar vale' : 'Nuevo vale'}</h2>
      <form ref={ref} onSubmit={enviar} noValidate>
        <fieldset disabled={ocupado} className="control-combustible-form">
          <legend className="sr-only">Datos del vale</legend>
          {campos.map(([campo, label, maximo], i) => (
            <CampoConError
              key={campo}
              etiqueta={label}
              htmlFor={`vale-${campo}`}
              error={errores[campo]}
              idError={`vale-${campo}-error`}
            >
              <input
                autoFocus={i === 0}
                id={`vale-${campo}`}
                type={campo === 'fechaCarga' ? 'datetime-local' : 'text'}
                step={campo === 'fechaCarga' ? '0.001' : undefined}
                inputMode={
                  campo === 'litros' || campo === 'precioLitro'
                    ? 'decimal'
                    : campo === 'odometroInicial' || campo === 'odometroFinal'
                      ? 'numeric'
                      : undefined
                }
                value={datos[campo]}
                maxLength={maximo}
                aria-invalid={!!errores[campo]}
                aria-describedby={errores[campo] ? `vale-${campo}-error` : undefined}
                onChange={(e) => setDatos({ ...datos, [campo]: e.target.value })}
              />
            </CampoConError>
          ))}
          <CampoConError
            etiqueta="Tipo de combustible"
            htmlFor="vale-tipo"
            error={errores.tipoCombustible}
            idError="vale-tipo-error"
          >
            <select
              id="vale-tipo"
              value={datos.tipoCombustible}
              aria-invalid={!!errores.tipoCombustible}
              aria-describedby={errores.tipoCombustible ? 'vale-tipo-error' : undefined}
              onChange={(e) => setDatos({ ...datos, tipoCombustible: e.target.value })}
            >
              <option>Diesel</option>
              <option>Gasolina</option>
            </select>
          </CampoConError>
          <div className="control-combustible-preview" role="status">
            {total ? (
              <>
                <p>{`Importe estimado: ${total.importe}\u00a0${datos.moneda.trim().toUpperCase() || '—'}`}</p>
                <p>
                  {total.kmPorLitro === null
                    ? 'Rendimiento pendiente: registra el odómetro final.'
                    : `${total.kilometrosRecorridos}\u00a0km · ${total.kmPorLitro}\u00a0km/L · ${total.litrosPor100Km}\u00a0L/100\u00a0km`}
                </p>
              </>
            ) : (
              <p>Revisa los datos numéricos para calcular el intervalo.</p>
            )}
          </div>
          <p>Rendimiento orientativo del intervalo declarado.</p>
        </fieldset>
        {error && (
          <p role="alert" className="field-error">
            {error}
          </p>
        )}
        <div className="control-combustible-actions">
          <button className="primary-button" type="submit" disabled={ocupado}>
            {ocupado ? 'Guardando…' : 'Guardar borrador'}
          </button>
          <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
            Volver al detalle
          </button>
        </div>
      </form>
    </section>
  );
}
