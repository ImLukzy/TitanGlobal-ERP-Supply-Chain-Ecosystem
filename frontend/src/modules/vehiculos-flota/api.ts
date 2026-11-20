import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  FiltrosVehiculoFlota, GuardarVehiculoFlotaRequest, IdResponse,
  PaginaVehiculosFlota, ResumenVehiculosFlota, VehiculoFlotaDetalle,
} from './tipos';

export function listarVehiculosFlota(filtros: FiltrosVehiculoFlota): Promise<PaginaVehiculosFlota> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  return apiGet<PaginaVehiculosFlota>(`/vehiculos-flota${query === '' ? '' : `?${query}`}`);
}

export function obtenerVehiculoFlota(id: number): Promise<VehiculoFlotaDetalle> {
  return apiGet<VehiculoFlotaDetalle>(`/vehiculos-flota/${id}`);
}

export function crearVehiculoFlota(request: GuardarVehiculoFlotaRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/vehiculos-flota', request);
}

export function actualizarVehiculoFlota(id: number, request: GuardarVehiculoFlotaRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/vehiculos-flota/${id}`, request);
}

export function eliminarVehiculoFlota(id: number): Promise<void> {
  return apiDelete(`/vehiculos-flota/${id}`);
}

export function listarTiposVehiculoFlota(): Promise<string[]> {
  return apiGet<string[]>('/vehiculos-flota/tipos');
}

export function obtenerResumenVehiculosFlota(): Promise<ResumenVehiculosFlota> {
  return apiGet<ResumenVehiculosFlota>('/vehiculos-flota/resumen');
}
