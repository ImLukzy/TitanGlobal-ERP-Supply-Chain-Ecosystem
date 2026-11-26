import { afterEach, expect, it, vi } from 'vitest';
import { anularNota, crearNota, editarNota, emitirNota, facturaParaNota, facturasElegibles, listarNotas, obtenerNota } from './api';

afterEach(() => vi.unstubAllGlobals());

it('consulta comprobantes emitidos y usa métodos y payloads del contrato del 52', async () => {
  const fetch = vi.fn<typeof globalThis.fetch>().mockImplementation(async () => new Response('{"items":[],"totalPaginas":1}'));
  vi.stubGlobal('fetch', fetch);
  const detalles = [{ detalleFacturaId: 7, cantidad: 1, precioUnitario: 10 }];
  await listarNotas('2', 'Credito', '', 3);
  await obtenerNota(1);
  await crearNota({ facturaId: 2, tipo: 'Credito', motivo: 'Corrección', detalles });
  await editarNota(1, { motivo: 'Corrección', detalles });
  await emitirNota(1);
  await anularNota(1, 'Corrección');
  await facturasElegibles();
  await facturaParaNota(2);
  expect(fetch.mock.calls.map(([url, init]) => [url, init?.method])).toEqual([
    ['/api/v1/notas-credito-debito?facturaId=2&tipo=Credito&pagina=3', 'GET'], ['/api/v1/notas-credito-debito/1', 'GET'],
    ['/api/v1/notas-credito-debito', 'POST'], ['/api/v1/notas-credito-debito/1', 'PUT'],
    ['/api/v1/notas-credito-debito/1/emitir', 'POST'], ['/api/v1/notas-credito-debito/1/anular', 'POST'],
    ['/api/v1/facturas?estado=Emitida&pagina=1&tamanoPagina=100', 'GET'], ['/api/v1/facturas/2', 'GET'],
  ]);
  expect(JSON.parse(String(fetch.mock.calls[3][1]?.body))).toEqual({ motivo: 'Corrección', detalles });
  expect(fetch.mock.calls[4][1]?.body).toBeUndefined();
});
