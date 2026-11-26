import { CampoConError } from "../../components/CampoConError";
import { nuevaLinea, previsualizar } from "./calculos";
import type { LineaValores } from "./tipos";
import type { Errores } from "./validacion";

export function LineasCargaEditor({
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
    requestAnimationFrame(() =>
      document.getElementById(`linea-${linea.clave}-sku`)?.focus(),
    );
  }
  return (
    <section aria-label="Líneas de carga">
      <h3>Picking y packing</h3>
      <p>
        El último bulto parcial conserva las dimensiones y el peso estimado del
        empaque completo.
      </p>
      {errores.lineas && (
        <p role="alert" className="field-error">
          {errores.lineas}
        </p>
      )}
      {lineas.map((linea, indice) => {
        const total = previsualizar([linea]);
        return (
          <fieldset key={linea.clave} className="consolidacion-carga-linea">
            <legend>Línea {indice + 1}</legend>
            <div className="consolidacion-carga-editor">
              {(
                [
                  ["sku", "SKU", 40],
                  ["descripcion", "Descripción", 200],
                  ["cantidad", "Cantidad prevista", undefined],
                  ["cantidadRecogida", "Cantidad recogida", undefined],
                  ["unidadesPorBulto", "Unidades por bulto", undefined],
                  ["largoCm", "Largo (cm)", undefined],
                  ["anchoCm", "Ancho (cm)", undefined],
                  ["altoCm", "Alto (cm)", undefined],
                  ["pesoBultoKg", "Peso por bulto (kg)", undefined],
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
                      inputMode={
                        campo === "sku" || campo === "descripcion"
                          ? undefined
                          : campo === "cantidad" ||
                              campo === "cantidadRecogida" ||
                              campo === "unidadesPorBulto"
                            ? "numeric"
                            : "decimal"
                      }
                      aria-invalid={!!error}
                      aria-describedby={error ? `${id}-error` : undefined}
                      onChange={(e) =>
                        cambiar(
                          lineas.map((item, i) =>
                            i === indice
                              ? { ...item, [campo]: e.target.value }
                              : item,
                          ),
                        )
                      }
                    />
                  </CampoConError>
                );
              })}
            </div>
            <p aria-live="polite">
              {total
                ? `Estimación: ${total.bultos}\u00a0bultos · ${total.volumenM3}\u00a0m³ · ${total.pesoKg}\u00a0kg`
                : "Completa los datos válidos para calcular la estimación."}
            </p>
            <button
              className="secondary-button"
              type="button"
              disabled={lineas.length === 1}
              onClick={() => {
                cambiar(lineas.filter((_, i) => i !== indice));
                requestAnimationFrame(() =>
                  document
                    .getElementById(
                      `linea-${lineas[indice === 0 ? 1 : indice - 1]?.clave}-sku`,
                    )
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
