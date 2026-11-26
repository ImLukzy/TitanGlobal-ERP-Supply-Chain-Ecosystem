import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarTomaInventario,
  anularTomaInventario,
  cerrarTomaInventario,
  crearTomaInventario,
  listarTomasInventario,
  obtenerResumenTomasInventario,
  obtenerTomaInventario,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de conteo físico (tomas de inventario)', () => {
  it('omite filtros vacíos al listar y consulta resumen y detalle', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(respuestaJson({ items: [], total: 0, pagina: 1, tamanoPagina: 10 }))
      .mockResolvedValueOnce(respuestaJson({ totalTomas: 3 }))
      .mockResolvedValueOnce(respuestaJson({ id: 9, codigo: 'TF-01' }));
    vi.stubGlobal('fetch', fetchMock);

    await listarTomasInventario({ busqueda: '', estado: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/tomas-inventario?pagina=1&tamanoPagina=10');

    await obtenerResumenTomasInventario();
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/tomas-inventario/resumen');

    await obtenerTomaInventario(9);
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/tomas-inventario/9');
  });

  it('envía POST/PUT para crear, actualizar, cerrar y anular tomas', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(respuestaJson({ id: 9 }))
      .mockResolvedValueOnce(respuestaJson({ id: 9 }))
      .mockResolvedValueOnce(respuestaJson({ id: 9, estado: 'Cerrada' }))
      .mockResolvedValueOnce(respuestaJson({ id: 9, estado: 'Anulada' }));
    vi.stubGlobal('fetch', fetchMock);

    await crearTomaInventario({
      codigo: 'TF-01',
      almacenId: 1,
      responsable: 'Ana Ruiz',
      lineas: [{ productoId: 2, cantidadContada: 10 }],
    });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/tomas-inventario');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });

    await actualizarTomaInventario(9, {
      responsable: 'Ana Ruiz',
      lineas: [{ productoId: 2, cantidadContada: 12 }],
    });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/tomas-inventario/9');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'PUT' });

    await cerrarTomaInventario(9);
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/tomas-inventario/9/cerrar');
    expect(fetchMock.mock.calls[2]?.[1]).toMatchObject({ method: 'POST' });

    await anularTomaInventario(9);
    expect(fetchMock.mock.calls[3]?.[0]).toBe('/api/v1/tomas-inventario/9/anular');
    expect(fetchMock.mock.calls[3]?.[1]).toMatchObject({ method: 'POST' });
  });
});
