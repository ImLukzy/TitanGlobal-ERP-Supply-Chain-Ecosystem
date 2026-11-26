import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  CategoriaProducto,
  FiltrosCategoriasProducto,
  GuardarCategoriaProductoRequest,
  IdCategoriaResponse,
  ResumenCategoriasProducto,
} from './tipos';

export function listarCategoriasProducto(
  filtros: FiltrosCategoriasProducto = {},
): Promise<CategoriaProducto[]> {
  const consulta = new URLSearchParams();
  if (filtros.buscar !== undefined && filtros.buscar !== '') {
    consulta.set('buscar', filtros.buscar);
  }
  if (filtros.incluirInactivas !== undefined) {
    consulta.set('incluirInactivas', String(filtros.incluirInactivas));
  }
  const query = consulta.toString();
  const separador = query === '' ? '' : `?${query}`;
  return apiGet<CategoriaProducto[]>(`/categorias-producto${separador}`);
}

export function obtenerCategoriaProducto(id: number): Promise<CategoriaProducto> {
  return apiGet<CategoriaProducto>(`/categorias-producto/${id}`);
}

export function crearCategoriaProducto(
  request: GuardarCategoriaProductoRequest,
): Promise<IdCategoriaResponse> {
  return apiPost<IdCategoriaResponse>('/categorias-producto', request);
}

export function actualizarCategoriaProducto(
  id: number,
  request: GuardarCategoriaProductoRequest,
): Promise<IdCategoriaResponse> {
  return apiPut<IdCategoriaResponse>(`/categorias-producto/${id}`, request);
}

export function desactivarCategoriaProducto(id: number): Promise<void> {
  return apiDelete(`/categorias-producto/${id}`);
}

export function obtenerResumenCategoriasProducto(): Promise<ResumenCategoriasProducto> {
  return apiGet<ResumenCategoriasProducto>('/categorias-producto/resumen');
}
