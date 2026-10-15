export type EstadoContrato = 'Borrador' | 'Vigente' | 'Finalizado' | 'Cancelado';
export type CondicionPago = 'Contado' | 'Credito30' | 'Credito60' | 'Credito90';

export interface ContratoListado {
  id: number;
  numero: string;
  clienteId: number;
  clienteNombre: string;
  titulo: string;
  fechaInicio: string;
  fechaFin: string;
  estado: EstadoContrato;
  vencido: boolean;
  montoComprometido: number;
  montoConsumido: number;
  porcentajeConsumido: number;
}

export interface OrdenPeriodo {
  ordenId: number;
  numeroOrden: string;
  fecha: string;
  total: number;
}

export interface ContratoDetalle extends ContratoListado {
  condicionPago: CondicionPago;
  observaciones: string | null;
  motivoCancelacion: string | null;
  fechaActivacion: string | null;
  fechaCierre: string | null;
  saldo: number;
  ordenesPeriodo: OrdenPeriodo[];
}

export interface PaginaContratos {
  items: ContratoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ContratosResumen {
  total: number;
  borradores: number;
  vigentes: number;
  vencidos: number;
  finalizados: number;
  cancelados: number;
  montoComprometidoVigente: number;
  montoConsumidoVigente: number;
  porcentajeConsumoVigente: number;
  porCondicion: { condicionPago: CondicionPago; contratos: number }[];
}

export interface FiltrosContrato {
  busqueda?: string;
  estado?: EstadoContrato | '';
  clienteId?: number | '';
  vencidos?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}

export interface GuardarContratoRequest {
  clienteId: number;
  titulo: string;
  fechaInicio: string;
  fechaFin: string;
  montoComprometido: number;
  condicionPago: CondicionPago;
  observaciones?: string;
}

export interface ClienteOpcion {
  id: number;
  nombre: string;
}

export interface ContratoResponse {
  id: number;
  numero: string;
}
