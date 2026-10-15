import { apiDelete, apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type {
  ClienteOpcion,
  DevolucionDetalle,
  FiltrosDevolucion,
  GuardarDevolucionRequest,
  OrdenDeCliente,
  PaginaDevoluciones,
  ProductoDisponible,
  ReclamoOpcion,
  RespuestaDevolucion,
  ResumenDevoluciones,
} from './tipos';

export function listarDevoluciones(filtros: FiltrosDevolucion): Promise<PaginaDevoluciones> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) consulta.set(clave, String(valor));
  }
  return apiGet<PaginaDevoluciones>(`/devoluciones-comerciales?${consulta}`);
}

export function obtenerDevolucion(id: number): Promise<DevolucionDetalle> {
  return apiGet<DevolucionDetalle>(`/devoluciones-comerciales/${id}`);
}

export function obtenerDisponibleDevolucion(ordenId: number): Promise<ProductoDisponible[]> {
  return apiGet<ProductoDisponible[]>(`/devoluciones-comerciales/ordenes/${ordenId}/disponible`);
}

export function listarOrdenesCliente(clienteId: number): Promise<OrdenDeCliente[]> {
  return apiGet<OrdenDeCliente[]>(`/ordenes-compra/clientes/${clienteId}/jerarquia`);
}

export function crearDevolucion(request: GuardarDevolucionRequest): Promise<RespuestaDevolucion> {
  return apiPost<RespuestaDevolucion>('/devoluciones-comerciales', request);
}

export function actualizarDevolucion(id: number, request: GuardarDevolucionRequest): Promise<RespuestaDevolucion> {
  return apiPut<RespuestaDevolucion>(`/devoluciones-comerciales/${id}`, request);
}

export function aprobarDevolucion(id: number): Promise<DevolucionDetalle> {
  return apiPostSinCuerpo<DevolucionDetalle>(`/devoluciones-comerciales/${id}/aprobar`);
}

export function recibirDevolucion(id: number): Promise<DevolucionDetalle> {
  return apiPostSinCuerpo<DevolucionDetalle>(`/devoluciones-comerciales/${id}/recibir`);
}

export function rechazarDevolucion(id: number, motivo: string): Promise<DevolucionDetalle> {
  return apiPost<DevolucionDetalle>(`/devoluciones-comerciales/${id}/rechazar`, { motivo });
}

export function eliminarDevolucion(id: number): Promise<void> {
  return apiDelete(`/devoluciones-comerciales/${id}`);
}

export function obtenerResumenDevoluciones(): Promise<ResumenDevoluciones> {
  return apiGet<ResumenDevoluciones>('/devoluciones-comerciales/resumen');
}

export async function listarClientesDevolucion(): Promise<ClienteOpcion[]> {
  const pagina = await apiGet<{ items: ClienteOpcion[] }>('/clientes?tamanoPagina=100');
  return pagina.items;
}

export async function listarReclamosDevolucion(): Promise<ReclamoOpcion[]> {
  const pagina = await apiGet<{ items: ReclamoOpcion[] }>('/reclamos-postventa?pagina=1&tamanoPagina=100');
  return pagina.items;
}
