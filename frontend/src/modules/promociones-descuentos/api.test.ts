import { afterEach, describe, expect, it, vi } from 'vitest';
import { actualizarPromocion, crearPromocion, eliminarPromocion, evaluarCompra, listarPromociones, obtenerPromocion, obtenerResumenPromociones } from './api';

afterEach(() => vi.unstubAllGlobals());

describe('API de promociones', () => {
  it('A2 envía las rutas, los filtros, los métodos y los cuerpos del contrato', async () => {
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, init: RequestInit = {}) => new Response(init.method === 'DELETE' ? null : '{}', { status: init.method === 'DELETE' ? 204 : 200 }));
    vi.stubGlobal('fetch', fetchMock);
    const request = { codigo: 'LAPTOP-10', nombre: 'Laptop diez', tipo: 'Producto' as const, productoId: 1, porcentajeDescuento: 10, fechaInicio: '2026-01-01', fechaFin: '2026-12-31', activa: true };
    await listarPromociones({ busqueda: 'Laptop', tipo: 'Producto', segmentoId: 2, vigentes: true, pagina: 1, tamanoPagina: 10 });
    await obtenerPromocion(7);
    await crearPromocion(request);
    await actualizarPromocion(7, request);
    await eliminarPromocion(7);
    await evaluarCompra({ clienteId: 1, fecha: '2026-01-01', lineas: [{ productoId: 2, cantidad: 2, precioUnitario: 2500 }] });
    await obtenerResumenPromociones();
    const llamadas = fetchMock.mock.calls;
    expect(String(llamadas[0]?.[0])).toContain('/promociones?busqueda=Laptop&tipo=Producto&segmentoId=2&vigentes=true&pagina=1&tamanoPagina=10');
    expect(llamadas.map((llamada) => llamada[1]?.method)).toEqual(['GET', 'GET', 'POST', 'PUT', 'DELETE', 'POST', 'GET']);
    expect(llamadas[1]?.[0]).toBe('/api/v1/promociones/7');
    expect(llamadas[2]?.[0]).toBe('/api/v1/promociones');
    expect(llamadas[2]?.[1]?.body).toBe(JSON.stringify(request));
    expect(llamadas[3]?.[0]).toBe('/api/v1/promociones/7');
    expect(llamadas[3]?.[1]?.body).toBe(JSON.stringify(request));
    expect(llamadas[4]?.[0]).toBe('/api/v1/promociones/7');
    expect(llamadas[5]?.[0]).toBe('/api/v1/promociones/evaluar');
    expect(llamadas[5]?.[1]?.body).toBe('{"clienteId":1,"fecha":"2026-01-01","lineas":[{"productoId":2,"cantidad":2,"precioUnitario":2500}]}');
    expect(llamadas[6]?.[0]).toBe('/api/v1/promociones/resumen');
  });
});
