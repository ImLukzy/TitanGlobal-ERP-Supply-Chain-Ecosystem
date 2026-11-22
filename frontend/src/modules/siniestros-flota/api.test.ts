import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { valores, valoresReclamo } from './siniestros.testUtils';
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
it('listado omite vacíos y conserva filtros', async () => {
  const f = respuesta();
  await api.listarSiniestros({ busqueda: '', estado: '', tipo: '', pagina: 1, tamanoPagina: 10 });
  expect(f.mock.calls[0][0]).toBe('/api/v1/siniestros-flota?pagina=1&tamanoPagina=10');
  await api.listarSiniestros({ busqueda: 'Patio Norte', estado: 'EnReclamo', tipo: 'Robo' });
  expect(f.mock.calls[1][0]).toBe(
    '/api/v1/siniestros-flota?busqueda=Patio+Norte&estado=EnReclamo&tipo=Robo',
  );
});
it.each(['detalle', 'resumen', 'crear', 'editar', 'eliminar', 'reclamar', 'cerrar', 'anular'])(
  'contrato %s',
  async (accion) => {
    const f = respuesta();
    const d = { ...valores },
      r = { ...valoresReclamo, montoReclamado: 1250.5 },
      c = {
        resultado: 'Aceptado' as const,
        montoReconocido: 1000.25,
        resolucion: 'Referencia manual',
      };
    let path = '/1',
      method = 'GET',
      body: unknown = undefined;
    switch (accion) {
      case 'detalle':
        await api.obtenerSiniestro(1);
        break;
      case 'resumen':
        path = '/resumen';
        await api.resumenSiniestros();
        break;
      case 'crear':
        path = '';
        method = 'POST';
        body = d;
        await api.crearSiniestro(d);
        break;
      case 'editar':
        method = 'PUT';
        body = d;
        await api.actualizarSiniestro(1, d);
        break;
      case 'eliminar':
        method = 'DELETE';
        await api.eliminarSiniestro(1);
        break;
      case 'reclamar':
        path = '/1/reclamo';
        method = 'POST';
        body = r;
        await api.reclamarSiniestro(1, r);
        break;
      case 'cerrar':
        path = '/1/cierre';
        method = 'POST';
        body = c;
        await api.cerrarSiniestro(1, c);
        break;
      case 'anular':
        path = '/1/anulacion';
        method = 'POST';
        body = { motivo: 'Cambio' };
        await api.anularSiniestro(1, 'Cambio');
    }
    expect(f.mock.calls[0][0]).toBe('/api/v1/siniestros-flota' + path);
    expect(f.mock.calls[0][1].method).toBe(method);
    if (body !== undefined) expect(JSON.parse(f.mock.calls[0][1].body)).toEqual(body);
  },
);
