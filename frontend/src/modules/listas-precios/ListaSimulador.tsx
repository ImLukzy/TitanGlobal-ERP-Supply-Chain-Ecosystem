import { useState } from "react";
import { ApiError } from "../../api/client";
import { CampoConError } from "../../components/CampoConError";
import { formateadorMoneda } from "../../lib/formato";
import { resolverPrecio } from "./api";
import type { ClienteOpcion, PrecioResuelto, ProductoOpcion } from "./tipos";

function formatearDiferencia(precio: number, precioBase: number): string {
  if (!precioBase) return "0.00 %";
  const diferencia = ((precio - precioBase) / precioBase) * 100;
  const signo = diferencia < 0 ? "−" : "";
  return `${signo}${Math.abs(diferencia).toFixed(2)} %`;
}

export function ListaSimulador({
  productos,
  clientes,
}: {
  productos: ProductoOpcion[];
  clientes: ClienteOpcion[];
}) {
  const [productoId, setProductoId] = useState("");
  const [clienteId, setClienteId] = useState("");
  const [fecha, setFecha] = useState(new Date().toISOString().slice(0, 10));
  const [resultado, setResultado] = useState<PrecioResuelto | null>(null);
  const [error, setError] = useState("");
  const [ocupado, setOcupado] = useState(false);

  async function calcular(): Promise<void> {
    setError("");
    setResultado(null);
    if (!productoId || !clienteId) {
      setError("Selecciona un producto y un cliente.");
      return;
    }
    setOcupado(true);
    try {
      setResultado(
        await resolverPrecio(Number(productoId), Number(clienteId), fecha),
      );
    } catch (cause: unknown) {
      setError(
        cause instanceof ApiError
          ? cause.message
          : "No se pudo calcular el precio.",
      );
    } finally {
      setOcupado(false);
    }
  }

  return (
    <section
      aria-labelledby="listas-simulador-title"
      className="listas-simulator"
    >
      <div className="clientes-panel-heading">
        <div>
          <span className="page-eyebrow">CONSULTA</span>
          <h2 id="listas-simulador-title">Simulador de precio</h2>
        </div>
      </div>
      <div className="listas-simulator-fields">
        <CampoConError
          etiqueta="Producto"
          htmlFor="precio-producto"
          error=""
          idError="precio-producto-error"
        >
          <select
            id="precio-producto"
            onChange={(event) => setProductoId(event.currentTarget.value)}
            value={productoId}
          >
            <option value="">Selecciona un producto</option>
            {productos.map((item) => (
              <option key={item.id} value={item.id}>
                {item.nombre}
              </option>
            ))}
          </select>
        </CampoConError>
        <CampoConError
          etiqueta="Cliente"
          htmlFor="precio-cliente"
          error=""
          idError="precio-cliente-error"
        >
          <select
            id="precio-cliente"
            onChange={(event) => setClienteId(event.currentTarget.value)}
            value={clienteId}
          >
            <option value="">Selecciona un cliente</option>
            {clientes.map((item) => (
              <option key={item.id} value={item.id}>
                {item.nombre}
              </option>
            ))}
          </select>
        </CampoConError>
        <CampoConError
          etiqueta="Fecha"
          htmlFor="precio-fecha"
          error=""
          idError="precio-fecha-error"
        >
          <input
            id="precio-fecha"
            onChange={(event) => setFecha(event.currentTarget.value)}
            type="date"
            value={fecha}
          />
        </CampoConError>
        <button
          className="primary-button"
          disabled={ocupado}
          onClick={() => void calcular()}
          type="button"
        >
          {ocupado ? "Calculando…" : "Calcular"}
        </button>
      </div>
      {error && (
        <p className="clientes-error" role="alert">
          {error}
        </p>
      )}
      {resultado && (
        <article aria-live="polite" className="listas-price-result">
          <h3>{resultado.productoNombre}</h3>
          <strong>{formateadorMoneda.format(resultado.precio)}</strong>
          <dl>
            <div>
              <dt>Precio base</dt>
              <dd>{formateadorMoneda.format(resultado.precioBase)}</dd>
            </div>
            <div>
              <dt>Diferencia</dt>
              <dd>
                {formatearDiferencia(
                  resultado.precio,
                  resultado.precioBase,
                )}
              </dd>
            </div>
            <div>
              <dt>Origen</dt>
              <dd>
                {resultado.origen === "Segmento"
                  ? `Lista ${resultado.listaCodigo} (segmento ${resultado.segmentoCodigo})`
                  : resultado.origen === "General"
                    ? `Lista ${resultado.listaCodigo}`
                    : "Precio base"}
              </dd>
            </div>
          </dl>
        </article>
      )}
    </section>
  );
}
