import { afterEach, describe, expect, it, vi } from 'vitest';
import { actualizarVehiculoFlota, crearVehiculoFlota, eliminarVehiculoFlota, listarVehiculosFlota, listarTiposVehiculoFlota, obtenerVehiculoFlota, obtenerResumenVehiculosFlota } from './api';
import type { GuardarVehiculoFlotaRequest } from './tipos';

afterEach(() => vi.unstubAllGlobals());

const request: GuardarVehiculoFlotaRequest = {
  placa: 'ABC-123', marca: 'Volvo', modelo: 'FH', anio: 2025,
  tipo: 'Camion', capacidadKg: 15000.25, activo: false, observaciones: 'Revisión programada',
};

describe('API de vehículos de flota', () => {
  it('envía búsqueda y filtros, incluido activo=false', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarVehiculosFlota({ busqueda: 'Volvo FH', tipo: 'Camion', activo: false, pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/vehiculos-flota?busqueda=Volvo+FH&tipo=Camion&activo=false&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite filtros vacíos y parámetros indefinidos', async () => {
    const fetchMock = vi.fn().mockImplementation(async () => Response.json({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarVehiculosFlota({ busqueda: '', tipo: undefined, activo: '', pagina: 1, tamanoPagina: 10 });
    await listarVehiculosFlota({});
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/vehiculos-flota?pagina=1&tamanoPagina=10');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/vehiculos-flota');
  });

  it.each([
    ['detalle', () => obtenerVehiculoFlota(7), '/7'],
    ['tipos', listarTiposVehiculoFlota, '/tipos'],
    ['resumen', obtenerResumenVehiculosFlota, '/resumen'],
  ] as const)('consulta %s con GET', async (_nombre, ejecutar, ruta) => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({}));
    vi.stubGlobal('fetch', fetchMock);
    await ejecutar();
    expect(fetchMock.mock.calls[0]?.[0]).toBe(`/api/v1/vehiculos-flota${ruta}`);
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it.each(['POST', 'PUT'] as const)('guarda mediante %s con el cuerpo exacto', async (metodo) => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ id: 7 }));
    vi.stubGlobal('fetch', fetchMock);
    const respuesta = metodo === 'POST' ? await crearVehiculoFlota(request) : await actualizarVehiculoFlota(7, request);
    expect(respuesta).toEqual({ id: 7 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe(`/api/v1/vehiculos-flota${metodo === 'PUT' ? '/7' : ''}`);
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: metodo, body: JSON.stringify(request) });
  });

  it('elimina con DELETE y acepta 204 sin cuerpo', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await expect(eliminarVehiculoFlota(7)).resolves.toBeUndefined();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/vehiculos-flota/7');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('conserva el mensaje literal del conflicto', async () => {
    const message = 'Ya existe un vehículo con la placa ABC-123.';
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ StatusCode: 409, Message: message }, { status: 409 })));
    await expect(crearVehiculoFlota(request)).rejects.toMatchObject({ statusCode: 409, message });
  });
});
