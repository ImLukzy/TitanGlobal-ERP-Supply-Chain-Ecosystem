export type TipoCentroTrabajo = 'Linea' | 'Celda';

export interface CentroTrabajoListado {
  id: number;
  codigo: string;
  nombre: string;
  tipo: TipoCentroTrabajo;
  planta: string;
  capacidadHorasDia: number;
  activo: boolean;
}

export interface CentroTrabajoDetalle extends CentroTrabajoListado {
  costoHora: number;
  responsable: string | null;
}

export interface GuardarCentroTrabajoRequest {
  codigo: string;
  nombre: string;
  tipo: TipoCentroTrabajo;
  planta: string;
  capacidadHorasDia: number;
  costoHora: number;
  responsable?: string;
  activo: boolean;
}

export interface PaginaCentrosTrabajo {
  items: CentroTrabajoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenCentrosTrabajo {
  totalCentros: number;
  activos: number;
  inactivos: number;
  capacidadTotalHorasDia: number;
  capacidadActivaHorasDia: number;
  porTipo: { tipo: TipoCentroTrabajo; totalCentros: number; capacidadHorasDia: number }[];
  porPlanta: { planta: string; totalCentros: number; activos: number }[];
}

export interface FiltrosCentroTrabajo {
  busqueda?: string;
  planta?: string;
  tipo?: TipoCentroTrabajo | '';
  activo?: boolean | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface IdResponse {
  id: number;
}
