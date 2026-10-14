export interface ProductoResumen {
  id: number;
  nombre: string;
  precio: number;
}

export interface DetalleOrdenItem {
  productoId: number;
  nombreProducto: string;
  cantidad: number;
  precioUnitario: number;
}

export interface OrdenCompraJerarquica {
  ordenId: number;
  numeroOrden: string;
  fecha: string;
  nombreCliente: string;
  detalles: DetalleOrdenItem[];
}

export interface ClienteFrecuencia {
  clienteId: number;
  totalOrdenes: number;
}

export interface CrearOrdenCompraRequest {
  numeroOrden: string;
  clienteId: number;
  total: number;
  descripcion?: string;
}

export interface CrearOrdenCompraResponse {
  id: number;
}

export interface TotalVentasResponse {
  totalVentas: number;
}

export interface PromedioCompraResponse {
  clienteId: number;
  promedioCompra: number;
}
