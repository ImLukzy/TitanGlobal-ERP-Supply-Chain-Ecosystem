export type EstadoLicitacion = 'Abierta' | 'Cerrada' | 'Adjudicada' | 'Cancelada';
export type TipoLicitacion = 'ConcursoPrecios' | 'SubastaInversa';

export interface LicitacionListado {
  id: number;
  numero: string;
  titulo: string;
  tipo: TipoLicitacion;
  estado: EstadoLicitacion;
  presupuestoReferencial: number;
  fechaCierre: string;
  totalOfertas: number;
}

export interface PaginaLicitaciones {
  items: LicitacionListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface OfertaLicitacion {
  id: number;
  proveedorId: number;
  proveedorNombre: string;
  monto: number;
  plazoEntregaDias: number;
  observaciones: string | null;
  fechaRegistro: string;
  esGanadora: boolean;
}

export interface LicitacionDetalle {
  id: number;
  numero: string;
  titulo: string;
  descripcion: string | null;
  tipo: TipoLicitacion;
  estado: EstadoLicitacion;
  presupuestoReferencial: number;
  fechaCierre: string;
  proveedorAdjudicadoId: number | null;
  proveedorAdjudicadoNombre: string | null;
  montoAdjudicado: number | null;
  fechaRegistro: string;
  ofertas: OfertaLicitacion[];
}

export interface CrearLicitacionRequest {
  numero: string;
  titulo: string;
  descripcion?: string;
  tipo: TipoLicitacion;
  presupuestoReferencial: number;
  fechaCierre: string;
}

export interface ActualizarLicitacionRequest {
  titulo: string;
  descripcion?: string;
  presupuestoReferencial: number;
  fechaCierre: string;
}

export interface CrearOfertaRequest {
  proveedorId: number;
  monto: number;
  plazoEntregaDias: number;
  observaciones?: string;
}

export interface MejorarOfertaRequest {
  monto: number;
}

export interface AdjudicarLicitacionRequest {
  ofertaId: number;
}

export interface LicitacionResponse {
  id: number;
}

export interface ConteoEstado {
  estado: EstadoLicitacion;
  total: number;
}

export interface ResumenLicitaciones {
  totalLicitaciones: number;
  totalOfertas: number;
  presupuestoAbierto: number;
  montoAdjudicado: number;
  porEstado: ConteoEstado[];
}

export interface FiltrosLicitacion {
  busqueda?: string;
  estado?: EstadoLicitacion | '';
  tipo?: TipoLicitacion | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface ProveedorOpcion {
  id: number;
  razonSocial: string;
}
