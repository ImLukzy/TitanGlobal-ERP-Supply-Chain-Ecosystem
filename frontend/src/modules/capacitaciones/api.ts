import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type {
  CapacitacionEmpleado,
  CursoListado,
  EmitirCertificacionRequest,
  FiltrosCursos,
  FiltrosHoras,
  FiltrosProgramaciones,
  GuardarCursoRequest,
  GuardarProgramacionRequest,
  IdResponse,
  PaginaCursos,
  PaginaHoras,
  PaginaProgramaciones,
  ProgramacionDetalle,
  RegistrarResultadosRequest,
  ResumenCapacitaciones,
} from './tipos';

// Los filtros vacíos no se envían: «Todos» omite el parámetro.
export function listarCursos(filtros: FiltrosCursos): Promise<PaginaCursos> {
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
  return apiGet<PaginaCursos>(`/cursos?${consulta.toString()}`);
}

export function obtenerCurso(id: number): Promise<CursoListado> {
  return apiGet<CursoListado>(`/cursos/${id}`);
}

export function crearCurso(request: GuardarCursoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/cursos', request);
}

export function actualizarCurso(id: number, request: GuardarCursoRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/cursos/${id}`, request);
}

export function desactivarCurso(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/cursos/${id}/desactivacion`);
}

export function activarCurso(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/cursos/${id}/activacion`);
}

export function listarProgramaciones(filtros: FiltrosProgramaciones): Promise<PaginaProgramaciones> {
  const consulta = new URLSearchParams();
  consulta.set('anio', String(filtros.anio));
  if (filtros.cursoId !== null) {
    consulta.set('cursoId', String(filtros.cursoId));
  }
  if (filtros.estado !== '') {
    consulta.set('estado', filtros.estado);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaProgramaciones>(`/programaciones-curso?${consulta.toString()}`);
}

export function obtenerProgramacion(id: number): Promise<ProgramacionDetalle> {
  return apiGet<ProgramacionDetalle>(`/programaciones-curso/${id}`);
}

export function crearProgramacion(request: GuardarProgramacionRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/programaciones-curso', request);
}

export function inscribirEmpleado(id: number, empleadoId: number): Promise<IdResponse> {
  return apiPost<IdResponse>(`/programaciones-curso/${id}/inscripciones`, { empleadoId });
}

export function retirarInscripcion(id: number, inscripcionId: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/programaciones-curso/${id}/inscripciones/${inscripcionId}/retiro`);
}

export function marcarRealizada(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/programaciones-curso/${id}/realizacion`);
}

export function cancelarProgramacion(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/programaciones-curso/${id}/cancelacion`);
}

export function registrarResultados(id: number, request: RegistrarResultadosRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/programaciones-curso/${id}/resultados`, request);
}

export function emitirCertificacion(id: number, inscripcionId: number, request: EmitirCertificacionRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/programaciones-curso/${id}/inscripciones/${inscripcionId}/certificacion`, request);
}

export function obtenerResumen(anio: number): Promise<ResumenCapacitaciones> {
  return apiGet<ResumenCapacitaciones>(`/capacitaciones/resumen?anio=${anio}`);
}

export function listarHoras(filtros: FiltrosHoras): Promise<PaginaHoras> {
  const consulta = new URLSearchParams();
  consulta.set('anio', String(filtros.anio));
  const termino = filtros.busqueda.trim();
  if (termino !== '') {
    consulta.set('busqueda', termino);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaHoras>(`/capacitaciones/horas?${consulta.toString()}`);
}

export function obtenerCapacitacionEmpleado(empleadoId: number, anio: number): Promise<CapacitacionEmpleado> {
  return apiGet<CapacitacionEmpleado>(`/capacitaciones/empleados/${empleadoId}?anio=${anio}`);
}
