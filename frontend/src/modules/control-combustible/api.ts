import { apiGet, apiPost, apiPostSinCuerpo, apiPut, apiDelete } from '../../api/client';
import type { ValeDetalle, GuardarVale, FiltrosVales, PaginaVales, ResumenVales } from './tipos';
const raiz = '/control-combustible';
export function listarVales(filtros: FiltrosVales): Promise<PaginaVales> {
  const q = new URLSearchParams();
  for (const [c, v] of Object.entries(filtros)) {
    if (v !== undefined && v !== '') q.set(c, String(v));
  }
  return apiGet(`${raiz}${q.size ? `?${q}` : ''}`);
}
export function obtenerVale(id: number): Promise<ValeDetalle> {
  return apiGet(`${raiz}/${id}`);
}
export function crearVale(d: GuardarVale): Promise<{ id: number }> {
  return apiPost(raiz, d);
}
export function actualizarVale(id: number, d: GuardarVale): Promise<{ id: number }> {
  return apiPut(`${raiz}/${id}`, d);
}
export function eliminarVale(id: number): Promise<void> {
  return apiDelete(`${raiz}/${id}`);
}
export function confirmarVale(id: number): Promise<{ id: number }> {
  return apiPostSinCuerpo(`${raiz}/${id}/confirmacion`);
}
export function anularVale(id: number, motivo: string): Promise<{ id: number }> {
  return apiPost(`${raiz}/${id}/anulacion`, { motivo });
}
export function resumenVales(): Promise<ResumenVales> {
  return apiGet(`${raiz}/resumen`);
}
