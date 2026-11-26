export interface LineaValores {
  clave: string;
  sku: string;
  descripcion: string;
  unidadMedida: string;
  cantidad: string;
}
export interface LineaRequest {
  sku: string;
  descripcion: string;
  unidadMedida: string;
  cantidad: number;
}
export interface LineaDetalle {
  id: number;
  posicion: number;
  sku: string;
  descripcion: string;
  unidadMedida: string;
  cantidad: string;
}
export interface CabeceraGuia {
  serie: string;
  numero: number;
  fechaInicioTraslado: string;
  remitenteNombre: string;
  remitenteDocumento: string;
  destinatarioNombre: string;
  destinatarioDocumento: string;
  direccionOrigen: string;
  direccionDestino: string;
  motivoTraslado: string;
  transportista: string;
  conductor: string;
  placa: string;
  pesoBrutoKg: number;
  observaciones?: string;
}
export interface GuardarGuia extends CabeceraGuia {
  lineas: LineaRequest[];
}
export interface Guia {
  id: number;
  serie: string;
  numero: number;
  estado: string;
  fechaInicioTraslado: string;
  destinatarioNombre: string;
  destinatarioDocumento: string;
  direccionDestino: string;
  pesoBrutoKg: string;
  totalLineas: number;
}
export interface GuiaDetalle extends Omit<CabeceraGuia, 'pesoBrutoKg' | 'observaciones'> {
  id: number;
  estado: string;
  totalLineas: number;
  pesoBrutoKg: string;
  observaciones?: string | null;
  fechaEmision?: string | null;
  fechaAnulacion?: string | null;
  motivoAnulacion?: string | null;
  lineas: LineaDetalle[];
}
export interface PaginaGuias {
  items: Guia[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface ResumenGuias {
  total: number;
  borradores: number;
  emitidas: number;
  anuladas: number;
}
export interface ValoresGuia extends Omit<
  CabeceraGuia,
  'numero' | 'pesoBrutoKg' | 'observaciones'
> {
  numero: string;
  pesoBrutoKg: string;
  observaciones: string;
  lineas: LineaValores[];
}
export interface FiltrosGuias {
  busqueda?: string;
  estado?: string;
  pagina?: number;
  tamanoPagina?: number;
}
