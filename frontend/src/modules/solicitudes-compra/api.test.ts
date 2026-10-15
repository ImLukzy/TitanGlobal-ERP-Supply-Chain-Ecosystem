import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarSolicitud,
  aprobarSolicitud,
  crearSolicitud,
  eliminarSolicitud,
  listarProductosSolicitud,
  listarProveedoresActivos,
  listarSolicitudes,
  obtenerResumenSolicitudes,
  obtenerSolicitud,
  rechazarSolicitud,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuesta(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

function prepararFetch(contenido: unknown = {}): ReturnType<typeof vi.fn> {
  const fetchMock = vi.fn(async () => respuesta(contenido));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('API de solicitudes de compra', () => {
  it('A2 lista con filtros camelCase y paginación exacta', async () => {
    const fetchMock = prepararFetch({ items: [], total: 0 });
    await listarSolicitudes({
      busqueda: 'SC-000001',
      estado: 'Pendiente',
      proveedorId: 2,
      fechaDesde: '2026-01-01',
      fechaHasta: '2026-01-31',
      pagina: 2,
      tamanoPagina: 10,
    });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/solicitudes-compra?busqueda=SC-000001&estado=Pendiente&proveedorId=2&fechaDesde=2026-01-01&fechaHasta=2026-01-31&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite filtros vacíos y opcionales', async () => {
    const fetchMock = prepararFetch({ items: [], total: 0 });
    await listarSolicitudes({ busqueda: '', estado: '', proveedorId: '', fechaDesde: '', fechaHasta: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/solicitudes-compra?pagina=1&tamanoPagina=10');
  });

  it('obtiene el detalle por id', async () => {
    const fetchMock = prepararFetch({ id: 3 });
    await obtenerSolicitud(3);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/solicitudes-compra/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea con POST y el cuerpo de solicitud', async () => {
    const fetchMock = prepararFetch({ id: 7, numero: 'SC-000007' });
    const request = { solicitante: 'Marta Ríos', proveedorId: 2, observaciones: 'Entrega urgente', lineas: [{ productoId: 1, cantidad: 2, precioEstimado: 2500 }] };
    await crearSolicitud(request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/solicitudes-compra');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(request) });
  });

  it('actualiza con PUT y el id', async () => {
    const fetchMock = prepararFetch({ id: 3, numero: 'SC-000003' });
    const request = { solicitante: 'Marta Ríos', lineas: [{ productoId: 4, cantidad: 1, precioEstimado: 900 }] };
    await actualizarSolicitud(3, request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/solicitudes-compra/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(request) });
  });

  it('aprueba con POST sin cuerpo', async () => {
    const fetchMock = prepararFetch({ id: 3 });
    await aprobarSolicitud(3);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/solicitudes-compra/3/aprobar');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
    expect(fetchMock.mock.calls[0]?.[1]).not.toHaveProperty('body');
  });

  it('rechaza con POST y { motivo }', async () => {
    const fetchMock = prepararFetch({ id: 3 });
    await rechazarSolicitud(3, { motivo: 'Presupuesto agotado' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/solicitudes-compra/3/rechazar');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ motivo: 'Presupuesto agotado' }) });
  });

  it('elimina con DELETE y el id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await eliminarSolicitud(5);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/solicitudes-compra/5');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('consulta resumen, proveedores activos y catálogo de productos', async () => {
    const fetchMock = prepararFetch({ items: [] });
    await obtenerResumenSolicitudes();
    await listarProveedoresActivos();
    await listarProductosSolicitud();
    expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([
      '/api/v1/solicitudes-compra/resumen',
      '/api/v1/proveedores?activo=true&tamanoPagina=100',
      '/api/v1/productos?tamanoPagina=100',
    ]);
  });
});
