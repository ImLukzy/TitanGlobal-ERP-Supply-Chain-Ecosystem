export interface ConductorListado {
  id: number;
  nombre: string;
  documentoLicencia: string;
  vencimientoLicencia: string;
  habilitado: boolean;
  activo: boolean;
}

export interface ConductorDetalle extends ConductorListado {
  observaciones: string | null;
}

export interface GuardarConductorRequest {
  nombre: string;
  documentoLicencia: string;
  vencimientoLicencia: string;
  habilitado: boolean;
  activo: boolean;
  observaciones?: string;
}

export interface PaginaConductores {
  items: ConductorListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosConductor {
  busqueda?: string;
  habilitado?: boolean | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface IdResponse {
  id: number;
}
