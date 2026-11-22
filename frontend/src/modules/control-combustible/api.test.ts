import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { valores } from './vales.testUtils';
afterEach(() => vi.unstubAllGlobals());
it('omite filtros vacíos y conserva tipo y estado', async () => {
  const fetch = vi
    .fn()
    .mockImplementation(async () => new Response(JSON.stringify({ items: [] }), { status: 200 }));
  vi.stubGlobal('fetch', fetch);
  await api.listarVales({
    busqueda: '',
    estado: undefined,
    tipoCombustible: '',
    pagina: 1,
    tamanoPagina: 10,
  });
  expect(String(fetch.mock.calls[0][0])).toBe(
    '/api/v1/control-combustible?pagina=1&tamanoPagina=10',
  );
  await api.listarVales({ busqueda: 'ABC 470', estado: 'Confirmado', tipoCombustible: 'Diesel' });
  expect(String(fetch.mock.calls[1][0])).toContain(
    'busqueda=ABC+470&estado=Confirmado&tipoCombustible=Diesel',
  );
});
it.each(['detalle', 'resumen', 'crear', 'editar', 'confirmar', 'anular', 'eliminar'])(
  'contrato %s',
  async (op) => {
    const fetch = vi.fn().mockImplementation(
      async () =>
        new Response(op === 'eliminar' ? null : JSON.stringify({ id: 1 }), {
          status: op === 'eliminar' ? 204 : 200,
        }),
    );
    vi.stubGlobal('fetch', fetch);
    const datos = {
      ...valores,
      litros: 50,
      precioLitro: 5.125,
      odometroInicial: 12000,
      odometroFinal: null,
      fechaCarga: valores.fechaCarga + 'Z',
    };
    const acciones = {
      detalle: () => api.obtenerVale(1),
      resumen: () => api.resumenVales(),
      crear: () => api.crearVale(datos),
      editar: () => api.actualizarVale(1, datos),
      confirmar: () => api.confirmarVale(1),
      anular: () => api.anularVale(1, 'Cambio'),
      eliminar: () => api.eliminarVale(1),
    };
    await acciones[op as keyof typeof acciones]();
    const [url, opciones] = fetch.mock.calls[0];
    const sufijos: Record<string, string> = {
      detalle: '/1',
      resumen: '/resumen',
      crear: '',
      editar: '/1',
      confirmar: '/1/confirmacion',
      anular: '/1/anulacion',
      eliminar: '/1',
    };
    expect(String(url)).toBe('/api/v1/control-combustible' + sufijos[op]);
    const metodos: Record<string, string> = {
      crear: 'POST',
      editar: 'PUT',
      confirmar: 'POST',
      anular: 'POST',
      eliminar: 'DELETE',
    };
    expect(opciones.method ?? 'GET').toBe(metodos[op] ?? 'GET');
    if (op === 'crear' || op === 'editar') expect(JSON.parse(opciones.body)).toEqual(datos);
    if (op === 'anular') expect(JSON.parse(opciones.body)).toEqual({ motivo: 'Cambio' });
    if (op === 'confirmar') expect(opciones.body).toBeUndefined();
  },
);
