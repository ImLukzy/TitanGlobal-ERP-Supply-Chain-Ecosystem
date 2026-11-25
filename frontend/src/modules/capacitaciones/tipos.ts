export type TipoCurso = 'Interno' | 'Externo' | 'Virtual';

export type EstadoProgramacion = 'Programada' | 'Realizada' | 'Cancelada';

export type EstadoInscripcion = 'Inscrito' | 'Aprobado' | 'Desaprobado' | 'Retirado';

export type FiltroActivo = 'Activos' | 'Inactivos' | 'Todos';

export interface IdResponse {
  id: number;
}

export interface Pagina<T> {
  items: T[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface CursoListado {
  id: number;
  codigo: string;
  nombre: string;
  horas: number;
  tipo: TipoCurso;
  proveedor: string | null;
  activo: boolean;
}

export type PaginaCursos = Pagina<CursoListado>;

export interface GuardarCursoRequest {
  codigo: string;
  nombre: string;
  horas: number;
  tipo: string;
  proveedor: string | null;
}

export interface FiltrosCursos {
  busqueda: string;
  tipo: '' | TipoCurso;
  activo: FiltroActivo;
  pagina: number;
  tamanoPagina: number;
}

export interface ProgramacionListado {
  id: number;
  cursoId: number;
  cursoCodigo: string;
  cursoNombre: string;
  horas: number;
  anio: number;
  fechaInicio: string;
  fechaFin: string;
  cupo: number;
  inscritos: number;
  cupoLibre: number;
  lugar: string | null;
  estado: EstadoProgramacion;
}

export type PaginaProgramaciones = Pagina<ProgramacionListado>;

export interface Certificacion {
  id: number;
  codigo: string;
  fechaEmision: string;
  fechaVencimiento: string | null;
  vencida: boolean;
}

export interface Inscripcion {
  id: number;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  estado: EstadoInscripcion;
  nota: number | null;
  fechaInscripcion: string;
  certificacion: Certificacion | null;
}

export interface ProgramacionDetalle extends ProgramacionListado {
  inscripciones: Inscripcion[];
}

export interface GuardarProgramacionRequest {
  cursoId: number;
  fechaInicio: string;
  fechaFin: string;
  cupo: number;
  lugar: string | null;
}

export interface ResultadoInscripcionRequest {
  inscripcionId: number;
  aprobado: boolean;
  nota: number | null;
}

export interface RegistrarResultadosRequest {
  resultados: ResultadoInscripcionRequest[];
}

export interface EmitirCertificacionRequest {
  codigo: string;
  fechaEmision: string;
  fechaVencimiento: string | null;
}

export interface FiltrosProgramaciones {
  anio: number;
  cursoId: number | null;
  estado: '' | EstadoProgramacion;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenCapacitaciones {
  anio: number;
  cursosActivos: number;
  programadas: number;
  realizadas: number;
  inscritos: number;
  horasTotales: number;
  certificacionesVencidas: number;
}

export interface HorasEmpleado {
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  cursos: number;
  horas: number;
}

export interface PaginaHoras extends Pagina<HorasEmpleado> {
  anio: number;
}

export interface FiltrosHoras {
  anio: number;
  busqueda: string;
  pagina: number;
  tamanoPagina: number;
}

export interface CursoEmpleado {
  inscripcionId: number;
  programacionId: number;
  cursoCodigo: string;
  cursoNombre: string;
  horas: number;
  fechaInicio: string;
  fechaFin: string;
  estado: EstadoInscripcion;
  nota: number | null;
}

export interface CertificacionEmpleado {
  id: number;
  inscripcionId: number;
  cursoCodigo: string;
  cursoNombre: string;
  codigo: string;
  fechaEmision: string;
  fechaVencimiento: string | null;
  vencida: boolean;
}

export interface CapacitacionEmpleado {
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  anio: number;
  horas: number;
  cursos: CursoEmpleado[];
  certificaciones: CertificacionEmpleado[];
}
