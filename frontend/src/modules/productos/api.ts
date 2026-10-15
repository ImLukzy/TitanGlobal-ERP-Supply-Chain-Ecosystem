import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type { FiltrosProducto, PaginaProductos, ProductoDetalle, ProductoRequest, ResumenProductos } from './tipos';

export function listarProductos(filtros: FiltrosProducto): Promise<PaginaProductos> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  return apiGet<PaginaProductos>(`/productos?${consulta.toString()}`);
}

export function obtenerProducto(id: number): Promise<ProductoDetalle> {
  return apiGet<ProductoDetalle>(`/productos/${id}`);
}

export function listarCategoriasProducto(): Promise<string[]> {
  return apiGet<string[]>('/productos/categorias');
}

export function obtenerResumenProductos(): Promise<ResumenProductos> {
  return apiGet<ResumenProductos>('/productos/resumen');
}

export function crearProducto(request: ProductoRequest): Promise<{ id: number }> {
  return apiPost<{ id: number }>('/productos', request);
}

export function actualizarProducto(id: number, request: ProductoRequest): Promise<{ id: number }> {
  return apiPut<{ id: number }>(`/productos/${id}`, request);
}

export function eliminarProducto(id: number): Promise<void> {
  return apiDelete(`/productos/${id}`);
}
