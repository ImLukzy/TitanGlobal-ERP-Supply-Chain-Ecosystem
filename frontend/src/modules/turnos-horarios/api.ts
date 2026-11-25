import { apiDelete, apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type {
  DiaProgramado,
  FiltrosAsignaciones,
  FiltrosTurnos,
  FinalizarAsignacionTurnoRequest,
  GuardarAsignacionTurnoRequest,
  GuardarTurnoRequest,
  IdResponse,
  PaginaAsignacionesTurno,
  PaginaTurnos,
  ResumenTurnos,
  TurnoDetalle,
} from './tipos';

// Los filtros vacíos no se envían: «Todos» y «Todas» omiten el parámetro.
export function listarTurnos(filtros: FiltrosTurnos): Promise<PaginaTurnos> {
  const consulta = new URLSearchParams();
  const termino = filtros.busqueda.trim();
  if (termino !== '') {
    consulta.set('busqueda', termino);
  }
  if (filtros.tipo !== '') {
    consulta.set('tipo', filtros.tipo);
  }
  if (filtros.activo === 'Activos') {
    consulta.set('activo', 'true');
  } else if (filtros.activo === 'Inactivos') {
    consulta.set('activo', 'false');
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaTurnos>(`/turnos?${consulta.toString()}`);
}

export function obtenerResumenTurnos(): Promise<ResumenTurnos> {
  return apiGet<ResumenTurnos>('/turnos/resumen');
}

export function obtenerTurno(id: number): Promise<TurnoDetalle> {
  return apiGet<TurnoDetalle>(`/turnos/${id}`);
}

export function crearTurno(request: GuardarTurnoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/turnos', request);
}

export function actualizarTurno(id: number, request: GuardarTurnoRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/turnos/${id}`, request);
}

export function desactivarTurno(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/turnos/${id}/desactivacion`);
}

export function activarTurno(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/turnos/${id}/activacion`);
}

export function listarAsignacionesTurno(filtros: FiltrosAsignaciones): Promise<PaginaAsignacionesTurno> {
  const consulta = new URLSearchParams();
  if (filtros.empleadoId !== null) {
    consulta.set('empleadoId', String(filtros.empleadoId));
  }
  if (filtros.turnoId !== null) {
    consulta.set('turnoId', String(filtros.turnoId));
  }
  if (filtros.estado !== '') {
    consulta.set('estado', filtros.estado);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaAsignacionesTurno>(`/asignaciones-turno?${consulta.toString()}`);
}

export function obtenerProgramacion(empleadoId: number, desde: string, hasta: string): Promise<DiaProgramado[]> {
  const consulta = new URLSearchParams();
  consulta.set('empleadoId', String(empleadoId));
  consulta.set('desde', desde);
  consulta.set('hasta', hasta);
  return apiGet<DiaProgramado[]>(`/asignaciones-turno/programacion?${consulta.toString()}`);
}

export function crearAsignacionTurno(request: GuardarAsignacionTurnoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/asignaciones-turno', request);
}

export function finalizarAsignacionTurno(id: number, request: FinalizarAsignacionTurnoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/asignaciones-turno/${id}/finalizacion`, request);
}

export function eliminarAsignacionTurno(id: number): Promise<void> {
  return apiDelete(`/asignaciones-turno/${id}`);
}
