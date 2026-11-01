export type NivelPermiso = 'Escritura' | 'Lectura' | 'Ninguno';

export interface UsuarioListado {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  email: string;
  activo: boolean;
  roles: string[];
}

export interface PaginaUsuarios {
  items: UsuarioListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface UsuarioDetalle extends UsuarioListado {
  fechaAlta: string;
  rolIds: number[];
  modulosConLectura: number;
  modulosConEscritura: number;
}

export interface GuardarUsuarioRequest {
  nombreUsuario: string;
  nombreCompleto: string;
  email: string;
  activo: boolean;
  rolIds: number[];
}

export interface PermisoEfectivo {
  moduloNumero: number;
  moduloNombre: string;
  dominio: string;
  permiso: NivelPermiso;
  otorgadoPor: string[];
}

export interface RolUsuarios {
  rolId: number;
  codigo: string;
  usuarios: number;
}

export interface ResumenUsuarios {
  total: number;
  activos: number;
  inactivos: number;
  sinRol: number;
  porRol: RolUsuarios[];
}

export interface FiltrosUsuarios {
  busqueda?: string;
  rolId?: number | '';
  activo?: boolean | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface RolOpcion {
  id: number;
  codigo: string;
  nombre: string;
}

export interface IdResponse {
  id: number;
}
