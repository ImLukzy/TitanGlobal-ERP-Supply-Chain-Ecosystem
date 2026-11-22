export type TipoRetorno = 'Pallet' | 'Retornable' | 'DevolucionRuta';
export type EstadoRetorno = 'Programado' | 'Recogido' | 'Recibido' | 'Cancelado';
export interface ValoresRetorno {
  codigo: string;
  referenciaOrigen: string;
  contraparte: string;
  puntoRecogida: string;
  puntoRecepcion: string;
  descripcion: string;
  unidad: string;
  tipo: TipoRetorno;
  fechaProgramada: string;
  cantidadProgramada: string;
  observaciones: string;
}
export interface GuardarRetorno extends Omit<
  ValoresRetorno,
  'cantidadProgramada' | 'observaciones'
> {
  cantidadProgramada: number;
  observaciones?: string;
}
export interface RetornoListado extends Omit<ValoresRetorno, 'observaciones'> {
  id: number;
  estado: EstadoRetorno;
}
export interface RetornoDetalle extends RetornoListado {
  observaciones?: string | null;
  cantidadRecogida?: string | null;
  cantidadRecibida?: string | null;
  noRecogida?: string | null;
  noRecibida?: string | null;
  responsableRecogida?: string | null;
  responsableRecepcion?: string | null;
  fechaRecogida?: string | null;
  fechaRecepcion?: string | null;
  fechaCancelacion?: string | null;
  motivoCancelacion?: string | null;
}
export interface PaginaRetornos {
  items: RetornoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface ResumenRetornos {
  total: number;
  programados: number;
  recogidos: number;
  recibidos: number;
  cancelados: number;
}
export interface FiltrosRetornos {
  busqueda?: string;
  estado?: string;
  tipo?: string;
  pagina?: number;
  tamanoPagina?: number;
}
export interface RegistrarCantidad {
  cantidad: number;
  responsable: string;
}
