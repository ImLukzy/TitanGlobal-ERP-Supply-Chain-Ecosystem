import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarAlmacen,
  crearAlmacen,
  eliminarAlmacen,
  listarAlmacenes,
  listarCiudadesAlmacen,
  obtenerAlmacen,
  obtenerResumenAlmacenes,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('API de almacenes', () => {
  it('lista almacenes con todos los filtros', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarAlmacenes({ busqueda: 'Lima', ciudad: 'Lima', tipo: 'Regional', activo: false, pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/almacenes?busqueda=Lima&ciudad=Lima&tipo=Regional&activo=false&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite filtros vacíos y undefined', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarAlmacenes({ busqueda: '', ciudad: undefined, tipo: '', activo: undefined, pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/almacenes?pagina=1&tamanoPagina=10');
  });

  it('obtiene el almacén por id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ id: 3 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerAlmacen(3);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/almacenes/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea un almacén con POST y el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ id: 7 }));
    vi.stubGlobal('fetch', fetchMock);
    const request = {
      codigo: 'ALM-ICA-01',
      nombre: 'Regional Ica',
      tipo: 'Regional' as const,
      direccion: 'Av. Central 100',
      ciudad: 'Ica',
      capacidadM3: 1250,
      responsable: 'Elena Paz',
      activo: true,
    };
    await crearAlmacen(request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/almacenes');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(request) });
  });

  it('actualiza un almacén con PUT e id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ id: 3 }));
    vi.stubGlobal('fetch', fetchMock);
    const request = {
      codigo: 'ALM-ARE-01',
      nombre: 'Regional Arequipa',
      tipo: 'Regional' as const,
      direccion: 'Av. Ejército 100',
      ciudad: 'Arequipa',
      capacidadM3: 3000,
      activo: true,
    };
    await actualizarAlmacen(3, request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/almacenes/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(request) });
  });

  it('elimina un almacén con DELETE e id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await eliminarAlmacen(6);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/almacenes/6');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('lista ciudades del catálogo', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json(['Arequipa', 'Lima']));
    vi.stubGlobal('fetch', fetchMock);
    await listarCiudadesAlmacen();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/almacenes/ciudades');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('obtiene el resumen de almacenes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ totalAlmacenes: 5 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerResumenAlmacenes();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/almacenes/resumen');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });
});
