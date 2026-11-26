import { useRef, useState, type FormEvent } from "react";
import { CampoConError } from "../../components/CampoConError";
import { nuevaLinea, previsualizar } from "./calculos";
import { LineasCargaEditor } from "./LineasCargaEditor";
import type {
  CargaDetalle,
  GuardarCarga,
  LineaRequest,
  ValoresCarga,
} from "./tipos";
import { validarCarga, type Errores } from "./validacion";

export function ConsolidacionFormulario({
  inicial,
  ocupado,
  error,
  guardar,
  cerrar,
}: {
  inicial: CargaDetalle | null;
  ocupado: boolean;
  error: string;
  guardar: (datos: GuardarCarga) => void;
  cerrar: () => void;
}) {
  const [datos, setDatos] = useState<ValoresCarga>(
    inicial
      ? {
          codigo: inicial.codigo,
          nombre: inicial.nombre,
          observaciones: inicial.observaciones ?? "",
          lineas: inicial.lineas.map((l) => ({
            clave: `existente-${l.id}`,
            sku: l.sku,
            descripcion: l.descripcion,
            cantidad: String(l.cantidad),
            cantidadRecogida: String(l.cantidadRecogida),
            unidadesPorBulto: String(l.unidadesPorBulto),
            largoCm: l.largoCm,
            anchoCm: l.anchoCm,
            altoCm: l.altoCm,
            pesoBultoKg: l.pesoBultoKg,
          })),
        }
      : { codigo: "", nombre: "", observaciones: "", lineas: [nuevaLinea()] },
  );
  const [errores, setErrores] = useState<Errores>({});
  const ref = useRef<HTMLFormElement>(null);
  const total = previsualizar(datos.lineas);
  function enviar(evento: FormEvent) {
    evento.preventDefault();
    if (ocupado) return;
    const fallos = validarCarga(datos);
    setErrores(fallos);
    if (Object.keys(fallos).length) {
      requestAnimationFrame(() =>
        ref.current
          ?.querySelector<HTMLElement>('[aria-invalid="true"]')
          ?.focus(),
      );
      return;
    }
    const lineas: LineaRequest[] = datos.lineas.map((l) => ({
      sku: l.sku.trim(),
      descripcion: l.descripcion.trim(),
      cantidad: Number(l.cantidad),
      cantidadRecogida: Number(l.cantidadRecogida),
      unidadesPorBulto: Number(l.unidadesPorBulto),
      largoCm: Number(l.largoCm),
      anchoCm: Number(l.anchoCm),
      altoCm: Number(l.altoCm),
      pesoBultoKg: Number(l.pesoBultoKg),
    }));
    guardar({
      codigo: datos.codigo.trim(),
      nombre: datos.nombre.trim(),
      observaciones: datos.observaciones.trim() || undefined,
      lineas,
    });
  }
  return (
    <section
      className="consolidacion-carga-card"
      aria-labelledby="carga-form-title"
    >
      <h2 id="carga-form-title">
        {inicial ? "Editar consolidación" : "Nueva consolidación"}
      </h2>
      <form ref={ref} onSubmit={enviar} noValidate>
        <fieldset disabled={ocupado} className="consolidacion-carga-form">
          <legend className="sr-only">Preparación de carga</legend>
          {(
            [
              ["codigo", "Código", 20],
              ["nombre", "Nombre", 100],
              ["observaciones", "Observaciones", 500],
            ] as const
          ).map(([campo, label, max], i) => (
            <CampoConError
              key={campo}
              etiqueta={label}
              htmlFor={`carga-${campo}`}
              error={errores[campo]}
              idError={`carga-${campo}-error`}
            >
              <input
                autoFocus={i === 0}
                id={`carga-${campo}`}
                value={datos[campo]}
                maxLength={max}
                aria-invalid={!!errores[campo]}
                aria-describedby={
                  errores[campo] ? `carga-${campo}-error` : undefined
                }
                onChange={(e) =>
                  setDatos({ ...datos, [campo]: e.target.value })
                }
              />
            </CampoConError>
          ))}
          <LineasCargaEditor
            lineas={datos.lineas}
            errores={errores}
            cambiar={(lineas) => {
              setDatos({ ...datos, lineas });
              setErrores({});
            }}
          />
          <p className="consolidacion-carga-totales" role="status">
            {total
              ? `Total estimado: ${total.bultos}\u00a0bultos · ${total.volumenM3}\u00a0m³ · ${total.pesoKg}\u00a0kg`
              : "Revisa las líneas para calcular el total estimado."}
          </p>
        </fieldset>
        {error && (
          <p role="alert" className="field-error">
            {error}
          </p>
        )}
        <div className="consolidacion-carga-actions">
          <button className="primary-button" disabled={ocupado} type="submit">
            {ocupado ? "Guardando…" : "Guardar borrador"}
          </button>
          <button
            className="secondary-button"
            disabled={ocupado}
            type="button"
            onClick={cerrar}
          >
            Volver al detalle
          </button>
        </div>
      </form>
    </section>
  );
}
