export interface LineaValores {
  clave: string;
  sku: string;
  descripcion: string;
  cantidad: string;
  cantidadRecogida: string;
  unidadesPorBulto: string;
  largoCm: string;
  anchoCm: string;
  altoCm: string;
  pesoBultoKg: string;
}
export interface LineaRequest {
  sku: string;
  descripcion: string;
  cantidad: number;
  cantidadRecogida: number;
  unidadesPorBulto: number;
  largoCm: number;
  anchoCm: number;
  altoCm: number;
  pesoBultoKg: number;
}
export interface LineaDetalle {
  id: number;
  posicion: number;
  sku: string;
  descripcion: string;
  cantidad: number;
  cantidadRecogida: number;
  unidadesPorBulto: number;
  largoCm: string;
  anchoCm: string;
  altoCm: string;
  pesoBultoKg: string;
  bultos: number;
  volumenM3: string;
  pesoKg: string;
}
export interface GuardarCarga {
  codigo: string;
  nombre: string;
  observaciones?: string;
  lineas: LineaRequest[];
}
export interface Carga {
  id: number;
  codigo: string;
  nombre: string;
  estado: string;
  totalUnidades: number;
  unidadesRecogidas: number;
  totalBultos: number;
  volumenM3: string;
  pesoKg: string;
}
export interface CargaDetalle extends Carga {
  observaciones?: string | null;
  fechaConfirmacion?: string | null;
  motivoCancelacion?: string | null;
  fechaCancelacion?: string | null;
  lineas: LineaDetalle[];
}
export interface PaginaCargas {
  items: Carga[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface ResumenCargas {
  total: number;
  borradores: number;
  confirmadas: number;
  canceladas: number;
}
export interface ValoresCarga {
  codigo: string;
  nombre: string;
  observaciones: string;
  lineas: LineaValores[];
}
export interface FiltrosCargas {
  busqueda?: string;
  estado?: string;
  pagina?: number;
  tamanoPagina?: number;
}
