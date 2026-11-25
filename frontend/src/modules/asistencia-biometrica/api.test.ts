import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  aprobarHorasExtra,
  calcularJornadas,
  eliminarMarcacion,
  importarMarcaciones,
  listarJornadas,
  listarMarcaciones,
  obtenerResumenAsistencia,
  rechazarHorasExtra,
  registrarMarcacion,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function simularFetch(contenido: unknown = { items: [], total: 0 }, status = 200) {
  const fetchMock = vi.fn().mockImplementation(async () => (status === 204 ? new Response(null, { status }) : Response.json(contenido, { status })));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('API de asistencia biométrica', () => {
  it('lista marcaciones con todos los filtros exactos en la query', async () => {
    const fetchMock = simularFetch();
    await listarMarcaciones({ empleadoId: 2, desde: '2026-11-02', hasta: '2026-11-06', tipo: 'Entrada', origen: 'Reloj', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/marcaciones?empleadoId=2&desde=2026-11-02&hasta=2026-11-06&tipo=Entrada&origen=Reloj&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite los filtros vacíos de marcaciones', async () => {
    const fetchMock = simularFetch();
    await listarMarcaciones({ empleadoId: null, desde: '', hasta: '', tipo: '', origen: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/marcaciones?pagina=1&tamanoPagina=10');
  });

  it('registra una marcación con POST y el cuerpo exacto', async () => {
    const fetchMock = simularFetch({ id: 9 });
    const cuerpo = { empleadoId: 1, tipo: 'Entrada' as const, instante: '2026-11-03T13:00:00.000Z', observacion: 'Olvidó marcar' };
    await expect(registrarMarcacion(cuerpo)).resolves.toEqual({ id: 9 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/marcaciones');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpo) });
  });

  it('importa el archivo del reloj con POST a la importación', async () => {
    const fetchMock = simularFetch({ recibidos: 1, importados: 1, yaImportados: 0, duplicados: 0, rechazados: [] });
    const cuerpo = { dispositivo: 'RELOJ-01', registros: [{ numeroRegistro: 'R-1', legajo: 'EMP-000001', tipo: 'Entrada', instante: '2026-11-03T13:00:00Z' }] };
    await importarMarcaciones(cuerpo);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/marcaciones/importacion');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpo) });
  });

  it('elimina una marcación con DELETE', async () => {
    const fetchMock = simularFetch(null, 204);
    await eliminarMarcacion(3);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/marcaciones/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('lista jornadas con todos los filtros exactos en la query', async () => {
    const fetchMock = simularFetch();
    await listarJornadas({ empleadoId: 1, desde: '2026-11-02', hasta: '2026-11-06', estado: 'SinMarcacion', estadoHorasExtra: 'Pendiente', pagina: 3, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/jornadas-asistencia?empleadoId=1&desde=2026-11-02&hasta=2026-11-06&estado=SinMarcacion&estadoHorasExtra=Pendiente&pagina=3&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite los filtros vacíos de jornadas', async () => {
    const fetchMock = simularFetch();
    await listarJornadas({ empleadoId: null, desde: '', hasta: '', estado: '', estadoHorasExtra: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/jornadas-asistencia?pagina=1&tamanoPagina=10');
  });

  it('pide el resumen del rango', async () => {
    const fetchMock = simularFetch({ jornadas: 0 });
    await obtenerResumenAsistencia('2026-11-02', '2026-11-06');
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/jornadas-asistencia/resumen?desde=2026-11-02&hasta=2026-11-06');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('calcula jornadas con POST y el empleado opcional', async () => {
    const fetchMock = simularFetch({ calculadas: 1, conservadas: 0, eliminadas: 0 });
    await calcularJornadas({ desde: '2026-11-02', hasta: '2026-11-06', empleadoId: 2 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/jornadas-asistencia/calculo');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ desde: '2026-11-02', hasta: '2026-11-06', empleadoId: 2 }) });
  });

  it('aprueba las horas extra con POST sin cuerpo', async () => {
    const fetchMock = simularFetch({ id: 2 });
    await aprobarHorasExtra(2);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/jornadas-asistencia/2/horas-extra/aprobacion');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
    expect(fetchMock.mock.calls[0]?.[1]).not.toHaveProperty('body');
  });

  it('rechaza las horas extra con POST y el motivo', async () => {
    const fetchMock = simularFetch({ id: 2 });
    await rechazarHorasExtra(2, { motivo: 'No autorizadas' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/jornadas-asistencia/2/horas-extra/rechazo');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ motivo: 'No autorizadas' }) });
  });
});
