import { apiGet, apiPost, apiPut, apiDelete } from '../../api/client';
import type {
  FiltrosRetornos,
  PaginaRetornos,
  RetornoDetalle,
  ResumenRetornos,
  GuardarRetorno,
  RegistrarCantidad,
} from './tipos';
const ruta = '/logistica-inversa';
export function listarRetornos(f: FiltrosRetornos = {}) {
  const q = new URLSearchParams();
  for (const [c, v] of Object.entries(f)) {
    if (v !== undefined && v !== '') q.set(c, String(v));
  }
  return apiGet<PaginaRetornos>(`${ruta}${q.size ? '?' + q : ''}`);
}
export function obtenerRetorno(id: number) {
  return apiGet<RetornoDetalle>(`${ruta}/${id}`);
}
export function resumenRetornos() {
  return apiGet<ResumenRetornos>(`${ruta}/resumen`);
}
export function crearRetorno(d: GuardarRetorno) {
  return apiPost<{ id: number }>(ruta, d);
}
export function actualizarRetorno(id: number, d: GuardarRetorno) {
  return apiPut<{ id: number }>(`${ruta}/${id}`, d);
}
export function eliminarRetorno(id: number) {
  return apiDelete(`${ruta}/${id}`);
}
export function recogerRetorno(id: number, d: RegistrarCantidad) {
  return apiPost<{ id: number }>(`${ruta}/${id}/recogida`, d);
}
export function recibirRetorno(id: number, d: RegistrarCantidad) {
  return apiPost<{ id: number }>(`${ruta}/${id}/recepcion`, d);
}
export function cancelarRetorno(id: number, motivo: string) {
  return apiPost<{ id: number }>(`${ruta}/${id}/cancelacion`, { motivo });
}
