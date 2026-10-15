import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type { EvaluacionCompra, EvaluarCompraRequest, FiltrosPromociones, GuardarPromocionRequest, IdResponse, OpcionCatalogo, OpcionSegmento, PaginaPromociones, PromocionDetalle, ResumenPromociones } from './tipos';

function crearQuery(parametros: Record<string, string | number | boolean | undefined>): string {
  const query = new URLSearchParams();
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor !== '' && valor !== undefined && valor !== false) query.set(clave, String(valor));
  }
  return query.size === 0 ? '' : `?${query.toString()}`;
}

export function listarPromociones(filtros: FiltrosPromociones): Promise<PaginaPromociones> {
  return apiGet<PaginaPromociones>(`/promociones${crearQuery({ ...filtros })}`);
}
export function obtenerPromocion(id: number): Promise<PromocionDetalle> {
  return apiGet<PromocionDetalle>(`/promociones/${id}`);
}
export function crearPromocion(request: GuardarPromocionRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/promociones', request);
}
export function actualizarPromocion(id: number, request: GuardarPromocionRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/promociones/${id}`, request);
}
export function eliminarPromocion(id: number): Promise<void> {
  return apiDelete(`/promociones/${id}`);
}
export function evaluarCompra(request: EvaluarCompraRequest): Promise<EvaluacionCompra> {
  return apiPost<EvaluacionCompra>('/promociones/evaluar', request);
}
export function obtenerResumenPromociones(): Promise<ResumenPromociones> {
  return apiGet<ResumenPromociones>('/promociones/resumen');
}
export async function listarClientesPromocion(): Promise<OpcionCatalogo[]> {
  const pagina = await apiGet<{ items: OpcionCatalogo[] }>('/clientes?tamanoPagina=100');
  return pagina.items;
}
export async function listarProductosPromocion(): Promise<(OpcionCatalogo & { precio: number })[]> {
  const pagina = await apiGet<{ items: (OpcionCatalogo & { precio: number })[] }>('/productos?tamanoPagina=100');
  return pagina.items;
}
export async function listarSegmentosPromocion(): Promise<OpcionSegmento[]> {
  const pagina = await apiGet<{ items: OpcionSegmento[] }>('/segmentos-clientes?tamanoPagina=100');
  return pagina.items;
}
