export const estadosMantenimiento = ['Programado', 'EnEjecucion', 'Completado', 'Cancelado'] as const;

export type EstadoMantenimiento = (typeof estadosMantenimiento)[number];

export const etiquetasEstado: Record<EstadoMantenimiento, string> = {
  Programado: 'Programado',
  EnEjecucion: 'En ejecución',
  Completado: 'Completado',
  Cancelado: 'Cancelado',
};

export interface CentroOpcion {
  id: number;
  codigo: string;
  nombre: string;
}

export interface PaginaCentros {
  items: CentroOpcion[];
  total: number;
  tamanoPagina: number;
}

export interface MantenimientoFila {
  id: number;
  numero: string;
  centroTrabajoId: number;
  centroTrabajoCodigo: string;
  centroTrabajoNombre: string;
  descripcion: string;
  fechaProgramada: string;
  duracionHorasPlanificada: number;
  estado: EstadoMantenimiento;
  vencido: boolean;
}

export interface MantenimientoDetalle extends MantenimientoFila {
  frecuenciaDias: number | null;
  responsable: string | null;
  fechaInicioReal: string | null;
  fechaFinReal: string | null;
  horasReales: number | null;
  observaciones: string | null;
  motivoCancelacion: string | null;
  fechaSiguienteSugerida: string | null;
}

export interface PaginaMantenimientos {
  items: MantenimientoFila[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface EstadoResumen {
  estado: EstadoMantenimiento;
  total: number;
}

export interface ResumenMantenimientos {
  totalMantenimientos: number;
  porEstado: EstadoResumen[];
  vencidos: number;
  proximos7Dias: number;
  horasPlanificadasPendientes: number;
}

export interface FiltrosMantenimientos {
  busqueda?: string;
  centroTrabajoId?: number;
  estado?: EstadoMantenimiento | '';
  desde?: string;
  hasta?: string;
  pagina: number;
  tamanoPagina: number;
}

export interface EditarMantenimientoRequest {
  descripcion: string;
  fechaProgramada: string;
  duracionHorasPlanificada: number;
  frecuenciaDias?: number;
  responsable?: string;
}

export interface CrearMantenimientoRequest extends EditarMantenimientoRequest {
  centroTrabajoId: number;
}

export interface CompletarMantenimientoRequest {
  horasReales: number;
  observaciones?: string;
}

export interface CancelarMantenimientoRequest {
  motivo: string;
}

export interface MantenimientoCreado {
  id: number;
  numero: string;
}

export interface IdResponse {
  id: number;
}
