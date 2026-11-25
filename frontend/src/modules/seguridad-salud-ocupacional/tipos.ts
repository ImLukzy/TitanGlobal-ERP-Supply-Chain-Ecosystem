export type TipoIncidente = 'AccidenteSinBaja' | 'AccidenteConBaja' | 'IncidenteSinLesion' | 'EnfermedadOcupacional';

export type SeveridadIncidente = 'Leve' | 'Moderada' | 'Grave' | 'Mortal';

export type EstadoIncidente = 'Abierto' | 'Cerrado';

export type TipoExamen = 'Ingreso' | 'Periodico' | 'Retiro';

export type ResultadoExamen = 'Apto' | 'AptoConRestricciones' | 'NoApto';

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

export interface IncidenteListado {
  id: number;
  numero: string;
  empleadoId: number;
  empleado: string;
  tipo: TipoIncidente;
  severidad: SeveridadIncidente;
  fecha: string;
  lugar: string | null;
  descripcion: string;
  diasBaja: number;
  estado: EstadoIncidente;
  fechaCierre: string | null;
  acciones: number;
  accionesPendientes: number;
}

export type PaginaIncidentes = Pagina<IncidenteListado>;

export interface AccionCorrectiva {
  id: number;
  descripcion: string;
  responsable: string;
  fechaCompromiso: string;
  completada: boolean;
  fechaCompletada: string | null;
}

export interface IncidenteDetalle {
  id: number;
  numero: string;
  empleadoId: number;
  empleado: string;
  tipo: TipoIncidente;
  severidad: SeveridadIncidente;
  fecha: string;
  lugar: string | null;
  descripcion: string;
  diasBaja: number;
  estado: EstadoIncidente;
  fechaCierre: string | null;
  acciones: AccionCorrectiva[];
}

export interface IncidenteCreado {
  id: number;
  numero: string;
}

export interface FiltrosIncidentes {
  busqueda: string;
  tipo: '' | TipoIncidente;
  severidad: '' | SeveridadIncidente;
  estado: '' | EstadoIncidente;
  desde: string;
  hasta: string;
  pagina: number;
  tamanoPagina: number;
}

export interface RegistrarIncidenteRequest {
  empleadoId: number;
  tipo: string;
  severidad: string;
  fecha: string;
  lugar: string | null;
  descripcion: string;
  diasBaja: number;
}

export interface GuardarAccionRequest {
  descripcion: string;
  responsable: string;
  fechaCompromiso: string;
}

export interface CompletarAccionRequest {
  fechaCompletada: string;
}

export interface EquipoProteccion {
  id: number;
  codigo: string;
  nombre: string;
  vidaUtilDias: number;
  activo: boolean;
}

export type PaginaEquipos = Pagina<EquipoProteccion>;

export interface FiltrosEquipos {
  busqueda: string;
  activo: FiltroActivo;
  pagina: number;
  tamanoPagina: number;
}

export interface GuardarEquipoRequest {
  codigo: string;
  nombre: string;
  vidaUtilDias: number;
}

export interface EntregaEpp {
  id: number;
  equipoId: number;
  equipoCodigo: string;
  equipoNombre: string;
  empleadoId: number;
  empleado: string;
  fechaEntrega: string;
  cantidad: number;
  fechaVencimiento: string;
  vencida: boolean;
}

export type PaginaEntregas = Pagina<EntregaEpp>;

export interface FiltrosEntregas {
  empleadoId: number | null;
  equipoId: number | null;
  vencidas: boolean;
  pagina: number;
  tamanoPagina: number;
}

export interface RegistrarEntregaRequest {
  equipoId: number;
  empleadoId: number;
  fechaEntrega: string;
  cantidad: number;
}

export interface ExamenMedico {
  id: number;
  empleadoId: number;
  empleado: string;
  tipo: TipoExamen;
  fecha: string;
  resultado: ResultadoExamen;
  restricciones: string | null;
  fechaVencimiento: string | null;
  clinica: string | null;
  vencido: boolean;
}

export type PaginaExamenes = Pagina<ExamenMedico>;

export interface FiltrosExamenes {
  empleadoId: number | null;
  tipo: '' | TipoExamen;
  resultado: '' | ResultadoExamen;
  vencidos: boolean;
  pagina: number;
  tamanoPagina: number;
}

export interface RegistrarExamenRequest {
  empleadoId: number;
  tipo: string;
  fecha: string;
  resultado: string;
  restricciones: string | null;
  fechaVencimiento: string | null;
  clinica: string | null;
}

export interface ResumenSst {
  anio: number;
  incidentesAbiertos: number;
  incidentesAnio: number;
  accidentesConBaja: number;
  diasBajaAnio: number;
  entregasEppVencidas: number;
  examenesVencidos: number;
  empleadosNoAptos: number;
}
