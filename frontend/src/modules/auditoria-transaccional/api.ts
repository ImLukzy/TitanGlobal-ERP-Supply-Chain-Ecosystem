import { apiGet } from '../../api/client';
import type { FiltrosAuditoria, PaginaAuditoria, RegistroDetalle, ResumenAuditoria } from './tipos';

export function listarAuditoria(filtros: FiltrosAuditoria): Promise<PaginaAuditoria> {
  const parametros = new URLSearchParams();
  if (filtros.entidad !== '') {
    parametros.set('entidad', filtros.entidad);
  }
  if (filtros.entidadId !== '') {
    parametros.set('entidadId', filtros.entidadId);
  }
  if (filtros.accion !== '') {
    parametros.set('accion', filtros.accion);
  }
  if (filtros.usuario !== '') {
    parametros.set('usuario', filtros.usuario);
  }
  if (filtros.fechaDesde !== '') {
    parametros.set('fechaDesde', filtros.fechaDesde);
  }
  if (filtros.fechaHasta !== '') {
    parametros.set('fechaHasta', filtros.fechaHasta);
  }
  parametros.set('pagina', String(filtros.pagina));
  parametros.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaAuditoria>(`/auditoria?${parametros.toString()}`);
}

export function obtenerRegistroAuditoria(id: number): Promise<RegistroDetalle> {
  return apiGet<RegistroDetalle>(`/auditoria/${id}`);
}

export function listarEntidadesAuditoria(): Promise<string[]> {
  return apiGet<string[]>('/auditoria/entidades');
}

export function obtenerResumenAuditoria(): Promise<ResumenAuditoria> {
  return apiGet<ResumenAuditoria>('/auditoria/resumen');
}
