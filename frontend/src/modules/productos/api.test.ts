import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarProducto,
  crearProducto,
  eliminarProducto,
  listarCategoriasProducto,
  listarProductos,
  obtenerProducto,
  obtenerResumenProductos,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

const respuestaJson = (body: unknown, status = 200) => Response.json(body, { status });

describe('API de productos', () => {
  it('lista productos con filtros exactos y omite valores vacíos', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarProductos({ busqueda: '', categoria: 'Computo', precioMin: '100', precioMax: '3000', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/productos?categoria=Computo&precioMin=100&precioMax=3000&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite los filtros vacíos y las claves undefined', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarProductos({
      busqueda: '',
      categoria: undefined as unknown as string,
      precioMin: '',
      precioMax: '',
      pagina: 1,
    });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/productos?pagina=1');
  });

  it('obtiene un producto por id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 9 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerProducto(9);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/productos/9');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('lista categorías del catálogo', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson(['Computo']));
    vi.stubGlobal('fetch', fetchMock);
    await listarCategoriasProducto();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/productos/categorias');
  });

  it('obtiene el resumen de productos', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ totalProductos: 10 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerResumenProductos();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/productos/resumen');
  });

  it('crea un producto con POST y el cuerpo del contrato', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 1 }));
    vi.stubGlobal('fetch', fetchMock);
    const cuerpo = { nombre: 'Laptop Pro', categoria: 'Computo', precio: 2500 };
    await crearProducto(cuerpo);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/productos');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpo) });
  });

  it('actualiza el id esperado con PUT y el cuerpo del contrato', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 4 }));
    vi.stubGlobal('fetch', fetchMock);
    const cuerpo = { nombre: 'Teclado Mecánico', categoria: 'Perifericos', precio: 200 };
    await actualizarProducto(4, cuerpo);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/productos/4');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(cuerpo) });
  });

  it('elimina el id esperado con DELETE', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await eliminarProducto(9);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/productos/9');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });
});
