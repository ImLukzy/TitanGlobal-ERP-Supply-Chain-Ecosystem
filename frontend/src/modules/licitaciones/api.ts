import { apiDelete, apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type {
  ActualizarLicitacionRequest,
  AdjudicarLicitacionRequest,
  CrearLicitacionRequest,
  CrearOfertaRequest,
  FiltrosLicitacion,
  LicitacionDetalle,
  LicitacionResponse,
  MejorarOfertaRequest,
  PaginaLicitaciones,
  ProveedorOpcion,
  ResumenLicitaciones,
} from './tipos';

export function listarLicitaciones(filtros: FiltrosLicitacion): Promise<PaginaLicitaciones> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  const separador = query === '' ? '' : `?${query}`;
  return apiGet<PaginaLicitaciones>(`/licitaciones${separador}`);
}

export function obtenerLicitacion(id: number): Promise<LicitacionDetalle> {
  return apiGet<LicitacionDetalle>(`/licitaciones/${id}`);
}

export function crearLicitacion(request: CrearLicitacionRequest): Promise<LicitacionResponse> {
  return apiPost<LicitacionResponse>('/licitaciones', request);
}

export function actualizarLicitacion(id: number, request: ActualizarLicitacionRequest): Promise<LicitacionResponse> {
  return apiPut<LicitacionResponse>(`/licitaciones/${id}`, request);
}

export function eliminarLicitacion(id: number): Promise<void> {
  return apiDelete(`/licitaciones/${id}`);
}

export function crearOferta(id: number, request: CrearOfertaRequest): Promise<LicitacionResponse> {
  return apiPost<LicitacionResponse>(`/licitaciones/${id}/ofertas`, request);
}

export function mejorarOferta(id: number, ofertaId: number, request: MejorarOfertaRequest): Promise<LicitacionResponse> {
  return apiPut<LicitacionResponse>(`/licitaciones/${id}/ofertas/${ofertaId}`, request);
}

export function cerrarLicitacion(id: number): Promise<LicitacionResponse> {
  return apiPostSinCuerpo<LicitacionResponse>(`/licitaciones/${id}/cerrar`);
}

export function adjudicarLicitacion(id: number, request: AdjudicarLicitacionRequest): Promise<LicitacionResponse> {
  return apiPost<LicitacionResponse>(`/licitaciones/${id}/adjudicar`, request);
}

export function cancelarLicitacion(id: number): Promise<LicitacionResponse> {
  return apiPostSinCuerpo<LicitacionResponse>(`/licitaciones/${id}/cancelar`);
}

export function obtenerResumenLicitaciones(): Promise<ResumenLicitaciones> {
  return apiGet<ResumenLicitaciones>('/licitaciones/resumen');
}

export async function listarProveedoresActivosLicitacion(): Promise<ProveedorOpcion[]> {
  const pagina = await apiGet<{ items: ProveedorOpcion[] }>('/proveedores?activo=true&tamanoPagina=100');
  return pagina.items;
}
