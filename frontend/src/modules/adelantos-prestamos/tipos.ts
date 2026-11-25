export type TipoPrestamo = 'Adelanto' | 'Prestamo';

export type EstadoPrestamo = 'Activo' | 'Cancelado' | 'Anulado';

export type EstadoCuota = 'Pendiente' | 'Descontada';

export interface FiltrosPrestamos {
  busqueda: string;
  tipo: '' | TipoPrestamo;
  estado: '' | EstadoPrestamo;
  empleadoId: number | null;
  pagina: number;
  tamanoPagina: number;
}

export interface PrestamoListado {
  id: number;
  numero: string;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  tipo: TipoPrestamo;
  monto: number;
  numeroCuotas: number;
  fechaOtorgamiento: string;
  saldo: number;
  estado: EstadoPrestamo;
}

export interface CuotaPrestamo {
  id: number;
  numeroCuota: number;
  anio: number;
  mes: number;
  monto: number;
  montoDescontado: number;
  estado: EstadoCuota;
}

export interface PrestamoDetalle extends PrestamoListado {
  primerAnio: number;
  primerMes: number;
  motivo: string;
  fechaAnulacion: string | null;
  motivoAnulacion: string | null;
  cuotas: CuotaPrestamo[];
}

export interface PaginaPrestamos {
  items: PrestamoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenPrestamos {
  activos: number;
  saldoPendienteTotal: number;
  otorgadoMes: number;
  cuotasDelMes: number;
}

export interface CuotaComprometida {
  anio: number;
  mes: number;
  monto: number;
}

export interface CapacidadPrestamo {
  empleadoId: number;
  fecha: string;
  sueldoVigente: number;
  netoEstimado: number;
  cuotaMaxima: number;
  cuotasComprometidasPorMes: CuotaComprometida[];
}

export interface RegistrarPrestamoRequest {
  empleadoId: number;
  tipo: TipoPrestamo;
  monto: number;
  numeroCuotas: number;
  fechaOtorgamiento: string;
  primerAnio: number;
  primerMes: number;
  motivo: string;
}

export interface AnularPrestamoRequest {
  motivo: string;
}

export interface PrestamoCreado {
  id: number;
  numero: string;
}

export interface IdResponse {
  id: number;
}
