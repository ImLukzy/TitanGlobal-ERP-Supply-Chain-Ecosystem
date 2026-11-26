export type EstadoLote = 'Disponible' | 'Cuarentena' | 'Bloqueado';

export type EstadoSerie = 'Disponible' | 'Asignada' | 'Baja';

export interface LoteListado {
  id: number;
  productoId: number;
  productoNombre: string;
  codigo: string;
  fechaVencimiento: string | null;
  cantidad: number;
  estado: EstadoLote;
  vencido: boolean;
}

export interface LoteDetalle extends LoteListado {
  fechaFabricacion: string | null;
  totalSeries: number;
}

export interface PaginaLotes {
  items: LoteListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenLotes {
  totalLotes: number;
  disponibles: number;
  enCuarentena: number;
  bloqueados: number;
  vencidos: number;
  porVencer: number;
}

export interface GuardarLoteRequest {
  productoId: number;
  codigo: string;
  fechaFabricacion?: string;
  fechaVencimiento?: string;
  cantidad: number;
  estado: EstadoLote;
}

export interface ActualizarLoteRequest {
  fechaFabricacion?: string;
  fechaVencimiento?: string;
  cantidad: number;
  estado: EstadoLote;
}

export interface FiltrosLotes {
  productoId?: number;
  estado?: EstadoLote | '';
  vencidos?: boolean | '';
  busqueda?: string;
  pagina?: number;
  tamanoPagina?: number;
}

export interface SerieListado {
  id: number;
  productoId: number;
  loteId: number | null;
  numeroSerie: string;
  estado: EstadoSerie;
}

export interface PaginaSeries {
  items: SerieListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface GuardarSerieRequest {
  productoId: number;
  loteId?: number;
  numeroSerie: string;
}

export interface CambiarEstadoSerieRequest {
  estado: EstadoSerie;
}

export interface FiltrosSeries {
  productoId?: number;
  loteId?: number;
  estado?: EstadoSerie | '';
  busqueda?: string;
  pagina?: number;
  tamanoPagina?: number;
}

export interface IdLoteSerieResponse {
  id: number;
}
