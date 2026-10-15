export type MotivoDevolucion = 'Defecto' | 'ErrorPedido' | 'Arrepentimiento' | 'Otro';
export type EstadoDevolucion = 'Solicitada' | 'Aprobada' | 'Recibida' | 'Rechazada';

export interface DevolucionListado {
  id: number;
  numero: string;
  fecha: string;
  clienteId: number;
  clienteNombre: string;
  ordenCompraId: number;
  numeroOrden: string;
  reclamoNumero: string | null;
  motivo: MotivoDevolucion;
  estado: EstadoDevolucion;
  unidades: number;
  total: number;
}

export interface LineaDevolucion {
  productoId: number;
  productoNombre: string;
  cantidad: number;
  precioUnitario: number;
  importe: number;
}

export interface DevolucionDetalle extends DevolucionListado {
  observaciones: string | null;
  motivoRechazo: string | null;
  fechaAprobacion: string | null;
  fechaRecepcion: string | null;
  fechaRechazo: string | null;
  lineas: LineaDevolucion[];
}

export interface PaginaDevoluciones {
  items: DevolucionListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosDevolucion {
  busqueda: string;
  estado: EstadoDevolucion | '';
  motivo: MotivoDevolucion | '';
  clienteId: number | '';
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenDevoluciones {
  total: number;
  solicitadas: number;
  aprobadas: number;
  recibidas: number;
  rechazadas: number;
  montoReembolsado: number;
  porMotivo: { motivo: MotivoDevolucion; devoluciones: number; unidades: number }[];
  tasaDevolucion: number;
}

export interface ProductoDisponible {
  productoId: number;
  productoNombre: string;
  comprado: number;
  devuelto: number;
  disponible: number;
  precioUnitario: number;
}

export interface GuardarDevolucionRequest {
  clienteId: number;
  ordenCompraId: number;
  reclamoPostventaId?: number;
  motivo: MotivoDevolucion;
  observaciones?: string;
  lineas: { productoId: number; cantidad: number }[];
}

export interface ClienteOpcion {
  id: number;
  nombre: string;
}

export interface OrdenOpcion {
  id: number;
  numero: string;
}

export interface OrdenDeCliente {
  ordenId: number;
  numeroOrden: string;
  fecha: string;
  nombreCliente: string;
  detalles: {
    productoId: number;
    nombreProducto: string;
    cantidad: number;
    precioUnitario: number;
  }[];
}

export interface ReclamoOpcion {
  id: number;
  numero: string;
  ordenCompraId: number;
}

export interface RespuestaDevolucion {
  id: number;
  numero: string;
}
