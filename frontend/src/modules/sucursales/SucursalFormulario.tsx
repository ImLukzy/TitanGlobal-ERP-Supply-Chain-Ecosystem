import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { camposTexto, camposOpcionales, validarSucursal, type Errores } from './validacion';
import { fechaParaInput, fechaParaApi } from './presentacion';
import type { SucursalDetalle, GuardarSucursal, ValoresSucursal, TipoSucursal } from './tipos';
export function SucursalFormulario({
  inicial,
  ocupado,
  error,
  guardar,
  cerrar,
}: {
  inicial: SucursalDetalle | null;
  ocupado: boolean;
  error: string;
  guardar: (d: GuardarSucursal) => void;
  cerrar: () => void;
}) {
  const [datos, setDatos] = useState<ValoresSucursal>(
    inicial
      ? {
          codigo: inicial.codigo,
          nombre: inicial.nombre,
          tipo: inicial.tipo,
          pais: inicial.pais,
          ciudad: inicial.ciudad,
          direccion: inicial.direccion,
          contacto: inicial.contacto || '',
          email: inicial.email || '',
          telefono: inicial.telefono || '',
          observaciones: inicial.observaciones || '',
          fechaApertura: inicial.fechaApertura ? fechaParaInput(inicial.fechaApertura) : '',
        }
      : {
          codigo: '',
          nombre: '',
          tipo: 'Oficina',
          pais: '',
          ciudad: '',
          direccion: '',
          contacto: '',
          email: '',
          telefono: '',
          observaciones: '',
          fechaApertura: '',
        },
  );
  const [errores, setErrores] = useState<Errores>({});
  const ref = useRef<HTMLFormElement>(null);
  function enviar(e: FormEvent) {
    e.preventDefault();
    if (ocupado) return;
    const f = validarSucursal(datos);
    setErrores(f);
    if (Object.keys(f).length) {
      requestAnimationFrame(() =>
        ref.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    guardar({
      codigo: datos.codigo.trim(),
      nombre: datos.nombre.trim(),
      tipo: datos.tipo,
      pais: datos.pais.trim(),
      ciudad: datos.ciudad.trim(),
      direccion: datos.direccion.trim(),
      contacto: datos.contacto.trim() || null,
      email: datos.email.trim() || null,
      telefono: datos.telefono.trim() || null,
      observaciones: datos.observaciones.trim() || null,
      fechaApertura: datos.fechaApertura ? fechaParaApi(datos.fechaApertura) : null,
    });
  }
  const campos = [
    ...camposTexto.map(([c, l, m]) => [c, l, m] as const),
    ...camposOpcionales.map(([c, l, m]) => [c, l, m] as const),
    ['fechaApertura', 'Fecha de apertura (UTC, opcional)', undefined],
  ] as const;
  return (
    <section className="sucursales-card" aria-labelledby="sucursal-form-title">
      <h2 id="sucursal-form-title">{inicial ? 'Editar sucursal' : 'Nueva sucursal'}</h2>
      <form ref={ref} onSubmit={enviar} noValidate>
        <fieldset disabled={ocupado} className="sucursales-form">
          <legend className="sr-only">Datos de la sucursal</legend>
          {campos.map(([c, l, m], i) => (
            <CampoConError
              key={c}
              etiqueta={l}
              htmlFor={`sucursal-${c}`}
              error={errores[c]}
              idError={`sucursal-${c}-error`}
            >
              {c === 'direccion' || c === 'observaciones' ? (
                <textarea
                  id={`sucursal-${c}`}
                  maxLength={m}
                  value={datos[c]}
                  onChange={(e) => setDatos((p) => ({ ...p, [c]: e.target.value }))}
                  aria-invalid={!!errores[c]}
                  aria-describedby={errores[c] ? `sucursal-${c}-error` : undefined}
                />
              ) : (
                <input
                  id={`sucursal-${c}`}
                  autoFocus={i === 0}
                  type={c === 'fechaApertura' ? 'datetime-local' : c === 'email' ? 'email' : 'text'}
                  step={c === 'fechaApertura' ? '0.001' : undefined}
                  maxLength={m}
                  value={datos[c]}
                  onChange={(e) => setDatos((p) => ({ ...p, [c]: e.target.value }))}
                  aria-invalid={!!errores[c]}
                  aria-describedby={errores[c] ? `sucursal-${c}-error` : undefined}
                />
              )}
            </CampoConError>
          ))}
          <CampoConError
            etiqueta="Tipo"
            htmlFor="sucursal-tipo"
            error={errores.tipo}
            idError="sucursal-tipo-error"
          >
            <select
              id="sucursal-tipo"
              value={datos.tipo}
              onChange={(e) => setDatos((p) => ({ ...p, tipo: e.target.value as TipoSucursal }))}
              aria-invalid={!!errores.tipo}
              aria-describedby={errores.tipo ? 'sucursal-tipo-error' : undefined}
            >
              {['Filial', 'Planta', 'Oficina'].map((t) => (
                <option key={t}>{t}</option>
              ))}
            </select>
          </CampoConError>
          {error && (
            <p role="alert" className="field-error">
              {error}
            </p>
          )}
          <div className="sucursales-actions">
            <button className="primary-button" type="submit">
              Guardar sucursal
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
