import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { valores } from './entregas.testUtils';
afterEach(() => vi.unstubAllGlobals());
it('omite filtros vacíos y combina búsqueda, estado y página', async () => {
  const fetch = vi
    .fn()
    .mockImplementation(async () => new Response(JSON.stringify({ items: [] }), { status: 200 }));
  vi.stubGlobal('fetch', fetch);
  await api.listarEntregas({ busqueda: '', estado: undefined, pagina: 1, tamanoPagina: 10 });
  expect(String(fetch.mock.calls[0][0])).toBe(
    '/api/v1/monitoreo-entregas?pagina=1&tamanoPagina=10',
  );
  await api.listarEntregas({ busqueda: 'Centro Sur', estado: 'EnRuta' });
  expect(String(fetch.mock.calls[1][0])).toContain('busqueda=Centro+Sur&estado=EnRuta');
});
it.each(['detalle', 'resumen', 'crear', 'editar', 'iniciar', 'completar', 'cancelar', 'eliminar'])(
  'contrato HTTP %s',
  async (op) => {
    const fetch = vi
      .fn()
      .mockImplementation(
        async () =>
          new Response(op === 'eliminar' ? null : JSON.stringify({ id: 1 }), {
            status: op === 'eliminar' ? 204 : 200,
          }),
      );
    vi.stubGlobal('fetch', fetch);
    const pod = {
      receptor: 'P',
      documentoReceptor: 'DOC',
      referenciaConstancia: 'POD',
      observaciones: 'OK',
    };
    const datos = { ...valores, fechaProgramada: valores.fechaProgramada + 'Z' };
    const acciones = {
      detalle: () => api.obtenerEntrega(1),
      resumen: () => api.resumenEntregas(),
      crear: () => api.crearEntrega(datos),
      editar: () => api.actualizarEntrega(1, datos),
      iniciar: () => api.iniciarEntrega(1),
      completar: () => api.completarEntrega(1, pod),
      cancelar: () => api.cancelarEntrega(1, 'Cambio'),
      eliminar: () => api.eliminarEntrega(1),
    };
    await acciones[op as keyof typeof acciones]();
    const [url, opciones] = fetch.mock.calls[0];
    const sufijos: Record<string, string> = {
      detalle: '/1',
      resumen: '/resumen',
      crear: '',
      editar: '/1',
      iniciar: '/1/salida',
      completar: '/1/entrega',
      cancelar: '/1/cancelacion',
      eliminar: '/1',
    };
    expect(String(url)).toBe('/api/v1/monitoreo-entregas' + sufijos[op]);
    const metodos: Record<string, string> = {
      crear: 'POST',
      editar: 'PUT',
      iniciar: 'POST',
      completar: 'POST',
      cancelar: 'POST',
      eliminar: 'DELETE',
    };
    expect(opciones.method ?? 'GET').toBe(metodos[op] ?? 'GET');
    if (op === 'crear' || op === 'editar') expect(JSON.parse(opciones.body)).toEqual(datos);
    if (op === 'completar') expect(JSON.parse(opciones.body)).toEqual(pod);
    if (op === 'cancelar') expect(JSON.parse(opciones.body)).toEqual({ motivo: 'Cambio' });
    if (op === 'iniciar') expect(opciones.body).toBeUndefined();
  },
);
