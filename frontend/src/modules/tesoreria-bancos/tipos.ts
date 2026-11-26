export type MonedaBanco = 'PEN' | 'USD';
export type TipoCuenta = 'Corriente' | 'Ahorros';
export type TipoMovimiento = 'Ingreso' | 'Egreso';

export interface GuardarCuenta {
  banco: string;
  numero: string;
  nombre: string;
  tipo: TipoCuenta;
  moneda: MonedaBanco;
}

export interface CuentaFila extends GuardarCuenta { id: number; saldo: number }
export interface CuentaDetalle extends CuentaFila { fechaRegistro: string }

export interface OperacionBancaria { importe: number; fechaOperacion: string; referencia: string | null }
export interface RegistrarMovimiento extends OperacionBancaria { tipo: TipoMovimiento }
export interface Movimiento extends RegistrarMovimiento {
  id: number;
  cuentaBancariaId: number;
  moneda: MonedaBanco;
  transferenciaBancariaId: number | null;
  fechaReversion: string | null;
  motivoReversion: string | null;
}
export interface RegistrarTransferencia extends OperacionBancaria { cuentaOrigenId: number; cuentaDestinoId: number }
export interface Transferencia {
  id: number;
  cuentaOrigenId: number;
  cuentaDestinoId: number;
  moneda: MonedaBanco;
  importe: number;
  fechaOperacion: string;
  movimientoEgresoId: number;
  movimientoIngresoId: number;
}
