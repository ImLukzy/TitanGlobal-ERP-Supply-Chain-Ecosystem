import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type { FiltrosRutas, GuardarRuta, PaginaRutas, ResumenRutas, RutaDetalle } from './tipos';

export function listarRutas(filtros: FiltrosRutas): Promise<PaginaRutas> {
  const params = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== undefined && valor !== '') params.set(clave, String(valor));
  }
  return apiGet(`/rutas-despacho${params.size ? `?${params}` : ''}`);
}
export function obtenerRuta(id: number): Promise<RutaDetalle> {
  return apiGet(`/rutas-despacho/${id}`);
}
export function crearRuta(datos: GuardarRuta): Promise<{ id: number }> {
  return apiPost('/rutas-despacho', datos);
}
export function actualizarRuta(id: number, datos: GuardarRuta): Promise<{ id: number }> {
  return apiPut(`/rutas-despacho/${id}`, datos);
}
export function eliminarRuta(id: number): Promise<void> {
  return apiDelete(`/rutas-despacho/${id}`);
}
export function listarZonas(): Promise<string[]> {
  return apiGet('/rutas-despacho/zonas');
}
export function resumenRutas(): Promise<ResumenRutas> {
  return apiGet('/rutas-despacho/resumen');
}
