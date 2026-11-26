export type MonedaCxP = 'PEN' | 'USD';

export interface GuardarCuentaPorPagar {
  proveedorId: number;
  ordenAbastecimientoId: number | null;
  serie: string;
  numeroDocumento: string;
  fechaDocumento: string;
  fechaVencimiento: string;
  moneda: MonedaCxP;
  baseImponible: number;
  importeImpuesto: number;
}

export interface CuentaFila {
  id: number;
  proveedorId: number;
  serie: string;
  numeroDocumento: string;
  moneda: MonedaCxP;
  total: number;
  saldo: number;
  fechaVencimiento: string;
  anulada: boolean;
  vencida: boolean;
}

export interface CuentaDetalle extends CuentaFila {
  ordenAbastecimientoId: number | null;
  fechaDocumento: string;
  baseImponible: number;
  importeImpuesto: number;
  importePagado: number;
  fechaAnulacion: string | null;
  motivoAnulacion: string | null;
}

export interface GuardarPago { importe: number; fechaPago: string }
export interface Pago extends GuardarPago {
  id: number;
  cuentaPorPagarId: number;
  moneda: MonedaCxP;
  fechaReversion: string | null;
  motivoReversion: string | null;
}

export interface ResumenGrupo {
  proveedorId: number;
  moneda: MonedaCxP;
  saldoAlDia: number;
  saldoVencido: number;
  vencido0A30: number;
  vencido31A60: number;
  vencido61A90: number;
  vencidoMasDe90: number;
}

export interface ResumenCuentas { fechaCorte: string; grupos: ResumenGrupo[] }
