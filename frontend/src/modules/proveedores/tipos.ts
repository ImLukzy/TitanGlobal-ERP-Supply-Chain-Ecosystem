export interface ProveedorListado {
  id: number;
  razonSocial: string;
  ruc: string;
  pais: string;
  activo: boolean;
}

export interface PaginaProveedores {
  items: ProveedorListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ProveedorDetalle extends ProveedorListado {
  email: string | null;
  telefono: string | null;
}

export interface GuardarProveedorRequest {
  razonSocial: string;
  ruc: string;
  pais: string;
  email?: string;
  telefono?: string;
  activo: boolean;
}

export interface PaisProveedores {
  pais: string;
  totalProveedores: number;
  activos: number;
}

export interface ResumenProveedores {
  totalProveedores: number;
  activos: number;
  inactivos: number;
  sinEmail: number;
  porPais: PaisProveedores[];
}

export interface FiltrosProveedor {
  busqueda?: string;
  pais?: string;
  activo?: boolean | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface IdResponse {
  id: number;
}
