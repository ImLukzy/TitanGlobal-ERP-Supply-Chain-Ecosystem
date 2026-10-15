export interface SolicitudListado {
  id: number;
  numero: string;
  fecha: string;
  solicitante: string;
  proveedorId: number | null;
  proveedorNombre: string | null;
  estado: EstadoSolicitud;
  totalLineas: number;
  montoEstimado: number;
}

export interface PaginaSolicitudes {
  items: SolicitudListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export type EstadoSolicitud = 'Pendiente' | 'Aprobada' | 'Rechazada';

export interface LineaSolicitud {
  productoId: number;
  productoNombre: string;
  cantidad: number;
  precioEstimado: number;
  subtotal: number;
}

export interface SolicitudDetalle extends SolicitudListado {
  observaciones: string | null;
  motivoRechazo: string | null;
  fechaDecision: string | null;
  lineas: LineaSolicitud[];
}

export interface LineaSolicitudRequest {
  productoId: number;
  cantidad: number;
  precioEstimado: number;
}

export interface GuardarSolicitudRequest {
  solicitante: string;
  proveedorId?: number;
  observaciones?: string;
  lineas: LineaSolicitudRequest[];
}

export interface RechazarSolicitudRequest {
  motivo: string;
}

export interface ProductoSolicitado {
  productoId: number;
  nombre: string;
  unidades: number;
}

export interface ResumenSolicitudes {
  total: number;
  pendientes: number;
  aprobadas: number;
  rechazadas: number;
  montoPendiente: number;
  montoAprobado: number;
  topProductosSolicitados: ProductoSolicitado[];
}

export interface FiltrosSolicitud {
  busqueda?: string;
  estado?: EstadoSolicitud | '';
  proveedorId?: number | '';
  fechaDesde?: string;
  fechaHasta?: string;
  pagina?: number;
  tamanoPagina?: number;
}

export interface SolicitudResponse {
  id: number;
  numero: string;
}

export interface ProveedorOpcion {
  id: number;
  razonSocial: string;
}

export interface ProductoOpcion {
  id: number;
  nombre: string;
  precio: number;
}
