export type TipoTurno = 'Fijo' | 'Rotativo';

export type EstadoAsignacion = 'Vigente' | 'Futura' | 'Finalizada';

export type FiltroActivo = 'Activos' | 'Inactivos' | 'Todos';

export type ObservacionDia = 'SinTurno' | 'Descanso' | 'Cesado';

export interface FiltrosTurnos {
  busqueda: string;
  tipo: '' | TipoTurno;
  activo: FiltroActivo;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosAsignaciones {
  empleadoId: number | null;
  turnoId: number | null;
  estado: '' | EstadoAsignacion;
  pagina: number;
  tamanoPagina: number;
}

export interface TurnoListado {
  id: number;
  codigo: string;
  nombre: string;
  tipo: TipoTurno;
  horaEntrada: string;
  horaSalida: string;
  cruzaMedianoche: boolean;
  toleranciaMinutos: number;
  diasLaborables: number[];
  diasTrabajo: number | null;
  diasDescanso: number | null;
  fechaInicioCiclo: string | null;
  activo: boolean;
}

export interface TurnoDetalle extends TurnoListado {
  totalAsignaciones: number;
  empleadosConTurnoHoy: number;
}

export interface PaginaTurnos {
  items: TurnoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenTurnos {
  totalTurnos: number;
  activos: number;
  fijos: number;
  rotativos: number;
  empleadosConTurnoHoy: number;
  empleadosActivosSinTurnoHoy: number;
}

export interface GuardarTurnoRequest {
  codigo: string;
  nombre: string;
  tipo: TipoTurno;
  horaEntrada: string;
  horaSalida: string;
  cruzaMedianoche: boolean;
  toleranciaMinutos: number;
  diasLaborables?: number[];
  diasTrabajo?: number;
  diasDescanso?: number;
  fechaInicioCiclo?: string;
}

export interface AsignacionTurnoListado {
  id: number;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  turnoId: number;
  turnoCodigo: string;
  turnoNombre: string;
  fechaInicio: string;
  fechaFin: string | null;
  estado: EstadoAsignacion;
}

export interface PaginaAsignacionesTurno {
  items: AsignacionTurnoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface GuardarAsignacionTurnoRequest {
  empleadoId: number;
  turnoId: number;
  fechaInicio: string;
  fechaFin?: string;
}

export interface FinalizarAsignacionTurnoRequest {
  fechaFin: string;
}

export interface DiaProgramado {
  fecha: string;
  turnoId: number | null;
  turnoCodigo: string | null;
  laborable: boolean;
  horaEntrada: string | null;
  horaSalida: string | null;
  cruzaMedianoche: boolean;
  toleranciaMinutos: number | null;
  observacion: ObservacionDia | null;
}

export interface IdResponse {
  id: number;
}
