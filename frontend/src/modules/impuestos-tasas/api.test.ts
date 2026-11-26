import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import type { GuardarImpuesto } from './tipos';

afterEach(() => vi.unstubAllGlobals());

it('envía los siete contratos reales al cliente HTTP del ERP', async () => {
  const fetch = vi.fn<typeof globalThis.fetch>().mockImplementation(async () => new Response('{}'));
  vi.stubGlobal('fetch', fetch);
  const alta: GuardarImpuesto = { codigo: 'IGV', nombre: 'Impuesto', tipo: 'IGV', pais: 'PER', descripcion: null, activo: true };
  await api.listarImpuestos();
  await api.obtenerImpuesto(7);
  await api.crearImpuesto(alta);
  await api.actualizarImpuesto(7, alta);
  await api.desactivarImpuesto(7);
  await api.registrarTasa(7, { porcentaje: 18, fechaInicio: '2026-10-01', fechaFin: null, observacion: null });
  await api.calcularImpuesto({ impuestoId: 7, baseImponible: 100, fechaOperacion: '2026-10-01' });
  expect(fetch.mock.calls.map(([url, init]) => [url, init?.method])).toEqual([
    ['/api/v1/impuestos', 'GET'], ['/api/v1/impuestos/7', 'GET'], ['/api/v1/impuestos', 'POST'],
    ['/api/v1/impuestos/7', 'PUT'], ['/api/v1/impuestos/7', 'DELETE'], ['/api/v1/impuestos/7/tasas', 'POST'],
    ['/api/v1/impuestos/calcular', 'POST'],
  ]);
  expect(JSON.parse(String(fetch.mock.calls[2][1]?.body))).toEqual(alta);
  expect(JSON.parse(String(fetch.mock.calls[5][1]?.body))).toEqual({
    porcentaje: 18, fechaInicio: '2026-10-01', fechaFin: null, observacion: null,
  });
});
