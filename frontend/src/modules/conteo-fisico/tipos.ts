export type EstadoTomaInventario = 'Abierta' | 'Cerrada' | 'Anulada';

export interface DetalleTomaInventario {
  id: number;
  productoId: number;
  productoNombre: string;
  stockSistema: number;
  cantidadContada: number;
  diferencia: number;
  observacion: string | null;
}

export interface TomaInventarioListado {
  id: number;
  codigo: string;
  almacenId: number;
  almacenCodigo: string;
  almacenNombre: string;
  fechaToma: string;
  estado: EstadoTomaInventario;
  responsable: string;
  cantidadProductos: number;
  lineasConDiferencia: number;
}

export interface TomaInventarioDetalle {
  id: number;
  codigo: string;
  almacenId: number;
  almacenCodigo: string;
  almacenNombre: string;
  fechaToma: string;
  estado: EstadoTomaInventario;
  responsable: string;
  observaciones: string | null;
  lineas: DetalleTomaInventario[];
}

export interface PaginaTomasInventario {
  items: TomaInventarioListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenTomasInventario {
  totalTomas: number;
  abiertas: number;
  cerradas: number;
  anuladas: number;
  lineasConDiferencia: number;
}

export interface LineaTomaInventarioRequest {
  productoId: number;
  cantidadContada: number;
  observacion?: string;
}

export interface CrearTomaInventarioRequest {
  codigo: string;
  almacenId: number;
  fechaToma?: string;
  responsable: string;
  observaciones?: string;
  lineas: LineaTomaInventarioRequest[];
}

export interface ActualizarTomaInventarioRequest {
  responsable: string;
  observaciones?: string;
  lineas: LineaTomaInventarioRequest[];
}

export interface FiltrosTomasInventario {
  almacenId?: number;
  estado?: EstadoTomaInventario | '';
  busqueda?: string;
  fechaDesde?: string;
  fechaHasta?: string;
  pagina?: number;
  tamanoPagina?: number;
}
