export interface Ruta {
  id: number;
  codigo: string;
  nombre: string;
  zona: string;
  origen: string;
  destino: string;
  distanciaKm: string;
  peajeEstimado: string;
  moneda: string;
  activo: boolean;
}
export interface RutaDetalle extends Ruta {
  observaciones?: string | null;
}
export interface GuardarRuta {
  codigo: string;
  nombre: string;
  zona: string;
  origen: string;
  destino: string;
  distanciaKm: number;
  peajeEstimado: number;
  moneda: string;
  activo: boolean;
  observaciones?: string;
}
export interface PaginaRutas {
  items: Ruta[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface ResumenRutas {
  totalRutas: number;
  activas: number;
  inactivas: number;
  porZona: { zona: string; totalRutas: number; activas: number }[];
}
export interface FiltrosRutas {
  busqueda?: string;
  zona?: string;
  activo?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}
