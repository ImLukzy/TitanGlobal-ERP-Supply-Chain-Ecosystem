export type TipoComprobante = 'Factura' | 'Boleta';
export type EstadoFactura = 'Borrador' | 'Emitida' | 'Anulada';

export interface GuardarLinea {
  productoId: number;
  impuestoId: number | null;
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
  descuentoPorcentaje: number;
}

export interface GuardarFactura {
  clienteId: number;
  ordenCompraId: number | null;
  tipoComprobante: TipoComprobante;
  serie: string;
  numero: number;
  fechaEmision: string;
  fechaVencimiento: string | null;
  moneda: string;
  observaciones: string | null;
  detalles: GuardarLinea[];
}

export interface FacturaFila extends Omit<GuardarFactura, 'ordenCompraId' | 'observaciones' | 'detalles'> {
  id: number;
  clienteNombre: string;
  clienteRuc: string;
  numeroComprobante: string;
  estado: EstadoFactura;
  subtotal: number;
  totalDescuento: number;
  totalImpuesto: number;
  total: number;
  cantidadDetalles: number;
}

export interface LineaFactura extends GuardarLinea {
  id: number;
  productoNombre: string;
  impuestoNombre: string | null;
  porcentajeImpuesto: number;
  baseImponible: number;
  importeImpuesto: number;
  importeDescuento: number;
  subtotal: number;
  total: number;
}

export interface FacturaDetalle extends Omit<FacturaFila, 'cantidadDetalles'> {
  ordenCompraId: number | null;
  numeroOrdenCompra: string | null;
  observaciones: string | null;
  fechaCreacion: string;
  fechaAnulacion: string | null;
  motivoAnulacion: string | null;
  detalles: LineaFactura[];
}

export interface FiltrosFactura {
  busqueda: string;
  clienteId: string;
  tipoComprobante: string;
  estado: string;
  moneda: string;
  fechaDesde: string;
  fechaHasta: string;
}

export interface PaginaFacturas {
  items: FacturaFila[];
  pagina: number;
  tamanoPagina: number;
  totalRegistros: number;
  totalPaginas: number;
}

export interface ResumenFacturas {
  moneda: string;
  totalComprobantes: number;
  totalFacturas: number;
  totalBoletas: number;
  totalBorradores: number;
  totalEmitidas: number;
  totalAnuladas: number;
  subtotalFacturado: number;
  totalDescuentos: number;
  totalImpuestos: number;
  totalFacturado: number;
}
