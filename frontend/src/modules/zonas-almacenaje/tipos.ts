export type TipoZona = 'Recepcion' | 'Almacenamiento' | 'Picking' | 'Despacho' | 'Cuarentena';
export type TemperaturaZona = 'Ambiente' | 'Refrigerada' | 'Congelada';

export interface ZonaListado {
  id: number;
  almacenId: number;
  almacenCodigo: string;
  codigo: string;
  nombre: string;
  tipo: TipoZona;
  temperatura: TemperaturaZona;
  capacidadM3: number;
  activa: boolean;
}

export interface ZonaDetalle extends ZonaListado {
  almacenNombre: string;
}

export interface PaginaZonas {
  items: ZonaListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface GuardarZonaRequest {
  almacenId: number;
  codigo: string;
  nombre: string;
  tipo: TipoZona;
  temperatura: TemperaturaZona;
  capacidadM3: number;
  activa: boolean;
}

export interface TipoZonasResumen {
  tipo: TipoZona;
  totalZonas: number;
  capacidadM3: number;
}

export interface OcupacionAlmacen {
  almacenId: number;
  almacenCodigo: string;
  capacidadAlmacenM3: number;
  capacidadAsignadaM3: number;
  porcentajeAsignado: number;
}

export interface ResumenZonas {
  totalZonas: number;
  activas: number;
  porTipo: TipoZonasResumen[];
  ocupacion: OcupacionAlmacen[];
}

export interface FiltrosZona {
  almacenId?: number | '';
  busqueda?: string;
  tipo?: TipoZona | '';
  temperatura?: TemperaturaZona | '';
  activa?: boolean | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface IdResponse {
  id: number;
}
