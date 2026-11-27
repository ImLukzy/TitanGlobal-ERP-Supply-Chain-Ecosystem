import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import { listarProveedores } from '../proveedores/api';
import type {
  ActualizarEvaluacionRequest,
  CrearEvaluacionRequest,
  EvaluacionDetalle,
  EvaluacionResponse,
  FiltrosEvaluacion,
  PaginaEvaluaciones,
  ProveedorOpcion,
  ResumenEvaluaciones,
} from './tipos';

export function listarEvaluaciones(filtros: FiltrosEvaluacion): Promise<PaginaEvaluaciones> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  const separador = query === '' ? '' : `?${query}`;
  return apiGet<PaginaEvaluaciones>(`/evaluaciones-proveedores${separador}`);
}

export function obtenerEvaluacion(id: number): Promise<EvaluacionDetalle> {
  return apiGet<EvaluacionDetalle>(`/evaluaciones-proveedores/${id}`);
}

export function crearEvaluacion(request: CrearEvaluacionRequest): Promise<EvaluacionResponse> {
  return apiPost<EvaluacionResponse>('/evaluaciones-proveedores', request);
}

export function actualizarEvaluacion(id: number, request: ActualizarEvaluacionRequest): Promise<EvaluacionResponse> {
  return apiPut<EvaluacionResponse>(`/evaluaciones-proveedores/${id}`, request);
}

export function eliminarEvaluacion(id: number): Promise<void> {
  return apiDelete(`/evaluaciones-proveedores/${id}`);
}

export function obtenerResumenEvaluaciones(): Promise<ResumenEvaluaciones> {
  return apiGet<ResumenEvaluaciones>('/evaluaciones-proveedores/resumen');
}

/** Todos los proveedores, activos e inactivos: la API acepta ambos. */
export async function listarProveedoresEvaluables(): Promise<ProveedorOpcion[]> {
  const pagina = await listarProveedores({ tamanoPagina: 100 });
  return pagina.items;
}
