export type TipoAlmacen = 'Central' | 'Regional' | 'Transito';

export interface AlmacenListado {
  id: number;
  codigo: string;
  nombre: string;
  tipo: TipoAlmacen;
  ciudad: string;
  capacidadM3: number;
  activo: boolean;
}

export interface PaginaAlmacenes {
  items: AlmacenListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface AlmacenDetalle extends AlmacenListado {
  direccion: string;
  responsable: string | null;
}

export interface GuardarAlmacenRequest {
  codigo: string;
  nombre: string;
  tipo: TipoAlmacen;
  direccion: string;
  ciudad: string;
  capacidadM3: number;
  responsable?: string;
  activo: boolean;
}

export interface ResumenTipoAlmacenes {
  tipo: TipoAlmacen;
  totalAlmacenes: number;
  capacidadM3: number;
}

export interface ResumenCiudadAlmacenes {
  ciudad: string;
  totalAlmacenes: number;
  activos: number;
}

export interface ResumenAlmacenes {
  totalAlmacenes: number;
  activos: number;
  inactivos: number;
  capacidadTotalM3: number;
  capacidadActivaM3: number;
  porTipo: ResumenTipoAlmacenes[];
  porCiudad: ResumenCiudadAlmacenes[];
}

export interface FiltrosAlmacen {
  busqueda?: string;
  ciudad?: string;
  tipo?: TipoAlmacen | '';
  activo?: boolean | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface IdResponse {
  id: number;
}
