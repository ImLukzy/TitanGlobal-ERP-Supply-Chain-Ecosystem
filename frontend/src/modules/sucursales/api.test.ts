import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { valores } from './sucursales.testUtils';
afterEach(() => vi.unstubAllGlobals());
function respuesta() {
  const mock = vi
    .fn()
    .mockImplementation(
      async () =>
        new Response(JSON.stringify({ id: 1 }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
  vi.stubGlobal('fetch', mock);
  return mock;
}
it('omite vacíos y preserva false, país y tipo', async () => {
  const f = respuesta();
  await api.listarSucursales({
    busqueda: '',
    tipo: '',
    pais: '',
    activa: false,
    pagina: 1,
    tamanoPagina: 10,
  });
  expect(f.mock.calls[0][0]).toBe('/api/v1/sucursales?activa=false&pagina=1&tamanoPagina=10');
  await api.listarSucursales({ busqueda: 'Norte', tipo: 'Oficina', pais: 'Peru', activa: true });
  expect(f.mock.calls[1][0]).toBe(
    '/api/v1/sucursales?busqueda=Norte&tipo=Oficina&pais=Peru&activa=true',
  );
});
it.each(['detalle', 'resumen', 'crear', 'editar', 'eliminar', 'activar', 'desactivar'])(
  'contrato %s',
  async (accion) => {
    const f = respuesta();
    const d = {
      ...valores,
      contacto: null,
      email: null,
      telefono: null,
      observaciones: null,
      fechaApertura: valores.fechaApertura + 'Z',
    };
    let path = '/1',
      method = 'GET',
      body: unknown = undefined;
    switch (accion) {
      case 'detalle':
        await api.obtenerSucursal(1);
        break;
      case 'resumen':
        path = '/resumen';
        await api.resumenSucursales();
        break;
      case 'crear':
        path = '';
        method = 'POST';
        body = d;
        await api.crearSucursal(d);
        break;
      case 'editar':
        method = 'PUT';
        body = d;
        await api.actualizarSucursal(1, d);
        break;
      case 'eliminar':
        method = 'DELETE';
        await api.eliminarSucursal(1);
        break;
      case 'activar':
        path = '/1/activacion';
        method = 'POST';
        await api.activarSucursal(1);
        break;
      case 'desactivar':
        path = '/1/desactivacion';
        method = 'POST';
        await api.desactivarSucursal(1);
    }
    expect(f.mock.calls[0][0]).toBe('/api/v1/sucursales' + path);
    expect(f.mock.calls[0][1].method).toBe(method);
    if (body !== undefined) expect(JSON.parse(f.mock.calls[0][1].body)).toEqual(body);
    else expect(f.mock.calls[0][1].body).toBeUndefined();
  },
);
