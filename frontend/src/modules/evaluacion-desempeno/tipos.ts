export type PeriodoCiclo = 'Semestral' | 'Anual';

export type EstadoCiclo = 'Configuracion' | 'Abierto' | 'Cerrado';

export type EstadoEvaluacion = 'Borrador' | 'Enviada' | 'Cerrada';

export interface IdResponse {
  id: number;
}

export interface FiltrosCiclos {
  busqueda: string;
  estado: '' | EstadoCiclo;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosEvaluaciones {
  cicloId: number | null;
  estado: '' | EstadoEvaluacion;
  busqueda: string;
  pagina: number;
  tamanoPagina: number;
}

export interface CicloListado {
  id: number;
  codigo: string;
  nombre: string;
  periodo: PeriodoCiclo;
  fechaInicioPeriodo: string;
  fechaFinPeriodo: string;
  fechaApertura: string;
  fechaCierre: string;
  estado: EstadoCiclo;
}

export interface PaginaCiclos {
  items: CicloListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface Competencia {
  id: number;
  nombre: string;
  descripcion: string | null;
  peso: number;
}

export interface EvaluacionesPorEstado {
  borrador: number;
  enviada: number;
  cerrada: number;
}

export interface CicloDetalle extends CicloListado {
  sumaPesos: number;
  competencias: Competencia[];
  evaluaciones: EvaluacionesPorEstado;
}

export interface DistribucionPuntaje {
  etiqueta: string;
  cantidad: number;
}

export interface PromedioCompetencia {
  competencia: string;
  promedio: number;
}

export interface ResultadosCiclo {
  evaluadas: number;
  puntajePromedio: number;
  distribucion: DistribucionPuntaje[];
  porCompetencia: PromedioCompetencia[];
}

export interface GuardarCicloRequest {
  codigo: string;
  nombre: string;
  periodo: string;
  fechaInicioPeriodo: string;
  fechaFinPeriodo: string;
  fechaApertura: string;
  fechaCierre: string;
}

export interface GuardarCompetenciaRequest {
  nombre: string;
  descripcion: string | null;
  peso: number;
}

export interface EvaluacionListado {
  id: number;
  cicloId: number;
  cicloCodigo: string;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  evaluadorId: number;
  evaluadorNombre: string;
  estado: EstadoEvaluacion;
  puntajeFinal: number | null;
  etiqueta: string | null;
}

export interface PaginaEvaluaciones {
  items: EvaluacionListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface CalificacionEvaluacion {
  competenciaId: number;
  competencia: string;
  peso: number;
  calificacion: number | null;
}

export interface EvaluacionDetalle extends EvaluacionListado {
  comentario: string | null;
  fechaEnvio: string | null;
  fechaCierre: string | null;
  calificaciones: CalificacionEvaluacion[];
}

export interface CrearEvaluacionRequest {
  cicloId: number;
  empleadoId: number;
  evaluadorId: number;
}

export interface GuardarCalificacionesRequest {
  calificaciones: { competenciaId: number; calificacion: number }[];
  comentario: string | null;
}
