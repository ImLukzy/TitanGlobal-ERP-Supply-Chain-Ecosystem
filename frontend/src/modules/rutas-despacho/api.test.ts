import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
afterEach(() => vi.unstubAllGlobals());
it('codifica filtros, conserva false y omite filtros vacíos', async () => {
  const fetch = vi.fn().mockImplementation(async () => Response.json({ items: [] }));
  vi.stubGlobal('fetch', fetch);
  await api.listarRutas({
    busqueda: 'Lima & Callao',
    zona: 'Centro',
    activo: false,
    pagina: 2,
    tamanoPagina: 10,
  });
  expect(fetch.mock.calls[0]?.[0]).toBe(
    '/api/v1/rutas-despacho?busqueda=Lima+%26+Callao&zona=Centro&activo=false&pagina=2&tamanoPagina=10',
  );
  await api.listarRutas({ busqueda: '', zona: undefined });
  expect(fetch.mock.calls[1]?.[0]).toBe('/api/v1/rutas-despacho');
});
it.each([
  [() => api.obtenerRuta(7), '/7'],
  [api.listarZonas, '/zonas'],
  [api.resumenRutas, '/resumen'],
] as const)('consulta mediante GET %s', async (ejecutar, ruta) => {
  const fetch = vi.fn().mockResolvedValue(Response.json({}));
  vi.stubGlobal('fetch', fetch);
  await ejecutar();
  expect(fetch.mock.calls[0]?.[0]).toBe(`/api/v1/rutas-despacho${ruta}`);
  expect(fetch.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
});
it.each(['POST', 'PUT'] as const)(
  'guarda con %s sin convertir moneda ni modificar el cuerpo',
  async (metodo) => {
    const datos = {
      codigo: 'R1',
      nombre: 'Ruta',
      zona: 'Centro',
      origen: 'Lima',
      destino: 'Callao',
      distanciaKm: 25.5,
      peajeEstimado: 0,
      moneda: 'USD',
      activo: false,
    };
    const fetch = vi.fn().mockResolvedValue(Response.json({ id: 7 }));
    vi.stubGlobal('fetch', fetch);
    const respuesta =
      metodo === 'POST' ? await api.crearRuta(datos) : await api.actualizarRuta(7, datos);
    expect(respuesta).toEqual({ id: 7 });
    expect(fetch.mock.calls[0]?.[0]).toBe(`/api/v1/rutas-despacho${metodo === 'PUT' ? '/7' : ''}`);
    expect(fetch.mock.calls[0]?.[1]).toMatchObject({ method: metodo, body: JSON.stringify(datos) });
  },
);
it('acepta 204 y conserva el mensaje de conflicto de baja', async () => {
  const fetch = vi
    .fn()
    .mockResolvedValueOnce(new Response(null, { status: 204 }))
    .mockResolvedValueOnce(
      Response.json(
        {
          StatusCode: 409,
          Message: 'No se puede eliminar la ruta de despacho porque está en uso.',
        },
        { status: 409 },
      ),
    );
  vi.stubGlobal('fetch', fetch);
  await expect(api.eliminarRuta(7)).resolves.toBeUndefined();
  expect(fetch.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  await expect(api.eliminarRuta(7)).rejects.toMatchObject({
    statusCode: 409,
    message: 'No se puede eliminar la ruta de despacho porque está en uso.',
  });
});
