import { apiGet, apiPost } from '../../api/client';
import type {
  ClienteFrecuencia,
  CrearOrdenCompraRequest,
  CrearOrdenCompraResponse,
  OrdenCompraJerarquica,
  ProductoResumen,
  PromedioCompraResponse,
  TotalVentasResponse,
} from './tipos';

function queryString(parametros: Record<string, string | number>): string {
  const query = new URLSearchParams();
  Object.entries(parametros).forEach(([clave, valor]) => {
    query.set(clave, String(valor));
  });
  return query.toString();
}

export function crearOrdenCompra(request: CrearOrdenCompraRequest): Promise<CrearOrdenCompraResponse> {
  return apiPost<CrearOrdenCompraResponse>('/ordenes-compra', request);
}

export function buscarProductos(termino: string, minPrecio: number): Promise<ProductoResumen[]> {
  const query = queryString({ termino, minPrecio });
  return apiGet<ProductoResumen[]>(`/ordenes-compra/productos/filtro?${query}`);
}

export function obtenerSumaVentas(fechaInicio: string): Promise<TotalVentasResponse> {
  const query = queryString({ fechaInicio });
  return apiGet<TotalVentasResponse>(`/ordenes-compra/ventas/suma?${query}`);
}

export function obtenerProductoMasCaro(): Promise<ProductoResumen | null> {
  return apiGet<ProductoResumen | null>('/ordenes-compra/productos/mas-caro');
}

export function obtenerPromedioCliente(clienteId: number): Promise<PromedioCompraResponse> {
  return apiGet<PromedioCompraResponse>(`/ordenes-compra/clientes/${clienteId}/promedio`);
}

export function obtenerProductosSinDescripcion(): Promise<ProductoResumen[]> {
  return apiGet<ProductoResumen[]>('/ordenes-compra/productos/sin-descripcion');
}

export function obtenerFrecuenciaClientes(): Promise<ClienteFrecuencia[]> {
  return apiGet<ClienteFrecuencia[]>('/ordenes-compra/clientes/frecuencia');
}

export function obtenerJerarquiaCliente(clienteId: number): Promise<OrdenCompraJerarquica[]> {
  return apiGet<OrdenCompraJerarquica[]>(`/ordenes-compra/clientes/${clienteId}/jerarquia`);
}

export function obtenerCategoriasCliente(clienteId: number): Promise<string[]> {
  return apiGet<string[]>(`/ordenes-compra/clientes/${clienteId}/categorias-unicas`);
}

export function obtenerClientesCompradores(productoId: number): Promise<string[]> {
  return apiGet<string[]>(`/ordenes-compra/productos/${productoId}/clientes-compradores`);
}
