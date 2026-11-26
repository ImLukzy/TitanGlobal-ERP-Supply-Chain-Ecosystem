export interface CategoriaProducto {
  id: number;
  nombre: string;
  descripcion: string | null;
  activa: boolean;
}

export interface GuardarCategoriaProductoRequest {
  nombre: string;
  descripcion?: string;
}

export interface ResumenCategoriasProducto {
  totalCategorias: number;
  categoriasActivas: number;
  categoriasInactivas: number;
}

export interface FiltrosCategoriasProducto {
  buscar?: string;
  incluirInactivas?: boolean;
}

export interface IdCategoriaResponse {
  id: number;
}
