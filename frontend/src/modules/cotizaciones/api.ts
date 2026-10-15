import {
  apiDelete,
  apiGet,
  apiPost,
  apiPostSinCuerpo,
  apiPut,
} from '../../api/client';
import type {
  CotizacionDetalle,
  CotizacionResponse,
  FiltrosCotizacion,
  GuardarCotizacionRequest,
  PaginaCotizaciones,
  ResumenCotizaciones,
  RechazarCotizacionRequest,
} from './tipos';

export function listarCotizaciones(
  filtros: FiltrosCotizacion,
): Promise<PaginaCotizaciones> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined && valor !== false) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  return apiGet<PaginaCotizaciones>(
    `/cotizaciones${query === '' ? '' : `?${query}`}`,
  );
}

export function obtenerCotizacion(id: number): Promise<CotizacionDetalle> {
  return apiGet<CotizacionDetalle>(`/cotizaciones/${id}`);
}

export function crearCotizacion(
  request: GuardarCotizacionRequest,
): Promise<CotizacionResponse> {
  return apiPost<CotizacionResponse>('/cotizaciones', request);
}

export function actualizarCotizacion(
  id: number,
  request: GuardarCotizacionRequest,
): Promise<CotizacionResponse> {
  return apiPut<CotizacionResponse>(`/cotizaciones/${id}`, request);
}

export function aceptarCotizacion(id: number): Promise<CotizacionDetalle> {
  return apiPostSinCuerpo<CotizacionDetalle>(`/cotizaciones/${id}/aceptar`);
}

export function rechazarCotizacion(
  id: number,
  request: RechazarCotizacionRequest,
): Promise<CotizacionDetalle> {
  return apiPost<CotizacionDetalle>(`/cotizaciones/${id}/rechazar`, request);
}

export function eliminarCotizacion(id: number): Promise<void> {
  return apiDelete(`/cotizaciones/${id}`);
}

export function obtenerResumenCotizaciones(): Promise<ResumenCotizaciones> {
  return apiGet<ResumenCotizaciones>('/cotizaciones/resumen');
}

export async function listarClientesCotizaciones(): Promise<
  { id: number; nombre: string }[]
> {
  const pagina = await apiGet<{ items: { id: number; nombre: string }[] }>(
    '/clientes?tamanoPagina=100',
  );
  return pagina.items;
}

export async function listarProductosCotizaciones(): Promise<
  { id: number; nombre: string; precio: number }[]
> {
  const pagina = await apiGet<{
    items: { id: number; nombre: string; precio: number }[];
  }>('/productos?tamanoPagina=100');
  return pagina.items;
}
