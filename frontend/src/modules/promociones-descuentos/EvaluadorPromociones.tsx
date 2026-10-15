import { useState } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { evaluarCompra } from './api';
import type { EvaluacionCompra, OpcionCatalogo, EvaluarCompraRequest } from './tipos';
import { formateadorMoneda, formatearCantidad } from '../../lib/formato';

interface Props { clientes: OpcionCatalogo[]; productos: (OpcionCatalogo & { precio: number })[]; errorCatalogos: string; }
interface LineaBorrador { clave: number; productoId: number; cantidad: number; precioUnitario: number; }

function hoyUtc(): string { return new Date().toISOString().slice(0, 10); }

export function EvaluadorPromociones({ clientes, productos, errorCatalogos }: Props) {
  const [clienteId, setClienteId] = useState(0);
  const [fecha, setFecha] = useState(hoyUtc());
  const [lineas, setLineas] = useState<LineaBorrador[]>([]);
  const [evaluacion, setEvaluacion] = useState<EvaluacionCompra | null>(null);
  const [error, setError] = useState('');
  const [ocupado, setOcupado] = useState(false);

  function agregarLinea(): void {
    const clave = Date.now() + Math.random();
    const primerProducto = productos[0];
    setLineas((actuales) => [...actuales, { clave, productoId: primerProducto?.id ?? 0, cantidad: 1, precioUnitario: primerProducto?.precio ?? 0 }]);
    setEvaluacion(null);
  }

  function cambiarProducto(clave: number, id: number): void {
    const producto = productos.find((item) => item.id === id);
    setLineas((actuales) => actuales.map((linea) => linea.clave === clave ? { ...linea, productoId: id, precioUnitario: producto?.precio ?? 0 } : linea));
    setEvaluacion(null);
  }

  async function ejecutar(): Promise<void> {
    setError('');
    setEvaluacion(null);
    if (!clienteId) { setError('Selecciona un cliente.'); return; }
    if (lineas.length === 0) { setError('Agrega al menos una línea.'); return; }
    if (lineas.some((linea) => linea.productoId <= 0)) { setError('Selecciona un producto para cada línea.'); return; }
    if (lineas.some((linea) => linea.cantidad <= 0)) { setError('La cantidad debe ser mayor que 0.'); return; }
    if (lineas.some((linea) => linea.precioUnitario <= 0)) { setError('El precio unitario debe ser mayor que 0.'); return; }
    const request: EvaluarCompraRequest = { clienteId, fecha, lineas: lineas.map(({ productoId, cantidad, precioUnitario }) => ({ productoId, cantidad, precioUnitario })) };
    setOcupado(true);
    try {
      setEvaluacion(await evaluarCompra(request));
    } catch (cause: unknown) {
      setError(cause instanceof ApiError ? cause.message : 'No se pudo evaluar la compra.');
    } finally {
      setOcupado(false);
    }
  }

  return <section aria-labelledby="evaluador-title" className="solicitudes-top-panel promociones-evaluator">
    <div className="clientes-panel-heading"><div><span className="page-eyebrow">SIMULACIÓN</span><h2 id="evaluador-title">Evaluar compra</h2></div></div>
    {errorCatalogos && <p className="clientes-error" role="alert">{errorCatalogos}</p>}
    <div className="promocion-form-row"><CampoConError error="" htmlFor="evaluador-cliente" etiqueta="Cliente" idError="evaluador-cliente-error"><select id="evaluador-cliente" onChange={(evento) => { setClienteId(Number(evento.currentTarget.value)); setEvaluacion(null); }} value={clienteId || ''}><option value="">Selecciona un cliente</option>{clientes.map((cliente) => <option key={cliente.id} value={cliente.id}>{cliente.nombre}</option>)}</select></CampoConError><CampoConError error="" htmlFor="evaluador-fecha" etiqueta="Fecha de compra" idError="evaluador-fecha-error"><input id="evaluador-fecha" onChange={(evento) => { setFecha(evento.currentTarget.value); setEvaluacion(null); }} type="date" value={fecha} /></CampoConError></div>
    <div className="promociones-lineas-heading"><div><h3>Líneas de compra</h3>{lineas.length > 0 && <small>{formatearCantidad(lineas.length, 'línea', 'líneas')}</small>}</div><button className="secondary-button" onClick={agregarLinea} type="button">Agregar línea</button></div>
    {lineas.length === 0 && <p role="status">Agrega productos para evaluar sus promociones.</p>}
    <div className="promociones-lineas">{lineas.map((linea, indice) => <fieldset className="promocion-linea" key={linea.clave}><legend>Línea {indice + 1}</legend><CampoConError error="" htmlFor={`evaluador-producto-${indice}`} etiqueta="Producto" idError=""><select id={`evaluador-producto-${indice}`} onChange={(evento) => cambiarProducto(linea.clave, Number(evento.currentTarget.value))} value={linea.productoId || ''}><option value="">Selecciona un producto</option>{productos.map((producto) => <option key={producto.id} value={producto.id}>{producto.nombre} · {formateadorMoneda.format(producto.precio)}</option>)}</select></CampoConError><CampoConError error="" htmlFor={`evaluador-cantidad-${indice}`} etiqueta="Cantidad" idError=""><input id={`evaluador-cantidad-${indice}`} min="1" onChange={(evento) => { const valor = Number(evento.currentTarget.value); setLineas((actuales) => actuales.map((item) => item.clave === linea.clave ? { ...item, cantidad: valor } : item)); setEvaluacion(null); }} type="number" value={linea.cantidad} /></CampoConError><CampoConError error="" htmlFor={`evaluador-precio-${indice}`} etiqueta="Precio unitario" idError=""><input id={`evaluador-precio-${indice}`} min="0" onChange={(evento) => { const valor = Number(evento.currentTarget.value); setLineas((actuales) => actuales.map((item) => item.clave === linea.clave ? { ...item, precioUnitario: valor } : item)); setEvaluacion(null); }} step="0.01" type="number" value={linea.precioUnitario || ''} /></CampoConError><button aria-label={`Quitar línea ${indice + 1}`} className="secondary-button" onClick={() => { setLineas((actuales) => actuales.filter((item) => item.clave !== linea.clave)); setEvaluacion(null); }} type="button">Quitar</button></fieldset>)}</div>
    {error && <p className="solicitudes-error" role="alert">{error}</p>}
    <div className="promociones-evaluate-action"><button className="primary-button" disabled={ocupado} onClick={() => void ejecutar()} type="button">{ocupado ? 'Evaluando…' : 'Evaluar'}</button></div>
    {evaluacion !== null && <ResultadoEvaluacion evaluacion={evaluacion} />}
  </section>;
}

function ResultadoEvaluacion({ evaluacion }: { evaluacion: EvaluacionCompra }) {
  const sinPromociones = evaluacion.descuentoTotal === 0;
  return <section aria-label="Resultado de la evaluación" className="promocion-resultado">
    <h3>Resultado para {evaluacion.clienteNombre}</h3>
    {sinPromociones && <p role="status">No hay promociones aplicables</p>}
    <div className="promocion-result-scroll"><table className="tabla-paginada promociones-result-table"><caption>Descuentos aplicados por línea</caption><thead><tr><th scope="col">Producto</th><th scope="col">Subtotal</th><th scope="col">Promoción</th><th scope="col">Descuento</th><th scope="col">Importe</th></tr></thead><tbody>{evaluacion.lineas.map((linea) => <tr key={linea.productoId}><th scope="row">{linea.productoNombre}</th><td>{formateadorMoneda.format(linea.subtotal)}</td><td>{linea.promocion?.codigo ?? '—'}</td><td>{formateadorMoneda.format(linea.descuento)}</td><td>{formateadorMoneda.format(linea.importe)}</td></tr>)}</tbody></table></div>
    {evaluacion.promocionCompra !== null && <p className="promocion-compra-aplicada">Promoción de compra: <strong>{evaluacion.promocionCompra.codigo}</strong> · descuento {formateadorMoneda.format(evaluacion.descuentoCompra)}</p>}
    <dl className="promocion-totales"><div><dt>Subtotal</dt><dd>{formateadorMoneda.format(evaluacion.subtotal)}</dd></div><div><dt>Descuento total</dt><dd>{formateadorMoneda.format(evaluacion.descuentoTotal)}</dd></div><div><dt>Total</dt><dd>{formateadorMoneda.format(evaluacion.total)}</dd></div></dl>
  </section>;
}
