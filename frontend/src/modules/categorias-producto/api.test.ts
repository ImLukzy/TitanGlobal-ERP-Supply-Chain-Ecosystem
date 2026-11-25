import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarCategoriaProducto,
  crearCategoriaProducto,
  desactivarCategoriaProducto,
  listarCategoriasProducto,
  obtenerCategoriaProducto,
  obtenerResumenCategoriasProducto,
  reactivarCategoriaProducto,
} from './api';

describe('categorias-producto api', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('construye la consulta al listar categorías y llama a los endpoints CRUD', async () => {
    const fetchMock = vi.fn().mockImplementation(async () =>
      new Response(JSON.stringify([]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );
    vi.stubGlobal('fetch', fetchMock);

    await listarCategoriasProducto({ buscar: 'Hardware', incluirInactivas: true });
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/v1/categorias-producto?buscar=Hardware&incluirInactivas=true',
      expect.anything(),
    );

    await obtenerCategoriaProducto(4);
    expect(fetchMock).toHaveBeenLastCalledWith('/api/v1/categorias-producto/4', expect.anything());

    await obtenerResumenCategoriasProducto();
    expect(fetchMock).toHaveBeenLastCalledWith('/api/v1/categorias-producto/resumen', expect.anything());

    await crearCategoriaProducto({ nombre: 'Redes', descripcion: 'Switches y routers' });
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/v1/categorias-producto',
      expect.objectContaining({ method: 'POST' }),
    );

    await actualizarCategoriaProducto(4, { nombre: 'Redes Empresariales' });
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/v1/categorias-producto/4',
      expect.objectContaining({ method: 'PUT' }),
    );

    fetchMock.mockResolvedValueOnce(new Response(null, { status: 204 }));
    await desactivarCategoriaProducto(4);
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/v1/categorias-producto/4',
      expect.objectContaining({ method: 'DELETE' }),
    );

    fetchMock.mockResolvedValueOnce(new Response(null, { status: 204 }));
    await reactivarCategoriaProducto(4);
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/v1/categorias-producto/4/reactivar',
      expect.objectContaining({ method: 'POST' }),
    );
  });
});
