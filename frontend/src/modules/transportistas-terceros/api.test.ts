import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
afterEach(() => vi.unstubAllGlobals());
it('conserva false, codifica filtros y omite vacíos', async () => {
  const fetch = vi.fn().mockImplementation(async () => Response.json({ items: [] }));
  vi.stubGlobal('fetch', fetch);
  await api.listarTransportistas({ busqueda: 'Lima & Sur', pais: '', activo: false, pagina: 2 });
  expect(fetch.mock.calls[0]?.[0]).toBe(
    '/api/v1/transportistas-terceros?busqueda=Lima+%26+Sur&activo=false&pagina=2',
  );
  await api.listarFletes({ estado: 'Cancelado', transportistaId: 4, tamanoPagina: 10 });
  expect(fetch.mock.calls[1]?.[0]).toBe(
    '/api/v1/fletes-terceros?estado=Cancelado&transportistaId=4&tamanoPagina=10',
  );
  await api.listarTransportistas({});
  expect(fetch.mock.calls[2]?.[0]).toBe('/api/v1/transportistas-terceros');
});
it.each([
  [() => api.obtenerTransportista(7), '/transportistas-terceros/7', 'GET', undefined],
  [() => api.resumenTransportistas(), '/transportistas-terceros/resumen', 'GET', undefined],
  [() => api.obtenerFlete(9), '/fletes-terceros/9', 'GET', undefined],
  [
    () => api.cancelarFlete(9, 'Ruta suspendida'),
    '/fletes-terceros/9/cancelacion',
    'POST',
    { motivo: 'Ruta suspendida' },
  ],
] as const)('respeta contrato %s', async (ejecutar, ruta, metodo, cuerpo) => {
  const fetch = vi.fn().mockResolvedValue(Response.json({ id: 9 }));
  vi.stubGlobal('fetch', fetch);
  await ejecutar();
  expect(fetch.mock.calls[0]?.[0]).toBe(`/api/v1${ruta}`);
  expect(fetch.mock.calls[0]?.[1]).toMatchObject({
    method: metodo,
    ...(cuerpo ? { body: JSON.stringify(cuerpo) } : {}),
  });
});
it('envía altas, edición y baja y conserva el literal 409', async () => {
  const datos = {
    razonSocial: 'Sur',
    documentoFiscal: 'X',
    pais: 'Chile',
    tipoServicio: '3PL',
    activo: false,
  };
  const fetch = vi.fn().mockImplementation(async () => Response.json({ id: 7 }));
  vi.stubGlobal('fetch', fetch);
  await api.crearTransportista(datos);
  await api.actualizarTransportista(7, datos);
  expect(fetch.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(datos) });
  expect(fetch.mock.calls[1]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(datos) });
  fetch.mockResolvedValueOnce(new Response(null, { status: 204 }));
  await api.eliminarTransportista(7);
  expect(fetch.mock.calls[2]?.[1]).toMatchObject({ method: 'DELETE' });
  fetch.mockResolvedValueOnce(
    Response.json(
      { StatusCode: 409, Message: 'No se puede eliminar un transportista con fletes registrados.' },
      { status: 409 },
    ),
  );
  await expect(api.eliminarTransportista(7)).rejects.toMatchObject({
    statusCode: 409,
    message: 'No se puede eliminar un transportista con fletes registrados.',
  });
});
it('registra importe numérico sin modificar fecha UTC ni moneda', async () => {
  const datos = {
    transportistaId: 1,
    referencia: 'F1',
    fecha: '2026-11-21T00:00:00Z',
    origen: 'Lima',
    destino: 'Callao',
    importe: 12.3,
    moneda: 'USD',
  };
  const fetch = vi.fn().mockResolvedValue(Response.json({ id: 7 }));
  vi.stubGlobal('fetch', fetch);
  await api.registrarFlete(datos);
  expect(fetch.mock.calls[0]?.[0]).toBe('/api/v1/fletes-terceros');
  expect(fetch.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(datos) });
});
