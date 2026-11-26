import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarLote,
  cambiarEstadoSerie,
  crearLote,
  crearSerie,
  listarLotes,
  listarSeries,
  obtenerLote,
  obtenerResumenLotes,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de lotes y series', () => {
  it('omite filtros vacíos y undefined al listar lotes y series', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(respuestaJson({ items: [], total: 0, pagina: 1, tamanoPagina: 10 }))
      .mockResolvedValueOnce(respuestaJson({ items: [], total: 0, pagina: 1, tamanoPagina: 10 }));
    vi.stubGlobal('fetch', fetchMock);

    await listarLotes({ busqueda: '', estado: '', vencidos: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/lotes-series/lotes?pagina=1&tamanoPagina=10');

    await listarSeries({ busqueda: '', estado: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[1]?.[0]).toBe(
      '/api/v1/lotes-series/series?pagina=1&tamanoPagina=10',
    );
  });

  it('consulta resumen y detalle de lote por GET', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        respuestaJson({
          totalLotes: 4,
          disponibles: 2,
          enCuarentena: 1,
          bloqueados: 1,
          vencidos: 0,
          porVencer: 1,
        }),
      )
      .mockResolvedValueOnce(respuestaJson({ id: 5, codigo: 'LOT-001' }));
    vi.stubGlobal('fetch', fetchMock);

    await obtenerResumenLotes();
    await obtenerLote(5);

    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/lotes-series/lotes/resumen');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/lotes-series/lotes/5');
  });

  it('envía POST y PUT con rutas y payloads exactos para lotes y series', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(respuestaJson({ id: 10 }))
      .mockResolvedValueOnce(respuestaJson({ id: 10 }))
      .mockResolvedValueOnce(respuestaJson({ id: 20 }))
      .mockResolvedValueOnce(respuestaJson({ id: 20 }));
    vi.stubGlobal('fetch', fetchMock);

    await crearLote({ productoId: 1, codigo: 'LOT-01', cantidad: 50, estado: 'Disponible' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/lotes-series/lotes');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });

    await actualizarLote(10, { cantidad: 45, estado: 'Cuarentena' });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/lotes-series/lotes/10');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'PUT' });

    await crearSerie({ productoId: 1, loteId: 10, numeroSerie: 'SN-001' });
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/lotes-series/series');
    expect(fetchMock.mock.calls[2]?.[1]).toMatchObject({ method: 'POST' });

    await cambiarEstadoSerie(20, { estado: 'Asignada' });
    expect(fetchMock.mock.calls[3]?.[0]).toBe('/api/v1/lotes-series/series/20/estado');
    expect(fetchMock.mock.calls[3]?.[1]).toMatchObject({
      method: 'PUT',
      body: JSON.stringify({ estado: 'Asignada' }),
    });
  });
});
