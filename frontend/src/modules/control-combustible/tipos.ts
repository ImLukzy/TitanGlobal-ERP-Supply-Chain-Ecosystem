export interface GuardarVale {
  codigo: string;
  placa: string;
  estacion: string;
  fechaCarga: string;
  tipoCombustible: string;
  litros: number;
  precioLitro: number;
  moneda: string;
  odometroInicial: number;
  odometroFinal: number | null;
  observaciones?: string;
}
export interface Vale extends Omit<GuardarVale, 'litros' | 'precioLitro' | 'observaciones'> {
  id: number;
  estado: string;
  litros: string;
  precioLitro: string;
  importe: string;
  kilometrosRecorridos: number | null;
  kmPorLitro: string | null;
  litrosPor100Km: string | null;
}
export interface ValeDetalle extends Vale {
  observaciones?: string | null;
  fechaConfirmacion?: string | null;
  fechaAnulacion?: string | null;
  motivoAnulacion?: string | null;
}
export interface ValoresVale extends Omit<
  GuardarVale,
  'litros' | 'precioLitro' | 'odometroInicial' | 'odometroFinal' | 'observaciones'
> {
  litros: string;
  precioLitro: string;
  odometroInicial: string;
  odometroFinal: string;
  observaciones: string;
}
export interface PaginaVales {
  items: Vale[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface ResumenVales {
  total: number;
  borradores: number;
  confirmados: number;
  anulados: number;
}
export interface FiltrosVales {
  busqueda?: string;
  estado?: string;
  tipoCombustible?: string;
  pagina?: number;
  tamanoPagina?: number;
}
