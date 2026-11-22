import { apiGet, apiPost, apiPostSinCuerpo, apiPut, apiDelete } from '../../api/client';
import type {
  EntregaDetalle,
  FiltrosEntregas,
  GuardarEntrega,
  RegistrarPod,
  PaginaEntregas,
  ResumenEntregas,
} from './tipos';
const raiz = '/monitoreo-entregas';
export function listarEntregas(filtros: FiltrosEntregas): Promise<PaginaEntregas> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== undefined && valor !== '') consulta.set(clave, String(valor));
  }
  return apiGet(`${raiz}${consulta.size ? `?${consulta}` : ''}`);
}
export function obtenerEntrega(id: number): Promise<EntregaDetalle> {
  return apiGet(`${raiz}/${id}`);
}
export function crearEntrega(datos: GuardarEntrega): Promise<{ id: number }> {
  return apiPost(raiz, datos);
}
export function actualizarEntrega(id: number, datos: GuardarEntrega): Promise<{ id: number }> {
  return apiPut(`${raiz}/${id}`, datos);
}
export function eliminarEntrega(id: number): Promise<void> {
  return apiDelete(`${raiz}/${id}`);
}
export function iniciarEntrega(id: number): Promise<{ id: number }> {
  return apiPostSinCuerpo(`${raiz}/${id}/salida`);
}
export function completarEntrega(id: number, datos: RegistrarPod): Promise<{ id: number }> {
  return apiPost(`${raiz}/${id}/entrega`, datos);
}
export function cancelarEntrega(id: number, motivo: string): Promise<{ id: number }> {
  return apiPost(`${raiz}/${id}/cancelacion`, { motivo });
}
export function resumenEntregas(): Promise<ResumenEntregas> {
  return apiGet(`${raiz}/resumen`);
}
