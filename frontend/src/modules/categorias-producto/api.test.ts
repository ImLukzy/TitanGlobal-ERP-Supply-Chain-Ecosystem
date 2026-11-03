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
    it('omite filtros vacíos y undefined al listar', async () => {
        const fetchMock = vi.fn().mockResolvedValue(respuestaJson([]));
        vi.stubGlobal('fetch', fetchMock);

        await listarCategoriasProducto({ buscar: '', incluirInactivas: undefined });

        expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/categorias-producto');
    });

    it('envía la búsqueda y el indicador de inactivas', async () => {
        const fetchMock = vi.fn().mockResolvedValue(respuestaJson([]));
        vi.stubGlobal('fetch', fetchMock);

        await listarCategoriasProducto({ buscar: 'hog', incluirInactivas: true });

        expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/categorias-producto?buscar=hog&incluirInactivas=true');
    });

    it('obtiene el detalle por id', async () => {
        const fetchMock = vi
            .fn()
            .mockResolvedValue(respuestaJson({ id: 2, nombre: 'Hogar', descripcion: null, activa: true }));
        vi.stubGlobal('fetch', fetchMock);

        const categoria = await obtenerCategoriaProducto(2);

        expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/categorias-producto/2');
        expect(categoria.nombre).toBe('Hogar');
    });

    it('crea con POST y el cuerpo exacto', async () => {
        const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 7 }));
        vi.stubGlobal('fetch', fetchMock);
        const request = { nombre: 'Jardinería', descripcion: 'Herramientas de jardín' };

        const resultado = await crearCategoriaProducto(request);

        expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/categorias-producto');
        expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(request) });
        expect(resultado.id).toBe(7);
    });

    it('actualiza con PUT y el cuerpo exacto', async () => {
        const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 2 }));
        vi.stubGlobal('fetch', fetchMock);
        const request = { nombre: 'Hogar y cocina' };

        await actualizarCategoriaProducto(2, request);

        expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/categorias-producto/2');
        expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(request) });
    });

    it('desactiva con DELETE y acepta la respuesta 204', async () => {
        const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
        vi.stubGlobal('fetch', fetchMock);

        await expect(desactivarCategoriaProducto(3)).resolves.toBeUndefined();

        expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/categorias-producto/3');
        expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
    });

    it('consulta el resumen', async () => {
        const resumen = { totalCategorias: 3, categoriasActivas: 2, categoriasInactivas: 1 };
        const fetchMock = vi.fn().mockResolvedValue(respuestaJson(resumen));
        vi.stubGlobal('fetch', fetchMock);

        const resultado = await obtenerResumenCategoriasProducto();

        expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/categorias-producto/resumen');
        expect(resultado).toEqual(resumen);
    });
});