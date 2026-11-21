export const estadosLista = ['Borrador', 'Activa', 'Retirada'] as const;

export type EstadoLista = (typeof estadosLista)[number];
export type DecisionLista = 'activar' | 'retirar';

export interface ProductoSeleccionado {
  id: number;
  nombre: string;
}

interface CabeceraListaMaterial {
  id: number;
  productoId: number;
  productoNombre: string;
  version: number;
  estado: EstadoLista;
}

export interface ListaMaterialFila extends CabeceraListaMaterial {
  totalComponentes: number;
}

export interface ComponenteListaMaterial extends ComponenteListaRequest {
  componenteProductoNombre: string;
}

export interface ListaMaterialDetalle extends CabeceraListaMaterial {
  componentes: ComponenteListaMaterial[];
}

export interface PaginaListasMateriales {
  items: ListaMaterialFila[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosListasMateriales {
  productoId?: number;
  estado: EstadoLista | '';
  pagina: number;
  tamanoPagina: number;
}

export interface ComponenteListaRequest {
  componenteProductoId: number;
  // Se conserva el decimal como texto en la interfaz y se envía como número JSON.
  cantidad: string;
  unidadMedida: string;
}

export interface EditarListaRequest {
  componentes: ComponenteListaRequest[];
}

export interface CrearListaRequest extends EditarListaRequest {
  productoId: number;
  version: number;
}
