export type EstadoOrdenAbastecimiento = 'Emitida' | 'Recibida' | 'Anulada';

export interface OrdenAbastecimientoListado {
  id: number;
  numero: string;
  fecha: string;
  proveedorId: number;
  proveedorNombre: string;
  solicitudNumero: string | null;
  estado: EstadoOrdenAbastecimiento;
  fechaEntregaEstimada: string;
  atrasada: boolean;
  montoTotal: number;
}

export interface LineaOrdenAbastecimiento {
  productoId: number;
  productoNombre: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
}

export interface OrdenAbastecimientoDetalle extends OrdenAbastecimientoListado {
  solicitudCompraId: number | null;
  fechaRecepcion: string | null;
  observaciones: string | null;
  motivoAnulacion: string | null;
  lineas: LineaOrdenAbastecimiento[];
}

export interface LineaOrdenAbastecimientoRequest {
  productoId: number;
  cantidad: number;
  precioUnitario: number;
}

export interface GuardarOrdenAbastecimientoRequest {
  proveedorId: number;
  fechaEntregaEstimada: string;
  observaciones?: string;
  lineas: LineaOrdenAbastecimientoRequest[];
}

export interface GenerarDesdeSolicitudRequest {
  proveedorId?: number;
  fechaEntregaEstimada: string;
  observaciones?: string;
}

export interface AnularOrdenRequest {
  motivo: string;
}

export interface OrdenAbastecimientoResponse {
  id: number;
  numero: string;
}

export interface PaginaOrdenesAbastecimiento {
  items: OrdenAbastecimientoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosOrdenAbastecimiento {
  busqueda?: string;
  estado?: EstadoOrdenAbastecimiento | '';
  proveedorId?: number | '';
  fechaDesde?: string;
  fechaHasta?: string;
  pagina?: number;
  tamanoPagina?: number;
}

export interface ProveedorMonto {
  proveedorId: number;
  razonSocial: string;
  ordenes: number;
  monto: number;
}

export interface ResumenOrdenesAbastecimiento {
  total: number;
  emitidas: number;
  recibidas: number;
  anuladas: number;
  atrasadas: number;
  montoPorRecibir: number;
  montoRecibido: number;
  topProveedores: ProveedorMonto[];
}

export interface ProveedorOrdenOpcion {
  id: number;
  razonSocial: string;
}

export interface ProductoOrdenOpcion {
  id: number;
  nombre: string;
  precio: number;
}
