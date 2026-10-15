export type TipoPromocion = 'Producto' | 'MontoMinimo';

export interface PromocionListado {
  id: number;
  codigo: string;
  nombre: string;
  tipo: TipoPromocion;
  productoId: number | null;
  productoNombre: string | null;
  montoMinimo: number | null;
  porcentajeDescuento: number;
  segmentoId: number | null;
  segmentoCodigo: string | null;
  fechaInicio: string;
  fechaFin: string;
  activa: boolean;
  vigente: boolean;
}

export type PromocionDetalle = PromocionListado;

export interface PaginaPromociones {
  items: PromocionListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenPromociones {
  total: number;
  activas: number;
  vigentes: number;
  deProducto: number;
  deMontoMinimo: number;
  deSegmento: number;
  porcentajePromedioVigentes: number;
}

export interface FiltrosPromociones {
  busqueda?: string;
  tipo?: TipoPromocion | '';
  segmentoId?: number | '';
  vigentes?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}

export interface GuardarPromocionRequest {
  codigo: string;
  nombre: string;
  tipo: TipoPromocion;
  productoId?: number;
  montoMinimo?: number;
  porcentajeDescuento: number;
  segmentoClienteId?: number;
  fechaInicio: string;
  fechaFin: string;
  activa: boolean;
}

export interface IdResponse { id: number; }
export interface OpcionCatalogo { id: number; nombre: string; }
export interface OpcionSegmento { id: number; codigo: string; nombre: string; }

export interface EvaluarCompraRequest {
  clienteId: number;
  fecha: string;
  lineas: { productoId: number; cantidad: number; precioUnitario: number }[];
}

export interface PromocionAplicada { id: number; codigo: string; nombre: string; porcentajeDescuento: number; }
export interface LineaEvaluada {
  productoId: number;
  productoNombre: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
  promocion: PromocionAplicada | null;
  descuento: number;
  importe: number;
}
export interface EvaluacionCompra {
  clienteId: number;
  clienteNombre: string;
  segmentoCodigo: string | null;
  fecha: string;
  lineas: LineaEvaluada[];
  totalTrasLineas: number;
  promocionCompra: PromocionAplicada | null;
  descuentoCompra: number;
  subtotal: number;
  descuentoTotal: number;
  total: number;
}
