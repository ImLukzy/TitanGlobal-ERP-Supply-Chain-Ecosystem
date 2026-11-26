import { useCallback, useState, type FormEvent } from 'react';
import { CampoModulo } from '../../components/CampoModulo';
import { useConsultaPantalla } from '../../lib/consultaPantalla';
import { diaDePeru } from '../../lib/fechaNegocioPeru';
import { obtenerJerarquiaCliente } from '../ordenes-compra/api';
import { cargarCatalogosFactura, fechaInicioPeru } from './api';
import type { FacturaDetalle, GuardarFactura, GuardarLinea } from './tipos';

interface Props {
  inicial?: FacturaDetalle;
  ocupado: boolean;
  guardar: (datos: GuardarFactura) => void;
  cancelar: () => void;
}

function texto(campos: FormData, clave: string) {
  return String(campos.get(clave) ?? '').trim();
}

function dia(instante?: string | null) {
  return instante ? diaDePeru(new Date(instante)) : '';
}

function conservarInstante(fecha: string, original?: string | null) {
  return original && dia(original) === fecha ? original : fechaInicioPeru(fecha);
}

export function FormularioFactura({ inicial, ocupado, guardar, cancelar }: Props) {
  const [cliente, seleccionarCliente] = useState(inicial?.clienteId.toString() ?? '');
  const [filas, definirFilas] = useState(() => inicial?.detalles.map((_, indice) => indice) ?? [0]);
  const [error, avisarError] = useState('');
  const catalogos = useConsultaPantalla(cargarCatalogosFactura);
  const cargarOrdenes = useCallback(() => obtenerJerarquiaCliente(Number(cliente)), [cliente]);
  const ordenes = useConsultaPantalla(cliente === '' ? null : cargarOrdenes);

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const campos = new FormData(evento.currentTarget);
    const emision = texto(campos, 'emision');
    const vencimiento = texto(campos, 'vencimiento');
    const serie = texto(campos, 'serie');
    const numero = Number(campos.get('numero'));
    const moneda = texto(campos, 'moneda').toUpperCase();
    const detalles: GuardarLinea[] = filas.map(fila => ({
      productoId: Number(campos.get('producto-' + fila)),
      impuestoId: Number(campos.get('impuesto-' + fila)) || null,
      descripcion: texto(campos, 'descripcion-' + fila),
      cantidad: Number(campos.get('cantidad-' + fila)),
      precioUnitario: Number(campos.get('precio-' + fila)),
      descuentoPorcentaje: Number(campos.get('descuento-' + fila)),
    }));
    if (!cliente || !serie || !emision || numero <= 0 || !/^[A-Z]{3}$/.test(moneda)) {
      avisarError('Indica el cliente, documento, fecha de emisión y moneda de tres letras.');
      return;
    }
    if (vencimiento && vencimiento < emision) {
      avisarError('La fecha de vencimiento no puede ser anterior a la fecha de emisión.');
      return;
    }
    if (!detalles.length || detalles.some(linea => linea.productoId <= 0 || !linea.descripcion || linea.cantidad <= 0)) {
      avisarError('Indica al menos un producto, su descripción y una cantidad positiva.');
      return;
    }
    guardar({
      clienteId: Number(cliente), ordenCompraId: Number(campos.get('orden')) || null,
      tipoComprobante: campos.get('tipo') === 'Boleta' ? 'Boleta' : 'Factura',
      serie, numero, moneda, observaciones: texto(campos, 'observaciones') || null,
      fechaEmision: conservarInstante(emision, inicial?.fechaEmision),
      fechaVencimiento: vencimiento ? conservarInstante(vencimiento, inicial?.fechaVencimiento) : null,
      detalles,
    });
  }

  if (catalogos.cargando) return <p role="status">Cargando clientes y productos…</p>;
  if (catalogos.datos === null) return <><p role="alert">{catalogos.error}</p><button type="button" onClick={cancelar}>Cancelar</button></>;

  return <form onSubmit={enviar} aria-label="Datos del comprobante">
    {error && <p role="alert" className="field-error">{error}</p>}
    <fieldset className="finanzas-campos" disabled={ocupado || catalogos.datos === null}>
      <CampoModulo etiqueta="Cliente">{id => <select id={id} value={cliente} required onChange={e => seleccionarCliente(e.target.value)}>
        <option value="">Seleccionar cliente</option>
        {catalogos.datos?.clientes.map(item => <option key={item.id} value={item.id}>{item.nombre} · {item.ruc}</option>)}
      </select>}</CampoModulo>
      <CampoModulo etiqueta="Orden de compra (opcional)">{id => <select id={id} name="orden" key={cliente + String(ordenes.cargando)}
        defaultValue={cliente === inicial?.clienteId.toString() ? inicial.ordenCompraId ?? '' : ''}>
        <option value="">Sin orden de compra</option>
        {ordenes.datos?.map(orden => <option key={orden.ordenId} value={orden.ordenId}>{orden.numeroOrden}</option>)}
      </select>}</CampoModulo>
      {ordenes.error && <p role="alert">{ordenes.error}</p>}
      <CampoModulo etiqueta="Tipo de comprobante">{id => <select id={id} name="tipo" defaultValue={inicial?.tipoComprobante ?? 'Factura'}>
        <option>Factura</option><option>Boleta</option>
      </select>}</CampoModulo>
      <CampoModulo etiqueta="Serie">{id => <input id={id} name="serie" required maxLength={10} defaultValue={inicial?.serie} />}</CampoModulo>
      <CampoModulo etiqueta="Número">{id => <input id={id} name="numero" type="number" required
        min={1} max={2147483647} defaultValue={inicial?.numero} />}</CampoModulo>
      <CampoModulo etiqueta="Moneda">{id => <input id={id} name="moneda" required maxLength={3}
        pattern="[A-Za-z]{3}" defaultValue={inicial?.moneda ?? 'PEN'} />}</CampoModulo>
      <CampoModulo etiqueta="Fecha de emisión">{id => <input id={id} name="emision" type="date" required
        defaultValue={dia(inicial?.fechaEmision) || diaDePeru()} />}</CampoModulo>
      <CampoModulo etiqueta="Fecha de vencimiento (opcional)">{id => <input id={id} name="vencimiento" type="date"
        defaultValue={dia(inicial?.fechaVencimiento)} />}</CampoModulo>
      <CampoModulo etiqueta="Observaciones">{id => <textarea id={id} name="observaciones" maxLength={500}
        defaultValue={inicial?.observaciones ?? ''} />}</CampoModulo>
    </fieldset>
    <h3>Líneas del comprobante</h3>
    {filas.map((fila, indice) => {
      const linea = inicial?.detalles[fila];
      return <fieldset key={fila} className="finanzas-campos" disabled={ocupado || catalogos.datos === null}>
        <legend>Línea {indice + 1}</legend>
        <CampoModulo etiqueta="Producto">{id => <select id={id} name={'producto-' + fila} required defaultValue={linea?.productoId ?? ''}>
          <option value="">Seleccionar producto</option>
          {catalogos.datos?.productos.map(item => <option key={item.id} value={item.id}>{item.nombre}</option>)}
        </select>}</CampoModulo>
        <CampoModulo etiqueta="Descripción">{id => <input id={id} name={'descripcion-' + fila} required
          maxLength={300} defaultValue={linea?.descripcion} />}</CampoModulo>
        <CampoModulo etiqueta="Cantidad">{id => <input id={id} name={'cantidad-' + fila} required type="number"
          min="0.0001" step="0.0001" defaultValue={linea?.cantidad ?? 1} />}</CampoModulo>
        <CampoModulo etiqueta="Precio unitario">{id => <input id={id} name={'precio-' + fila} required type="number"
          min={0} step="0.01" defaultValue={linea?.precioUnitario} />}</CampoModulo>
        <CampoModulo etiqueta="Descuento (%)">{id => <input id={id} name={'descuento-' + fila} required type="number"
          min={0} max={100} step="0.01" defaultValue={linea?.descuentoPorcentaje ?? 0} />}</CampoModulo>
        <CampoModulo etiqueta="Impuesto">{id => <select id={id} name={'impuesto-' + fila} defaultValue={linea?.impuestoId ?? ''}>
          <option value="">Sin impuesto</option>
          {catalogos.datos?.impuestos.map(item => <option key={item.id} value={item.id}>{item.nombre}</option>)}
          {linea?.impuestoId != null && !catalogos.datos?.impuestos.some(item => item.id === linea.impuestoId) &&
            <option value={linea.impuestoId}>{linea.impuestoNombre ?? 'Impuesto original'}</option>}
        </select>}</CampoModulo>
        <button type="button" className="secondary-button" disabled={filas.length === 1}
          onClick={() => definirFilas(actual => actual.filter(item => item !== fila))}>Quitar línea {indice + 1}</button>
      </fieldset>;
    })}
    <div className="finanzas-acciones">
      <button type="button" className="secondary-button" disabled={ocupado}
        onClick={() => definirFilas(actual => [...actual, Math.max(inicial?.detalles.length ?? 0, ...actual) + 1])}>Añadir línea</button>
      <button type="button" className="secondary-button" disabled={ocupado} onClick={cancelar}>Cancelar</button>
      <button type="submit" className="primary-button" disabled={ocupado || catalogos.datos === null}>Guardar borrador</button>
    </div>
  </form>;
}
