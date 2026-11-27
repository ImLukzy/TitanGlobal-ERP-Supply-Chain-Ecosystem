import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { GuardarRuta, RutaDetalle } from './tipos';
import { validarRuta, type ErroresRuta, type ValoresRuta } from './validacion';

export function RutaFormulario({
  inicial,
  ocupado,
  error,
  guardar,
  cerrar,
}: {
  inicial: RutaDetalle | null;
  ocupado: boolean;
  error: string;
  guardar: (datos: GuardarRuta) => void;
  cerrar: () => void;
}) {
  const [datos, setDatos] = useState<ValoresRuta>(
    inicial
      ? { ...inicial, observaciones: inicial.observaciones ?? '' }
      : {
          codigo: '',
          nombre: '',
          zona: '',
          origen: '',
          destino: '',
          distanciaKm: '',
          peajeEstimado: '0',
          moneda: 'PEN',
          activo: true,
          observaciones: '',
        },
  );
  const [errores, setErrores] = useState<ErroresRuta>({});
  const formulario = useRef<HTMLFormElement>(null);
  function enviar(evento: FormEvent) {
    evento.preventDefault();
    if (ocupado) return;
    const fallos = validarRuta(datos);
    setErrores(fallos);
    if (Object.keys(fallos).length) {
      requestAnimationFrame(() =>
        formulario.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    guardar({
      codigo: datos.codigo.trim(),
      nombre: datos.nombre.trim(),
      zona: datos.zona.trim(),
      origen: datos.origen.trim(),
      destino: datos.destino.trim(),
      distanciaKm: Number(datos.distanciaKm),
      peajeEstimado: Number(datos.peajeEstimado),
      moneda: datos.moneda.trim().toUpperCase(),
      activo: datos.activo,
      observaciones: datos.observaciones.trim() || undefined,
    });
  }
  return (
    <section className="rutas-despacho-card" aria-labelledby="ruta-form-title">
      <h2 id="ruta-form-title">{inicial ? 'Editar ruta' : 'Nueva ruta'}</h2>
      <form ref={formulario} onSubmit={enviar} noValidate>
        <fieldset disabled={ocupado} className="rutas-despacho-form">
          <legend className="sr-only">Datos de la ruta</legend>
          {(
            [
              ['codigo', 'Código', 20],
              ['nombre', 'Nombre', 100],
              ['zona', 'Zona de la ruta', 100],
              ['origen', 'Origen', 120],
              ['destino', 'Destino', 120],
              ['distanciaKm', 'Distancia (km)', undefined],
              ['peajeEstimado', 'Peaje estimado', undefined],
              ['moneda', 'Moneda', 3],
              ['observaciones', 'Observaciones', 500],
            ] as const
          ).map(([campo, etiqueta, maximo], indice) => (
            <CampoConError
              key={campo}
              etiqueta={etiqueta}
              htmlFor={`ruta-${campo}`}
              error={errores[campo]}
              idError={`ruta-${campo}-error`}
            >
              <input
                autoFocus={indice === 0}
                id={`ruta-${campo}`}
                value={datos[campo]}
                maxLength={maximo}
                inputMode={
                  campo === 'distanciaKm' || campo === 'peajeEstimado' ? 'decimal' : undefined
                }
                aria-invalid={!!errores[campo]}
                aria-describedby={errores[campo] ? `ruta-${campo}-error` : undefined}
                onChange={(e) => setDatos({ ...datos, [campo]: e.target.value })}
              />
            </CampoConError>
          ))}
          <label>
            <input
              type="checkbox"
              checked={datos.activo}
              onChange={(e) => setDatos({ ...datos, activo: e.target.checked })}
            />{' '}
            Activa
          </label>
        </fieldset>
        {error && (
          <p role="alert" className="field-error">
            {error}
          </p>
        )}
        <div className="rutas-despacho-actions">
          <button className="primary-button" type="submit" disabled={ocupado}>
            {ocupado ? 'Guardando…' : 'Guardar ruta'}
          </button>
          <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
            Volver al detalle
          </button>
        </div>
      </form>
    </section>
  );
}
