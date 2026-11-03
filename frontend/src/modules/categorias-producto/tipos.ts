export interface CategoriaProducto {
    id: number;
    nombre: string;
    descripcion: string | null;
    activa: boolean;
}

export interface ResumenCategoriasProducto {
    totalCategorias: number;
    categoriasActivas: number;
    categoriasInactivas: number;
}

export interface GuardarCategoriaProductoRequest {
    nombre: string;
    descripcion?: string;
}

export interface FiltrosCategoriaProducto {
    buscar?: string;
    incluirInactivas?: boolean;
}

export interface IdResponse {
    id: number;
}