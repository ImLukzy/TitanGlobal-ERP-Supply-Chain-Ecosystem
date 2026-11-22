import { apiGet, apiPost, apiPut, apiDelete, apiPostSinCuerpo } from '../../api/client';
import type {
  FiltrosSucursales,
  PaginaSucursales,
  SucursalDetalle,
  ResumenSucursales,
  GuardarSucursal,
} from './tipos';
const raiz = '/sucursales';
export function listarSucursales(f: FiltrosSucursales = {}) {
  const q = new URLSearchParams();
  for (const [k, v] of Object.entries(f)) {
    if (v !== undefined && v !== '') q.set(k, String(v));
  }
  return apiGet<PaginaSucursales>(`${raiz}${q.size ? '?' + q : ''}`);
}
export function obtenerSucursal(id: number) {
  return apiGet<SucursalDetalle>(`${raiz}/${id}`);
}
export function crearSucursal(d: GuardarSucursal) {
  return apiPost<{ id: number }>(raiz, d);
}
export function actualizarSucursal(id: number, d: GuardarSucursal) {
  return apiPut<{ id: number }>(`${raiz}/${id}`, d);
}
export function eliminarSucursal(id: number) {
  return apiDelete(`${raiz}/${id}`);
}
export function activarSucursal(id: number) {
  return apiPostSinCuerpo<{ id: number }>(`${raiz}/${id}/activacion`);
}
export function desactivarSucursal(id: number) {
  return apiPostSinCuerpo<{ id: number }>(`${raiz}/${id}/desactivacion`);
}
export function resumenSucursales() {
  return apiGet<ResumenSucursales>(`${raiz}/resumen`);
}
