import { afterEach, expect, it, vi } from 'vitest';
import { cambiarVigencia, guardarCentro, listarCentros, obtenerCentro } from './api';

afterEach(() => vi.unstubAllGlobals());

it('envía filtros, altas, edición y vigencia al contrato real', async () => {
  const fetch = vi.fn<typeof globalThis.fetch>().mockImplementation(async () => new Response('{}'));
  vi.stubGlobal('fetch', fetch);
  const datos = { codigo: 'CC02', nombre: 'Ventas', descripcion: null, centroCostoPadreId: 1, responsableEmpleadoId: 3 };
  await listarCentros('false', '1', 2);
  await obtenerCentro(2);
  await guardarCentro(datos);
  await guardarCentro(datos, 2);
  await cambiarVigencia(2, false);
  await cambiarVigencia(2, true);
  expect(fetch.mock.calls.map(([url, init]) => [url, init?.method])).toEqual([
    ['/api/v1/centros-costo?activo=false&centroCostoPadreId=1&pagina=2&tamanoPagina=20', 'GET'],
    ['/api/v1/centros-costo/2', 'GET'], ['/api/v1/centros-costo', 'POST'], ['/api/v1/centros-costo/2', 'PUT'],
    ['/api/v1/centros-costo/2/desactivar', 'POST'], ['/api/v1/centros-costo/2/reactivar', 'POST'],
  ]);
  expect(JSON.parse(String(fetch.mock.calls[3][1]?.body))).toEqual(datos);
  expect(fetch.mock.calls[5][1]?.body).toBeUndefined();
});
