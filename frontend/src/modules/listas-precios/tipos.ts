export interface ProductoOpcion {
  id: number;
  nombre: string;
  precioBase: number;
}
export interface ClienteOpcion {
  id: number;
  nombre: string;
}
export interface SegmentoOpcion {
  id: number;
  codigo: string;
  nombre: string;
}
export interface ListaListado {
  id: number;
  codigo: string;
  nombre: string;
  segmentoId: number | null;
  segmentoCodigo: string | null;
  prioridad: number;
  fechaInicio: string;
  fechaFin: string | null;
  activa: boolean;
  vigente: boolean;
  productos: number;
}
export interface ItemLista {
  productoId: number;
  productoNombre: string;
  precioBase: number;
  precio: number;
  diferenciaPorcentaje: number;
}
export interface ListaDetalle extends ListaListado {
  items: ItemLista[];
}
export interface PaginaListas {
  items: ListaListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface ResumenListas {
  total: number;
  activas: number;
  vigentes: number;
  deSegmento: number;
  generales: number;
  productosConPrecioEspecial: number;
}
export interface FiltrosListas {
  busqueda?: string;
  segmentoId?: number | "";
  vigentes?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}
export interface GuardarLista {
  codigo: string;
  nombre: string;
  segmentoClienteId?: number;
  prioridad: number;
  fechaInicio: string;
  fechaFin?: string;
  activa: boolean;
  items: { productoId: number; precio: number }[];
}
export interface PrecioResuelto {
  productoId: number;
  productoNombre: string;
  clienteId: number | null;
  clienteNombre: string | null;
  segmentoCodigo: string | null;
  fecha: string;
  precio: number;
  precioBase: number;
  origen: "Segmento" | "General" | "Base";
  listaId: number | null;
  listaCodigo: string | null;
}
