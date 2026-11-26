import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  consultarKardex,
  listarMovimientos,
  listarSaldosKardex,
  obtenerResumenKardex,
  registrarMovimiento,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de kárdex valorizado', () => {
  it('omite filtros vacíos al listar movimientos y consulta resumen y saldos', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(respuestaJson({ items: [], total: 0, pagina: 1, tamanoPagina: 10 }))
      .mockResolvedValueOnce(respuestaJson({ totalMovimientos: 0 }))
      .mockResolvedValueOnce(respuestaJson([]));
    vi.stubGlobal('fetch', fetchMock);

    await listarMovimientos({ tipo: '', fechaDesde: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      '/api/v1/kardex-valorizado/movimientos?pagina=1&tamanoPagina=10',
    );

    await obtenerResumenKardex();
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/kardex-valorizado/resumen');

    await listarSaldosKardex();
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/kardex-valorizado/saldos');
  });

  it('consulta kárdex PEPS y registra movimientos con POST', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(respuestaJson({ productoId: 1, almacenId: 2, metodo: 'PEPS', lineas: [] }))
      .mockResolvedValueOnce(respuestaJson({ id: 55 }));
    vi.stubGlobal('fetch', fetchMock);

    await consultarKardex(1, 2, 'PEPS');
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      '/api/v1/kardex-valorizado/kardex?productoId=1&almacenId=2&metodo=PEPS',
    );

    const request = { productoId: 1, almacenId: 2, tipo: 'Entrada' as const, cantidad: 10, costoUnitario: 25.5 };
    await registrarMovimiento(request);
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/kardex-valorizado/movimientos');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({
      method: 'POST',
      body: JSON.stringify(request),
    });
  });
});
