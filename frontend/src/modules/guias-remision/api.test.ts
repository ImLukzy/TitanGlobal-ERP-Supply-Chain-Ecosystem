import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { valores } from './guias.testUtils';
afterEach(() => vi.unstubAllGlobals());
it('omite filtros vacíos y conserva los aplicados', async () => {
  const fetch = vi
    .fn()
    .mockImplementation(async () => new Response(JSON.stringify({ items: [] }), { status: 200 }));
  vi.stubGlobal('fetch', fetch);
  await api.listarGuias({
    busqueda: '',
    estado: undefined,
    pagina: 1,
    tamanoPagina: 10,
  });
  expect(String(fetch.mock.calls[0][0])).toBe('/api/v1/guias-remision?pagina=1&tamanoPagina=10');
  await api.listarGuias({ busqueda: 'T 1', estado: 'Emitida' });
  expect(String(fetch.mock.calls[1][0])).toContain('busqueda=T+1&estado=Emitida');
});
it.each(['detalle', 'resumen', 'crear', 'editar', 'emitir', 'anular', 'eliminar'])(
  'envía el contrato %s',
  async (operacion) => {
    const fetch = vi
      .fn()
      .mockImplementation(
        async () =>
          new Response(operacion === 'eliminar' ? null : JSON.stringify({ id: 1 }), {
            status: operacion === 'eliminar' ? 204 : 200,
          }),
      );
    vi.stubGlobal('fetch', fetch);
    const datos = {
      ...valores,
      numero: 1,
      pesoBrutoKg: 12.34,
      lineas: [{ sku: 'S', descripcion: 'D', unidadMedida: 'UND', cantidad: 1.125 }],
    };
    const acciones = {
      detalle: () => api.obtenerGuia(1),
      resumen: () => api.resumenGuias(),
      crear: () => api.crearGuia(datos),
      editar: () => api.actualizarGuia(1, datos),
      emitir: () => api.emitirGuia(1),
      anular: () => api.anularGuia(1, 'Cambio'),
      eliminar: () => api.eliminarGuia(1),
    };
    await acciones[operacion as keyof typeof acciones]();
    const [url, opciones] = fetch.mock.calls[0];
    const sufijos: Record<string, string> = {
      detalle: '/1',
      resumen: '/resumen',
      crear: '',
      editar: '/1',
      emitir: '/1/emision',
      anular: '/1/anulacion',
      eliminar: '/1',
    };
    expect(String(url)).toBe('/api/v1/guias-remision' + sufijos[operacion]);
    const metodos: Record<string, string> = {
      crear: 'POST',
      editar: 'PUT',
      emitir: 'POST',
      anular: 'POST',
      eliminar: 'DELETE',
    };
    expect(opciones.method ?? 'GET').toBe(metodos[operacion] ?? 'GET');
    if (operacion === 'crear' || operacion === 'editar')
      expect(JSON.parse(opciones.body)).toEqual(datos);
    if (operacion === 'anular') expect(JSON.parse(opciones.body)).toEqual({ motivo: 'Cambio' });
    if (operacion === 'emitir') expect(opciones.body).toBeUndefined();
  },
);
