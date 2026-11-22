import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { camposTexto, validarSiniestro, type Errores } from './validacion';
import { fechaParaInput, fechaParaApi } from './presentacion';
import type { SiniestroDetalle, ValoresSiniestro, GuardarSiniestro, TipoSiniestro } from './tipos';
export function SiniestroFormulario({
  inicial,
  ocupado,
  error,
  guardar,
  cerrar,
}: {
  inicial: SiniestroDetalle | null;
  ocupado: boolean;
  error: string;
  guardar: (d: GuardarSiniestro) => void;
  cerrar: () => void;
}) {
  const [datos, setDatos] = useState<ValoresSiniestro>(
    inicial
      ? {
          codigo: inicial.codigo,
          placa: inicial.placa,
          lugar: inicial.lugar,
          descripcion: inicial.descripcion,
          tipo: inicial.tipo,
          fechaIncidente: fechaParaInput(inicial.fechaIncidente),
          observaciones: inicial.observaciones || '',
        }
      : {
          codigo: '',
          placa: '',
          lugar: '',
          descripcion: '',
          tipo: 'Accidente',
          fechaIncidente: '',
          observaciones: '',
        },
  );
  const [errores, setErrores] = useState<Errores>({});
  const ref = useRef<HTMLFormElement>(null);
  function enviar(e: FormEvent) {
    e.preventDefault();
    if (ocupado) return;
    const f = validarSiniestro(datos);
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
      placa: datos.placa.trim().toUpperCase(),
      lugar: datos.lugar.trim(),
      descripcion: datos.descripcion.trim(),
      fechaIncidente: fechaParaApi(datos.fechaIncidente),
      observaciones: datos.observaciones.trim() || undefined,
    });
  }
  const campos = [
    ...camposTexto.map(([c, l, m]) => [c, l, m] as const),
    ['fechaIncidente', 'Fecha del incidente (UTC)', undefined],
    ['observaciones', 'Observaciones', 500],
  ] as const;
  return (
    <section className="siniestros-flota-card" aria-labelledby="siniestro-form-title">
      <h2 id="siniestro-form-title">{inicial ? 'Editar siniestro' : 'Nuevo siniestro'}</h2>
      <form ref={ref} onSubmit={enviar} noValidate>
        <fieldset className="siniestros-flota-form" disabled={ocupado}>
          <legend className="sr-only">Datos del incidente</legend>
          {campos.map(([c, l, m], i) => (
            <CampoConError
              key={c}
              etiqueta={l}
              htmlFor={`siniestro-${c}`}
              error={errores[c]}
              idError={`siniestro-${c}-error`}
            >
              {c === 'descripcion' || c === 'observaciones' ? (
                <textarea
                  id={`siniestro-${c}`}
                  maxLength={m}
                  value={datos[c]}
                  onChange={(e) => setDatos((p) => ({ ...p, [c]: e.target.value }))}
                  aria-invalid={!!errores[c]}
                  aria-describedby={errores[c] ? `siniestro-${c}-error` : undefined}
                />
              ) : (
                <input
                  autoFocus={i === 0}
                  id={`siniestro-${c}`}
                  maxLength={m}
                  type={c === 'fechaIncidente' ? 'datetime-local' : 'text'}
                  step={c === 'fechaIncidente' ? '0.001' : undefined}
                  value={datos[c]}
                  onChange={(e) => setDatos((p) => ({ ...p, [c]: e.target.value }))}
                  aria-invalid={!!errores[c]}
                  aria-describedby={errores[c] ? `siniestro-${c}-error` : undefined}
                />
              )}
            </CampoConError>
          ))}
          <CampoConError
            etiqueta="Tipo"
            htmlFor="siniestro-tipo"
            error={errores.tipo}
            idError="siniestro-tipo-error"
          >
            <select
              id="siniestro-tipo"
              value={datos.tipo}
              onChange={(e) => setDatos((p) => ({ ...p, tipo: e.target.value as TipoSiniestro }))}
              aria-invalid={!!errores.tipo}
              aria-describedby={errores.tipo ? 'siniestro-tipo-error' : undefined}
            >
              {['Accidente', 'Robo', 'DanoMaterial'].map((t) => (
                <option key={t}>{t}</option>
              ))}
            </select>
          </CampoConError>
          {error && (
            <p role="alert" className="field-error">
              {error}
            </p>
          )}
          <div className="siniestros-flota-actions">
            <button type="submit" className="primary-button">
              Guardar incidente
            </button>
            <button type="button" className="secondary-button" onClick={cerrar}>
              Volver al detalle
            </button>
          </div>
        </fieldset>
      </form>
    </section>
  );
}
