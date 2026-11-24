export interface Transportista {
  id: number;
  razonSocial: string;
  documentoFiscal: string;
  pais: string;
  tipoServicio: string;
  activo: boolean;
}
export interface GuardarTransportista {
  razonSocial: string;
  documentoFiscal: string;
  pais: string;
  tipoServicio: string;
  activo: boolean;
  contacto?: string | null;
  email?: string | null;
  telefono?: string | null;
  observaciones?: string | null;
}
export interface TransportistaDetalle extends Transportista, GuardarTransportista {}
export interface Flete {
  id: number;
  transportistaId: number;
  transportistaRazonSocial: string;
  referencia: string;
  fecha: string;
  origen: string;
  destino: string;
  importe: string;
  moneda: string;
  estado: string;
}
export interface FleteDetalle extends Flete {
  observaciones?: string | null;
  motivoCancelacion?: string | null;
  fechaCancelacion?: string | null;
}
export interface RegistrarFlete {
  transportistaId: number;
  referencia: string;
  fecha: string;
  origen: string;
  destino: string;
  importe: number;
  moneda: string;
  observaciones?: string | null;
}
export interface Pagina<T> {
  items: T[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface Resumen {
  totalTransportistas: number;
  activos: number;
  inactivos: number;
  porTipoServicio: { tipoServicio: string; totalTransportistas: number; activos: number }[];
}
export interface FiltrosTransportistas {
  busqueda?: string;
  pais?: string;
  tipoServicio?: string;
  activo?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}
export interface FiltrosFletes {
  busqueda?: string;
  transportistaId?: number;
  estado?: string;
  pagina?: number;
  tamanoPagina?: number;
}
