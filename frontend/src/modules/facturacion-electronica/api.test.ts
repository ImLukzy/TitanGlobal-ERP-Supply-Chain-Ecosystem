import { afterEach, expect, it, vi } from 'vitest';
import { anularFactura, emitirFactura, guardarFactura, listarFacturas, obtenerFactura, resumenFacturas } from './api';
import type { GuardarFactura } from './tipos';

afterEach(() => vi.unstubAllGlobals());

it('envía métodos, filtros y límites UTC inclusivos de un día de Perú', async () => {
  const fetch = vi.fn<typeof globalThis.fetch>().mockImplementation(async () => new Response('{}'));
  vi.stubGlobal('fetch', fetch);
  const datos: GuardarFactura = { clienteId: 1, ordenCompraId: null, tipoComprobante: 'Factura', serie: 'F001', numero: 1,
    fechaEmision: '2026-10-01T05:00:00Z', fechaVencimiento: null, moneda: 'PEN', observaciones: null,
    detalles: [{ productoId: 2, impuestoId: null, descripcion: 'Línea de prueba', cantidad: 1, precioUnitario: 10, descuentoPorcentaje: 0 }] };
  await listarFacturas({ busqueda: ' F001 ', clienteId: '', tipoComprobante: 'Factura', estado: '', moneda: '',
    fechaDesde: '2026-10-01', fechaHasta: '2026-10-01' }, 2);
  await resumenFacturas('', '', '');
  await obtenerFactura(1);
  await guardarFactura(datos);
  await guardarFactura(datos, 1);
  await emitirFactura(1);
  await anularFactura(1, 'Corrección');
  const listado = new URL(String(fetch.mock.calls[0][0]), 'https://localhost');
  expect(listado.pathname).toBe('/api/v1/facturas');
  expect(Object.fromEntries(listado.searchParams)).toEqual({ busqueda: 'F001', tipoComprobante: 'Factura', pagina: '2',
    fechaDesde: '2026-10-01T05:00:00Z', fechaHasta: '2026-10-02T04:59:59.9999999Z' });
  expect(fetch.mock.calls.slice(1).map(([url, init]) => [url, init?.method])).toEqual([
    ['/api/v1/facturas/resumen?moneda=PEN', 'GET'], ['/api/v1/facturas/1', 'GET'],
    ['/api/v1/facturas', 'POST'], ['/api/v1/facturas/1', 'PUT'], ['/api/v1/facturas/1/emitir', 'POST'],
    ['/api/v1/facturas/1/anular', 'POST'],
  ]);
  expect(fetch.mock.calls[5][1]?.body).toBeUndefined();
  expect(JSON.parse(String(fetch.mock.calls[6][1]?.body))).toEqual({ motivo: 'Corrección' });
});
