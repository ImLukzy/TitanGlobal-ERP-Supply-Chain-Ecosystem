import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type { ConductorDetalle, FiltrosConductor, GuardarConductorRequest, IdResponse, PaginaConductores } from './tipos';

export function listarConductores(filtros: FiltrosConductor): Promise<PaginaConductores> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) { consulta.set(clave, String(valor)); }
  }
  const query = consulta.toString();
  return apiGet<PaginaConductores>(`/conductores${query === '' ? '' : `?${query}`}`);
}

export function obtenerConductor(id: number): Promise<ConductorDetalle> {
  return apiGet<ConductorDetalle>(`/conductores/${id}`);
}

export function crearConductor(request: GuardarConductorRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/conductores', request);
}

export function actualizarConductor(id: number, request: GuardarConductorRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/conductores/${id}`, request);
}

export function eliminarConductor(id: number): Promise<void> {
  return apiDelete(`/conductores/${id}`);
}
