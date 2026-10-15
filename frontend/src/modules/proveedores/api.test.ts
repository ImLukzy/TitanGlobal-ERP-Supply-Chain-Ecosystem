import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarProveedor,
  crearProveedor,
  eliminarProveedor,
  listarPaisesProveedor,
  listarProveedores,
  obtenerProveedor,
  obtenerResumenProveedores,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de proveedores', () => {
  it('lista proveedores con filtros exactos', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarProveedores({ busqueda: 'Andina', pais: 'Perú', activo: false, pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores?busqueda=Andina&pais=Per%C3%BA&activo=false&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite filtros vacíos y undefined', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarProveedores({ busqueda: '', pais: undefined, activo: undefined, pagina: 1, tamanoPagina: 20 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores?pagina=1&tamanoPagina=20');
  });

  it('obtiene el proveedor por id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 3 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerProveedor(3);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea proveedores con POST y el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 7 }));
    vi.stubGlobal('fetch', fetchMock);
    const request = { razonSocial: 'Nuevo Proveedor', ruc: '20777777777', pais: 'Perú', activo: true };
    await crearProveedor(request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(request) });
  });

  it('actualiza el proveedor con PUT e id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 3 }));
    vi.stubGlobal('fetch', fetchMock);
    const request = { razonSocial: 'Proveedor Editado', ruc: '20333333333', pais: 'Perú', activo: true };
    await actualizarProveedor(3, request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(request) });
  });

  it('elimina el proveedor con DELETE e id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await eliminarProveedor(6);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores/6');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('lista países del catálogo', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson(['Chile', 'China', 'Perú']));
    vi.stubGlobal('fetch', fetchMock);
    await listarPaisesProveedor();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores/paises');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('obtiene el resumen de proveedores', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ totalProveedores: 6 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerResumenProveedores();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores/resumen');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });
});
