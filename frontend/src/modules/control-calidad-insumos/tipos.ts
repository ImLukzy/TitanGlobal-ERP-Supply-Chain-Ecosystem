export const estadosInspeccion = ['Pendiente', 'Aprobada', 'Rechazada', 'Anulada'] as const;
export const resultadosDictamen = ['Aprobada', 'Rechazada'] as const;

export type EstadoInspeccion = (typeof estadosInspeccion)[number];
export type ResultadoDictamen = (typeof resultadosDictamen)[number];

export interface OpcionCatalogo {
  id: number;
  nombre: string;
}

export interface PaginaCatalogo {
  items: OpcionCatalogo[];
  total: number;
  tamanoPagina: number;
}

export interface InspeccionInsumoFila {
  id: number;
  numero: string;
  productoId: number;
  productoNombre: string;
  proveedorId: number;
  proveedorRazonSocial: string;
  lote: string;
  fechaInspeccion: string;
  cantidadRecibida: number;
  unidadMedida: string;
  estado: EstadoInspeccion;
  porcentajeDefectos: number | null;
}

export interface InspeccionInsumoDetalle extends InspeccionInsumoFila {
  cantidadMuestra: number;
  toleranciaDefectosPorcentaje: number;
  inspector: string;
  cantidadDefectuosa: number | null;
  fechaDictamen: string | null;
  observaciones: string | null;
  motivoAnulacion: string | null;
  superaTolerancia: boolean | null;
}

export interface PaginaInspeccionesInsumos {
  items: InspeccionInsumoFila[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface EstadoResumen {
  estado: EstadoInspeccion;
  total: number;
}

export interface ProveedorResumen {
  proveedorId: number;
  proveedorRazonSocial: string;
  aprobadas: number;
  rechazadas: number;
  tasaRechazoPorcentaje: number;
}

export interface ResumenInspeccionesInsumos {
  totalInspecciones: number;
  porEstado: EstadoResumen[];
  tasaRechazoPorcentaje: number;
  porProveedor: ProveedorResumen[];
}

export interface FiltrosInspeccionesInsumos {
  busqueda?: string;
  productoId?: number;
  proveedorId?: number;
  estado?: EstadoInspeccion | '';
  desde?: string;
  hasta?: string;
  pagina: number;
  tamanoPagina: number;
}

export interface GuardarInspeccionRequest {
  productoId: number;
  proveedorId: number;
  lote: string;
  fechaInspeccion: string;
  cantidadRecibida: number;
  cantidadMuestra: number;
  unidadMedida: string;
  toleranciaDefectosPorcentaje: number;
  inspector: string;
}

export interface DictaminarInspeccionRequest {
  resultado: ResultadoDictamen;
  cantidadDefectuosa: number;
  observaciones?: string;
}

export interface AnularInspeccionRequest {
  motivo: string;
}

export interface InspeccionCreada {
  id: number;
  numero: string;
}

export interface IdResponse {
  id: number;
}
