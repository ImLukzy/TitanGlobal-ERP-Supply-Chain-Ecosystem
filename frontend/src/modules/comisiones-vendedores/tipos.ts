export type EstadoLiquidacion = 'Pendiente' | 'Calculada' | 'Pagada';

export interface VendedorListado {
  id: number;
  codigo: string;
  nombre: string;
  porcentajeComision: number;
  activo: boolean;
  ordenesAsignadas: number;
}

export interface VendedorDetalle extends VendedorListado {
  email: string | null;
  ventasTotales: number;
}

export interface Pagina<T> {
  items: T[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosVendedor {
  busqueda?: string;
  activo?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}

export interface GuardarVendedorRequest {
  codigo: string;
  nombre: string;
  email: string | null;
  porcentajeComision: number;
  activo: boolean;
}

export interface VendedorResponse {
  id: number;
}

export interface OrdenVendedor {
  ordenCompraId: number;
  numeroOrden: string;
  fecha: string;
  clienteNombre: string;
  total: number;
  vendedorId: number | null;
  vendedorCodigo: string | null;
  vendedorNombre: string | null;
}

export interface FiltrosOrden {
  vendedorId?: number;
  sinAsignar?: boolean;
  anio: number;
  mes: number;
  pagina?: number;
  tamanoPagina?: number;
}

export interface AsignarVendedorRequest {
  vendedorId: number;
}

export interface ComisionVendedor {
  vendedorId: number;
  codigo: string;
  nombre: string;
  ordenes: number;
  ventas: number;
  porcentaje: number;
  comision: number;
  estado: EstadoLiquidacion;
  liquidacionId: number | null;
}

export interface ComisionesMes {
  anio: number;
  mes: number;
  liquidado: boolean;
  filas: ComisionVendedor[];
  totalVentas: number;
  totalComision: number;
}

export interface Liquidacion extends ComisionVendedor {
  id: number;
  anio: number;
  mes: number;
  fechaCalculo: string;
  fechaPago: string | null;
}
