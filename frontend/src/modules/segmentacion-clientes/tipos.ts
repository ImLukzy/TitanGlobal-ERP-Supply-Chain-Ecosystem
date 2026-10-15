export interface SegmentoListado {
  id: number;
  codigo: string;
  nombre: string;
  prioridad: number;
  montoMinimo: number;
  ordenesMinimas: number;
  activo: boolean;
  clientes: number;
}

export interface SegmentoDetalle extends SegmentoListado {
  descripcion: string | null;
}

export interface GuardarSegmentoRequest {
  codigo: string;
  nombre: string;
  descripcion: string | null;
  prioridad: number;
  montoMinimo: number;
  ordenesMinimas: number;
  activo: boolean;
}

export interface ClienteClasificado {
  clienteId: number;
  nombre: string;
  ruc: string;
  totalOrdenes: number;
  montoTotal: number;
  segmentoId: number | null;
  segmentoCodigo: string | null;
  segmentoNombre: string | null;
}

export interface Pagina<T> {
  items: T[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface SegmentoClientesResumen {
  segmentoId: number;
  codigo: string;
  nombre: string;
  clientes: number;
  montoTotal: number;
}

export interface ResumenSegmentacion {
  totalClientes: number;
  clientesSegmentados: number;
  clientesSinSegmento: number;
  porSegmento: SegmentoClientesResumen[];
}

export interface FiltrosSegmentos {
  busqueda?: string;
  activo?: boolean | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface FiltrosClasificacion {
  segmentoId?: number | '';
  sinSegmento?: boolean | '';
  busqueda?: string;
  pagina?: number;
  tamanoPagina?: number;
}

export interface IdResponse {
  id: number;
}
