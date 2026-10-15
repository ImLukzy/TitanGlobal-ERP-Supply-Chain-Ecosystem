import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarCotizacion,
  aceptarCotizacion,
  crearCotizacion,
  eliminarCotizacion,
  listarClientesCotizaciones,
  listarCotizaciones,
  listarProductosCotizaciones,
  obtenerCotizacion,
  obtenerResumenCotizaciones,
  rechazarCotizacion,
} from './api';

afterEach(() => vi.unstubAllGlobals());

function prepararFetch(contenido: unknown = {}): ReturnType<typeof vi.fn> {
  const fetchMock = vi.fn(async () => Response.json(contenido));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('API de cotizaciones', () => {
  it('A2 lista con filtros exactos y omite los vacíos', async () => {
    const fetchMock = prepararFetch({ items: [] });
    await listarCotizaciones({
      busqueda: 'COT-DEV-001',
      estado: 'Emitida',
      clienteId: 1,
      vencidas: true,
      fechaDesde: '2026-01-01',
      fechaHasta: '2026-01-31',
      pagina: 2,
      tamanoPagina: 10,
    });
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      '/api/v1/cotizaciones?busqueda=COT-DEV-001&estado=Emitida&clienteId=1&vencidas=true&fechaDesde=2026-01-01&fechaHasta=2026-01-31&pagina=2&tamanoPagina=10',
    );
    await listarCotizaciones({
      busqueda: '',
      estado: '',
      clienteId: '',
      vencidas: false,
      fechaDesde: '',
      fechaHasta: '',
      pagina: 1,
      tamanoPagina: 10,
    });
    expect(fetchMock.mock.calls[1]?.[0]).toBe(
      '/api/v1/cotizaciones?pagina=1&tamanoPagina=10',
    );
  });

  it('A2 consulta el detalle y resumen', async () => {
    const fetchMock = prepararFetch({ id: 2 });
    await obtenerCotizacion(2);
    await obtenerResumenCotizaciones();
    expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([
      '/api/v1/cotizaciones/2',
      '/api/v1/cotizaciones/resumen',
    ]);
  });

  it('carga clientes y productos con los tamaños de página del contrato', async () => {
    const fetchMock = prepararFetch({ items: [] });
    await listarClientesCotizaciones();
    await listarProductosCotizaciones();
    expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([
      '/api/v1/clientes?tamanoPagina=100',
      '/api/v1/productos?tamanoPagina=100',
    ]);
  });

  it('A2 crea con POST y el DTO de entrada', async () => {
    const fetchMock = prepararFetch({ id: 1, numero: 'COT-000001' });
    const request = {
      clienteId: 1,
      fechaValidez: '2027-01-01',
      lineas: [
        {
          productoId: 1,
          cantidad: 2,
          precioUnitario: 2500,
          descuentoPorcentaje: 10,
        },
      ],
    };
    await crearCotizacion(request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/cotizaciones');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({
      method: 'POST',
      body: JSON.stringify(request),
    });
  });

  it('A2 actualiza con PUT y el id', async () => {
    const fetchMock = prepararFetch({ id: 1 });
    const request = { clienteId: 1, fechaValidez: '2027-01-01', lineas: [] };
    await actualizarCotizacion(1, request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/cotizaciones/1');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({
      method: 'PUT',
      body: JSON.stringify(request),
    });
  });

  it('A2 acepta con POST sin cuerpo', async () => {
    const fetchMock = prepararFetch({ id: 1 });
    await aceptarCotizacion(1);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/cotizaciones/1/aceptar');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
    expect(fetchMock.mock.calls[0]?.[1]).not.toHaveProperty('body');
  });

  it('A2 rechaza con POST y el motivo', async () => {
    const fetchMock = prepararFetch({ id: 1 });
    await rechazarCotizacion(1, { motivo: 'Cambio de presupuesto' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      '/api/v1/cotizaciones/1/rechazar',
    );
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({
      method: 'POST',
      body: JSON.stringify({ motivo: 'Cambio de presupuesto' }),
    });
  });

  it('A2 elimina con DELETE', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await eliminarCotizacion(5);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/cotizaciones/5');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });
});
