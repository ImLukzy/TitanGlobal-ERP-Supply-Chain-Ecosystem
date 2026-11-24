import { afterEach, describe, expect, it, vi } from 'vitest';
import { actualizarConductor, crearConductor, eliminarConductor, listarConductores, obtenerConductor } from './api';
import type { GuardarConductorRequest } from './tipos';

afterEach(() => vi.unstubAllGlobals());
const request: GuardarConductorRequest = {
  nombre: 'Rosa Quispe', documentoLicencia: 'PLT-QA-01', vencimientoLicencia: '2027-12-31T00:00:00Z',
  habilitado: false, activo: true, observaciones: 'Revisión local',
};

describe('API de conductores', () => {
  it('envía búsqueda y paginación conservando habilitado=false', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarConductores({ busqueda: 'Rosa Quispe', habilitado: false, pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/conductores?busqueda=Rosa+Quispe&habilitado=false&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite parámetros vacíos e indefinidos', async () => {
    const fetchMock = vi.fn().mockImplementation(async () => Response.json({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarConductores({ busqueda: '', habilitado: '', pagina: 1, tamanoPagina: undefined });
    await listarConductores({});
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/conductores?pagina=1');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/conductores');
  });

  it('consulta el detalle con GET', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({}));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerConductor(7);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/conductores/7');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it.each(['POST', 'PUT'] as const)('guarda con %s y cuerpo exacto con fecha UTC', async (metodo) => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ id: 7 }));
    vi.stubGlobal('fetch', fetchMock);
    const resultado = metodo === 'POST' ? await crearConductor(request) : await actualizarConductor(7, request);
    expect(resultado).toEqual({ id: 7 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe(`/api/v1/conductores${metodo === 'PUT' ? '/7' : ''}`);
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: metodo, body: JSON.stringify(request) });
  });

  it('elimina con DELETE y acepta 204 sin JSON', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await expect(eliminarConductor(7)).resolves.toBeUndefined();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/conductores/7');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('conserva el mensaje literal del conflicto', async () => {
    const message = 'Ya existe un conductor con el documento de licencia PLT-QA-01.';
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ StatusCode: 409, Message: message }, { status: 409 })));
    await expect(crearConductor(request)).rejects.toMatchObject({ statusCode: 409, message });
  });
});
