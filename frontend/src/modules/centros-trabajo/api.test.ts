import { afterEach, describe, expect, it, vi } from 'vitest';
import { actualizarCentroTrabajo, crearCentroTrabajo, eliminarCentroTrabajo, listarCentrosTrabajo, listarPlantasCentroTrabajo, obtenerCentroTrabajo, obtenerResumenCentrosTrabajo } from './api';
import type { GuardarCentroTrabajoRequest } from './tipos';

afterEach(() => vi.unstubAllGlobals());

const request: GuardarCentroTrabajoRequest = {
  codigo: 'CT-NEW-01', nombre: 'Celda nueva', tipo: 'Celda', planta: 'Planta Lima',
  capacidadHorasDia: 8.25, costoHora: 85.5, responsable: 'Rosa', activo: false,
};

describe('API de centros de trabajo', () => {
  it('envía filtros y paginación, incluido activo=false', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarCentrosTrabajo({ busqueda: 'CT Lima', planta: 'Planta Lima', tipo: 'Celda', activo: false, pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/centros-trabajo?busqueda=CT+Lima&planta=Planta+Lima&tipo=Celda&activo=false&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite filtros vacíos y no añade interrogación sin parámetros', async () => {
    const fetchMock = vi.fn().mockImplementation(async () => Response.json({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarCentrosTrabajo({ busqueda: '', planta: undefined, tipo: '', activo: '', pagina: 1, tamanoPagina: 10 });
    await listarCentrosTrabajo({});
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/centros-trabajo?pagina=1&tamanoPagina=10');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/centros-trabajo');
  });

  it.each([
    ['detalle', () => obtenerCentroTrabajo(7), '/7'],
    ['plantas', listarPlantasCentroTrabajo, '/plantas'],
    ['resumen', obtenerResumenCentrosTrabajo, '/resumen'],
  ] as const)('consulta %s con GET', async (_nombre, ejecutar, ruta) => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({}));
    vi.stubGlobal('fetch', fetchMock);
    await ejecutar();
    expect(fetchMock.mock.calls[0]?.[0]).toBe(`/api/v1/centros-trabajo${ruta}`);
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it.each(['POST', 'PUT'] as const)('guarda mediante %s con el cuerpo exacto', async (metodo) => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ id: 7 }));
    vi.stubGlobal('fetch', fetchMock);
    const respuesta = metodo === 'POST' ? await crearCentroTrabajo(request) : await actualizarCentroTrabajo(7, request);
    expect(respuesta).toEqual({ id: 7 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe(`/api/v1/centros-trabajo${metodo === 'PUT' ? '/7' : ''}`);
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: metodo, body: JSON.stringify(request) });
  });

  it('elimina mediante DELETE y acepta 204 sin JSON', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await expect(eliminarCentroTrabajo(7)).resolves.toBeUndefined();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/centros-trabajo/7');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('propaga el mensaje literal de conflicto', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ StatusCode: 409, Message: 'Ya existe un centro de trabajo con el código CT-NEW-01.' }, { status: 409 })));
    await expect(crearCentroTrabajo(request)).rejects.toMatchObject({ statusCode: 409, message: 'Ya existe un centro de trabajo con el código CT-NEW-01.' });
  });
});
