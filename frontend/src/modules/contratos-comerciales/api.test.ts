import { afterEach, describe, expect, it, vi } from 'vitest';
import { activarContrato, actualizarContrato, cancelarContrato, crearContrato, eliminarContrato, finalizarContrato, listarContratos, listarClientesContrato, obtenerContrato, obtenerResumenContratos } from './api';

afterEach(() => vi.unstubAllGlobals());

describe('API de contratos comerciales', () => {
  it('A2 implementa las nueve rutas, métodos, query y cuerpos del contrato', async () => {
    const fetchMock = vi.fn(async (_url: RequestInfo | URL, init: RequestInit = {}) => new Response(init.method === 'DELETE' ? null : '{}', { status: init.method === 'DELETE' ? 204 : 200 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarContratos({ busqueda: 'Acme', estado: 'Vigente', clienteId: 1, vencidos: true, pagina: 2, tamanoPagina: 10 });
    await obtenerContrato(8);
    await crearContrato({ clienteId: 1, titulo: 'Convenio', fechaInicio: '2026-01-01', fechaFin: '2026-12-31', montoComprometido: 2000, condicionPago: 'Credito30' });
    await actualizarContrato(8, { clienteId: 1, titulo: 'Convenio', fechaInicio: '2026-01-01', fechaFin: '2026-12-31', montoComprometido: 2000, condicionPago: 'Contado' });
    await activarContrato(8);
    await finalizarContrato(8);
    await cancelarContrato(8, 'Cierre solicitado');
    await eliminarContrato(8);
    await obtenerResumenContratos();
    await listarClientesContrato();
    const llamadas = fetchMock.mock.calls;
    expect(llamadas[0]?.[0]).toContain('/contratos-comerciales?busqueda=Acme&estado=Vigente&clienteId=1&vencidos=true&pagina=2&tamanoPagina=10');
    expect(llamadas.map((llamada) => (llamada[1] as RequestInit).method)).toEqual(['GET', 'GET', 'POST', 'PUT', 'POST', 'POST', 'POST', 'DELETE', 'GET', 'GET']);
    expect(llamadas[2]?.[1]?.body).toContain('"titulo":"Convenio"');
    expect(llamadas[3]?.[0]).toContain('/contratos-comerciales/8');
    expect(llamadas[4]?.[0]).toContain('/activar');
    expect((llamadas[4]?.[1] as RequestInit).body).toBeUndefined();
    expect(llamadas[5]?.[0]).toContain('/finalizar');
    expect(llamadas[6]?.[1]?.body).toBe('{"motivo":"Cierre solicitado"}');
    expect(llamadas[7]?.[0]).toContain('/contratos-comerciales/8');
    expect(llamadas[8]?.[0]).toContain('/contratos-comerciales/resumen');
  });
});
