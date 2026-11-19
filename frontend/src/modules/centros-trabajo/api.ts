import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  CentroTrabajoDetalle, FiltrosCentroTrabajo, GuardarCentroTrabajoRequest,
  IdResponse, PaginaCentrosTrabajo, ResumenCentrosTrabajo,
} from './tipos';

export function listarCentrosTrabajo(filtros: FiltrosCentroTrabajo): Promise<PaginaCentrosTrabajo> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  return apiGet<PaginaCentrosTrabajo>(`/centros-trabajo${query === '' ? '' : `?${query}`}`);
}

export function obtenerCentroTrabajo(id: number): Promise<CentroTrabajoDetalle> {
  return apiGet<CentroTrabajoDetalle>(`/centros-trabajo/${id}`);
}

export function crearCentroTrabajo(request: GuardarCentroTrabajoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/centros-trabajo', request);
}

export function actualizarCentroTrabajo(id: number, request: GuardarCentroTrabajoRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/centros-trabajo/${id}`, request);
}

export function eliminarCentroTrabajo(id: number): Promise<void> {
  return apiDelete(`/centros-trabajo/${id}`);
}

export function listarPlantasCentroTrabajo(): Promise<string[]> {
  return apiGet<string[]>('/centros-trabajo/plantas');
}

export function obtenerResumenCentrosTrabajo(): Promise<ResumenCentrosTrabajo> {
  return apiGet<ResumenCentrosTrabajo>('/centros-trabajo/resumen');
}
