import { apiGet, apiPost, apiPostSinCuerpo } from '../../api/client';
import type {
  AusenciaCreada,
  AusenciaDetalle,
  FiltrosAusencias,
  IdResponse,
  PaginaAusencias,
  RechazarAusenciaRequest,
  RegistrarAusenciaRequest,
  ResumenAusencias,
  SaldoVacacional,
} from './tipos';

// Los filtros vacíos no se envían: «Todos» y las fechas sin rellenar omiten el parámetro.
export function listarAusencias(filtros: FiltrosAusencias): Promise<PaginaAusencias> {
  const consulta = new URLSearchParams();
  const termino = filtros.busqueda.trim();
  if (termino !== '') {
    consulta.set('busqueda', termino);
  }
  if (filtros.empleadoId !== null) {
    consulta.set('empleadoId', String(filtros.empleadoId));
  }
  if (filtros.tipo !== '') {
    consulta.set('tipo', filtros.tipo);
  }
  if (filtros.estado !== '') {
    consulta.set('estado', filtros.estado);
  }
  if (filtros.desde !== '') {
    consulta.set('desde', filtros.desde);
  }
  if (filtros.hasta !== '') {
    consulta.set('hasta', filtros.hasta);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaAusencias>(`/ausencias?${consulta.toString()}`);
}

export function obtenerResumenAusencias(): Promise<ResumenAusencias> {
  return apiGet<ResumenAusencias>('/ausencias/resumen');
}

export function obtenerAusencia(id: number): Promise<AusenciaDetalle> {
  return apiGet<AusenciaDetalle>(`/ausencias/${id}`);
}

// Sin fecha la API calcula el saldo a hoy.
export function obtenerSaldoVacacional(empleadoId: number, fecha: string): Promise<SaldoVacacional> {
  const consulta = fecha === '' ? '' : `?fecha=${encodeURIComponent(fecha)}`;
  return apiGet<SaldoVacacional>(`/ausencias/saldo-vacacional/${empleadoId}${consulta}`);
}

export function registrarAusencia(request: RegistrarAusenciaRequest): Promise<AusenciaCreada> {
  return apiPost<AusenciaCreada>('/ausencias', request);
}

export function aprobarAusencia(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/ausencias/${id}/aprobacion`);
}

export function rechazarAusencia(id: number, request: RechazarAusenciaRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/ausencias/${id}/rechazo`, request);
}

export function cancelarAusencia(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/ausencias/${id}/cancelacion`);
}
