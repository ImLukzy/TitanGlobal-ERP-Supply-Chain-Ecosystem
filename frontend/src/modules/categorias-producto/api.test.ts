import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarCategoriaProducto,
  crearCategoriaProducto,
  desactivarCategoriaProducto,
  listarCategoriasProducto,
  obtenerCategoriaProducto,
  obtenerResumenCategoriasProducto,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de categorías de producto', () => {
  it('omite búsqueda vacía y envía incluirInactivas cuando se indica', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson([]));
    vi.stubGlobal('fetch', fetchMock);

    await listarCategoriasProducto({ buscar: '', incluirInactivas: false });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/categorias-producto?incluirInactivas=false');
  });

  it('envía buscar e incluirInactivas en la query string', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson([]));
    vi.stubGlobal('fetch', fetchMock);

    await listarCategoriasProducto({ buscar: 'Electrónica', incluirInactivas: true });
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      '/api/v1/categorias-producto?buscar=Electr%C3%B3nica&incluirInactivas=true',
    );
  });

  it('consulta detalle y resumen por GET', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        respuestaJson({ id: 3, nombre: 'Herramientas', descripcion: null, activa: true }),
      )
      .mockResolvedValueOnce(
        respuestaJson({ totalCategorias: 5, categoriasActivas: 4, categoriasInactivas: 1 }),
      );
    vi.stubGlobal('fetch', fetchMock);

    await obtenerCategoriaProducto(3);
    await obtenerResumenCategoriasProducto();

    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/categorias-producto/3');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/categorias-producto/resumen');
  });

  it('crea, actualiza y desactiva categorías con los métodos HTTP correctos', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(respuestaJson({ id: 9 }))
      .mockResolvedValueOnce(respuestaJson({ id: 9 }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);

    await crearCategoriaProducto({ nombre: 'Insumos', descripcion: 'Uso general' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/categorias-producto');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({
      method: 'POST',
      body: JSON.stringify({ nombre: 'Insumos', descripcion: 'Uso general' }),
    });

    await actualizarCategoriaProducto(9, { nombre: 'Insumos Industriales' });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/categorias-producto/9');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({
      method: 'PUT',
      body: JSON.stringify({ nombre: 'Insumos Industriales' }),
    });

    await desactivarCategoriaProducto(9);
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/categorias-producto/9');
    expect(fetchMock.mock.calls[2]?.[1]).toMatchObject({ method: 'DELETE' });
  });
});
