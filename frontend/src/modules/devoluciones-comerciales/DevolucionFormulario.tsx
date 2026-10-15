import { useEffect, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { formateadorMoneda } from '../../lib/formato';
import { listarOrdenesCliente, obtenerDisponibleDevolucion } from './api';
import type {
  ClienteOpcion,
  DevolucionDetalle,
  GuardarDevolucionRequest,
  MotivoDevolucion,
  OrdenDeCliente,
  ProductoDisponible,
  ReclamoOpcion,
} from './tipos';

interface Props {
  clientes: ClienteOpcion[];
  reclamos: ReclamoOpcion[];
  devolucion: DevolucionDetalle | null;
  cancelar: () => void;
  guardar: (request: GuardarDevolucionRequest) => Promise<void>;
}

type Campo = 'clienteId' | 'ordenCompraId' | 'motivo' | 'observaciones' | 'lineas';

export function DevolucionFormulario({ clientes, reclamos, devolucion, cancelar, guardar }: Props) {
  const [clienteId, setClienteId] = useState(devolucion?.clienteId ?? 0);
  const [ordenId, setOrdenId] = useState(devolucion?.ordenCompraId ?? 0);
  const reclamoInicial = reclamos.find((item) => item.numero === devolucion?.reclamoNumero)?.id ?? 0;
  const [reclamoId, setReclamoId] = useState(reclamoInicial);
  const [motivo, setMotivo] = useState<MotivoDevolucion>(devolucion?.motivo ?? 'Defecto');
  const [observaciones, setObservaciones] = useState(devolucion?.observaciones ?? '');
  const cantidadesIniciales = Object.fromEntries(
    devolucion?.lineas.map((linea) => [linea.productoId, linea.cantidad]) ?? [],
  );
  const [cantidades, setCantidades] = useState<Record<number, number>>(cantidadesIniciales);
  const [ordenes, setOrdenes] = useState<OrdenDeCliente[]>([]);
  const [disponibles, setDisponibles] = useState<ProductoDisponible[]>([]);
  const [errores, setErrores] = useState<Partial<Record<Campo, string>>>({});
  const [errorApi, setErrorApi] = useState('');
  const [errorDisponible, setErrorDisponible] = useState('');
  const [ocupado, setOcupado] = useState(false);

  useEffect(() => {
    if (!clienteId) {
      setOrdenes([]);
      return;
    }
    let vigente = true;
    void listarOrdenesCliente(clienteId)
      .then((items) => {
        if (vigente) {
          setOrdenes(items);
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          const detalle = error instanceof ApiError ? error.message : 'No se pudieron cargar las órdenes.';
          setErrorDisponible(detalle);
        }
      });
    return () => {
      vigente = false;
    };
  }, [clienteId]);

  useEffect(() => {
    if (!ordenId) {
      setDisponibles([]);
      return;
    }
    let vigente = true;
    void obtenerDisponibleDevolucion(ordenId)
      .then((items) => {
        if (vigente) {
          const cantidadesActuales = new Map(
            devolucion?.lineas.map((linea) => [linea.productoId, linea.cantidad]) ?? [],
          );
          const ajustados = items.map((item) => {
            const cantidadActual = cantidadesActuales.get(item.productoId) ?? 0;
            return {
              ...item,
              devuelto: Math.max(0, item.devuelto - cantidadActual),
              disponible: item.disponible + cantidadActual,
            };
          });
          setDisponibles(ajustados);
          setErrorDisponible('');
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          const detalle = error instanceof ApiError ? error.message : 'No se pudo cargar la disponibilidad.';
          setErrorDisponible(detalle);
        }
      });
    return () => {
      vigente = false;
    };
  }, [devolucion, ordenId]);

  const reclamosOrden = reclamos.filter((reclamo) => reclamo.ordenCompraId === ordenId);
  const lineas = disponibles
    .filter((producto) => (cantidades[producto.productoId] ?? 0) > 0)
    .map((producto) => ({
      productoId: producto.productoId,
      cantidad: cantidades[producto.productoId] ?? 0,
    }));
  const total = Math.round(
    lineas.reduce((suma, linea) => {
      const precio = disponibles.find((item) => item.productoId === linea.productoId)?.precioUnitario ?? 0;
      return suma + Math.round(linea.cantidad * precio * 100) / 100;
    }, 0) * 100,
  ) / 100;

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const siguientes: Partial<Record<Campo, string>> = {};
    if (!clienteId) {
      siguientes.clienteId = 'Selecciona un cliente.';
    }
    if (!ordenId) {
      siguientes.ordenCompraId = 'Selecciona una orden de compra.';
    }
    if (!['Defecto', 'ErrorPedido', 'Arrepentimiento', 'Otro'].includes(motivo)) {
      siguientes.motivo = 'El motivo no es válido.';
    }
    if (!lineas.length) {
      siguientes.lineas = 'La devolución debe tener al menos una línea.';
    }
    for (const producto of disponibles) {
      const cantidad = cantidades[producto.productoId] ?? 0;
      const orden = ordenes.find((item) => item.ordenId === ordenId);
      if (cantidad < 0) {
        siguientes.lineas = 'La cantidad debe ser mayor que 0.';
      }
      if (cantidad > producto.disponible) {
        siguientes.lineas = producto.disponible === 1
          ? `Solo queda 1 unidad de ${producto.productoNombre} por devolver en la orden ${orden?.numeroOrden ?? ''}.`
          : `Solo quedan ${producto.disponible} unidades de ${producto.productoNombre} por devolver en la orden ${orden?.numeroOrden ?? ''}.`;
      }
    }
    if (observaciones.trim().length > 500) {
      siguientes.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
    }
    setErrores(siguientes);
    if (Object.keys(siguientes).length) {
      return;
    }
    setOcupado(true);
    setErrorApi('');
    try {
      const request: GuardarDevolucionRequest = {
        clienteId,
        ordenCompraId: ordenId,
        reclamoPostventaId: reclamoId || undefined,
        motivo,
        observaciones: observaciones.trim() || undefined,
        lineas,
      };
      await guardar(request);
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo guardar la devolución.');
    } finally {
      setOcupado(false);
    }
  }

  function cambiarCliente(valor: string): void {
    setClienteId(Number(valor));
    setOrdenId(0);
    setReclamoId(0);
    setCantidades({});
  }

  function cambiarOrden(valor: string): void {
    setOrdenId(Number(valor));
    setReclamoId(0);
    setCantidades({});
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="devolucion-form-title"
        aria-modal="true"
        className="clientes-dialog devolucion-form-dialog"
        role="dialog"
      >
        <h2 id="devolucion-form-title">
          {devolucion ? 'Editar devolución' : 'Nueva devolución comercial'}
        </h2>
        <form noValidate onSubmit={(event) => void enviar(event)}>
          <CampoConError
            error={errores.clienteId}
            htmlFor="devolucion-cliente"
            etiqueta="Cliente"
            idError="devolucion-cliente-error"
          >
            <select
              aria-describedby={errores.clienteId ? 'devolucion-cliente-error' : undefined}
              aria-invalid={Boolean(errores.clienteId)}
              id="devolucion-cliente"
              onChange={(event) => cambiarCliente(event.currentTarget.value)}
              value={clienteId || ''}
            >
              <option value="">Selecciona un cliente</option>
              {clientes.map((cliente) => (
                <option key={cliente.id} value={cliente.id}>{cliente.nombre}</option>
              ))}
            </select>
          </CampoConError>
          <CampoConError
            error={errores.ordenCompraId}
            htmlFor="devolucion-orden"
            etiqueta="Orden de compra"
            idError="devolucion-orden-error"
          >
            <select
              aria-describedby={errores.ordenCompraId ? 'devolucion-orden-error' : undefined}
              aria-invalid={Boolean(errores.ordenCompraId)}
              id="devolucion-orden"
              onChange={(event) => cambiarOrden(event.currentTarget.value)}
              value={ordenId || ''}
            >
              <option value="">Selecciona una orden</option>
              {ordenes.map((orden) => (
                <option key={orden.ordenId} value={orden.ordenId}>{orden.numeroOrden}</option>
              ))}
            </select>
          </CampoConError>
          <CampoConError
            error=""
            htmlFor="devolucion-reclamo"
            etiqueta="Reclamo relacionado (opcional)"
            idError="devolucion-reclamo-error"
          >
            <select
              id="devolucion-reclamo"
              onChange={(event) => setReclamoId(Number(event.currentTarget.value))}
              value={reclamoId || ''}
            >
              <option value="">Sin reclamo</option>
              {reclamosOrden.map((reclamo) => (
                <option key={reclamo.id} value={reclamo.id}>{reclamo.numero}</option>
              ))}
            </select>
          </CampoConError>
          <CampoConError
            error={errores.motivo}
            htmlFor="devolucion-motivo"
            etiqueta="Motivo"
            idError="devolucion-motivo-error"
          >
            <select
              aria-describedby={errores.motivo ? 'devolucion-motivo-error' : undefined}
              aria-invalid={Boolean(errores.motivo)}
              id="devolucion-motivo"
              onChange={(event) => setMotivo(event.currentTarget.value as MotivoDevolucion)}
              value={motivo}
            >
              <option value="Defecto">Defecto</option>
              <option value="ErrorPedido">Error de pedido</option>
              <option value="Arrepentimiento">Arrepentimiento</option>
              <option value="Otro">Otro</option>
            </select>
          </CampoConError>
          <CampoConError
            error={errores.observaciones}
            htmlFor="devolucion-observaciones"
            etiqueta="Observaciones"
            idError="devolucion-observaciones-error"
          >
            <textarea
              aria-describedby={errores.observaciones ? 'devolucion-observaciones-error' : undefined}
              aria-invalid={Boolean(errores.observaciones)}
              id="devolucion-observaciones"
              maxLength={520}
              onChange={(event) => setObservaciones(event.currentTarget.value)}
              value={observaciones}
            />
          </CampoConError>
          {ordenId > 0 && (
            <section className="devolucion-availability">
              <h3>Productos disponibles para devolución</h3>
              {errorDisponible && <p className="clientes-error" role="alert">{errorDisponible}</p>}
              {disponibles.length > 0 && (
                <div className="devolucion-lines-scroll">
                  <table className="tabla-paginada devolucion-lines-table">
                    <thead>
                      <tr>
                        <th scope="col">Producto</th>
                        <th scope="col">Comprado</th>
                        <th scope="col">Devuelto</th>
                        <th scope="col">Disponible</th>
                        <th scope="col">Precio</th>
                        <th scope="col">Devolver</th>
                        <th scope="col">Importe</th>
                      </tr>
                    </thead>
                    <tbody>
                      {disponibles.map((item) => {
                        const cantidad = cantidades[item.productoId] ?? 0;
                        const importe = Math.round(cantidad * item.precioUnitario * 100) / 100;
                        return (
                          <tr key={item.productoId}>
                            <th scope="row">{item.productoNombre}</th>
                            <td>{item.comprado}</td>
                            <td>{item.devuelto}</td>
                            <td>{item.disponible}</td>
                            <td>{formateadorMoneda.format(item.precioUnitario)}</td>
                            <td>
                              <input
                                aria-label={`Cantidad de ${item.productoNombre}`}
                                max={item.disponible}
                                min="0"
                                onChange={(event) => {
                                  const valor = Number(event.currentTarget.value);
                                  setCantidades((actual) => ({ ...actual, [item.productoId]: valor }));
                                }}
                                type="number"
                                value={cantidad}
                              />
                            </td>
                            <td>{formateadorMoneda.format(importe)}</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </section>
          )}
          <p className="devolucion-live-total">
            Total estimado
            <strong>{formateadorMoneda.format(total)}</strong>
          </p>
          {errores.lineas && <p className="field-error" role="alert">{errores.lineas}</p>}
          {errorApi && <p className="clientes-error" role="alert">{errorApi}</p>}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
            <button className="primary-button" disabled={ocupado} type="submit">
              {ocupado ? 'Guardando…' : 'Guardar devolución'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
