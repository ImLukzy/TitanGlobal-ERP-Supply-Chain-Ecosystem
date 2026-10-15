import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  FiltrosZona,
  GuardarZonaRequest,
  IdResponse,
  PaginaZonas,
  ResumenZonas,
  ZonaDetalle,
} from './tipos';

export function listarZonas(filtros: FiltrosZona): Promise<PaginaZonas> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  const suffix = query === '' ? '' : `?${query}`;
  return apiGet<PaginaZonas>(`/zonas-almacenaje${suffix}`);
}

export function obtenerZona(id: number): Promise<ZonaDetalle> {
  return apiGet<ZonaDetalle>(`/zonas-almacenaje/${id}`);
}

export function crearZona(request: GuardarZonaRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/zonas-almacenaje', request);
}

export function actualizarZona(id: number, request: GuardarZonaRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/zonas-almacenaje/${id}`, request);
}

export function eliminarZona(id: number): Promise<void> {
  return apiDelete(`/zonas-almacenaje/${id}`);
}

export function obtenerResumenZonas(almacenId?: number | ''): Promise<ResumenZonas> {
  const suffix = almacenId === undefined || almacenId === '' ? '' : `?almacenId=${almacenId}`;
  return apiGet<ResumenZonas>(`/zonas-almacenaje/resumen${suffix}`);
}
