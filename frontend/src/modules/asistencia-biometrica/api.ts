import { apiDelete, apiGet, apiPost, apiPostSinCuerpo } from '../../api/client';
import type {
  CalcularJornadasRequest,
  FiltrosJornadas,
  FiltrosMarcaciones,
  IdResponse,
  ImportarMarcacionesRequest,
  PaginaJornadas,
  PaginaMarcaciones,
  RechazarHorasExtraRequest,
  RegistrarMarcacionRequest,
  ResultadoCalculo,
  ResultadoImportacion,
  ResumenAsistencia,
} from './tipos';

// Los filtros vacíos no se envían: «Todos» y las fechas vacías omiten el parámetro.
export function listarMarcaciones(filtros: FiltrosMarcaciones): Promise<PaginaMarcaciones> {
  const consulta = new URLSearchParams();
  if (filtros.empleadoId !== null) {
    consulta.set('empleadoId', String(filtros.empleadoId));
  }
  if (filtros.desde !== '') {
    consulta.set('desde', filtros.desde);
  }
  if (filtros.hasta !== '') {
    consulta.set('hasta', filtros.hasta);
  }
  if (filtros.tipo !== '') {
    consulta.set('tipo', filtros.tipo);
  }
  if (filtros.origen !== '') {
    consulta.set('origen', filtros.origen);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaMarcaciones>(`/marcaciones?${consulta.toString()}`);
}

export function registrarMarcacion(request: RegistrarMarcacionRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/marcaciones', request);
}

export function importarMarcaciones(request: ImportarMarcacionesRequest): Promise<ResultadoImportacion> {
  return apiPost<ResultadoImportacion>('/marcaciones/importacion', request);
}

export function eliminarMarcacion(id: number): Promise<void> {
  return apiDelete(`/marcaciones/${id}`);
}

export function listarJornadas(filtros: FiltrosJornadas): Promise<PaginaJornadas> {
  const consulta = new URLSearchParams();
  if (filtros.empleadoId !== null) {
    consulta.set('empleadoId', String(filtros.empleadoId));
  }
  if (filtros.desde !== '') {
    consulta.set('desde', filtros.desde);
  }
  if (filtros.hasta !== '') {
    consulta.set('hasta', filtros.hasta);
  }
  if (filtros.estado !== '') {
    consulta.set('estado', filtros.estado);
  }
  if (filtros.estadoHorasExtra !== '') {
    consulta.set('estadoHorasExtra', filtros.estadoHorasExtra);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaJornadas>(`/jornadas-asistencia?${consulta.toString()}`);
}

export function obtenerResumenAsistencia(desde: string, hasta: string): Promise<ResumenAsistencia> {
  const consulta = new URLSearchParams();
  if (desde !== '') {
    consulta.set('desde', desde);
  }
  if (hasta !== '') {
    consulta.set('hasta', hasta);
  }
  return apiGet<ResumenAsistencia>(`/jornadas-asistencia/resumen?${consulta.toString()}`);
}

export function calcularJornadas(request: CalcularJornadasRequest): Promise<ResultadoCalculo> {
  return apiPost<ResultadoCalculo>('/jornadas-asistencia/calculo', request);
}

export function aprobarHorasExtra(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/jornadas-asistencia/${id}/horas-extra/aprobacion`);
}

export function rechazarHorasExtra(id: number, request: RechazarHorasExtraRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/jornadas-asistencia/${id}/horas-extra/rechazo`, request);
}
