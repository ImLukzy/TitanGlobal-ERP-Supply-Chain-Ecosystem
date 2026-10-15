import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { formateadorMoneda } from '../../lib/formato';
import type {
  GuardarSolicitudRequest,
  LineaSolicitudRequest,
  ProductoOpcion,
  ProveedorOpcion,
  SolicitudDetalle,
} from './tipos';

interface SolicitudFormularioProps {
  solicitud: SolicitudDetalle | null;
  proveedores: ProveedorOpcion[];
  productos: ProductoOpcion[];
  cancelar: () => void;
  guardar: (request: GuardarSolicitudRequest) => Promise<void>;
}

interface LineaFormulario {
  productoId: number | '';
  cantidad: string;
  precioEstimado: string;
}

interface ErroresFormulario {
  solicitante?: string;
  observaciones?: string;
  lineas?: string;
  campos: Record<string, string>;
}

function lineaVacia(): LineaFormulario {
  return { productoId: '', cantidad: '1', precioEstimado: '' };
}

function crearLineas(solicitud: SolicitudDetalle | null): LineaFormulario[] {
  if (solicitud === null || solicitud.lineas.length === 0) {
    return [lineaVacia()];
  }
  return solicitud.lineas.map((linea) => ({
    productoId: linea.productoId,
    cantidad: String(linea.cantidad),
    precioEstimado: String(linea.precioEstimado),
  }));
}

export function SolicitudFormulario({
  solicitud,
  proveedores,
  productos,
  cancelar,
  guardar,
}: SolicitudFormularioProps) {
  const [solicitante, setSolicitante] = useState(solicitud?.solicitante ?? '');
  const [proveedorId, setProveedorId] = useState<number | ''>(solicitud?.proveedorId ?? '');
  const [observaciones, setObservaciones] = useState(solicitud?.observaciones ?? '');
  const [lineas, setLineas] = useState<LineaFormulario[]>(() => crearLineas(solicitud));
  const [errores, setErrores] = useState<ErroresFormulario>({ campos: {} });
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);
  const total = lineas.reduce((suma, linea) => suma + Number(linea.cantidad || 0) * Number(linea.precioEstimado || 0), 0);

  function cambiarLinea(indice: number, cambio: Partial<LineaFormulario>): void {
    setLineas((actuales) => actuales.map((linea, actual) => actual === indice ? { ...linea, ...cambio } : linea));
  }

  function validar(): ErroresFormulario {
    const nuevos: ErroresFormulario = { campos: {} };
    const nombre = solicitante.trim();
    if (nombre === '') {
      nuevos.solicitante = 'El solicitante es obligatorio.';
    } else if (nombre.length > 100) {
      nuevos.solicitante = 'El solicitante no puede superar 100 caracteres.';
    }
    if (observaciones.trim().length > 500) {
      nuevos.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
    }
    if (lineas.length === 0) {
      nuevos.lineas = 'La solicitud debe tener al menos una línea.';
      return nuevos;
    }
    const productosVistos = new Set<number>();
    lineas.forEach((linea, indice) => {
      if (linea.productoId === '') {
        nuevos.campos[`producto-${indice}`] = 'Selecciona un producto.';
      } else if (productosVistos.has(linea.productoId)) {
        nuevos.campos[`producto-${indice}`] = `El producto ${linea.productoId} está repetido en la solicitud.`;
      }
      if (linea.productoId !== '') {
        productosVistos.add(linea.productoId);
      }
      if (Number(linea.cantidad) <= 0 || linea.cantidad.trim() === '') {
        nuevos.campos[`cantidad-${indice}`] = 'La cantidad debe ser mayor que 0.';
      }
      if (Number(linea.precioEstimado) < 0) {
        nuevos.campos[`precio-${indice}`] = 'El precio estimado no puede ser negativo.';
      }
    });
    return nuevos;
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const nuevosErrores = validar();
    setErrores(nuevosErrores);
    if (nuevosErrores.solicitante || nuevosErrores.observaciones || nuevosErrores.lineas || Object.keys(nuevosErrores.campos).length > 0) {
      return;
    }
    const request: GuardarSolicitudRequest = {
      solicitante: solicitante.trim(),
      lineas: lineas.map((linea): LineaSolicitudRequest => ({
        productoId: Number(linea.productoId),
        cantidad: Number(linea.cantidad),
        precioEstimado: Number(linea.precioEstimado || 0),
      })),
    };
    if (proveedorId !== '') {
      request.proveedorId = proveedorId;
    }
    if (observaciones.trim() !== '') {
      request.observaciones = observaciones.trim();
    }
    setErrorApi('');
    setGuardando(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo guardar la solicitud.');
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="solicitud-form-title" className="solicitudes-panel solicitudes-form">
      <div className="solicitudes-panel-heading">
        <div>
          <span className="page-eyebrow">SOLICITUD DE COMPRA</span>
          <h2 id="solicitud-form-title">{solicitud === null ? 'Nueva solicitud' : `Editar ${solicitud.numero}`}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <div className="solicitudes-form-fields">
          <CampoConError etiqueta="Solicitante" htmlFor="solicitud-solicitante" error={errores.solicitante} idError="solicitud-solicitante-error">
            <input
              id="solicitud-solicitante"
              aria-describedby={errores.solicitante ? 'solicitud-solicitante-error' : undefined}
              aria-invalid={Boolean(errores.solicitante)}
              maxLength={110}
              value={solicitante}
              onChange={(event) => setSolicitante(event.currentTarget.value)}
            />
          </CampoConError>
          <label className="solicitudes-provider-field" htmlFor="solicitud-proveedor">
            Proveedor sugerido <span>(opcional)</span>
            <select id="solicitud-proveedor" value={proveedorId} onChange={(event) => setProveedorId(event.currentTarget.value === '' ? '' : Number(event.currentTarget.value))}>
              <option value="">Sin proveedor</option>
              {proveedores.map((proveedor) => <option key={proveedor.id} value={proveedor.id}>{proveedor.razonSocial}</option>)}
            </select>
          </label>
          <CampoConError etiqueta="Observaciones" htmlFor="solicitud-observaciones" error={errores.observaciones} idError="solicitud-observaciones-error">
            <textarea
              id="solicitud-observaciones"
              aria-describedby={errores.observaciones ? 'solicitud-observaciones-error' : undefined}
              aria-invalid={Boolean(errores.observaciones)}
              maxLength={520}
              rows={2}
              value={observaciones}
              onChange={(event) => setObservaciones(event.currentTarget.value)}
            />
          </CampoConError>
        </div>
        <div className="solicitudes-lines-heading">
          <h3>Líneas de la solicitud</h3>
          <button className="secondary-button" onClick={() => setLineas((actuales) => [...actuales, lineaVacia()])} type="button">
            Añadir línea
          </button>
        </div>
        {errores.lineas && <p className="solicitudes-field-error" role="alert">{errores.lineas}</p>}
        <div className="solicitudes-lines">
          {lineas.map((linea, indice) => {
            const subtotal = Number(linea.cantidad || 0) * Number(linea.precioEstimado || 0);
            return (
              <fieldset className="solicitud-line" key={indice}>
                <legend>Línea {indice + 1}</legend>
                <CampoConError etiqueta="Producto" htmlFor={`linea-${indice}-producto`} error={errores.campos[`producto-${indice}`]} idError={`linea-${indice}-producto-error`}>
                  <select
                    id={`linea-${indice}-producto`}
                    aria-describedby={errores.campos[`producto-${indice}`] ? `linea-${indice}-producto-error` : undefined}
                    aria-invalid={Boolean(errores.campos[`producto-${indice}`])}
                    value={linea.productoId}
                    onChange={(event) => {
                      const id = event.currentTarget.value === '' ? '' : Number(event.currentTarget.value);
                      const elegido = productos.find((item) => item.id === id);
                      cambiarLinea(indice, { productoId: id, precioEstimado: elegido ? String(elegido.precio) : linea.precioEstimado });
                    }}
                  >
                    <option value="">Seleccionar producto</option>
                    {productos.map((item) => <option key={item.id} value={item.id}>{item.nombre}</option>)}
                  </select>
                </CampoConError>
                <CampoConError etiqueta="Cantidad" htmlFor={`linea-${indice}-cantidad`} error={errores.campos[`cantidad-${indice}`]} idError={`linea-${indice}-cantidad-error`}>
                  <input
                    id={`linea-${indice}-cantidad`}
                    aria-describedby={errores.campos[`cantidad-${indice}`] ? `linea-${indice}-cantidad-error` : undefined}
                    aria-invalid={Boolean(errores.campos[`cantidad-${indice}`])}
                    min="1"
                    type="number"
                    value={linea.cantidad}
                    onChange={(event) => cambiarLinea(indice, { cantidad: event.currentTarget.value })}
                  />
                </CampoConError>
                <CampoConError etiqueta="Precio estimado" htmlFor={`linea-${indice}-precio`} error={errores.campos[`precio-${indice}`]} idError={`linea-${indice}-precio-error`}>
                  <input
                    id={`linea-${indice}-precio`}
                    aria-describedby={errores.campos[`precio-${indice}`] ? `linea-${indice}-precio-error` : undefined}
                    aria-invalid={Boolean(errores.campos[`precio-${indice}`])}
                    min="0"
                    step="0.01"
                    type="number"
                    value={linea.precioEstimado}
                    onChange={(event) => cambiarLinea(indice, { precioEstimado: event.currentTarget.value })}
                  />
                </CampoConError>
                <p className="solicitud-line-subtotal">Subtotal <strong>{formateadorMoneda.format(subtotal)}</strong></p>
                <button
                  aria-label={`Quitar línea ${indice + 1}`}
                  className="danger-button solicitud-remove-line"
                  onClick={() => setLineas((actuales) => actuales.filter((_, actual) => actual !== indice))}
                  type="button"
                >
                  Quitar
                </button>
              </fieldset>
            );
          })}
        </div>
        <p className="solicitud-total">Total estimado: <strong>{formateadorMoneda.format(total)}</strong></p>
        {errorApi && <p className="solicitudes-error" role="alert">{errorApi}</p>}
        <div className="solicitudes-form-actions">
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar solicitud'}</button>
        </div>
      </form>
    </section>
  );
}
