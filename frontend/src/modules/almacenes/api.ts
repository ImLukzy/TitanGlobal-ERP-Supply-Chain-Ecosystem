import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  AlmacenDetalle,
  FiltrosAlmacen,
  GuardarAlmacenRequest,
  IdResponse,
  PaginaAlmacenes,
  ResumenAlmacenes,
} from './tipos';

export function listarAlmacenes(filtros: FiltrosAlmacen): Promise<PaginaAlmacenes> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  const separador = query === '' ? '' : `?${query}`;
  return apiGet<PaginaAlmacenes>(`/almacenes${separador}`);
}

export function obtenerAlmacen(id: number): Promise<AlmacenDetalle> {
  return apiGet<AlmacenDetalle>(`/almacenes/${id}`);
}

export function crearAlmacen(request: GuardarAlmacenRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/almacenes', request);
}

export function actualizarAlmacen(id: number, request: GuardarAlmacenRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/almacenes/${id}`, request);
}

export function eliminarAlmacen(id: number): Promise<void> {
  return apiDelete(`/almacenes/${id}`);
}

export function listarCiudadesAlmacen(): Promise<string[]> {
  return apiGet<string[]>('/almacenes/ciudades');
}

export function obtenerResumenAlmacenes(): Promise<ResumenAlmacenes> {
  return apiGet<ResumenAlmacenes>('/almacenes/resumen');
}
