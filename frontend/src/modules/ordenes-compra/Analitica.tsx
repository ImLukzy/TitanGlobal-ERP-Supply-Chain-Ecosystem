import { useCallback, useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { ApiError } from '../../api/client';
import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import {
  buscarProductos,
  obtenerCategoriasCliente,
  obtenerClientesCompradores,
  obtenerFrecuenciaClientes,
  obtenerJerarquiaCliente,
  obtenerProductoMasCaro,
  obtenerProductosSinDescripcion,
  obtenerPromedioCliente,
  obtenerSumaVentas,
} from './api';
import type {
  ClienteFrecuencia,
  OrdenCompraJerarquica,
  ProductoResumen,
  PromedioCompraResponse,
  TotalVentasResponse,
} from './tipos';

interface TarjetaProps<T> {
  titulo: string;
  patron: string;
  autoCarga?: boolean;
  vacio: (resultado: T) => boolean;
  consultar: () => Promise<T>;
  children?: ReactNode;
  renderizar: (resultado: T) => ReactNode;
}

function TarjetaAnalitica<T>({ titulo, patron, autoCarga = false, vacio, consultar, children, renderizar }: TarjetaProps<T>) {
  const [cargando, setCargando] = useState(false);
  const [consultado, setConsultado] = useState(false);
  const [error, setError] = useState('');
  const [resultado, setResultado] = useState<T | null>(null);

  const ejecutarConsulta = useCallback(async (event?: FormEvent<HTMLFormElement>): Promise<void> => {
    event?.preventDefault();
    setCargando(true);
    setError('');
    try {
      const respuesta = await consultar();
      setResultado(respuesta);
      setConsultado(true);
    } catch (fallo: unknown) {
      setError(fallo instanceof ApiError ? fallo.message : 'No se pudo consultar la analítica.');
      setResultado(null);
      setConsultado(true);
    } finally {
      setCargando(false);
    }
  }, [consultar]);

  useEffect(() => {
    if (autoCarga) {
      void ejecutarConsulta();
    }
  }, [autoCarga, ejecutarConsulta]);

  const sinDatos = consultado && error === '' && resultado !== null && vacio(resultado);

  return (
    <section aria-labelledby={`analytics-${patron.replaceAll(/\D/g, '-')}`} className="analytics-card">
      <div className="analytics-card-heading">
        <div><h3 id={`analytics-${patron.replaceAll(/\D/g, '-')}`}>{titulo}</h3><span className="pattern-chip">{patron}</span></div>
        {autoCarga && <span className="auto-tag">Actualizado al abrir</span>}
      </div>
      <form className="analytics-controls" onSubmit={(event) => void ejecutarConsulta(event)}>
        {children}
        <button className="secondary-button" disabled={cargando} type="submit">
          {cargando ? 'Consultando…' : 'Consultar'}
        </button>
      </form>
      <div className="analytics-result">
        {cargando && <p aria-live="polite" className="result-loading" role="status">Consultando {titulo.toLocaleLowerCase('es-PE')}…</p>}
        {!cargando && error !== '' && <p className="result-error" role="alert">{error}</p>}
        {!cargando && error === '' && sinDatos && <p className="result-empty">Sin resultados</p>}
        {!cargando && error === '' && consultado && !sinDatos && resultado !== null && renderizar(resultado)}
      </div>
    </section>
  );
}

function TablaProductos({ productos, caption }: { productos: ProductoResumen[]; caption: string }) {
  return (
    <div className="table-scroll" role="region" aria-label={caption} tabIndex={0}>
      <table className="results-table">
        <caption>{caption}</caption>
        <thead><tr><th scope="col">ID</th><th scope="col">Producto</th><th scope="col">Precio</th></tr></thead>
        <tbody>{productos.map((producto) => (
          <tr key={producto.id}><td>{producto.id}</td><td>{producto.nombre}</td><td>{formateadorMoneda.format(producto.precio)}</td></tr>
        ))}</tbody>
      </table>
    </div>
  );
}

function TablaFrecuencia({ clientes }: { clientes: ClienteFrecuencia[] }) {
  return (
    <div className="table-scroll" role="region" aria-label="Ranking de frecuencia de clientes" tabIndex={0}>
      <table className="results-table">
        <caption>Órdenes de compra por cliente</caption>
        <thead><tr><th scope="col">Posición</th><th scope="col">Cliente ID</th><th scope="col">Órdenes</th></tr></thead>
        <tbody>{clientes.map((cliente, indice) => (
          <tr key={cliente.clienteId}><td>{String(indice + 1).padStart(2, '0')}</td><td>{cliente.clienteId}</td><td>{cliente.totalOrdenes}</td></tr>
        ))}</tbody>
      </table>
    </div>
  );
}

function ListaJerarquica({ ordenes }: { ordenes: OrdenCompraJerarquica[] }) {
  return (
    <div className="orders-list">
      {ordenes.map((orden) => (
        <details className="order-accordion" key={orden.ordenId} open>
          <summary>
            <span><strong>{orden.numeroOrden}</strong><small>{orden.nombreCliente} · {formatearFecha(orden.fecha)}</small></span>
            <span className="order-total">{formateadorMoneda.format(orden.detalles.reduce((suma, detalle) => suma + detalle.cantidad * detalle.precioUnitario, 0))}</span>
          </summary>
          <div className="table-scroll" role="region" aria-label={`Detalle de ${orden.numeroOrden}`} tabIndex={0}>
            <table className="results-table detail-table">
              <caption>Detalle de {orden.numeroOrden}</caption>
              <thead><tr><th scope="col">Producto</th><th scope="col">Cantidad</th><th scope="col">Precio unitario</th><th scope="col">Subtotal</th></tr></thead>
              <tbody>{orden.detalles.map((detalle) => (
                <tr key={detalle.productoId}>
                  <td>{detalle.nombreProducto}</td><td>{detalle.cantidad}</td><td>{formateadorMoneda.format(detalle.precioUnitario)}</td><td>{formateadorMoneda.format(detalle.cantidad * detalle.precioUnitario)}</td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        </details>
      ))}
    </div>
  );
}

function CampoParametro({ id, etiqueta, tipo = 'number', valor, alCambiar, minimo, paso }: {
  id: string;
  etiqueta: string;
  tipo?: 'date' | 'number' | 'search';
  valor: string;
  alCambiar: (valor: string) => void;
  minimo?: string;
  paso?: string;
}) {
  return (
    <div className="parameter-field">
      <label htmlFor={id}>{etiqueta}</label>
      <input id={id} min={minimo} onChange={(event) => alCambiar(event.currentTarget.value)} placeholder={tipo === 'search' ? 'Ej. Laptop' : undefined} step={paso} type={tipo} value={valor} />
    </div>
  );
}

export function Analitica() {
  const [termino, setTermino] = useState('');
  const [precioMinimo, setPrecioMinimo] = useState('0');
  const [fechaInicio, setFechaInicio] = useState('2026-01-01');
  const [clientePromedio, setClientePromedio] = useState('1');
  const [clienteJerarquia, setClienteJerarquia] = useState('1');
  const [clienteCategorias, setClienteCategorias] = useState('1');
  const [productoCompradores, setProductoCompradores] = useState('1');

  return (
    <section aria-labelledby="analytics-title" className="business-section analytics-section">
      <div className="business-section-heading">
        <div><div className="page-eyebrow">CONSULTAS</div><h2 id="analytics-title">Analítica</h2></div>
        <span className="section-note">9 consultas · patrones LINQ 1–12</span>
      </div>
      <div className="analytics-grid">
        <TarjetaAnalitica titulo="Buscar productos" patron="Patrones 1–3" consultar={() => buscarProductos(termino, Number(precioMinimo) || 0)} vacio={(datos) => datos.length === 0} renderizar={(datos) => <TablaProductos caption="Productos que cumplen los filtros" productos={datos} />}>
          <CampoParametro alCambiar={setTermino} etiqueta="Término" id="filtro-termino" tipo="search" valor={termino} />
          <CampoParametro alCambiar={setPrecioMinimo} etiqueta="Precio mínimo" id="filtro-precio" minimo="0" paso="0.01" valor={precioMinimo} />
        </TarjetaAnalitica>
        <TarjetaAnalitica titulo="Suma de ventas" patron="Patrones 4 y 6" consultar={() => obtenerSumaVentas(fechaInicio)} vacio={() => false} renderizar={(datos: TotalVentasResponse) => <div className="metric-result"><span>Total desde la fecha indicada</span><strong>{formateadorMoneda.format(datos.totalVentas)}</strong></div>}>
          <CampoParametro alCambiar={setFechaInicio} etiqueta="Fecha de inicio" id="ventas-fecha" tipo="date" valor={fechaInicio} />
        </TarjetaAnalitica>
        <TarjetaAnalitica autoCarga titulo="Producto más caro" patron="Patrón 5" consultar={obtenerProductoMasCaro} vacio={(datos) => datos == null} renderizar={(producto) => producto == null ? null : <div className="featured-product"><span className="featured-product-icon">$</span><span><strong>{producto.nombre}</strong><small>{formateadorMoneda.format(producto.precio)}</small></span></div>} />
        <TarjetaAnalitica titulo="Promedio por cliente" patron="Patrón 7" consultar={() => obtenerPromedioCliente(Number(clientePromedio))} vacio={() => false} renderizar={(datos: PromedioCompraResponse) => <div className="metric-result"><span>Promedio del cliente {datos.clienteId}</span><strong>{formateadorMoneda.format(datos.promedioCompra)}</strong></div>}>
          <CampoParametro alCambiar={setClientePromedio} etiqueta="Cliente ID" id="promedio-cliente" minimo="1" valor={clientePromedio} />
        </TarjetaAnalitica>
        <TarjetaAnalitica autoCarga titulo="Productos sin descripción" patron="Patrón 8" consultar={obtenerProductosSinDescripcion} vacio={(datos) => datos.length === 0} renderizar={(datos) => <TablaProductos caption="Productos sin descripción registrada" productos={datos} />} />
        <TarjetaAnalitica autoCarga titulo="Frecuencia de clientes" patron="Patrón 9" consultar={obtenerFrecuenciaClientes} vacio={(datos) => datos.length === 0} renderizar={(datos) => <TablaFrecuencia clientes={datos} />} />
        <TarjetaAnalitica titulo="Jerarquía de órdenes" patron="Patrón 10" consultar={() => obtenerJerarquiaCliente(Number(clienteJerarquia))} vacio={(datos) => datos.length === 0} renderizar={(datos) => <ListaJerarquica ordenes={datos} />}>
          <CampoParametro alCambiar={setClienteJerarquia} etiqueta="Cliente ID" id="jerarquia-cliente" minimo="1" valor={clienteJerarquia} />
        </TarjetaAnalitica>
        <TarjetaAnalitica titulo="Categorías compradas" patron="Patrón 11" consultar={() => obtenerCategoriasCliente(Number(clienteCategorias))} vacio={(datos) => datos.length === 0} renderizar={(datos) => <div aria-label="Categorías compradas" className="chip-list">{datos.map((categoria) => <span className="result-chip" key={categoria}>{categoria}</span>)}</div>}>
          <CampoParametro alCambiar={setClienteCategorias} etiqueta="Cliente ID" id="categorias-cliente" minimo="1" valor={clienteCategorias} />
        </TarjetaAnalitica>
        <TarjetaAnalitica titulo="Clientes compradores" patron="Patrón 12" consultar={() => obtenerClientesCompradores(Number(productoCompradores))} vacio={(datos) => datos.length === 0} renderizar={(datos) => <div aria-label="Clientes compradores" className="chip-list">{datos.map((cliente) => <span className="result-chip" key={cliente}>{cliente}</span>)}</div>}>
          <CampoParametro alCambiar={setProductoCompradores} etiqueta="Producto ID" id="compradores-producto" minimo="1" valor={productoCompradores} />
        </TarjetaAnalitica>
      </div>
    </section>
  );
}
