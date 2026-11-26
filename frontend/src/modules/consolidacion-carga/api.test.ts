import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
afterEach(() => vi.unstubAllGlobals());
it('codifica consultas y omite filtros vacíos', async () => {
  const fetch = vi.fn().mockImplementation(async () => Response.json({ items: [] }));
  vi.stubGlobal('fetch', fetch);
  await api.listarCargas({
    busqueda: 'Norte & Sur',
    estado: 'Borrador',
    pagina: 2,
    tamanoPagina: 10,
  });
  expect(fetch.mock.calls[0]?.[0]).toBe(
    '/api/v1/consolidaciones-carga?busqueda=Norte+%26+Sur&estado=Borrador&pagina=2&tamanoPagina=10',
  );
  await api.listarCargas({ busqueda: '', estado: undefined });
  expect(fetch.mock.calls[1]?.[0]).toBe('/api/v1/consolidaciones-carga');
});
it.each([
  [() => api.obtenerCarga(7), '/7'],
  [api.resumenCargas, '/resumen'],
] as const)('consulta GET %s', async (ejecutar, ruta) => {
  const fetch = vi.fn().mockResolvedValue(Response.json({}));
  vi.stubGlobal('fetch', fetch);
  await ejecutar();
  expect(fetch.mock.calls[0]?.[0]).toBe(`/api/v1/consolidaciones-carga${ruta}`);
  expect(fetch.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
});
it('confirma con POST sin cuerpo y cancela con motivo literal', async () => {
  const fetch = vi.fn().mockImplementation(async () => Response.json({ id: 7 }));
  vi.stubGlobal('fetch', fetch);
  await api.confirmarCarga(7);
  await api.cancelarCarga(7, 'Cambio de plan');
  expect(fetch.mock.calls[0]?.[0]).toBe('/api/v1/consolidaciones-carga/7/confirmacion');
  expect(fetch.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
  expect(fetch.mock.calls[0]?.[1].body).toBeUndefined();
  expect(fetch.mock.calls[1]?.[1]).toMatchObject({
    method: 'POST',
    body: JSON.stringify({ motivo: 'Cambio de plan' }),
  });
});
it.each(['POST', 'PUT'] as const)('guarda cabecera y líneas con %s', async (metodo) => {
  const datos = {
    codigo: 'C1',
    nombre: 'Carga',
    lineas: [
      {
        sku: 'S',
        descripcion: 'D',
        cantidad: 1,
        cantidadRecogida: 0,
        unidadesPorBulto: 0,
        largoCm: 0,
        anchoCm: 0,
        altoCm: 0,
        pesoBultoKg: 0,
      },
    ],
  };
  const fetch = vi.fn().mockResolvedValue(Response.json({ id: 7 }));
  vi.stubGlobal('fetch', fetch);
  if (metodo === 'POST') await api.crearCarga(datos);
  else await api.actualizarCarga(7, datos);
  expect(fetch.mock.calls[0]?.[0]).toBe(
    `/api/v1/consolidaciones-carga${metodo === 'PUT' ? '/7' : ''}`,
  );
  expect(fetch.mock.calls[0]?.[1]).toMatchObject({ method: metodo, body: JSON.stringify(datos) });
});
it('acepta 204 y conserva un conflicto del servidor', async () => {
  const fetch = vi
    .fn()
    .mockResolvedValueOnce(new Response(null, { status: 204 }))
    .mockResolvedValueOnce(
      Response.json(
        { StatusCode: 409, Message: 'Solo se puede eliminar una consolidación en borrador.' },
        { status: 409 },
      ),
    );
  vi.stubGlobal('fetch', fetch);
  await expect(api.eliminarCarga(7)).resolves.toBeUndefined();
  expect(fetch.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  await expect(api.eliminarCarga(7)).rejects.toMatchObject({
    statusCode: 409,
    message: 'Solo se puede eliminar una consolidación en borrador.',
  });
});
