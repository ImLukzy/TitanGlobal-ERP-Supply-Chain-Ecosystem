export interface ProductoFila {
  id: number;
  nombre: string;
  categoria: string;
  precio: number;
  tieneDescripcion: boolean;
}

export interface PaginaProductos {
  items: ProductoFila[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ProductoDetalle extends ProductoFila {
  descripcion: string | null;
  unidadesVendidas: number;
  montoVendido: number;
  clientesDistintos: number;
}

export interface FiltrosProducto {
  busqueda: string;
  categoria: string;
  precioMin: string;
  precioMax: string;
  pagina: number;
  tamanoPagina?: number;
}

export interface ProductoRequest {
  nombre: string;
  categoria: string;
  precio: number;
  descripcion?: string;
}

export interface ResumenProductos {
  totalProductos: number;
  productosSinDescripcion: number;
  precioPromedio: number;
  porCategoria: { categoria: string; totalProductos: number; precioPromedio: number; precioMaximo: number }[];
  masVendidos: { productoId: number; nombre: string; unidadesVendidas: number; montoVendido: number }[];
}
