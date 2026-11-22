export type TipoSiniestro = 'Accidente' | 'Robo' | 'DanoMaterial';
export type EstadoSiniestro = 'Registrado' | 'EnReclamo' | 'Cerrado' | 'Anulado';
export type ResultadoReclamo = 'Aceptado' | 'Rechazado';
export interface ValoresSiniestro {
  codigo: string;
  placa: string;
  lugar: string;
  descripcion: string;
  tipo: TipoSiniestro;
  fechaIncidente: string;
  observaciones: string;
}
export interface GuardarSiniestro extends Omit<ValoresSiniestro, 'observaciones'> {
  observaciones?: string;
}
export interface SiniestroListado extends Omit<ValoresSiniestro, 'observaciones'> {
  id: number;
  estado: EstadoSiniestro;
}
export interface SiniestroDetalle extends SiniestroListado {
  observaciones?: string | null;
  numeroPoliza?: string | null;
  aseguradora?: string | null;
  numeroReclamo?: string | null;
  moneda?: string | null;
  montoReclamado?: string | null;
  resultado?: ResultadoReclamo | null;
  montoReconocido?: string | null;
  resolucion?: string | null;
  motivoAnulacion?: string | null;
  fechaReclamo?: string | null;
  fechaCierre?: string | null;
  fechaAnulacion?: string | null;
}
export interface ValoresReclamo {
  numeroPoliza: string;
  aseguradora: string;
  numeroReclamo: string;
  moneda: string;
  montoReclamado: string;
}
export interface RegistrarReclamo extends Omit<ValoresReclamo, 'montoReclamado'> {
  montoReclamado: number;
}
export interface ValoresCierre {
  resultado: ResultadoReclamo;
  montoReconocido: string;
  resolucion: string;
}
export interface CerrarReclamo extends Omit<ValoresCierre, 'montoReconocido'> {
  montoReconocido: number;
}
export interface PaginaSiniestros {
  items: SiniestroListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface ResumenSiniestros {
  total: number;
  registrados: number;
  enReclamo: number;
  cerrados: number;
  anulados: number;
}
export interface FiltrosSiniestros {
  busqueda?: string;
  estado?: string;
  tipo?: string;
  pagina?: number;
  tamanoPagina?: number;
}
