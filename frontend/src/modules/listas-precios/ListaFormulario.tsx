import { useState, type FormEvent } from "react";
import { ApiError } from "../../api/client";
import { CampoConError } from "../../components/CampoConError";
import type {
  GuardarLista,
  ListaDetalle,
  ProductoOpcion,
  SegmentoOpcion,
} from "./tipos";

interface Props {
  lista: ListaDetalle | null;
  productos: ProductoOpcion[];
  segmentos: SegmentoOpcion[];
  guardar: (value: GuardarLista) => Promise<void>;
  cerrar: () => void;
}
export function ListaFormulario({
  lista,
  productos,
  segmentos,
  guardar,
  cerrar,
}: Props) {
  const [codigo, setCodigo] = useState(lista?.codigo ?? "");
  const [nombre, setNombre] = useState(lista?.nombre ?? "");
  const [segmentoId, setSegmentoId] = useState(
    lista?.segmentoId ? String(lista.segmentoId) : "",
  );
  const [prioridad, setPrioridad] = useState(String(lista?.prioridad ?? 20));
  const [inicio, setInicio] = useState(
    lista?.fechaInicio.slice(0, 10) ?? new Date().toISOString().slice(0, 10),
  );
  const [fin, setFin] = useState(lista?.fechaFin?.slice(0, 10) ?? "");
  const [activa, setActiva] = useState(lista?.activa ?? true);
  const [filas, setFilas] = useState(
    () =>
      lista?.items.map((item) => ({
        productoId: item.productoId,
        precio: String(item.precio),
      })) ?? [{ productoId: 0, precio: "" }],
  );
  const [error, setError] = useState("");
  const [ocupado, setOcupado] = useState(false);
  const [errores, setErrores] = useState<Record<string, string>>({});

  function actualizarFila(
    indice: number,
    campo: "productoId" | "precio",
    valor: string,
  ): void {
    setFilas((actual) =>
      actual.map((fila, index) =>
        index === indice
          ? { ...fila, [campo]: campo === "productoId" ? Number(valor) : valor }
          : fila,
      ),
    );
  }
  function validar(): Record<string, string> {
    const siguientes: Record<string, string> = {};
    const code = codigo.trim().toUpperCase();
    if (!code) siguientes.codigo = "El código es obligatorio.";
    else if (!/^[A-Z0-9-]{2,20}$/.test(code))
      siguientes.codigo =
        "El código solo admite letras, números y guiones (2 a 20 caracteres).";
    if (!nombre.trim())
      siguientes.nombre = "El nombre de la lista es obligatorio.";
    else if (nombre.trim().length > 100)
      siguientes.nombre =
        "El nombre de la lista no puede superar 100 caracteres.";
    if (Number(prioridad) < 1 || Number(prioridad) > 999)
      siguientes.prioridad = "La prioridad debe estar entre 1 y 999.";
    if (fin && fin < inicio)
      siguientes.fin = "La fecha de fin no puede ser anterior a la de inicio.";
    if (filas.length === 0 || filas.some((fila) => !fila.productoId))
      siguientes.items = "La lista debe tener al menos un producto.";
    if (filas.some((fila) => !Number(fila.precio) || Number(fila.precio) <= 0))
      siguientes.precio = "El precio debe ser mayor que 0.";
    const ids = filas.map((fila) => fila.productoId).filter(Boolean);
    if (new Set(ids).size !== ids.length)
      siguientes.repetido = `El producto ${ids.find((id, index) => ids.indexOf(id) !== index)} está repetido en la lista.`;
    return siguientes;
  }
  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const validaciones = validar();
    setErrores(validaciones);
    if (Object.keys(validaciones).length) return;
    setOcupado(true);
    setError("");
    try {
      await guardar({
        codigo: codigo.trim().toUpperCase(),
        nombre: nombre.trim(),
        segmentoClienteId: segmentoId ? Number(segmentoId) : undefined,
        prioridad: Number(prioridad),
        fechaInicio: inicio,
        fechaFin: fin || undefined,
        activa,
        items: filas.map((fila) => ({
          productoId: fila.productoId,
          precio: Number(fila.precio),
        })),
      });
    } catch (cause: unknown) {
      setError(
        cause instanceof ApiError
          ? cause.message
          : "No se pudo guardar la lista de precios.",
      );
    } finally {
      setOcupado(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="lista-form-title"
        aria-modal="true"
        className="clientes-dialog listas-form-dialog"
        role="dialog"
      >
        <h2 id="lista-form-title">
          {lista ? "Editar lista de precios" : "Nueva lista de precios"}
        </h2>
        <form noValidate onSubmit={(event) => void enviar(event)}>
          <CampoConError
            etiqueta="Código"
            htmlFor="lista-codigo"
            error={errores.codigo}
            idError="lista-codigo-error"
          >
            <input
              aria-describedby={
                errores.codigo ? "lista-codigo-error" : undefined
              }
              id="lista-codigo"
              onChange={(event) =>
                setCodigo(event.currentTarget.value.toUpperCase())
              }
              value={codigo}
            />
          </CampoConError>
          <CampoConError
            etiqueta="Nombre"
            htmlFor="lista-nombre"
            error={errores.nombre}
            idError="lista-nombre-error"
          >
            <input
              aria-describedby={
                errores.nombre ? "lista-nombre-error" : undefined
              }
              id="lista-nombre"
              onChange={(event) => setNombre(event.currentTarget.value)}
              value={nombre}
            />
          </CampoConError>
          <div className="listas-form-grid">
            <CampoConError
              etiqueta="Segmento (opcional)"
              htmlFor="lista-segmento"
              error=""
              idError="lista-segmento-error"
            >
              <select
                id="lista-segmento"
                onChange={(event) => setSegmentoId(event.currentTarget.value)}
                value={segmentoId}
              >
                <option value="">General</option>
                {segmentos.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.nombre}
                  </option>
                ))}
              </select>
            </CampoConError>
            <CampoConError
              etiqueta="Prioridad"
              htmlFor="lista-prioridad"
              error={errores.prioridad}
              idError="lista-prioridad-error"
            >
              <input
                id="lista-prioridad"
                min="1"
                onChange={(event) => setPrioridad(event.currentTarget.value)}
                type="number"
                value={prioridad}
              />
            </CampoConError>
          </div>
          <div className="listas-form-grid">
            <CampoConError
              etiqueta="Fecha de inicio"
              htmlFor="lista-inicio"
              error=""
              idError="lista-inicio-error"
            >
              <input
                id="lista-inicio"
                onChange={(event) => setInicio(event.currentTarget.value)}
                type="date"
                value={inicio}
              />
            </CampoConError>
            <CampoConError
              etiqueta="Fecha de fin (opcional)"
              htmlFor="lista-fin"
              error={errores.fin}
              idError="lista-fin-error"
            >
              <input
                id="lista-fin"
                onChange={(event) => setFin(event.currentTarget.value)}
                type="date"
                value={fin}
              />
            </CampoConError>
          </div>
          <label className="cotizaciones-expired">
            <input
              checked={activa}
              onChange={(event) => setActiva(event.currentTarget.checked)}
              type="checkbox"
            />
            Activa
          </label>
          <fieldset className="listas-items-field">
            <legend>Productos y precios</legend>
            {filas.map((fila, indice) => (
              <div className="listas-item-row" key={indice}>
                <label htmlFor={`lista-producto-${indice}`}>Producto</label>
                <select
                  id={`lista-producto-${indice}`}
                  onChange={(event) =>
                    actualizarFila(
                      indice,
                      "productoId",
                      event.currentTarget.value,
                    )
                  }
                  value={fila.productoId || ""}
                >
                  <option value="">Selecciona</option>
                  {productos.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.nombre} · Base {item.precioBase.toFixed(2)}
                    </option>
                  ))}
                </select>
                <label htmlFor={`lista-precio-${indice}`}>Precio</label>
                <input
                  id={`lista-precio-${indice}`}
                  min="0"
                  onChange={(event) =>
                    actualizarFila(indice, "precio", event.currentTarget.value)
                  }
                  step="0.01"
                  type="number"
                  value={fila.precio}
                />
              </div>
            ))}
            <button
              className="secondary-button"
              onClick={() =>
                setFilas((actual) => [...actual, { productoId: 0, precio: "" }])
              }
              type="button"
            >
              Añadir producto
            </button>
            {(errores.items || errores.precio || errores.repetido) && (
              <p className="clientes-error" role="alert">
                {errores.items || errores.precio || errores.repetido}
              </p>
            )}
          </fieldset>
          {error && (
            <p className="clientes-error" role="alert">
              {error}
            </p>
          )}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" onClick={cerrar} type="button">
              Cancelar
            </button>
            <button className="primary-button" disabled={ocupado} type="submit">
              {ocupado ? "Guardando…" : "Guardar lista"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
