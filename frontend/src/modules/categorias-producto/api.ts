import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
    CategoriaProducto,
    FiltrosCategoriaProducto,
    GuardarCategoriaProductoRequest,
    IdResponse,
    ResumenCategoriasProducto,
} from './tipos';

export function listarCategoriasProducto(filtros: FiltrosCategoriaProducto): Promise<CategoriaProducto[]> {
    const consulta = new URLSearchParams();
    for (const [clave, valor] of Object.entries(filtros)) {
        if (valor !== '' && valor !== undefined) {
            consulta.set(clave, String(valor));
        }
    }
    const query = consulta.toString();
    const separador = query === '' ? '' : `?${query}`;
    return apiGet<CategoriaProducto[]>(`/categorias-producto${separador}`);
}

export function obtenerCategoriaProducto(id: number): Promise<CategoriaProducto> {
    return apiGet<CategoriaProducto>(`/categorias-producto/${id}`);
}

export function crearCategoriaProducto(request: GuardarCategoriaProductoRequest): Promise<IdResponse> {
    return apiPost<IdResponse>('/categorias-producto', request);
}

export function actualizarCategoriaProducto(
    id: number,
    request: GuardarCategoriaProductoRequest,
): Promise<IdResponse> {
    return apiPut<IdResponse>(`/categorias-producto/${id}`, request);
}

export function desactivarCategoriaProducto(id: number): Promise<void> {
    return apiDelete(`/categorias-producto/${id}`);
}

export function obtenerResumenCategoriasProducto(): Promise<ResumenCategoriasProducto> {
    return apiGet<ResumenCategoriasProducto>('/categorias-producto/resumen');
}