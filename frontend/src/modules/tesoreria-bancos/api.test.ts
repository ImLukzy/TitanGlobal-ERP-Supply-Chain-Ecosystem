import { afterEach, expect, it, vi } from 'vitest';
import { guardarCuenta, listarCuentas, listarMovimientos, obtenerCuenta, obtenerTransferencia,
  registrarMovimiento, registrarTransferencia, revertirMovimiento } from './api';
import type { GuardarCuenta } from './tipos';

afterEach(() => vi.unstubAllGlobals());

it('usa los nueve contratos de cuentas, movimientos y transferencias', async () => {
  const fetch = vi.fn<typeof globalThis.fetch>().mockImplementation(async () => new Response('{}'));
  vi.stubGlobal('fetch', fetch);
  const datos: GuardarCuenta = { banco: 'BANCO', numero: '001', nombre: 'Cuenta', tipo: 'Corriente', moneda: 'PEN' };
  const operacion = { importe: 10, fechaOperacion: '2026-10-01', referencia: null };
  await listarCuentas('PEN', '', 2);
  await obtenerCuenta(1);
  await guardarCuenta(datos);
  await guardarCuenta(datos, 1);
  await listarMovimientos(1, 'Egreso', '2026-10-01', '', 2);
  await registrarMovimiento(1, { ...operacion, tipo: 'Ingreso' });
  await revertirMovimiento(1, 2, 'Corrección');
  await registrarTransferencia({ ...operacion, cuentaOrigenId: 1, cuentaDestinoId: 2 });
  await obtenerTransferencia(4);
  expect(fetch.mock.calls.map(([url, init]) => [url, init?.method])).toEqual([
    ['/api/v1/tesoreria-bancos/cuentas?moneda=PEN&pagina=2&tamanoPagina=20', 'GET'],
    ['/api/v1/tesoreria-bancos/cuentas/1', 'GET'], ['/api/v1/tesoreria-bancos/cuentas', 'POST'],
    ['/api/v1/tesoreria-bancos/cuentas/1', 'PUT'],
    ['/api/v1/tesoreria-bancos/cuentas/1/movimientos?tipo=Egreso&desde=2026-10-01&pagina=2&tamanoPagina=20', 'GET'],
    ['/api/v1/tesoreria-bancos/cuentas/1/movimientos', 'POST'],
    ['/api/v1/tesoreria-bancos/cuentas/1/movimientos/2/revertir', 'POST'],
    ['/api/v1/tesoreria-bancos/transferencias', 'POST'], ['/api/v1/tesoreria-bancos/transferencias/4', 'GET'],
  ]);
  expect(JSON.parse(String(fetch.mock.calls[5][1]?.body))).toEqual({ ...operacion, tipo: 'Ingreso' });
});
