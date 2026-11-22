import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { valores } from './retornos.testUtils';
afterEach(() => vi.unstubAllGlobals());
function respuesta() {
  const mock = vi.fn().mockImplementation(
    async () =>
      new Response(JSON.stringify({ id: 1 }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
  );
  vi.stubGlobal('fetch', mock);
  return mock;
}
it('listado omite filtros vacíos y conserva valores', async () => {
  const f = respuesta();
  await api.listarRetornos({ busqueda: '', estado: '', tipo: '', pagina: 1, tamanoPagina: 10 });
  expect(f.mock.calls[0][0]).toBe('/api/v1/logistica-inversa?pagina=1&tamanoPagina=10');
  await api.listarRetornos({ busqueda: 'Pallet Norte', estado: 'Recogido', tipo: 'Pallet' });
  expect(f.mock.calls[1][0]).toBe(
    '/api/v1/logistica-inversa?busqueda=Pallet+Norte&estado=Recogido&tipo=Pallet',
  );
});
it.each(['detalle', 'resumen', 'crear', 'editar', 'eliminar', 'recoger', 'recibir', 'cancelar'])(
  'contrato %s',
  async (accion) => {
    const f = respuesta();
    const d = { ...valores, cantidadProgramada: 10.125 };
    const c = { cantidad: 9.125, responsable: 'Ana' };
    let path = '/1',
      method = 'GET',
      body: unknown = undefined;
    switch (accion) {
      case 'detalle':
        await api.obtenerRetorno(1);
        break;
      case 'resumen':
        path = '/resumen';
        await api.resumenRetornos();
        break;
      case 'crear':
        path = '';
        method = 'POST';
        body = d;
        await api.crearRetorno(d);
        break;
      case 'editar':
        method = 'PUT';
        body = d;
        await api.actualizarRetorno(1, d);
        break;
      case 'eliminar':
        method = 'DELETE';
        await api.eliminarRetorno(1);
        break;
      case 'recoger':
        path = '/1/recogida';
        method = 'POST';
        body = c;
        await api.recogerRetorno(1, c);
        break;
      case 'recibir':
        path = '/1/recepcion';
        method = 'POST';
        body = c;
        await api.recibirRetorno(1, c);
        break;
      case 'cancelar':
        path = '/1/cancelacion';
        method = 'POST';
        body = { motivo: 'Cambio' };
        await api.cancelarRetorno(1, 'Cambio');
    }
    expect(f.mock.calls[0][0]).toBe('/api/v1/logistica-inversa' + path);
    expect(f.mock.calls[0][1].method).toBe(method);
    if (body !== undefined) expect(JSON.parse(f.mock.calls[0][1].body)).toEqual(body);
  },
);
