export interface RolListado {
  id: number;
  codigo: string;
  nombre: string;
  esSistema: boolean;
  modulosConLectura: number;
  modulosConEscritura: number;
}

export interface RolDetalle extends RolListado {
  descripcion: string | null;
}

export interface PaginaRoles {
  items: RolListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface GuardarRolRequest {
  codigo: string;
  nombre: string;
  descripcion?: string;
}

export interface IdResponse {
  id: number;
}

export interface PermisoModulo {
  moduloNumero: number;
  moduloNombre: string;
  dominio: string;
  puedeLeer: boolean;
  puedeEscribir: boolean;
}

export interface PermisoModuloRequest {
  moduloNumero: number;
  puedeLeer: boolean;
  puedeEscribir: boolean;
}

export interface GuardarPermisosRequest {
  permisos: PermisoModuloRequest[];
}

export type NivelPermiso = 'Escritura' | 'Lectura' | 'Ninguno';

export interface RolResumen {
  id: number;
  codigo: string;
}

export interface ModuloMatriz {
  moduloNumero: number;
  moduloNombre: string;
  dominio: string;
  permisos: NivelPermiso[];
}

export interface MatrizPermisos {
  roles: RolResumen[];
  modulos: ModuloMatriz[];
}

export interface FiltrosRol {
  busqueda?: string;
  pagina?: number;
  tamanoPagina?: number;
}
