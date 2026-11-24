import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  aprobarAusencia,
  cancelarAusencia,
  listarAusencias,
  obtenerAusencia,
  obtenerResumenAusencias,
  obtenerSaldoVacacional,
  rechazarAusencia,
  registrarAusencia,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de ausencias', () => {
  it('lista ausencias con todos los filtros exactos en la query', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarAusencias({
      busqueda: ' ana ',
      tipo: 'Vacaciones',
      estado: 'Pendiente',
      desde: '2026-11-01',
      hasta: '2026-11-30',
      empleadoId: 7,
      pagina: 2,
      tamanoPagina: 10,
    });
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      '/api/v1/ausencias?busqueda=ana&empleadoId=7&tipo=Vacaciones&estado=Pendiente&desde=2026-11-01&hasta=2026-11-30&pagina=2&tamanoPagina=10',
    );
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite los filtros vacíos', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarAusencias({ busqueda: '  ', tipo: '', estado: '', desde: '', hasta: '', empleadoId: null, pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ausencias?pagina=1&tamanoPagina=10');
  });

  it('pide el resumen', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ pendientes: 1 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerResumenAusencias();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ausencias/resumen');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('obtiene el detalle por id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 5 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerAusencia(5);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ausencias/5');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('obtiene el saldo con y sin fecha', async () => {
    const fetchMock = vi.fn().mockImplementation(async () => respuestaJson({ saldo: 6.5 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerSaldoVacacional(3, '2027-01-04');
    await obtenerSaldoVacacional(3, '');
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ausencias/saldo-vacacional/3?fecha=2027-01-04');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/ausencias/saldo-vacacional/3');
  });

  it('registra una ausencia con el cuerpo JSON', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 9, numero: 'AUS-000009' }));
    vi.stubGlobal('fetch', fetchMock);
    const resultado = await registrarAusencia({ empleadoId: 2, tipo: 'PermisoConGoce', fechaInicio: '2027-01-04', fechaFin: '2027-01-05', motivo: 'Trámite' });
    expect(resultado).toEqual({ id: 9, numero: 'AUS-000009' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ausencias');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
    expect(JSON.parse(String((fetchMock.mock.calls[0]?.[1] as RequestInit).body))).toEqual({
      empleadoId: 2,
      tipo: 'PermisoConGoce',
      fechaInicio: '2027-01-04',
      fechaFin: '2027-01-05',
      motivo: 'Trámite',
    });
  });

  it('aprueba y cancela sin cuerpo', async () => {
    const fetchMock = vi.fn().mockImplementation(async () => respuestaJson({ id: 4 }));
    vi.stubGlobal('fetch', fetchMock);
    await aprobarAusencia(4);
    await cancelarAusencia(4);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ausencias/4/aprobacion');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/ausencias/4/cancelacion');
    for (const llamada of fetchMock.mock.calls) {
      expect(llamada[1]).toMatchObject({ method: 'POST' });
      expect((llamada[1] as RequestInit).body).toBeUndefined();
    }
  });

  it('rechaza con el motivo en el cuerpo', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 4 }));
    vi.stubGlobal('fetch', fetchMock);
    await rechazarAusencia(4, { motivo: 'Cierre contable' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ausencias/4/rechazo');
    expect(JSON.parse(String((fetchMock.mock.calls[0]?.[1] as RequestInit).body))).toEqual({ motivo: 'Cierre contable' });
  });
});
