export type TipoMarcacion = 'Entrada' | 'Salida';

export type OrigenMarcacion = 'Reloj' | 'Manual';

export type EstadoJornada = 'Asistencia' | 'Tardanza' | 'SinMarcacion' | 'Falta' | 'DescansoTrabajado';

export type EstadoHorasExtra = 'NoAplica' | 'Pendiente' | 'Aprobada' | 'Rechazada';

export interface FiltrosJornadas {
  empleadoId: number | null;
  desde: string;
  hasta: string;
  estado: '' | EstadoJornada;
  estadoHorasExtra: '' | EstadoHorasExtra;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosMarcaciones {
  empleadoId: number | null;
  desde: string;
  hasta: string;
  tipo: '' | TipoMarcacion;
  origen: '' | OrigenMarcacion;
  pagina: number;
  tamanoPagina: number;
}

export interface Marcacion {
  id: number;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  tipo: TipoMarcacion;
  instante: string;
  origen: OrigenMarcacion;
  dispositivo: string | null;
  numeroRegistro: string | null;
  observacion: string | null;
}

export interface PaginaMarcaciones {
  items: Marcacion[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface JornadaAsistencia {
  id: number;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  fecha: string;
  turnoId: number | null;
  turnoCodigo: string | null;
  horaEntradaProgramada: string | null;
  horaSalidaProgramada: string | null;
  toleranciaMinutos: number;
  entrada: string | null;
  salida: string | null;
  minutosTrabajados: number;
  minutosTardanza: number;
  minutosExtra: number;
  estado: EstadoJornada;
  estadoHorasExtra: EstadoHorasExtra;
  decididoPor: string | null;
  fechaDecision: string | null;
  motivoRechazo: string | null;
}

export interface PaginaJornadas {
  items: JornadaAsistencia[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenAsistencia {
  jornadas: number;
  asistencias: number;
  tardanzas: number;
  sinMarcacion: number;
  faltas: number;
  minutosTardanza: number;
  minutosExtraPendientes: number;
  minutosExtraAprobados: number;
}

export interface RegistrarMarcacionRequest {
  empleadoId: number;
  tipo: TipoMarcacion;
  instante: string;
  observacion: string;
}

export interface RegistroReloj {
  numeroRegistro: string;
  legajo: string;
  tipo: string;
  instante: string | null;
}

export interface ImportarMarcacionesRequest {
  dispositivo: string;
  registros: RegistroReloj[];
}

export interface RegistroRechazado {
  numeroRegistro: string;
  motivo: string;
}

export interface ResultadoImportacion {
  recibidos: number;
  importados: number;
  yaImportados: number;
  duplicados: number;
  rechazados: RegistroRechazado[];
}

export interface CalcularJornadasRequest {
  desde: string;
  hasta: string;
  empleadoId?: number;
}

export interface ResultadoCalculo {
  calculadas: number;
  conservadas: number;
  eliminadas: number;
}

export interface RechazarHorasExtraRequest {
  motivo: string;
}

export interface IdResponse {
  id: number;
}
