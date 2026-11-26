import { apiGet, apiPost } from '../../api/client';
import { conFiltros, todasLasPaginas, type PaginaApi } from '../../lib/accionesFinanzas';
import { listarOrdenesAbastecimiento } from '../ordenes-abastecimiento/api';
import { listarProveedores } from '../proveedores/api';
import type { CuentaDetalle, CuentaFila, GuardarCuentaPorPagar, GuardarPago, Pago, ResumenCuentas } from './tipos';

const base = '/cuentas-por-pagar';

export function listarCuentas(proveedorId: string, moneda: string, soloVencidas: boolean, pagina: number) {
  return apiGet<PaginaApi<CuentaFila>>(conFiltros(base, { proveedorId, moneda, soloVencidas: soloVencidas || undefined, pagina }));
}
export function resumenCuentas(proveedorId: string, moneda: string) {
  return apiGet<ResumenCuentas>(conFiltros(base + '/resumen', { proveedorId, moneda }));
}
export function detalleCuenta(id: number) {
  return apiGet<CuentaDetalle>(base + '/' + id);
}
export function listarPagos(id: number, pagina: number) {
  return apiGet<PaginaApi<Pago>>(conFiltros(base + '/' + id + '/pagos', { pagina }));
}
export function registrarCuenta(datos: GuardarCuentaPorPagar) {
  return apiPost<CuentaDetalle>(base, datos);
}
export function registrarPago(cuenta: number, datos: GuardarPago) {
  return apiPost<Pago>(base + '/' + cuenta + '/pagos', datos);
}
export function revertirPago(cuenta: number, pago: number, motivo: string) {
  return apiPost<Pago>(base + '/' + cuenta + '/pagos/' + pago + '/revertir', { motivo });
}
export function anularCuenta(cuenta: number, motivo: string) {
  return apiPost<CuentaDetalle>(base + '/' + cuenta + '/anular', { motivo });
}

export function proveedoresCuenta() {
  return todasLasPaginas(async pagina => {
    const datos = await listarProveedores({ pagina, tamanoPagina: 100 });
    return { ...datos, totalRegistros: datos.total, totalPaginas: Math.ceil(datos.total / datos.tamanoPagina) };
  });
}
export function ordenesProveedor(proveedorId: number) {
  return todasLasPaginas(async pagina => {
    const datos = await listarOrdenesAbastecimiento({ proveedorId, pagina, tamanoPagina: 100 });
    return { ...datos, totalRegistros: datos.total, totalPaginas: Math.ceil(datos.total / datos.tamanoPagina) };
  });
}
