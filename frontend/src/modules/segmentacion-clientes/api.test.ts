import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarSegmento,
  crearSegmento,
  eliminarSegmento,
  listarClasificacion,
  listarClientesSegmento,
  listarSegmentos,
  obtenerResumenSegmentacion,
  obtenerSegmento,
} from './api';
import type { GuardarSegmentoRequest } from './tipos';

const request: GuardarSegmentoRequest = {
  codigo: 'NUEVO',
  nombre: 'Nuevo segmento',
  descripcion: 'Descripción',
  prioridad: 5,
  montoMinimo: 100,
  ordenesMinimas: 1,
  activo: true,
};

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('API de segmentación de clientes', () => {
  it('construye las queries de segmentos y clasificación omitiendo vacíos', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      void input;
      return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
    });
    vi.stubGlobal('fetch', fetchMock);
    await listarSegmentos({ busqueda: '', activo: false, pagina: 1, tamanoPagina: 10 });
    await listarClasificacion({ segmentoId: '', sinSegmento: true, busqueda: '', pagina: 1, tamanoPagina: 10 });
    expect(String(fetchMock.mock.calls[0]?.[0])).toContain('/segmentos-clientes?activo=false&pagina=1&tamanoPagina=10');
    expect(String(fetchMock.mock.calls[0]?.[0])).not.toContain('busqueda=');
    expect(String(fetchMock.mock.calls[1]?.[0])).toContain('/segmentos-clientes/clasificacion?sinSegmento=true&pagina=1&tamanoPagina=10');
    expect(String(fetchMock.mock.calls[1]?.[0])).not.toContain('segmentoId=');
  });

  it('llama las ocho operaciones con rutas, métodos y cuerpos exactos', async () => {
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      if (init?.method === 'DELETE') {
        return new Response(null, { status: 204 });
      }
      return Response.json({ id: 9, items: [], total: 0, pagina: 1, tamanoPagina: 10 });
    });
    vi.stubGlobal('fetch', fetchMock);
    await listarSegmentos({ pagina: 1, tamanoPagina: 10 });
    await obtenerSegmento(3);
    await crearSegmento(request);
    await actualizarSegmento(3, request);
    await eliminarSegmento(3);
    await listarClientesSegmento(3, 2, 10);
    await listarClasificacion({ segmentoId: 2, busqueda: 'Beta', pagina: 1, tamanoPagina: 10 });
    await obtenerResumenSegmentacion();
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe('/api/v1/segmentos-clientes?pagina=1&tamanoPagina=10');
    expect(String(fetchMock.mock.calls[1]?.[0])).toBe('/api/v1/segmentos-clientes/3');
    expect(fetchMock.mock.calls[2]?.[1]?.method).toBe('POST');
    expect(String(fetchMock.mock.calls[2]?.[1]?.body)).toBe(JSON.stringify(request));
    expect(fetchMock.mock.calls[3]?.[1]?.method).toBe('PUT');
    expect(String(fetchMock.mock.calls[3]?.[0])).toBe('/api/v1/segmentos-clientes/3');
    expect(fetchMock.mock.calls[4]?.[1]?.method).toBe('DELETE');
    expect(String(fetchMock.mock.calls[4]?.[0])).toBe('/api/v1/segmentos-clientes/3');
    expect(String(fetchMock.mock.calls[5]?.[0])).toBe('/api/v1/segmentos-clientes/3/clientes?pagina=2&tamanoPagina=10');
    expect(String(fetchMock.mock.calls[6]?.[0])).toBe('/api/v1/segmentos-clientes/clasificacion?segmentoId=2&busqueda=Beta&pagina=1&tamanoPagina=10');
    expect(String(fetchMock.mock.calls[7]?.[0])).toBe('/api/v1/segmentos-clientes/resumen');
  });
});
