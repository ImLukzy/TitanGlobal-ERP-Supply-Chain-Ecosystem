export type CalificacionEvaluacion = 'Excelente' | 'Bueno' | 'Regular' | 'Deficiente';

export interface EvaluacionListado {
  id: number;
  proveedorId: number;
  proveedorNombre: string;
  periodo: string;
  puntajeFinal: number;
  calificacion: CalificacionEvaluacion;
}

export interface PaginaEvaluaciones {
  items: EvaluacionListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface EvaluacionDetalle extends EvaluacionListado {
  puntajeCumplimiento: number;
  puntajeCalidad: number;
  puntajePlazos: number;
  observaciones: string | null;
  fechaRegistro: string;
}

export interface PuntajesEvaluacionRequest {
  puntajeCumplimiento: number;
  puntajeCalidad: number;
  puntajePlazos: number;
  observaciones?: string;
}

export interface CrearEvaluacionRequest extends PuntajesEvaluacionRequest {
  proveedorId: number;
  periodo: string;
}

export type ActualizarEvaluacionRequest = PuntajesEvaluacionRequest;

export interface EvaluacionResponse {
  id: number;
}

export interface ConteoCalificacion {
  calificacion: CalificacionEvaluacion;
  total: number;
}

export interface RankingProveedor {
  proveedorId: number;
  proveedorNombre: string;
  evaluaciones: number;
  promedio: number;
  ultimaCalificacion: CalificacionEvaluacion;
}

export interface ResumenEvaluaciones {
  totalEvaluaciones: number;
  proveedoresEvaluados: number;
  promedioGeneral: number;
  porCalificacion: ConteoCalificacion[];
  ranking: RankingProveedor[];
}

export interface FiltrosEvaluacion {
  proveedorId?: number | '';
  periodo?: string;
  calificacion?: CalificacionEvaluacion | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface ProveedorOpcion {
  id: number;
  razonSocial: string;
}
