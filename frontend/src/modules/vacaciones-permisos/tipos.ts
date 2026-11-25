export type TipoAusencia =
  | 'Vacaciones'
  | 'PermisoConGoce'
  | 'PermisoSinGoce'
  | 'LicenciaMaternidad'
  | 'LicenciaPaternidad'
  | 'DescansoMedico';

export type EstadoAusencia = 'Pendiente' | 'Aprobada' | 'Rechazada' | 'Cancelada';

export interface FiltrosAusencias {
  busqueda: string;
  tipo: '' | TipoAusencia;
  estado: '' | EstadoAusencia;
  desde: string;
  hasta: string;
  empleadoId: number | null;
  pagina: number;
  tamanoPagina: number;
}

export interface AusenciaListado {
  id: number;
  numero: string;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  tipo: TipoAusencia;
  fechaInicio: string;
  fechaFin: string;
  dias: number;
  estado: EstadoAusencia;
  fechaSolicitud: string;
}

export interface AusenciaDetalle extends AusenciaListado {
  motivo: string | null;
  decididoPor: string | null;
  fechaDecision: string | null;
  motivoRechazo: string | null;
  fechaCancelacion: string | null;
}

export interface PaginaAusencias {
  items: AusenciaListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenAusencias {
  pendientes: number;
  ausentesHoy: number;
  vacacionesHoy: number;
  descansosMedicosHoy: number;
  aprobadasMes: number;
}

export interface SaldoVacacional {
  empleadoId: number;
  fechaIngreso: string;
  fecha: string;
  mesesServicio: number;
  diasDevengados: number;
  diasGozados: number;
  diasProgramados: number;
  saldo: number;
}

export interface RegistrarAusenciaRequest {
  empleadoId: number;
  tipo: TipoAusencia;
  fechaInicio: string;
  fechaFin: string;
  motivo: string | null;
}

export interface RechazarAusenciaRequest {
  motivo: string;
}

export interface AusenciaCreada {
  id: number;
  numero: string;
}

export interface IdResponse {
  id: number;
}
