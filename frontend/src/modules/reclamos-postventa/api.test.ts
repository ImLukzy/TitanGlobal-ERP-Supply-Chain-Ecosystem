import { afterEach, describe, expect, it, vi } from 'vitest';
import { actualizarReclamo, cerrarReclamo, crearReclamo, iniciarReclamo, listarReclamos, obtenerReclamo, obtenerResumenReclamos, rechazarReclamo, resolverReclamo } from './api';

describe('API de reclamos postventa', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('consulta bandeja, detalle y resumen con rutas y filtros exactos', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      void input;
      void init;
      return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
    });
    vi.stubGlobal('fetch', fetchMock);
    await listarReclamos({ pagina: 1, tamanoPagina: 10, busqueda: 'REC-1', estado: 'Abierto', tipo: 'Defecto', prioridad: 'Alta', clienteId: 1, vencidos: true });
    await obtenerReclamo(7);
    await obtenerResumenReclamos();
    expect(String(fetchMock.mock.calls[0]?.[0])).toContain('/reclamos-postventa?pagina=1&tamanoPagina=10&busqueda=REC-1&estado=Abierto&tipo=Defecto&prioridad=Alta&clienteId=1&vencidos=true');
    expect(String(fetchMock.mock.calls[1]?.[0])).toBe('/api/v1/reclamos-postventa/7');
    expect(String(fetchMock.mock.calls[2]?.[0])).toBe('/api/v1/reclamos-postventa/resumen');
  });

  it('envía alta, edición y acciones con sus métodos y cuerpos exactos', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      void input;
      void init;
      return Response.json({ id: 8, numero: 'REC-000008' });
    });
    vi.stubGlobal('fetch', fetchMock);
    const request = { clienteId: 1, ordenCompraId: 2, productoId: 5, tipo: 'Defecto' as const, prioridad: 'Alta' as const, descripcion: 'Revisar producto' };
    await crearReclamo(request);
    await actualizarReclamo(8, request);
    await iniciarReclamo(8);
    await resolverReclamo(8, { resolucion: 'Se reemplazó' });
    await cerrarReclamo(8);
    await rechazarReclamo(8, { motivo: 'Fuera de garantía' });
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe('/api/v1/reclamos-postventa');
    expect(String(fetchMock.mock.calls[1]?.[0])).toBe('/api/v1/reclamos-postventa/8');
    expect(String(fetchMock.mock.calls[2]?.[0])).toBe('/api/v1/reclamos-postventa/8/iniciar');
    expect(String(fetchMock.mock.calls[3]?.[0])).toBe('/api/v1/reclamos-postventa/8/resolver');
    expect(String(fetchMock.mock.calls[4]?.[0])).toBe('/api/v1/reclamos-postventa/8/cerrar');
    expect(String(fetchMock.mock.calls[5]?.[0])).toBe('/api/v1/reclamos-postventa/8/rechazar');
    expect(fetchMock.mock.calls.map((call) => call[1]?.method)).toEqual(['POST', 'PUT', 'POST', 'POST', 'POST', 'POST']);
    expect(JSON.parse(String(fetchMock.mock.calls[0]?.[1]?.body))).toEqual(request);
    expect(JSON.parse(String(fetchMock.mock.calls[1]?.[1]?.body))).toEqual(request);
    expect(fetchMock.mock.calls[2]?.[1]?.body).toBeUndefined();
    expect(JSON.parse(String(fetchMock.mock.calls[3]?.[1]?.body))).toEqual({ resolucion: 'Se reemplazó' });
    expect(fetchMock.mock.calls[4]?.[1]?.body).toBeUndefined();
    expect(JSON.parse(String(fetchMock.mock.calls[5]?.[1]?.body))).toEqual({ motivo: 'Fuera de garantía' });
  });
});
