export type TipoVehiculo = 'Camion' | 'Furgon' | 'Moto' | 'Otro';

export interface VehiculoFlotaListado {
  id: number;
  placa: string;
  marca: string;
  modelo: string;
  anio: number;
  tipo: TipoVehiculo;
  capacidadKg: number;
  activo: boolean;
}

export interface VehiculoFlotaDetalle extends VehiculoFlotaListado {
  observaciones: string | null;
}

export interface GuardarVehiculoFlotaRequest {
  placa: string;
  marca: string;
  modelo: string;
  anio: number;
  tipo: TipoVehiculo;
  capacidadKg: number;
  activo: boolean;
  observaciones?: string;
}

export interface PaginaVehiculosFlota {
  items: VehiculoFlotaListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenVehiculosFlota {
  totalVehiculos: number;
  activos: number;
  inactivos: number;
  capacidadTotalKg: number;
  porTipo: { tipo: TipoVehiculo; totalVehiculos: number; activos: number }[];
}

export interface FiltrosVehiculoFlota {
  busqueda?: string;
  tipo?: string;
  activo?: boolean | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface IdResponse {
  id: number;
}
