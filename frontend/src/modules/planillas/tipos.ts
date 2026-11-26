export type TipoPlanilla = 'Mensual' | 'Complementaria';

export type EstadoPlanilla = 'Borrador' | 'Calculada' | 'Aprobada' | 'Pagada';

export interface IdResponse {
  id: number;
}

export interface FiltrosPlanillas {
  anio: number | null;
  tipo: '' | TipoPlanilla;
  estado: '' | EstadoPlanilla;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosDetalles {
  busqueda: string;
  soloIncidencias: boolean;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosBonos {
  anio: number | null;
  mes: number | null;
  empleadoId: number | null;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosAfiliaciones {
  empleadoId: number | null;
  pagina: number;
  tamanoPagina: number;
}

export interface PlanillaListado {
  id: number;
  numero: string;
  anio: number;
  mes: number;
  tipo: TipoPlanilla;
  estado: EstadoPlanilla;
  empleados: number;
  incidencias: number;
  totalIngresos: number;
  totalDescuentos: number;
  totalNeto: number;
}

export interface PaginaPlanillas {
  items: PlanillaListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenPlanillas {
  ultimoPeriodo: string | null;
  estadoUltimo: EstadoPlanilla | null;
  netoUltimo: number;
  empleadosUltimo: number;
  incidenciasUltimo: number;
  borradoresYCalculadas: number;
}

export interface PlanillaDetalle {
  id: number;
  numero: string;
  anio: number;
  mes: number;
  tipo: TipoPlanilla;
  estado: EstadoPlanilla;
  empleados: number;
  incidencias: number;
  totalIngresos: number;
  totalDescuentos: number;
  totalNeto: number;
  totalAportesEmpleador: number;
  fechaCalculo: string | null;
  calculadoPor: string | null;
  aprobadoPor: string | null;
  fechaAprobacion: string | null;
  fechaPago: string | null;
}

export interface PlanillaEmpleado {
  id: number;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  cargo: string;
  sueldoBasico: number;
  diasContrato: number;
  diasSinGoce: number;
  diasFalta: number;
  diasLaborados: number;
  ingresoBasico: number;
  minutosExtra: number;
  ingresoHorasExtra: number;
  ingresoBonos: number;
  totalIngresos: number;
  codigoPension: string | null;
  descuentoPension: number;
  descuentoQuinta: number;
  descuentoPrestamos: number;
  totalDescuentos: number;
  neto: number;
  aporteEssalud: number;
  incidencia: string | null;
  numeroBoleta: string | null;
}

export interface PaginaPlanillaEmpleados {
  items: PlanillaEmpleado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface BoletaPago {
  empresa: string;
  numeroBoleta: string;
  numeroPlanilla: string;
  tipo: TipoPlanilla;
  anio: number;
  mes: number;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  cargo: string;
  codigoPension: string | null;
  sueldoBasico: number;
  diasContrato: number;
  diasSinGoce: number;
  diasFalta: number;
  diasLaborados: number;
  ingresoBasico: number;
  minutosExtra: number;
  ingresoHorasExtra: number;
  ingresoBonos: number;
  totalIngresos: number;
  descuentoPension: number;
  descuentoQuinta: number;
  descuentoPrestamos: number;
  totalDescuentos: number;
  aporteEssalud: number;
  neto: number;
  fechaPago: string | null;
}

export interface ParametroPlanilla {
  clave: string;
  valor: number;
  descripcion: string;
}

export interface ParametroPension {
  codigo: string;
  sistema: 'ONP' | 'AFP';
  tasaAporte: number;
  tasaPrimaSeguro: number;
  tasaComision: number;
  remuneracionMaximaAsegurable: number | null;
}

export interface TramoQuinta {
  orden: number;
  hastaUit: number | null;
  tasa: number;
}

export interface ParametrosPlanilla {
  parametros: ParametroPlanilla[];
  pensiones: ParametroPension[];
  tramos: TramoQuinta[];
}

export interface CrearPlanillaRequest {
  anio: number;
  mes: number;
  tipo: TipoPlanilla;
}

export interface PlanillaCreada {
  id: number;
  numero: string;
}

export interface ResultadoCalculoPlanilla {
  id: number;
  empleados: number;
  incidencias: number;
}

export interface PagarPlanillaRequest {
  fechaPago: string;
}

export interface BonoPlanilla {
  id: number;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  anio: number;
  mes: number;
  tipoPlanilla: TipoPlanilla;
  concepto: string;
  monto: number;
  planillaId: number | null;
  planillaNumero: string | null;
}

export interface PaginaBonos {
  items: BonoPlanilla[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface GuardarBonoRequest {
  empleadoId: number;
  anio: number;
  mes: number;
  tipoPlanilla: TipoPlanilla;
  concepto: string;
  monto: number;
}

export interface AfiliacionPension {
  id: number;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  codigoPension: string;
  sistema: 'ONP' | 'AFP';
  cuspp: string | null;
  fechaAfiliacion: string;
}

export interface PaginaAfiliaciones {
  items: AfiliacionPension[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface GuardarAfiliacionRequest {
  codigoPension: string;
  cuspp?: string;
  fechaAfiliacion: string;
}
