import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type { ClienteDetalle, GuardarClienteRequest, IdResponse, PaginaClientes, ResumenClientes } from './tipos';

export function listarClientes(busqueda: string, pagina: number, tamanoPagina: number): Promise<PaginaClientes> {
  const consulta = new URLSearchParams({ busqueda, pagina: String(pagina), tamanoPagina: String(tamanoPagina) });
  return apiGet<PaginaClientes>(`/clientes?${consulta.toString()}`);
}

export function obtenerCliente(id: number): Promise<ClienteDetalle> {
  return apiGet<ClienteDetalle>(`/clientes/${id}`);
}

export function crearCliente(request: GuardarClienteRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/clientes', request);
}

export function actualizarCliente(id: number, request: GuardarClienteRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/clientes/${id}`, request);
}

export function eliminarCliente(id: number): Promise<void> {
  return apiDelete(`/clientes/${id}`);
}

export function obtenerResumenClientes(): Promise<ResumenClientes> {
  return apiGet<ResumenClientes>('/clientes/resumen');
}
