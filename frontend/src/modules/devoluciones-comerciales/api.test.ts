import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarDevolucion,
  aprobarDevolucion,
  crearDevolucion,
  eliminarDevolucion,
  listarDevoluciones,
  obtenerDisponibleDevolucion,
  obtenerDevolucion,
  obtenerResumenDevoluciones,
  recibirDevolucion,
  rechazarDevolucion,
} from './api';
import type { FiltrosDevolucion, GuardarDevolucionRequest } from './tipos';

afterEach(() => vi.unstubAllGlobals());

describe('API de devoluciones comerciales', () => {
  it('A2 usa rutas, métodos, filtros y cuerpos exactos', async () => {
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, init: RequestInit = {}) => new Response(init.method === 'DELETE' ? null : '{}', { status: init.method === 'DELETE' ? 204 : 200 }));
    vi.stubGlobal('fetch', fetchMock);
    const filtros: FiltrosDevolucion = { busqueda: 'Acme', estado: 'Solicitada', motivo: 'Defecto', clienteId: 1, pagina: 2, tamanoPagina: 10 };
    const request: GuardarDevolucionRequest = { clienteId: 1, ordenCompraId: 1, reclamoPostventaId: 4, motivo: 'Defecto', observaciones: 'Falla', lineas: [{ productoId: 1, cantidad: 1 }] };
    await listarDevoluciones(filtros);
    await obtenerDevolucion(8);
    await obtenerDisponibleDevolucion(5);
    await crearDevolucion(request);
    await actualizarDevolucion(8, request);
    await aprobarDevolucion(8);
    await recibirDevolucion(8);
    await rechazarDevolucion(8, 'Fuera del plazo');
    await eliminarDevolucion(8);
    await obtenerResumenDevoluciones();
    const llamadas = fetchMock.mock.calls;
    expect(String(llamadas[0]?.[0])).toContain('/devoluciones-comerciales?busqueda=Acme&estado=Solicitada&motivo=Defecto&clienteId=1&pagina=2&tamanoPagina=10');
    expect(String(llamadas[1]?.[0])).toContain('/devoluciones-comerciales/8');
    expect(String(llamadas[2]?.[0])).toContain('/devoluciones-comerciales/ordenes/5/disponible');
    expect(llamadas.map((llamada) => (llamada[1] as RequestInit).method)).toEqual(['GET', 'GET', 'GET', 'POST', 'PUT', 'POST', 'POST', 'POST', 'DELETE', 'GET']);
    expect((llamadas[3]?.[1] as RequestInit).body).toContain('"ordenCompraId":1');
    expect(String(llamadas[4]?.[0])).toContain('/devoluciones-comerciales/8');
    expect((llamadas[5]?.[1] as RequestInit).body).toBeUndefined();
    expect((llamadas[6]?.[1] as RequestInit).body).toBeUndefined();
    expect((llamadas[7]?.[1] as RequestInit).body).toBe('{"motivo":"Fuera del plazo"}');
    expect(String(llamadas[8]?.[0])).toContain('/devoluciones-comerciales/8');
    expect(String(llamadas[9]?.[0])).toContain('/devoluciones-comerciales/resumen');
  });
});
