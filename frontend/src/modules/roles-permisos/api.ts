import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  FiltrosRol,
  GuardarPermisosRequest,
  GuardarRolRequest,
  IdResponse,
  MatrizPermisos,
  PaginaRoles,
  PermisoModulo,
  RolDetalle,
} from './tipos';

export function listarRoles(filtros: FiltrosRol): Promise<PaginaRoles> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  const separador = query === '' ? '' : `?${query}`;
  return apiGet<PaginaRoles>(`/roles${separador}`);
}

export function obtenerRol(id: number): Promise<RolDetalle> {
  return apiGet<RolDetalle>(`/roles/${id}`);
}

export function crearRol(request: GuardarRolRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/roles', request);
}

export function actualizarRol(id: number, request: GuardarRolRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/roles/${id}`, request);
}

export function eliminarRol(id: number): Promise<void> {
  return apiDelete(`/roles/${id}`);
}

export function obtenerPermisosRol(id: number): Promise<PermisoModulo[]> {
  return apiGet<PermisoModulo[]>(`/roles/${id}/permisos`);
}

export function guardarPermisosRol(id: number, request: GuardarPermisosRequest): Promise<PermisoModulo[]> {
  return apiPut<PermisoModulo[]>(`/roles/${id}/permisos`, request);
}

export function obtenerMatrizPermisos(): Promise<MatrizPermisos> {
  return apiGet<MatrizPermisos>('/roles/matriz');
}
