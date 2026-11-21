import { apiGet, apiPost, apiPostSinCuerpo, apiPut, apiDelete } from '../../api/client';
import type { GuiaDetalle, FiltrosGuias, GuardarGuia, PaginaGuias, ResumenGuias } from './tipos';
const raiz = '/guias-remision';
export function listarGuias(filtros: FiltrosGuias): Promise<PaginaGuias> {
  const params = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== undefined && valor !== '') params.set(clave, String(valor));
  }
  return apiGet(`${raiz}${params.size ? `?${params}` : ''}`);
}
export function obtenerGuia(id: number): Promise<GuiaDetalle> {
  return apiGet(`${raiz}/${id}`);
}
export function crearGuia(datos: GuardarGuia): Promise<{ id: number }> {
  return apiPost(raiz, datos);
}
export function actualizarGuia(id: number, datos: GuardarGuia): Promise<{ id: number }> {
  return apiPut(`${raiz}/${id}`, datos);
}
export function eliminarGuia(id: number): Promise<void> {
  return apiDelete(`${raiz}/${id}`);
}
export function emitirGuia(id: number): Promise<{ id: number }> {
  return apiPostSinCuerpo(`${raiz}/${id}/emision`);
}
export function anularGuia(id: number, motivo: string): Promise<{ id: number }> {
  return apiPost(`${raiz}/${id}/anulacion`, { motivo });
}
export function resumenGuias(): Promise<ResumenGuias> {
  return apiGet(`${raiz}/resumen`);
}
