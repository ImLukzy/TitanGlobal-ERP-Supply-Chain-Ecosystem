import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarEvaluacion,
  crearEvaluacion,
  eliminarEvaluacion,
  listarEvaluaciones,
  listarProveedoresEvaluables,
  obtenerEvaluacion,
  obtenerResumenEvaluaciones,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function prepararFetch() {
  const fetchMock = vi.fn(async (...args: Parameters<typeof fetch>) => {
    const options = args[1];
    return Response.json({ id: 7, items: [], method: options?.method ?? 'GET' });
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('API de evaluaciones de proveedores', () => {
  it('A2 lista con la query exacta', async () => {
    const fetchMock = prepararFetch();
    await listarEvaluaciones({ proveedorId: 1, periodo: '2026-10', calificacion: 'Excelente', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/evaluaciones-proveedores?proveedorId=1&periodo=2026-10&calificacion=Excelente&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]?.method).toBe('GET');
  });

  it('A2 omite filtros vacíos y undefined; sin filtros no añade signo de interrogación', async () => {
    const fetchMock = prepararFetch();
    await listarEvaluaciones({ proveedorId: '', periodo: '', calificacion: '', pagina: 1, tamanoPagina: undefined });
    await listarEvaluaciones({});
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/evaluaciones-proveedores?pagina=1');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/evaluaciones-proveedores');
  });

  it('A2 consulta el detalle y el resumen por sus rutas', async () => {
    const fetchMock = prepararFetch();
    await obtenerEvaluacion(3);
    await obtenerResumenEvaluaciones();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/evaluaciones-proveedores/3');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/evaluaciones-proveedores/resumen');
  });

  it('A2 crea con POST y el cuerpo exacto', async () => {
    const fetchMock = prepararFetch();
    await crearEvaluacion({ proveedorId: 1, periodo: '2026-10', puntajeCumplimiento: 90, puntajeCalidad: 85, puntajePlazos: 80, observaciones: 'Bien' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/evaluaciones-proveedores');
    expect(fetchMock.mock.calls[0]?.[1]?.method).toBe('POST');
    expect(JSON.parse(String(fetchMock.mock.calls[0]?.[1]?.body))).toEqual({
      proveedorId: 1,
      periodo: '2026-10',
      puntajeCumplimiento: 90,
      puntajeCalidad: 85,
      puntajePlazos: 80,
      observaciones: 'Bien',
    });
  });

  it('A2 actualiza con PUT y elimina con DELETE', async () => {
    const fetchMock = prepararFetch();
    await actualizarEvaluacion(4, { puntajeCumplimiento: 100, puntajeCalidad: 90, puntajePlazos: 80 });
    await eliminarEvaluacion(4);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/evaluaciones-proveedores/4');
    expect(fetchMock.mock.calls[0]?.[1]?.method).toBe('PUT');
    expect(JSON.parse(String(fetchMock.mock.calls[0]?.[1]?.body))).toEqual({ puntajeCumplimiento: 100, puntajeCalidad: 90, puntajePlazos: 80 });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/evaluaciones-proveedores/4');
    expect(fetchMock.mock.calls[1]?.[1]?.method).toBe('DELETE');
  });

  it('A2 el selector reutiliza la lista de proveedores con tamanoPagina=100', async () => {
    const fetchMock = prepararFetch();
    await listarProveedoresEvaluables();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores?tamanoPagina=100');
  });
});
