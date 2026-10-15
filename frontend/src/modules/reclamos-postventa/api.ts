import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type { AccionReclamo, FiltrosReclamos, GuardarReclamoRequest, PaginaReclamos, ReclamoDetalle, RespuestaReclamo, ResumenReclamos } from './tipos';

const base = '/reclamos-postventa';

export function listarReclamos(filtros: FiltrosReclamos): Promise<PaginaReclamos> {
  const query = new URLSearchParams();
  query.set('pagina', String(filtros.pagina ?? 1));
  query.set('tamanoPagina', String(filtros.tamanoPagina ?? 10));
  if (filtros.busqueda) query.set('busqueda', filtros.busqueda);
  if (filtros.estado) query.set('estado', filtros.estado);
  if (filtros.tipo) query.set('tipo', filtros.tipo);
  if (filtros.prioridad) query.set('prioridad', filtros.prioridad);
  if (filtros.clienteId) query.set('clienteId', String(filtros.clienteId));
  if (filtros.vencidos) query.set('vencidos', 'true');
  return apiGet(`${base}?${query.toString()}`);
}

export function obtenerReclamo(id: number): Promise<ReclamoDetalle> {
  return apiGet(`${base}/${id}`);
}

export function crearReclamo(request: GuardarReclamoRequest): Promise<RespuestaReclamo> {
  return apiPost(base, request);
}

export function actualizarReclamo(id: number, request: GuardarReclamoRequest): Promise<RespuestaReclamo> {
  return apiPut(`${base}/${id}`, request);
}

export function iniciarReclamo(id: number): Promise<ReclamoDetalle> {
  return apiPostSinCuerpo(`${base}/${id}/iniciar`);
}

export function resolverReclamo(id: number, request: AccionReclamo): Promise<ReclamoDetalle> {
  return apiPost(`${base}/${id}/resolver`, request);
}

export function cerrarReclamo(id: number): Promise<ReclamoDetalle> {
  return apiPostSinCuerpo(`${base}/${id}/cerrar`);
}

export function rechazarReclamo(id: number, request: AccionReclamo): Promise<ReclamoDetalle> {
  return apiPost(`${base}/${id}/rechazar`, request);
}

export function obtenerResumenReclamos(): Promise<ResumenReclamos> {
  return apiGet(`${base}/resumen`);
}
