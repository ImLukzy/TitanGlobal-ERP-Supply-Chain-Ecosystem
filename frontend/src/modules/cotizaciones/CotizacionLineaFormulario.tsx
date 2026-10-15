import { CampoConError } from '../../components/CampoConError';
import { formateadorMoneda } from '../../lib/formato';

export interface LineaFormulario {
  productoId: number | '';
  cantidad: string;
  precioUnitario: string;
  descuentoPorcentaje: string;
}

interface Props {
  indice: number;
  linea: LineaFormulario;
  productos: { id: number; nombre: string; precio: number }[];
  errores: Record<string, string>;
  importe: number;
  cambiarLinea: (indice: number, cambio: Partial<LineaFormulario>) => void;
  quitar: (indice: number) => void;
}

export function CotizacionLineaFormulario({
  indice,
  linea,
  productos,
  errores,
  importe,
  cambiarLinea,
  quitar,
}: Props) {
  const idProducto = `cotizacion-producto-${indice}`;
  const idCantidad = `cotizacion-cantidad-${indice}`;
  const idPrecio = `cotizacion-precio-${indice}`;
  const idDescuento = `cotizacion-descuento-${indice}`;
  const errorProducto = errores[`producto-${indice}`];
  const errorCantidad = errores[`cantidad-${indice}`];
  const errorPrecio = errores[`precio-${indice}`];
  const errorDescuento = errores[`descuento-${indice}`];

  function seleccionarProducto(id: number | ''): void {
    const producto = productos.find((item) => item.id === id);
    cambiarLinea(indice, {
      productoId: id,
      precioUnitario: producto ? String(producto.precio) : linea.precioUnitario,
    });
  }

  return (
    <fieldset className="solicitud-line cotizacion-line">
      <legend>Línea {indice + 1}</legend>
      <CampoConError
        etiqueta="Producto"
        htmlFor={idProducto}
        error={errorProducto}
        idError={`${idProducto}-error`}
      >
        <select
          id={idProducto}
          aria-describedby={errorProducto ? `${idProducto}-error` : undefined}
          aria-invalid={Boolean(errorProducto)}
          value={linea.productoId}
          onChange={(event) => {
            const id = event.currentTarget.value
              ? Number(event.currentTarget.value)
              : '';
            seleccionarProducto(id);
          }}
        >
          <option value="">Seleccionar producto</option>
          {productos.map((producto) => (
            <option key={producto.id} value={producto.id}>
              {producto.nombre}
            </option>
          ))}
        </select>
      </CampoConError>
      <CampoConError
        etiqueta="Cantidad"
        htmlFor={idCantidad}
        error={errorCantidad}
        idError={`${idCantidad}-error`}
      >
        <input
          id={idCantidad}
          aria-describedby={errorCantidad ? `${idCantidad}-error` : undefined}
          aria-invalid={Boolean(errorCantidad)}
          min="1"
          type="number"
          value={linea.cantidad}
          onChange={(event) =>
            cambiarLinea(indice, { cantidad: event.currentTarget.value })
          }
        />
      </CampoConError>
      <CampoConError
        etiqueta="Precio unitario"
        htmlFor={idPrecio}
        error={errorPrecio}
        idError={`${idPrecio}-error`}
      >
        <input
          id={idPrecio}
          aria-describedby={errorPrecio ? `${idPrecio}-error` : undefined}
          aria-invalid={Boolean(errorPrecio)}
          min="0.01"
          step="0.01"
          type="number"
          value={linea.precioUnitario}
          onChange={(event) =>
            cambiarLinea(indice, { precioUnitario: event.currentTarget.value })
          }
        />
      </CampoConError>
      <CampoConError
        etiqueta="Descuento %"
        htmlFor={idDescuento}
        error={errorDescuento}
        idError={`${idDescuento}-error`}
      >
        <input
          id={idDescuento}
          aria-describedby={errorDescuento ? `${idDescuento}-error` : undefined}
          aria-invalid={Boolean(errorDescuento)}
          max="100"
          min="0"
          step="0.01"
          type="number"
          value={linea.descuentoPorcentaje}
          onChange={(event) =>
            cambiarLinea(indice, {
              descuentoPorcentaje: event.currentTarget.value,
            })
          }
        />
      </CampoConError>
      <p className="solicitud-line-subtotal">
        Importe <strong>{formateadorMoneda.format(importe)}</strong>
      </p>
      <button
        aria-label={`Quitar línea ${indice + 1}`}
        className="danger-button solicitud-remove-line"
        onClick={() => quitar(indice)}
        type="button"
      >
        Quitar
      </button>
    </fieldset>
  );
}
