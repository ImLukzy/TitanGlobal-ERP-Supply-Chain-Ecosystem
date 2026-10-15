import { afterEach, describe, expect, it, vi } from 'vitest';
import { actualizarVendedor, asignarVendedor, crearVendedor, eliminarVendedor, listarOrdenes, listarVendedores, liquidarMes, obtenerComisiones, obtenerVendedor, pagarLiquidacion, quitarVendedor } from './api';

afterEach(() => vi.unstubAllGlobals());

describe('API de comisiones de vendedores', () => {
  it('A2 implementa las once operaciones con ruta, método, query y cuerpos exactos', async () => {
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, init: RequestInit = {}) => {
      const metodo = init.method ?? 'GET';
      return new Response(metodo === 'DELETE' ? null : '{}', { status: metodo === 'DELETE' ? 204 : 200 });
    });
    vi.stubGlobal('fetch', fetchMock);
    const vendedor = { codigo: 'V-ANA', nombre: 'Ana Torres', email: 'ana@titanglobal.example', porcentajeComision: 5, activo: true };
    await listarVendedores({ busqueda: 'Ana', activo: false, pagina: 2, tamanoPagina: 10 });
    await obtenerVendedor(7);
    await crearVendedor(vendedor);
    await actualizarVendedor(7, vendedor);
    await eliminarVendedor(7);
    await listarOrdenes({ vendedorId: 7, sinAsignar: true, anio: 2026, mes: 2, pagina: 2, tamanoPagina: 10 });
    await asignarVendedor(42, { vendedorId: 7 });
    await quitarVendedor(42);
    await obtenerComisiones(2026, 2);
    await liquidarMes(2026, 2);
    await pagarLiquidacion(9);

    const llamadas = fetchMock.mock.calls;
    expect(llamadas.map((llamada) => String(llamada[0]))).toEqual([
      '/api/v1/comisiones-vendedores/vendedores?busqueda=Ana&activo=false&pagina=2&tamanoPagina=10',
      '/api/v1/comisiones-vendedores/vendedores/7',
      '/api/v1/comisiones-vendedores/vendedores',
      '/api/v1/comisiones-vendedores/vendedores/7',
      '/api/v1/comisiones-vendedores/vendedores/7',
      '/api/v1/comisiones-vendedores/ordenes?vendedorId=7&sinAsignar=true&anio=2026&mes=2&pagina=2&tamanoPagina=10',
      '/api/v1/comisiones-vendedores/ordenes/42/vendedor',
      '/api/v1/comisiones-vendedores/ordenes/42/vendedor',
      '/api/v1/comisiones-vendedores/comisiones?anio=2026&mes=2',
      '/api/v1/comisiones-vendedores/liquidaciones',
      '/api/v1/comisiones-vendedores/liquidaciones/9/pagar',
    ]);
    expect(llamadas.map((llamada) => (llamada[1] as RequestInit).method ?? 'GET')).toEqual([
      'GET', 'GET', 'POST', 'PUT', 'DELETE', 'GET', 'PUT', 'DELETE', 'GET', 'POST', 'POST',
    ]);
    expect(llamadas[2]?.[1]?.body).toBe(JSON.stringify(vendedor));
    expect(llamadas[3]?.[1]?.body).toBe(JSON.stringify(vendedor));
    expect(llamadas[6]?.[1]?.body).toBe('{"vendedorId":7}');
    expect(llamadas[9]?.[1]?.body).toBe('{"anio":2026,"mes":2}');
    expect(llamadas[10]?.[1]?.body).toBeUndefined();
  });
});
