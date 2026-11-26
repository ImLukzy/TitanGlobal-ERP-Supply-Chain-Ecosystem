import { apiGet, apiPost, apiPut } from '../../api/client';
import { conFiltros, todasLasPaginas, type PaginaApi } from '../../lib/accionesFinanzas';
import type { CuentaDetalle, CuentaFila, GuardarCuenta, Movimiento, RegistrarMovimiento, RegistrarTransferencia, Transferencia } from './tipos';

const ruta = '/tesoreria-bancos';
const cuentas = ruta + '/cuentas';
const transferencias = ruta + '/transferencias';

export function listarCuentas(moneda: string, tipo: string, pagina: number, tamanoPagina = 20) {
  return apiGet<PaginaApi<CuentaFila>>(conFiltros(cuentas, { moneda, tipo, pagina, tamanoPagina }));
}
export function cuentasDeMoneda(moneda: string) {
  return todasLasPaginas(pagina => listarCuentas(moneda, '', pagina, 100));
}
export function obtenerCuenta(id: number) {
  return apiGet<CuentaDetalle>(cuentas + '/' + id);
}
export function guardarCuenta(datos: GuardarCuenta, id?: number) {
  return id === undefined ? apiPost<CuentaDetalle>(cuentas, datos) : apiPut<CuentaDetalle>(cuentas + '/' + id, datos);
}
export function listarMovimientos(id: number, tipo: string, desde: string, hasta: string, pagina: number, tamanoPagina = 20) {
  return apiGet<PaginaApi<Movimiento>>(conFiltros(cuentas + '/' + id + '/movimientos', { tipo, desde, hasta, pagina, tamanoPagina }));
}
export function registrarMovimiento(id: number, datos: RegistrarMovimiento) {
  return apiPost<Movimiento>(cuentas + '/' + id + '/movimientos', datos);
}
export function revertirMovimiento(cuentaId: number, movimientoId: number, motivo: string) {
  return apiPost<Movimiento>(cuentas + '/' + cuentaId + '/movimientos/' + movimientoId + '/revertir', { motivo });
}
export function registrarTransferencia(datos: RegistrarTransferencia) {
  return apiPost<Transferencia>(transferencias, datos);
}
export function obtenerTransferencia(id: number) {
  return apiGet<Transferencia>(transferencias + '/' + id);
}
