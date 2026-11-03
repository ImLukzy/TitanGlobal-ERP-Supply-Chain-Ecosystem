import { apiGet, apiPost, apiPut } from '../../api/client';
import type {
  CeseEmpleadoRequest,
  EmpleadoDetalle,
  FiltrosEmpleados,
  GuardarEmpleadoRequest,
  IdResponse,
  PaginaEmpleados,
} from './tipos';

// Los filtros vacíos no se envían: el estado «Todos» omite el parámetro estado.
export function listarEmpleados(filtros: FiltrosEmpleados): Promise<PaginaEmpleados> {
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
  return apiGet<PaginaEmpleados>(`/empleados?${consulta.toString()}`);
}

export function obtenerEmpleado(id: number): Promise<EmpleadoDetalle> {
  return apiGet<EmpleadoDetalle>(`/empleados/${id}`);
}

export function crearEmpleado(request: GuardarEmpleadoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/empleados', request);
}

export function actualizarEmpleado(id: number, request: GuardarEmpleadoRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/empleados/${id}`, request);
}

export function registrarCeseEmpleado(id: number, request: CeseEmpleadoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/empleados/${id}/cese`, request);
}
