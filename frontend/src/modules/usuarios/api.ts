import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  FiltrosUsuarios,
  GuardarUsuarioRequest,
  IdResponse,
  PaginaUsuarios,
  PermisoEfectivo,
  ResumenUsuarios,
  RolOpcion,
  UsuarioDetalle,
} from './tipos';

interface PaginaRolesRespuesta {
  items: { id: number; codigo: string; nombre: string }[];
}

export function listarUsuarios(filtros: FiltrosUsuarios): Promise<PaginaUsuarios> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  const separador = query === '' ? '' : `?${query}`;
  return apiGet<PaginaUsuarios>(`/usuarios${separador}`);
}

export function obtenerUsuario(id: number): Promise<UsuarioDetalle> {
  return apiGet<UsuarioDetalle>(`/usuarios/${id}`);
}

export function crearUsuario(request: GuardarUsuarioRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/usuarios', request);
}

export function actualizarUsuario(id: number, request: GuardarUsuarioRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/usuarios/${id}`, request);
}

export function eliminarUsuario(id: number): Promise<void> {
  return apiDelete(`/usuarios/${id}`);
}

export function obtenerPermisosUsuario(id: number): Promise<PermisoEfectivo[]> {
  return apiGet<PermisoEfectivo[]>(`/usuarios/${id}/permisos`);
}

export function obtenerResumenUsuarios(): Promise<ResumenUsuarios> {
  return apiGet<ResumenUsuarios>('/usuarios/resumen');
}

// Los roles se leen del módulo 72 (spec 41) para las casillas y el filtro de rol.
export async function listarRolesUsuarios(): Promise<RolOpcion[]> {
  const respuesta = await apiGet<PaginaRolesRespuesta>('/roles?tamanoPagina=100');
  return respuesta.items.map((rol) => {
    return { id: rol.id, codigo: rol.codigo, nombre: rol.nombre };
  });
}
