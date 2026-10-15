import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarOrdenAbastecimiento,
  anularOrdenAbastecimiento,
  crearOrdenAbastecimiento,
  generarOrdenDesdeSolicitud,
  listarOrdenesAbastecimiento,
  listarProductosOrden,
  listarProveedoresActivosOrden,
  obtenerOrdenAbastecimiento,
  obtenerResumenOrdenesAbastecimiento,
  recibirOrdenAbastecimiento,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function prepararFetch() {
  const fetchMock = vi.fn(async (...args: Parameters<typeof fetch>) => {
    const options = args[1];
    return Response.json({ id: 7, numero: 'OA-000007', method: options?.method ?? 'GET' });
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('API de órdenes de abastecimiento', () => {
  it('A2 lista con query exacta y omite filtros vacíos', async () => {
    const fetchMock = prepararFetch();
    await listarOrdenesAbastecimiento({ busqueda: 'Andes & Cía', estado: 'Emitida', proveedorId: 1, fechaDesde: '', fechaHasta: '2026-10-14', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ordenes-abastecimiento?busqueda=Andes+%26+C%C3%ADa&estado=Emitida&proveedorId=1&fechaHasta=2026-10-14&pagina=2&tamanoPagina=10');
  });

  it('A2 consulta el detalle y el resumen por sus rutas', async () => {
    const fetchMock = prepararFetch();
    await obtenerOrdenAbastecimiento(9);
    await obtenerResumenOrdenesAbastecimiento();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ordenes-abastecimiento/9');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/ordenes-abastecimiento/resumen');
  });

  it('A2 crea una orden manual con el cuerpo exacto', async () => {
    const fetchMock = prepararFetch();
    const request = { proveedorId: 2, fechaEntregaEstimada: '2026-11-20', observaciones: 'Urgente', lineas: [{ productoId: 1, cantidad: 2, precioUnitario: 2500 }] };
    await crearOrdenAbastecimiento(request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ordenes-abastecimiento');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(request) });
  });

  it('A2 genera una orden desde solicitud con el cuerpo exacto', async () => {
    const fetchMock = prepararFetch();
    const request = { proveedorId: 2, fechaEntregaEstimada: '2026-11-20', observaciones: 'Revisar entrega' };
    await generarOrdenDesdeSolicitud(3, request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ordenes-abastecimiento/desde-solicitud/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(request) });
  });

  it('A2 actualiza una orden con PUT y su id', async () => {
    const fetchMock = prepararFetch();
    const request = { proveedorId: 2, fechaEntregaEstimada: '2026-11-20', lineas: [{ productoId: 2, cantidad: 1, precioUnitario: 900 }] };
    await actualizarOrdenAbastecimiento(4, request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ordenes-abastecimiento/4');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(request) });
  });

  it('A2 recibe una orden con POST sin cuerpo', async () => {
    const fetchMock = prepararFetch();
    await recibirOrdenAbastecimiento(5);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ordenes-abastecimiento/5/recibir');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
    expect(fetchMock.mock.calls[0]?.[1]).not.toHaveProperty('body');
  });

  it('A2 anula una orden con su motivo', async () => {
    const fetchMock = prepararFetch();
    await anularOrdenAbastecimiento(6, { motivo: 'Cambio de requerimiento' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ordenes-abastecimiento/6/anular');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ motivo: 'Cambio de requerimiento' }) });
  });

  it('consulta proveedores activos y productos para los formularios', async () => {
    const fetchMock = prepararFetch();
    await listarProveedoresActivosOrden();
    await listarProductosOrden();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores?activo=true&tamanoPagina=100');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/productos?tamanoPagina=100');
  });
});
