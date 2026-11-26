import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type {
  CompletarAccionRequest,
  FiltrosEntregas,
  FiltrosEquipos,
  FiltrosExamenes,
  FiltrosIncidentes,
  GuardarAccionRequest,
  GuardarEquipoRequest,
  IdResponse,
  IncidenteCreado,
  IncidenteDetalle,
  PaginaEntregas,
  PaginaEquipos,
  PaginaExamenes,
  PaginaIncidentes,
  RegistrarEntregaRequest,
  RegistrarExamenRequest,
  RegistrarIncidenteRequest,
  ResumenSst,
} from './tipos';

function paginar(consulta: URLSearchParams, pagina: number, tamanoPagina: number): string {
  consulta.set('pagina', String(pagina));
  consulta.set('tamanoPagina', String(tamanoPagina));
  return consulta.toString();
}

// Los filtros vacíos no se envían: «Todos» omite el parámetro.
export function listarIncidentes(filtros: FiltrosIncidentes): Promise<PaginaIncidentes> {
  const consulta = new URLSearchParams();
  const termino = filtros.busqueda.trim();
  if (termino !== '') {
    consulta.set('busqueda', termino);
  }
  if (filtros.tipo !== '') {
    consulta.set('tipo', filtros.tipo);
  }
  if (filtros.severidad !== '') {
    consulta.set('severidad', filtros.severidad);
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
  return apiGet<PaginaIncidentes>(`/incidentes-sst?${paginar(consulta, filtros.pagina, filtros.tamanoPagina)}`);
}

export function obtenerIncidente(id: number): Promise<IncidenteDetalle> {
  return apiGet<IncidenteDetalle>(`/incidentes-sst/${id}`);
}

export function registrarIncidente(request: RegistrarIncidenteRequest): Promise<IncidenteCreado> {
  return apiPost<IncidenteCreado>('/incidentes-sst', request);
}

export function agregarAccion(id: number, request: GuardarAccionRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/incidentes-sst/${id}/acciones`, request);
}

export function completarAccion(id: number, accionId: number, request: CompletarAccionRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/incidentes-sst/${id}/acciones/${accionId}/completar`, request);
}

export function cerrarIncidente(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/incidentes-sst/${id}/cierre`);
}

export function obtenerResumen(anio: number): Promise<ResumenSst> {
  return apiGet<ResumenSst>(`/incidentes-sst/resumen?anio=${anio}`);
}

export function listarEquipos(filtros: FiltrosEquipos): Promise<PaginaEquipos> {
  const consulta = new URLSearchParams();
  const termino = filtros.busqueda.trim();
  if (termino !== '') {
    consulta.set('busqueda', termino);
  }
  if (filtros.activo === 'Activos') {
    consulta.set('activo', 'true');
  } else if (filtros.activo === 'Inactivos') {
    consulta.set('activo', 'false');
  }
  return apiGet<PaginaEquipos>(`/equipos-proteccion?${paginar(consulta, filtros.pagina, filtros.tamanoPagina)}`);
}

export function crearEquipo(request: GuardarEquipoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/equipos-proteccion', request);
}

export function actualizarEquipo(id: number, request: GuardarEquipoRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/equipos-proteccion/${id}`, request);
}

export function desactivarEquipo(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/equipos-proteccion/${id}/desactivacion`);
}

export function activarEquipo(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/equipos-proteccion/${id}/activacion`);
}

export function listarEntregas(filtros: FiltrosEntregas): Promise<PaginaEntregas> {
  const consulta = new URLSearchParams();
  if (filtros.empleadoId !== null) {
    consulta.set('empleadoId', String(filtros.empleadoId));
  }
  if (filtros.equipoId !== null) {
    consulta.set('equipoId', String(filtros.equipoId));
  }
  if (filtros.vencidas) {
    consulta.set('vencidas', 'true');
  }
  return apiGet<PaginaEntregas>(`/entregas-epp?${paginar(consulta, filtros.pagina, filtros.tamanoPagina)}`);
}

export function registrarEntrega(request: RegistrarEntregaRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/entregas-epp', request);
}

export function listarExamenes(filtros: FiltrosExamenes): Promise<PaginaExamenes> {
  const consulta = new URLSearchParams();
  if (filtros.empleadoId !== null) {
    consulta.set('empleadoId', String(filtros.empleadoId));
  }
  if (filtros.tipo !== '') {
    consulta.set('tipo', filtros.tipo);
  }
  if (filtros.resultado !== '') {
    consulta.set('resultado', filtros.resultado);
  }
  if (filtros.vencidos) {
    consulta.set('vencidos', 'true');
  }
  return apiGet<PaginaExamenes>(`/examenes-medicos?${paginar(consulta, filtros.pagina, filtros.tamanoPagina)}`);
}

export function registrarExamen(request: RegistrarExamenRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/examenes-medicos', request);
}
