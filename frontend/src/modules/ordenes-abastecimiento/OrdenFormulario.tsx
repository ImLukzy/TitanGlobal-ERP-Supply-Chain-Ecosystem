import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { formateadorMoneda } from '../../lib/formato';
import type {
  GuardarOrdenAbastecimientoRequest,
  LineaOrdenAbastecimientoRequest,
  OrdenAbastecimientoDetalle,
  ProductoOrdenOpcion,
  ProveedorOrdenOpcion,
} from './tipos';

interface OrdenFormularioProps {
  orden: OrdenAbastecimientoDetalle | null;
  proveedores: ProveedorOrdenOpcion[];
  productos: ProductoOrdenOpcion[];
  cancelar: () => void;
  guardar: (request: GuardarOrdenAbastecimientoRequest) => Promise<void>;
}

interface LineaFormulario {
  productoId: number | '';
  cantidad: string;
  precioUnitario: string;
}

interface ErroresFormulario {
  proveedor?: string;
  entrega?: string;
  observaciones?: string;
  lineas?: string;
  campos: Record<string, string>;
}

function fechaHoyUtc(): string {
  return new Date().toISOString().slice(0, 10);
}

function lineaVacia(): LineaFormulario {
  return { productoId: '', cantidad: '1', precioUnitario: '' };
}

function crearLineas(orden: OrdenAbastecimientoDetalle | null): LineaFormulario[] {
  if (orden === null || orden.lineas.length === 0) {
    return [lineaVacia()];
  }
  return orden.lineas.map((linea) => ({
    productoId: linea.productoId,
    cantidad: String(linea.cantidad),
    precioUnitario: String(linea.precioUnitario),
  }));
}

export function OrdenFormulario({ orden, proveedores, productos, cancelar, guardar }: OrdenFormularioProps) {
  const hoy = fechaHoyUtc();
  const [proveedorId, setProveedorId] = useState<number | ''>(orden?.proveedorId ?? '');
  const [entrega, setEntrega] = useState(orden?.fechaEntregaEstimada.slice(0, 10) ?? hoy);
  const [observaciones, setObservaciones] = useState(orden?.observaciones ?? '');
  const [lineas, setLineas] = useState<LineaFormulario[]>(() => crearLineas(orden));
  const [errores, setErrores] = useState<ErroresFormulario>({ campos: {} });
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);
  const total = lineas.reduce((suma, linea) => suma + Number(linea.cantidad || 0) * Number(linea.precioUnitario || 0), 0);

  function cambiarLinea(indice: number, cambio: Partial<LineaFormulario>): void {
    setLineas((actuales) => actuales.map((linea, actual) => actual === indice ? { ...linea, ...cambio } : linea));
  }

  function validar(): ErroresFormulario {
    const nuevos: ErroresFormulario = { campos: {} };
    if (proveedorId === '') {
      nuevos.proveedor = 'Indica el proveedor de la orden.';
    }
    if (entrega === '') {
      nuevos.entrega = 'La fecha de entrega estimada es obligatoria.';
    } else if (entrega < hoy) {
      nuevos.entrega = 'La fecha de entrega estimada no puede ser anterior a hoy.';
    }
    if (observaciones.trim().length > 500) {
      nuevos.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
    }
    if (lineas.length === 0) {
      nuevos.lineas = 'La orden debe tener al menos una línea.';
      return nuevos;
    }
    const productosVistos = new Set<number>();
    lineas.forEach((linea, indice) => {
      if (linea.productoId === '') {
        nuevos.campos[`producto-${indice}`] = 'Selecciona un producto.';
      } else if (productosVistos.has(linea.productoId)) {
        nuevos.campos[`producto-${indice}`] = `El producto ${linea.productoId} está repetido en la orden.`;
      }
      if (linea.productoId !== '') {
        productosVistos.add(linea.productoId);
      }
      if (Number(linea.cantidad) <= 0 || linea.cantidad.trim() === '') {
        nuevos.campos[`cantidad-${indice}`] = 'La cantidad debe ser mayor que 0.';
      }
      if (Number(linea.precioUnitario) <= 0 || linea.precioUnitario.trim() === '') {
        nuevos.campos[`precio-${indice}`] = 'El precio unitario debe ser mayor que 0.';
      }
    });
    return nuevos;
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const nuevosErrores = validar();
    setErrores(nuevosErrores);
    if (nuevosErrores.proveedor || nuevosErrores.entrega || nuevosErrores.observaciones || nuevosErrores.lineas || Object.keys(nuevosErrores.campos).length > 0) {
      return;
    }
    const request: GuardarOrdenAbastecimientoRequest = {
      proveedorId: Number(proveedorId),
      fechaEntregaEstimada: entrega,
      lineas: lineas.map((linea): LineaOrdenAbastecimientoRequest => ({
        productoId: Number(linea.productoId),
        cantidad: Number(linea.cantidad),
        precioUnitario: Number(linea.precioUnitario),
      })),
    };
    if (observaciones.trim() !== '') {
      request.observaciones = observaciones.trim();
    }
    setErrorApi('');
    setGuardando(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo guardar la orden.');
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="orden-form-title" className="ordenes-panel orden-form">
      <div className="ordenes-panel-heading">
        <div>
          <span className="page-eyebrow">ORDEN DE ABASTECIMIENTO</span>
          <h2 id="orden-form-title">{orden === null ? 'Nueva orden' : `Editar ${orden.numero}`}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <div className="orden-form-fields">
          <CampoConError etiqueta="Proveedor" htmlFor="orden-proveedor" error={errores.proveedor} idError="orden-proveedor-error">
            <select
              id="orden-proveedor"
              aria-describedby={errores.proveedor ? 'orden-proveedor-error' : undefined}
              aria-invalid={Boolean(errores.proveedor)}
              onChange={(event) => setProveedorId(event.currentTarget.value === '' ? '' : Number(event.currentTarget.value))}
              value={proveedorId}
            >
              <option value="">Selecciona proveedor</option>
              {proveedores.map((proveedor) => <option key={proveedor.id} value={proveedor.id}>{proveedor.razonSocial}</option>)}
            </select>
          </CampoConError>
          <CampoConError etiqueta="Fecha de entrega estimada" htmlFor="orden-entrega" error={errores.entrega} idError="orden-entrega-error">
            <input
              id="orden-entrega"
              aria-describedby={errores.entrega ? 'orden-entrega-error' : undefined}
              aria-invalid={Boolean(errores.entrega)}
              min={hoy}
              onChange={(event) => setEntrega(event.currentTarget.value)}
              type="date"
              value={entrega}
            />
          </CampoConError>
          <CampoConError etiqueta="Observaciones" htmlFor="orden-observaciones" error={errores.observaciones} idError="orden-observaciones-error">
            <textarea
              id="orden-observaciones"
              aria-describedby={errores.observaciones ? 'orden-observaciones-error' : undefined}
              aria-invalid={Boolean(errores.observaciones)}
              maxLength={520}
              onChange={(event) => setObservaciones(event.currentTarget.value)}
              value={observaciones}
            />
          </CampoConError>
        </div>
        <div className="orden-lines-heading">
          <h3>Líneas de la orden</h3>
          <button className="secondary-button" onClick={() => setLineas((actuales) => [...actuales, lineaVacia()])} type="button">Añadir línea</button>
        </div>
        {errores.lineas && <p className="orden-field-error" role="alert">{errores.lineas}</p>}
        <div className="orden-lines">
          {lineas.map((linea, indice) => {
            const subtotal = Number(linea.cantidad || 0) * Number(linea.precioUnitario || 0);
            return (
              <fieldset className="orden-line" key={indice}>
                <legend>Línea {indice + 1}</legend>
                <CampoConError etiqueta="Producto" htmlFor={`orden-linea-${indice}-producto`} error={errores.campos[`producto-${indice}`]} idError={`orden-linea-${indice}-producto-error`}>
                  <select
                    id={`orden-linea-${indice}-producto`}
                    aria-describedby={errores.campos[`producto-${indice}`] ? `orden-linea-${indice}-producto-error` : undefined}
                    aria-invalid={Boolean(errores.campos[`producto-${indice}`])}
                    onChange={(event) => {
                      const id = event.currentTarget.value === '' ? '' : Number(event.currentTarget.value);
                      const elegido = productos.find((producto) => producto.id === id);
                      cambiarLinea(indice, { productoId: id, precioUnitario: elegido ? String(elegido.precio) : linea.precioUnitario });
                    }}
                    value={linea.productoId}
                  >
                    <option value="">Selecciona producto</option>
                    {productos.map((producto) => <option key={producto.id} value={producto.id}>{producto.nombre}</option>)}
                  </select>
                </CampoConError>
                <CampoConError etiqueta="Cantidad" htmlFor={`orden-linea-${indice}-cantidad`} error={errores.campos[`cantidad-${indice}`]} idError={`orden-linea-${indice}-cantidad-error`}>
                  <input
                    id={`orden-linea-${indice}-cantidad`}
                    aria-describedby={errores.campos[`cantidad-${indice}`] ? `orden-linea-${indice}-cantidad-error` : undefined}
                    aria-invalid={Boolean(errores.campos[`cantidad-${indice}`])}
                    min="1"
                    onChange={(event) => cambiarLinea(indice, { cantidad: event.currentTarget.value })}
                    type="number"
                    value={linea.cantidad}
                  />
                </CampoConError>
                <CampoConError etiqueta="Precio unitario" htmlFor={`orden-linea-${indice}-precio`} error={errores.campos[`precio-${indice}`]} idError={`orden-linea-${indice}-precio-error`}>
                  <input
                    id={`orden-linea-${indice}-precio`}
                    aria-describedby={errores.campos[`precio-${indice}`] ? `orden-linea-${indice}-precio-error` : undefined}
                    aria-invalid={Boolean(errores.campos[`precio-${indice}`])}
                    min="0.01"
                    onChange={(event) => cambiarLinea(indice, { precioUnitario: event.currentTarget.value })}
                    step="0.01"
                    type="number"
                    value={linea.precioUnitario}
                  />
                </CampoConError>
                <p className="orden-line-subtotal">Subtotal <strong>{formateadorMoneda.format(subtotal)}</strong></p>
                <button
                  aria-label={`Quitar línea ${indice + 1}`}
                  className="danger-button orden-remove-line"
                  onClick={() => setLineas((actuales) => actuales.filter((_, actual) => actual !== indice))}
                  type="button"
                >
                  Quitar
                </button>
              </fieldset>
            );
          })}
        </div>
        <p className="orden-total">Total estimado: <strong>{formateadorMoneda.format(total)}</strong></p>
        {errorApi && <p className="orden-error" role="alert">{errorApi}</p>}
        <div className="orden-form-actions">
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar orden'}</button>
        </div>
      </form>
    </section>
  );
}
