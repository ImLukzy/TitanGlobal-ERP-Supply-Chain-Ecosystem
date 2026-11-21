import { CampoConError } from '../../components/CampoConError';
import { nuevaLinea } from './validacion';
import type { LineaValores } from './tipos';
import type { Errores } from './validacion';

export function LineasGuiaEditor({
  lineas,
  errores,
  cambiar,
}: {
  lineas: LineaValores[];
  errores: Errores;
  cambiar: (lineas: LineaValores[]) => void;
}) {
  function agregar() {
    const linea = nuevaLinea();
    cambiar([...lineas, linea]);
    requestAnimationFrame(() => document.getElementById(`linea-${linea.clave}-sku`)?.focus());
  }
  return (
    <section aria-label="Líneas de guia">
      <h3>Mercancías declaradas</h3>
      {errores.lineas && (
        <p role="alert" className="field-error">
          {errores.lineas}
        </p>
      )}
      {lineas.map((linea, indice) => {
        return (
          <fieldset key={linea.clave} className="guias-remision-linea">
            <legend>Línea {indice + 1}</legend>
            <div className="guias-remision-editor">
              {(
                [
                  ['sku', 'SKU', 40],
                  ['descripcion', 'Descripción', 200],
                  ['unidadMedida', 'Unidad de medida', 10],
                  ['cantidad', 'Cantidad', undefined],
                ] as const
              ).map(([campo, etiqueta, maximo]) => {
                const id = `linea-${linea.clave}-${campo}`;
                const error = errores[`${linea.clave}-${campo}`];
                return (
                  <CampoConError
                    key={campo}
                    etiqueta={etiqueta}
                    htmlFor={id}
                    error={error}
                    idError={`${id}-error`}
                  >
                    <input
                      id={id}
                      value={linea[campo]}
                      maxLength={maximo}
                      inputMode={campo === 'cantidad' ? 'decimal' : undefined}
                      aria-invalid={!!error}
                      aria-describedby={error ? `${id}-error` : undefined}
                      onChange={(e) =>
                        cambiar(
                          lineas.map((item, i) =>
                            i === indice ? { ...item, [campo]: e.target.value } : item,
                          ),
                        )
                      }
                    />
                  </CampoConError>
                );
              })}
            </div>
            <button
              className="secondary-button"
              type="button"
              disabled={lineas.length === 1}
              onClick={() => {
                cambiar(lineas.filter((_, i) => i !== indice));
                requestAnimationFrame(() =>
                  document
                    .getElementById(`linea-${lineas[indice === 0 ? 1 : indice - 1]?.clave}-sku`)
                    ?.focus(),
                );
              }}
            >
              Quitar línea {indice + 1}
            </button>
          </fieldset>
        );
      })}
      <button
        className="secondary-button"
        type="button"
        disabled={lineas.length >= 100}
        onClick={agregar}
      >
        Agregar línea
      </button>
    </section>
  );
}
