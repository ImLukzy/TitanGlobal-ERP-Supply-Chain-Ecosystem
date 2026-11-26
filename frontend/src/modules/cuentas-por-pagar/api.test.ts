import { afterEach, expect, it, vi } from 'vitest';
import { anularCuenta, detalleCuenta, listarCuentas, listarPagos,
  registrarCuenta, registrarPago, resumenCuentas, revertirPago } from './api';
import type { GuardarCuentaPorPagar } from './tipos';

afterEach(() => vi.unstubAllGlobals());

it('envía los ocho contratos de cuentas por pagar y separa los filtros del resumen', async () => {
  const fetch = vi.fn<typeof globalThis.fetch>().mockImplementation(async () => new Response('{}'));
  vi.stubGlobal('fetch', fetch);
  const alta: GuardarCuentaPorPagar = {
    proveedorId: 6, ordenAbastecimientoId: null, serie: 'F001', numeroDocumento: '42',
    fechaDocumento: '2026-10-01', fechaVencimiento: '2026-11-01', moneda: 'USD', baseImponible: 80, importeImpuesto: 20,
  };
  const pago = { importe: 100, fechaPago: '2026-10-01' };
  await listarCuentas('6', 'USD', true, 2);
  await resumenCuentas('6', 'USD');
  await detalleCuenta(3);
  await listarPagos(3, 2);
  await registrarCuenta(alta);
  await registrarPago(3, pago);
  await revertirPago(3, 5, 'Duplicado');
  await anularCuenta(3, 'Documento cancelado');
  expect(fetch.mock.calls.map(([url, init]) => [url, init?.method])).toEqual([
    ['/api/v1/cuentas-por-pagar?proveedorId=6&moneda=USD&soloVencidas=true&pagina=2', 'GET'],
    ['/api/v1/cuentas-por-pagar/resumen?proveedorId=6&moneda=USD', 'GET'],
    ['/api/v1/cuentas-por-pagar/3', 'GET'],
    ['/api/v1/cuentas-por-pagar/3/pagos?pagina=2', 'GET'],
    ['/api/v1/cuentas-por-pagar', 'POST'],
    ['/api/v1/cuentas-por-pagar/3/pagos', 'POST'],
    ['/api/v1/cuentas-por-pagar/3/pagos/5/revertir', 'POST'],
    ['/api/v1/cuentas-por-pagar/3/anular', 'POST'],
  ]);
  expect(JSON.parse(String(fetch.mock.calls[4][1]?.body))).toEqual(alta);
  expect(JSON.parse(String(fetch.mock.calls[5][1]?.body))).toEqual(pago);
});
