import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  buscarCentros,
  cancelarMantenimiento,
  completarMantenimiento,
  crearMantenimiento,
  editarMantenimiento,
  iniciarMantenimiento,
  listarMantenimientos,
  obtenerMantenimiento,
  obtenerResumenMantenimientos,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function prepararFetch(contenido: unknown = { id: 7 }) {
  const fetchMock = vi.fn<(input: RequestInfo | URL, init?: RequestInit) => Promise<Response>>(() =>
    Promise.resolve(Response.json(contenido)),
  );
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function llamada(fetchMock: ReturnType<typeof prepararFetch>, indice = 0): { url: string; init: RequestInit } {
  const [url, init] = fetchMock.mock.calls[indice] as [string, RequestInit];
  return { url, init };
}

describe('API de mantenimiento preventivo', () => {
  it('lista sin enviar los filtros vacíos ni los indefinidos', async () => {
    const fetchMock = prepararFetch({ items: [], total: 0, pagina: 1, tamanoPagina: 20 });

    await listarMantenimientos({
      busqueda: '',
      centroTrabajoId: undefined,
      estado: '',
      desde: '',
      hasta: undefined,
      pagina: 1,
      tamanoPagina: 20,
    });

    expect(llamada(fetchMock).url).toBe('/api/v1/mantenimientos-preventivos?pagina=1&tamanoPagina=20');
    expect(llamada(fetchMock).init.method).toBe('GET');
  });

  it('lista con todos los filtros combinados', async () => {
    const fetchMock = prepararFetch({ items: [], total: 0, pagina: 2, tamanoPagina: 20 });

    await listarMantenimientos({
      busqueda: 'MP-0000',
      centroTrabajoId: 4,
      estado: 'EnEjecucion',
      desde: '2026-12-01',
      hasta: '2026-12-31',
      pagina: 2,
      tamanoPagina: 20,
    });

    expect(llamada(fetchMock).url).toBe(
      '/api/v1/mantenimientos-preventivos?busqueda=MP-0000&centroTrabajoId=4&estado=EnEjecucion&desde=2026-12-01&hasta=2026-12-31&pagina=2&tamanoPagina=20',
    );
  });

  it('consulta el resumen y el detalle con GET', async () => {
    const fetchMock = prepararFetch({});

    await obtenerResumenMantenimientos();
    await obtenerMantenimiento(7);

    expect(fetchMock.mock.calls.map((call) => [call[0], (call[1] as RequestInit).method])).toEqual([
      ['/api/v1/mantenimientos-preventivos/resumen', 'GET'],
      ['/api/v1/mantenimientos-preventivos/7', 'GET'],
    ]);
  });

  it('crea con POST con todos los campos y edita con PUT sin los opcionales', async () => {
    const fetchMock = prepararFetch({ id: 7, numero: 'MP-000007' });
    const crear = {
      centroTrabajoId: 4,
      descripcion: 'Cambio de rodamientos',
      fechaProgramada: '2026-12-01',
      duracionHorasPlanificada: 2.5,
      frecuenciaDias: 30,
      responsable: 'Luis Rojas',
    };
    const editar = { descripcion: 'Cambio de rodamientos', fechaProgramada: '2026-12-02', duracionHorasPlanificada: 3 };

    await expect(crearMantenimiento(crear)).resolves.toEqual({ id: 7, numero: 'MP-000007' });
    await editarMantenimiento(7, editar);

    expect(llamada(fetchMock, 0).url).toBe('/api/v1/mantenimientos-preventivos');
    expect(llamada(fetchMock, 0).init).toMatchObject({ method: 'POST', body: JSON.stringify(crear) });
    expect(llamada(fetchMock, 1).url).toBe('/api/v1/mantenimientos-preventivos/7');
    expect(llamada(fetchMock, 1).init).toMatchObject({ method: 'PUT', body: JSON.stringify(editar) });
  });

  it('inicia sin cuerpo, completa con las horas reales y cancela con el motivo', async () => {
    const fetchMock = prepararFetch();

    await iniciarMantenimiento(7);
    await completarMantenimiento(7, { horasReales: 3.25 });
    await cancelarMantenimiento(8, { motivo: 'Línea detenida por inventario' });

    expect(llamada(fetchMock, 0).url).toBe('/api/v1/mantenimientos-preventivos/7/iniciar');
    expect(llamada(fetchMock, 0).init.method).toBe('POST');
    expect(llamada(fetchMock, 0).init.body).toBeUndefined();
    expect(llamada(fetchMock, 1).url).toBe('/api/v1/mantenimientos-preventivos/7/completar');
    expect(llamada(fetchMock, 1).init).toMatchObject({ method: 'POST', body: JSON.stringify({ horasReales: 3.25 }) });
    expect(llamada(fetchMock, 2).url).toBe('/api/v1/mantenimientos-preventivos/8/cancelar');
    expect(llamada(fetchMock, 2).init).toMatchObject({
      method: 'POST',
      body: JSON.stringify({ motivo: 'Línea detenida por inventario' }),
    });
  });

  it('busca centros activos para el formulario y todos para el filtro', async () => {
    const centro = { id: 4, codigo: 'CT-LIM-L01', nombre: 'Línea de ensamble', tipo: 'Linea', planta: 'Lima', capacidadHorasDia: 16, activo: true };
    const fetchMock = prepararFetch({ items: [centro], total: 1, pagina: 1, tamanoPagina: 20 });

    await expect(buscarCentros('lim', true)).resolves.toEqual({
      items: [{ id: 4, codigo: 'CT-LIM-L01', nombre: 'Línea de ensamble' }],
      total: 1,
      tamanoPagina: 20,
    });
    await buscarCentros('', false);

    expect(llamada(fetchMock, 0).url).toBe('/api/v1/centros-trabajo?busqueda=lim&activo=true&pagina=1&tamanoPagina=20');
    expect(llamada(fetchMock, 1).url).toBe('/api/v1/centros-trabajo?pagina=1&tamanoPagina=20');
  });
});
