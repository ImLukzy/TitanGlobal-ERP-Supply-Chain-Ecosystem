import { apiDelete, apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type {
  CicloDetalle,
  CrearEvaluacionRequest,
  EvaluacionDetalle,
  FiltrosCiclos,
  FiltrosEvaluaciones,
  GuardarCalificacionesRequest,
  GuardarCicloRequest,
  GuardarCompetenciaRequest,
  IdResponse,
  PaginaCiclos,
  PaginaEvaluaciones,
  ResultadosCiclo,
} from './tipos';

// Los filtros vacíos no se envían: «Todos» omite el parámetro.
export function listarCiclos(filtros: FiltrosCiclos): Promise<PaginaCiclos> {
  const consulta = new URLSearchParams();
  const termino = filtros.busqueda.trim();
  if (termino !== '') {
    consulta.set('busqueda', termino);
  }
  if (filtros.estado !== '') {
    consulta.set('estado', filtros.estado);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaCiclos>(`/ciclos-evaluacion?${consulta.toString()}`);
}

export function obtenerCiclo(id: number): Promise<CicloDetalle> {
  return apiGet<CicloDetalle>(`/ciclos-evaluacion/${id}`);
}

export function obtenerResultadosCiclo(id: number): Promise<ResultadosCiclo> {
  return apiGet<ResultadosCiclo>(`/ciclos-evaluacion/${id}/resultados`);
}

export function crearCiclo(request: GuardarCicloRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/ciclos-evaluacion', request);
}

export function actualizarCiclo(id: number, request: GuardarCicloRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/ciclos-evaluacion/${id}`, request);
}

export function crearCompetencia(cicloId: number, request: GuardarCompetenciaRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/ciclos-evaluacion/${cicloId}/competencias`, request);
}

export function actualizarCompetencia(cicloId: number, competenciaId: number, request: GuardarCompetenciaRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/ciclos-evaluacion/${cicloId}/competencias/${competenciaId}`, request);
}

export function eliminarCompetencia(cicloId: number, competenciaId: number): Promise<void> {
  return apiDelete(`/ciclos-evaluacion/${cicloId}/competencias/${competenciaId}`);
}

export function abrirCiclo(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/ciclos-evaluacion/${id}/apertura`);
}

export function cerrarCiclo(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/ciclos-evaluacion/${id}/cierre`);
}

export function listarEvaluaciones(filtros: FiltrosEvaluaciones): Promise<PaginaEvaluaciones> {
  const consulta = new URLSearchParams();
  if (filtros.cicloId !== null) {
    consulta.set('cicloId', String(filtros.cicloId));
  }
  if (filtros.estado !== '') {
    consulta.set('estado', filtros.estado);
  }
  const termino = filtros.busqueda.trim();
  if (termino !== '') {
    consulta.set('busqueda', termino);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaEvaluaciones>(`/evaluaciones?${consulta.toString()}`);
}

export function obtenerEvaluacion(id: number): Promise<EvaluacionDetalle> {
  return apiGet<EvaluacionDetalle>(`/evaluaciones/${id}`);
}

export function crearEvaluacion(request: CrearEvaluacionRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/evaluaciones', request);
}

export function guardarCalificaciones(id: number, request: GuardarCalificacionesRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/evaluaciones/${id}/calificaciones`, request);
}

export function enviarEvaluacion(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/evaluaciones/${id}/envio`);
}

export function cerrarEvaluacion(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/evaluaciones/${id}/cierre`);
}
