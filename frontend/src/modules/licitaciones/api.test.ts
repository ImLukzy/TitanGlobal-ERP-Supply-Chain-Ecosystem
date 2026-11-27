import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarLicitacion, adjudicarLicitacion, cancelarLicitacion, cerrarLicitacion, crearLicitacion, crearOferta,
  eliminarLicitacion, listarLicitaciones, listarProveedoresActivosLicitacion, mejorarOferta, obtenerLicitacion, obtenerResumenLicitaciones,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function prepararFetch() {
  const fetchMock = vi.fn(async (...args: Parameters<typeof fetch>) => {
    const metodo = args[1]?.method ?? 'GET';
    return metodo === 'DELETE' ? new Response(null, { status: 204 }) : Response.json({ id: 7, items: [] });
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('API de licitaciones', () => {
  it('A2 lista con la query exacta y omite filtros vacíos', async () => {
    const fetchMock = prepararFetch();
    await listarLicitaciones({ busqueda: 'cobre', estado: 'Abierta', tipo: 'SubastaInversa', pagina: 2, tamanoPagina: 10 });
    await listarLicitaciones({ busqueda: '', estado: '', tipo: '', pagina: 1 });
    await listarLicitaciones({});
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/licitaciones?busqueda=cobre&estado=Abierta&tipo=SubastaInversa&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/licitaciones?pagina=1');
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/licitaciones');
  });

  it('A2 consulta detalle, resumen y proveedores activos por sus rutas', async () => {
    const fetchMock = prepararFetch();
    await obtenerLicitacion(3);
    await obtenerResumenLicitaciones();
    await listarProveedoresActivosLicitacion();
    expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([
      '/api/v1/licitaciones/3',
      '/api/v1/licitaciones/resumen',
      '/api/v1/proveedores?activo=true&tamanoPagina=100',
    ]);
  });

  it('A2 crea y actualiza con el método y el cuerpo exactos', async () => {
    const fetchMock = prepararFetch();
    const datos = { numero: 'LIC-1', titulo: 'T', tipo: 'ConcursoPrecios' as const, presupuestoReferencial: 100, fechaCierre: '2099-01-01T00:00:00.000Z' };
    await crearLicitacion(datos);
    await actualizarLicitacion(4, { titulo: 'T2', presupuestoReferencial: 200, fechaCierre: datos.fechaCierre });
    expect(fetchMock.mock.calls[0]?.[1]?.method).toBe('POST');
    expect(JSON.parse(String(fetchMock.mock.calls[0]?.[1]?.body))).toEqual(datos);
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/licitaciones/4');
    expect(fetchMock.mock.calls[1]?.[1]?.method).toBe('PUT');
    expect(JSON.parse(String(fetchMock.mock.calls[1]?.[1]?.body))).toEqual({ titulo: 'T2', presupuestoReferencial: 200, fechaCierre: datos.fechaCierre });
  });

  it('A2 ofertas, mejora, cierre, adjudicación, cancelación y baja usan sus rutas', async () => {
    const fetchMock = prepararFetch();
    await crearOferta(2, { proveedorId: 1, monto: 50, plazoEntregaDias: 10 });
    await mejorarOferta(2, 9, { monto: 40 });
    await cerrarLicitacion(2);
    await adjudicarLicitacion(2, { ofertaId: 9 });
    await cancelarLicitacion(2);
    await eliminarLicitacion(2);
    const llamadas = fetchMock.mock.calls.map((call) => `${call[1]?.method ?? 'GET'} ${String(call[0])}`);
    expect(llamadas).toEqual([
      'POST /api/v1/licitaciones/2/ofertas',
      'PUT /api/v1/licitaciones/2/ofertas/9',
      'POST /api/v1/licitaciones/2/cerrar',
      'POST /api/v1/licitaciones/2/adjudicar',
      'POST /api/v1/licitaciones/2/cancelar',
      'DELETE /api/v1/licitaciones/2',
    ]);
    expect(JSON.parse(String(fetchMock.mock.calls[3]?.[1]?.body))).toEqual({ ofertaId: 9 });
    expect(fetchMock.mock.calls[2]?.[1]?.body).toBeUndefined();
  });
});
