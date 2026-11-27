import { apiGet, apiPost, apiPostSinCuerpo, apiPut, apiDelete } from '../../api/client';
import type {
  CargaDetalle,
  FiltrosCargas,
  GuardarCarga,
  PaginaCargas,
  ResumenCargas,
} from './tipos';
const raiz = '/consolidaciones-carga';
export function listarCargas(filtros: FiltrosCargas): Promise<PaginaCargas> {
  const params = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== undefined && valor !== '') params.set(clave, String(valor));
  }
  return apiGet(`${raiz}${params.size ? `?${params}` : ''}`);
}
export function obtenerCarga(id: number): Promise<CargaDetalle> {
  return apiGet(`${raiz}/${id}`);
}
export function crearCarga(datos: GuardarCarga): Promise<{ id: number }> {
  return apiPost(raiz, datos);
}
export function actualizarCarga(id: number, datos: GuardarCarga): Promise<{ id: number }> {
  return apiPut(`${raiz}/${id}`, datos);
}
export function eliminarCarga(id: number): Promise<void> {
  return apiDelete(`${raiz}/${id}`);
}
export function confirmarCarga(id: number): Promise<{ id: number }> {
  return apiPostSinCuerpo(`${raiz}/${id}/confirmacion`);
}
export function cancelarCarga(id: number, motivo: string): Promise<{ id: number }> {
  return apiPost(`${raiz}/${id}/cancelacion`, { motivo });
}
export function resumenCargas(): Promise<ResumenCargas> {
  return apiGet(`${raiz}/resumen`);
}
