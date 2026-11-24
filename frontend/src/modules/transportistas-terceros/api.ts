import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  FiltrosFletes,
  FiltrosTransportistas,
  Flete,
  FleteDetalle,
  GuardarTransportista,
  Pagina,
  RegistrarFlete,
  Resumen,
  Transportista,
  TransportistaDetalle,
} from './tipos';

function consulta(filtros: FiltrosTransportistas | FiltrosFletes): string {
  const params = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== undefined && valor !== '') params.set(clave, String(valor));
  }
  return params.size > 0 ? `?${params}` : '';
}
export function listarTransportistas(
  filtros: FiltrosTransportistas,
): Promise<Pagina<Transportista>> {
  return apiGet(`/transportistas-terceros${consulta(filtros)}`);
}
export function obtenerTransportista(id: number): Promise<TransportistaDetalle> {
  return apiGet(`/transportistas-terceros/${id}`);
}
export function crearTransportista(datos: GuardarTransportista): Promise<{ id: number }> {
  return apiPost('/transportistas-terceros', datos);
}
export function actualizarTransportista(
  id: number,
  datos: GuardarTransportista,
): Promise<{ id: number }> {
  return apiPut(`/transportistas-terceros/${id}`, datos);
}
export function eliminarTransportista(id: number): Promise<void> {
  return apiDelete(`/transportistas-terceros/${id}`);
}
export function resumenTransportistas(): Promise<Resumen> {
  return apiGet('/transportistas-terceros/resumen');
}
export function listarFletes(filtros: FiltrosFletes): Promise<Pagina<Flete>> {
  return apiGet(`/fletes-terceros${consulta(filtros)}`);
}
export function obtenerFlete(id: number): Promise<FleteDetalle> {
  return apiGet(`/fletes-terceros/${id}`);
}
export function registrarFlete(datos: RegistrarFlete): Promise<{ id: number }> {
  return apiPost('/fletes-terceros', datos);
}
export function cancelarFlete(id: number, motivo: string): Promise<{ id: number }> {
  return apiPost(`/fletes-terceros/${id}/cancelacion`, { motivo });
}
