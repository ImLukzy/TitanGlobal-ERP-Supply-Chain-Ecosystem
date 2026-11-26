import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  listarTransferencias,
  obtenerResumenTransferencias,
  obtenerTransferencia,
  registrarTransferencia,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de transferencias entre almacenes', () => {
  it('omite filtros vacíos y consulta listado, resumen y detalle', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(respuestaJson({ items: [], total: 0, pagina: 1, tamanoPagina: 10 }))
      .mockResolvedValueOnce(
        respuestaJson({ totalTransferencias: 3, unidadesTransferidas: 40, valorTransferido: 1200 }),
      )
      .mockResolvedValueOnce(respuestaJson({ id: 7 }));
    vi.stubGlobal('fetch', fetchMock);

    await listarTransferencias({ fechaDesde: '', fechaHasta: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      '/api/v1/transferencias-almacen?pagina=1&tamanoPagina=10',
    );

    await obtenerResumenTransferencias();
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/transferencias-almacen/resumen');

    await obtenerTransferencia(7);
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/transferencias-almacen/7');
  });

  it('envía POST a /api/v1/transferencias-almacen con payload completo', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(respuestaJson({ id: 12, costoUnitario: 35 }));
    vi.stubGlobal('fetch', fetchMock);

    const request = {
      productoId: 4,
      almacenOrigenId: 1,
      almacenDestinoId: 2,
      cantidad: 15,
      observaciones: 'Traslado urgente',
    };
    const respuesta = await registrarTransferencia(request);

    expect(respuesta).toEqual({ id: 12, costoUnitario: 35 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/transferencias-almacen');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({
      method: 'POST',
      body: JSON.stringify(request),
    });
  });
});
