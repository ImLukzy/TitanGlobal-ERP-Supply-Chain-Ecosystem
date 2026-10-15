export type EstadoCotizacion = 'Emitida' | 'Aceptada' | 'Rechazada';

export interface CotizacionListado {
  id: number;
  numero: string;
  fecha: string;
  clienteId: number;
  clienteNombre: string;
  fechaValidez: string;
  estado: EstadoCotizacion;
  vencida: boolean;
  total: number;
  ordenCompraNumero: string | null;
}

export interface LineaCotizacion {
  productoId: number;
  productoNombre: string;
  cantidad: number;
  precioUnitario: number;
  descuentoPorcentaje: number;
  subtotal: number;
  descuento: number;
  importe: number;
}

export interface CotizacionDetalle extends CotizacionListado {
  observaciones: string | null;
  motivoRechazo: string | null;
  fechaDecision: string | null;
  ordenCompraId: number | null;
  subtotal: number;
  descuento: number;
  lineas: LineaCotizacion[];
}

export interface LineaCotizacionRequest {
  productoId: number;
  cantidad: number;
  precioUnitario: number;
  descuentoPorcentaje: number;
}

export interface GuardarCotizacionRequest {
  clienteId: number;
  fechaValidez: string;
  observaciones?: string;
  lineas: LineaCotizacionRequest[];
}

export interface RechazarCotizacionRequest {
  motivo: string;
}

export interface PaginaCotizaciones {
  items: CotizacionListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenCotizaciones {
  total: number;
  emitidas: number;
  vencidas: number;
  aceptadas: number;
  rechazadas: number;
  montoVigente: number;
  montoAceptado: number;
  tasaAceptacion: number;
  topClientes: {
    clienteId: number;
    nombre: string;
    cotizaciones: number;
    montoAceptado: number;
  }[];
}

export interface ClienteOpcion {
  id: number;
  nombre: string;
}

export interface ProductoOpcion {
  id: number;
  nombre: string;
  precio: number;
}

export interface FiltrosCotizacion {
  busqueda?: string;
  estado?: EstadoCotizacion | '';
  clienteId?: number | '';
  vencidas?: boolean;
  fechaDesde?: string;
  fechaHasta?: string;
  pagina?: number;
  tamanoPagina?: number;
}

export interface CotizacionResponse {
  id: number;
  numero: string;
}
