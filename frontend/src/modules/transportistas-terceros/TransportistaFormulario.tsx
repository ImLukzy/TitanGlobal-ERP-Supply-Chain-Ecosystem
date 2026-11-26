import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { GuardarTransportista, TransportistaDetalle } from './tipos';
import { validarTransportista, type Errores } from './validacion';

export function TransportistaFormulario({
  inicial,
  ocupado,
  error,
  guardar,
  cerrar,
}: {
  inicial: TransportistaDetalle | null;
  ocupado: boolean;
  error: string;
  guardar: (datos: GuardarTransportista) => void;
  cerrar: () => void;
}) {
  const [datos, setDatos] = useState<GuardarTransportista>(
    inicial ?? {
      razonSocial: '',
      documentoFiscal: '',
      pais: '',
      tipoServicio: 'Ambos',
      activo: true,
    },
  );
  const [errores, setErrores] = useState<Errores>({});
  const formulario = useRef<HTMLFormElement>(null);
  function enviar(evento: FormEvent) {
    evento.preventDefault();
    const fallos = validarTransportista(datos);
    setErrores(fallos);
    if (Object.keys(fallos).length > 0) {
      requestAnimationFrame(() =>
        formulario.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    const limpios = {
      ...datos,
      razonSocial: datos.razonSocial.trim(),
      documentoFiscal: datos.documentoFiscal.trim(),
      pais: datos.pais.trim(),
    };
    for (const clave of ['contacto', 'email', 'telefono', 'observaciones'] as const) {
      limpios[clave] = datos[clave]?.trim() || undefined;
    }
    guardar(limpios);
  }
  return (
    <section className="transportistas-terceros-card" aria-labelledby="transportista-form-title">
      <h2 id="transportista-form-title">
        {inicial ? 'Editar transportista' : 'Nuevo transportista'}
      </h2>
      <form ref={formulario} onSubmit={enviar} noValidate>
        <fieldset disabled={ocupado} className="transportistas-terceros-form">
          <legend className="sr-only">Datos del transportista</legend>
          {(
            [
              ['razonSocial', 'Razón social', 200],
              ['documentoFiscal', 'Documento fiscal', 30],
              ['pais', 'País', 100],
              ['contacto', 'Contacto', 100],
              ['email', 'Email', 150],
              ['telefono', 'Teléfono', 20],
              ['observaciones', 'Observaciones', 500],
            ] as const
          ).map(([campo, etiqueta, maximo], indice) => (
            <CampoConError
              key={campo}
              etiqueta={etiqueta}
              htmlFor={`transportista-${campo}`}
              error={errores[campo]}
              idError={`transportista-${campo}-error`}
            >
              <input
                autoFocus={indice === 0}
                id={`transportista-${campo}`}
                maxLength={maximo}
                value={datos[campo] ?? ''}
                aria-invalid={!!errores[campo]}
                aria-describedby={errores[campo] ? `transportista-${campo}-error` : undefined}
                onChange={(e) => setDatos({ ...datos, [campo]: e.target.value })}
              />
            </CampoConError>
          ))}
          <CampoConError
            etiqueta="Tipo de servicio"
            htmlFor="transportista-servicio"
            error={errores.tipoServicio}
            idError="transportista-servicio-error"
          >
            <select
              id="transportista-servicio"
              value={datos.tipoServicio}
              onChange={(e) => setDatos({ ...datos, tipoServicio: e.target.value })}
            >
              {['3PL', 'Flete', 'Ambos'].map((tipo) => (
                <option key={tipo}>{tipo}</option>
              ))}
            </select>
          </CampoConError>
          <label>
            <input
              type="checkbox"
              checked={datos.activo}
              onChange={(e) => setDatos({ ...datos, activo: e.target.checked })}
            />{' '}
            Activo
          </label>
        </fieldset>
        {error && (
          <p role="alert" className="field-error">
            {error}
          </p>
        )}
        <div className="transportistas-terceros-actions">
          <button className="primary-button" disabled={ocupado} type="submit">
            {ocupado ? 'Guardando…' : 'Guardar transportista'}
          </button>
          <button className="secondary-button" disabled={ocupado} type="button" onClick={cerrar}>
            Volver al detalle
          </button>
        </div>
      </form>
    </section>
  );
}
