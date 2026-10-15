export type TipoReclamo = 'Defecto' | 'Retraso' | 'Faltante' | 'Facturacion' | 'Otro';
export type PrioridadReclamo = 'Alta' | 'Media' | 'Baja';
export type EstadoReclamo = 'Abierto' | 'EnProceso' | 'Resuelto' | 'Cerrado' | 'Rechazado';

export interface ReclamoListado {
  id: number;
  numero: string;
  fecha: string;
  clienteId: number;
  clienteNombre: string;
  ordenCompraId: number;
  numeroOrden: string;
  productoNombre: string | null;
  tipo: TipoReclamo;
  prioridad: PrioridadReclamo;
  estado: EstadoReclamo;
  fechaLimite: string;
  vencido: boolean;
}

export interface ReclamoDetalle extends ReclamoListado {
  descripcion: string;
  resolucion: string | null;
  motivoRechazo: string | null;
  fechaInicio: string | null;
  fechaResolucion: string | null;
  fechaCierre: string | null;
  atendidoATiempo: boolean | null;
}

export interface PaginaReclamos {
  items: ReclamoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosReclamos {
  busqueda?: string;
  estado?: EstadoReclamo | '';
  tipo?: TipoReclamo | '';
  prioridad?: PrioridadReclamo | '';
  clienteId?: number | '';
  vencidos?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}

export interface TipoResumenReclamos { tipo: TipoReclamo; reclamos: number; }
export interface ResumenReclamos {
  total: number;
  abiertos: number;
  enProceso: number;
  resueltos: number;
  cerrados: number;
  rechazados: number;
  vencidos: number;
  porTipo: TipoResumenReclamos[];
  horasPromedioResolucion: number;
  cumplimientoPlazo: number;
}

export interface ClienteOpcion { id: number; nombre: string; }
export interface OrdenOpcion { id: number; numero: string; productos: ProductoOpcion[]; }
export interface ProductoOpcion { id: number; nombre: string; }
export interface GuardarReclamoRequest {
  clienteId: number;
  ordenCompraId: number;
  productoId?: number;
  tipo: TipoReclamo;
  prioridad: PrioridadReclamo;
  descripcion: string;
}
export interface RespuestaReclamo { id: number; numero: string; }
export interface AccionReclamo { resolucion?: string; motivo?: string; }
