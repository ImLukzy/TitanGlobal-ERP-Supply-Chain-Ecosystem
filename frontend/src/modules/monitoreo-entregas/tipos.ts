export interface GuardarEntrega {
  codigo: string;
  referenciaGuia: string;
  destinatario: string;
  direccionDestino: string;
  fechaProgramada: string;
  observaciones?: string;
}
export interface RegistrarPod {
  receptor: string;
  documentoReceptor: string;
  referenciaConstancia: string;
  observaciones?: string;
}
export interface Entrega extends Omit<GuardarEntrega, 'observaciones'> {
  id: number;
  estado: string;
}
export interface EventoEntrega {
  id: number;
  estado: string;
  fecha: string;
  descripcion: string;
}
export interface EntregaDetalle extends Entrega {
  observaciones?: string | null;
  fechaSalida?: string | null;
  fechaEntrega?: string | null;
  fechaCancelacion?: string | null;
  receptor?: string | null;
  documentoReceptor?: string | null;
  referenciaConstancia?: string | null;
  observacionesEntrega?: string | null;
  motivoCancelacion?: string | null;
  historial: EventoEntrega[];
}
export interface PaginaEntregas {
  items: Entrega[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface ResumenEntregas {
  total: number;
  programadas: number;
  enRuta: number;
  entregadas: number;
  canceladas: number;
}
export interface FiltrosEntregas {
  busqueda?: string;
  estado?: string;
  pagina?: number;
  tamanoPagina?: number;
}
export interface ValoresEntrega extends Omit<GuardarEntrega, 'observaciones'> {
  observaciones: string;
}
