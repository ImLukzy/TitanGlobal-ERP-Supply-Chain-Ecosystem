import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  anularInspeccionInsumo,
  buscarProductos,
  buscarProveedoresActivos,
  crearInspeccionInsumo,
  dictaminarInspeccionInsumo,
  editarInspeccionInsumo,
  listarInspeccionesInsumos,
  obtenerInspeccionInsumo,
  obtenerResumenInspeccionesInsumos,
} from './api';
import type { GuardarInspeccionRequest } from './tipos';

afterEach(() => {
  vi.unstubAllGlobals();
});

function prepararFetch(contenido: unknown = { id: 7 }) {
  const fetchMock = vi.fn<(input: RequestInfo | URL, init?: RequestInit) => Promise<Response>>(() =>
    Promise.resolve(Response.json(contenido)),
  );
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function llamada(fetchMock: ReturnType<typeof prepararFetch>): { url: string; init: RequestInit } {
  const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
  return { url, init };
}

const guardar: GuardarInspeccionRequest = {
  productoId: 3,
  proveedorId: 1,
  lote: 'MO-2026-001',
  fechaInspeccion: '2026-11-02',
  cantidadRecibida: 500.125,
  cantidadMuestra: 50,
  unidadMedida: 'unidad',
  toleranciaDefectosPorcentaje: 5,
  inspector: 'Rosa Díaz',
};

describe('API de inspecciones de insumos', () => {
  it('lista sin enviar los filtros vacíos ni los indefinidos', async () => {
    const fetchMock = prepararFetch({ items: [], total: 0, pagina: 1, tamanoPagina: 20 });

    await listarInspeccionesInsumos({
      busqueda: '',
      productoId: undefined,
      proveedorId: undefined,
      estado: '',
      desde: '',
      hasta: undefined,
      pagina: 1,
      tamanoPagina: 20,
    });

    expect(llamada(fetchMock).url).toBe('/api/v1/inspecciones-insumos?pagina=1&tamanoPagina=20');
    expect(llamada(fetchMock).init.method).toBe('GET');
  });

  it('lista con todos los filtros combinados', async () => {
    const fetchMock = prepararFetch({ items: [], total: 0, pagina: 2, tamanoPagina: 20 });

    await listarInspeccionesInsumos({
      busqueda: 'MO-2026',
      productoId: 3,
      proveedorId: 1,
      estado: 'Aprobada',
      desde: '2026-10-01',
      hasta: '2026-10-31',
      pagina: 2,
      tamanoPagina: 20,
    });

    expect(llamada(fetchMock).url).toBe(
      '/api/v1/inspecciones-insumos?busqueda=MO-2026&productoId=3&proveedorId=1&estado=Aprobada&desde=2026-10-01&hasta=2026-10-31&pagina=2&tamanoPagina=20',
    );
  });

  it('consulta el resumen y el detalle con GET', async () => {
    const fetchMock = prepararFetch({});

    await obtenerResumenInspeccionesInsumos();
    await obtenerInspeccionInsumo(7);

    expect(fetchMock.mock.calls.map((call) => [call[0], (call[1] as RequestInit).method])).toEqual([
      ['/api/v1/inspecciones-insumos/resumen', 'GET'],
      ['/api/v1/inspecciones-insumos/7', 'GET'],
    ]);
  });

  it('crea con POST y edita con PUT con el cuerpo exacto', async () => {
    const fetchMock = prepararFetch({ id: 7, numero: 'CCI-000007' });

    await expect(crearInspeccionInsumo(guardar)).resolves.toEqual({ id: 7, numero: 'CCI-000007' });
    await editarInspeccionInsumo(7, guardar);

    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/inspecciones-insumos');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(guardar) });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/inspecciones-insumos/7');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(guardar) });
  });

  it('dictamina sin observaciones y anula con el motivo', async () => {
    const fetchMock = prepararFetch();

    await dictaminarInspeccionInsumo(7, { resultado: 'Rechazada', cantidadDefectuosa: 2.5 });
    await anularInspeccionInsumo(7, { motivo: 'Registro duplicado' });

    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/inspecciones-insumos/7/dictaminar');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({
      method: 'POST',
      body: JSON.stringify({ resultado: 'Rechazada', cantidadDefectuosa: 2.5 }),
    });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/inspecciones-insumos/7/anular');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ motivo: 'Registro duplicado' }) });
  });

  it('busca productos y proveedores activos para los selectores', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json({ items: [{ id: 3, nombre: 'Mouse Óptico', categoria: 'x', precio: 1 }], total: 1, pagina: 1, tamanoPagina: 20 }))
      .mockResolvedValueOnce(Response.json({ items: [{ id: 1, razonSocial: 'Andes Suministros S.A.C.', ruc: '1', pais: 'Peru', activo: true }], total: 1, pagina: 1, tamanoPagina: 20 }));
    vi.stubGlobal('fetch', fetchMock);

    await expect(buscarProductos('mouse', 1)).resolves.toEqual({ items: [{ id: 3, nombre: 'Mouse Óptico' }], total: 1, tamanoPagina: 20 });
    await expect(buscarProveedoresActivos('', 1)).resolves.toEqual({
      items: [{ id: 1, nombre: 'Andes Suministros S.A.C.' }],
      total: 1,
      tamanoPagina: 20,
    });

    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/productos?busqueda=mouse&pagina=1&tamanoPagina=20');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/proveedores?activo=true&pagina=1&tamanoPagina=20');
  });
});
