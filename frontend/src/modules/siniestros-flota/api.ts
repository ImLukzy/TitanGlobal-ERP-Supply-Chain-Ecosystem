import { apiGet, apiPost, apiPut, apiDelete } from '../../api/client';
import type {
  FiltrosSiniestros,
  PaginaSiniestros,
  SiniestroDetalle,
  ResumenSiniestros,
  GuardarSiniestro,
  RegistrarReclamo,
  CerrarReclamo,
} from './tipos';
const raiz = '/siniestros-flota';
export function listarSiniestros(f: FiltrosSiniestros = {}) {
  const q = new URLSearchParams();
  for (const [k, v] of Object.entries(f)) {
    if (v !== undefined && v !== '') q.set(k, String(v));
  }
  return apiGet<PaginaSiniestros>(`${raiz}${q.size ? '?' + q : ''}`);
}
export function obtenerSiniestro(id: number) {
  return apiGet<SiniestroDetalle>(`${raiz}/${id}`);
}
export function crearSiniestro(d: GuardarSiniestro) {
  return apiPost<{ id: number }>(raiz, d);
}
export function actualizarSiniestro(id: number, d: GuardarSiniestro) {
  return apiPut<{ id: number }>(`${raiz}/${id}`, d);
}
export function eliminarSiniestro(id: number) {
  return apiDelete(`${raiz}/${id}`);
}
export function reclamarSiniestro(id: number, d: RegistrarReclamo) {
  return apiPost<{ id: number }>(`${raiz}/${id}/reclamo`, d);
}
export function cerrarSiniestro(id: number, d: CerrarReclamo) {
  return apiPost<{ id: number }>(`${raiz}/${id}/cierre`, d);
}
export function anularSiniestro(id: number, motivo: string) {
  return apiPost<{ id: number }>(`${raiz}/${id}/anulacion`, { motivo });
}
export function resumenSiniestros() {
  return apiGet<ResumenSiniestros>(`${raiz}/resumen`);
}
