import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  FiltrosProveedor,
  GuardarProveedorRequest,
  IdResponse,
  PaginaProveedores,
  ProveedorDetalle,
  ResumenProveedores,
} from './tipos';

export function listarProveedores(filtros: FiltrosProveedor): Promise<PaginaProveedores> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  const separador = query === '' ? '' : `?${query}`;
  return apiGet<PaginaProveedores>(`/proveedores${separador}`);
}

export function obtenerProveedor(id: number): Promise<ProveedorDetalle> {
  return apiGet<ProveedorDetalle>(`/proveedores/${id}`);
}

export function crearProveedor(request: GuardarProveedorRequest): Promise<{ id: number }> {
  return apiPost<{ id: number }>('/proveedores', request);
}

export function actualizarProveedor(id: number, request: GuardarProveedorRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/proveedores/${id}`, request);
}

export function eliminarProveedor(id: number): Promise<void> {
  return apiDelete(`/proveedores/${id}`);
}

export function listarPaisesProveedor(): Promise<string[]> {
  return apiGet<string[]>('/proveedores/paises');
}

export function obtenerResumenProveedores(): Promise<ResumenProveedores> {
  return apiGet<ResumenProveedores>('/proveedores/resumen');
}
