import { afterEach, describe, expect, it, vi } from 'vitest';
import { actualizarZona, crearZona, eliminarZona, listarZonas, obtenerResumenZonas, obtenerZona } from './api';
import type { GuardarZonaRequest } from './tipos';

const request: GuardarZonaRequest = {
  almacenId: 1,
  codigo: 'NUEVA-01',
  nombre: 'Zona nueva',
  tipo: 'Almacenamiento',
  temperatura: 'Ambiente',
  capacidadM3: 25,
  activa: true,
};

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('API de zonas de almacenaje', () => {
  it('omite los filtros vacíos y serializa los filtros definidos', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      void input;
      return Response.json({ items: [], total: 0, pagina: 2, tamanoPagina: 10 });
    });
    vi.stubGlobal('fetch', fetchMock);
    await listarZonas({ almacenId: '', busqueda: '', tipo: 'Picking', temperatura: '', activa: false, pagina: 2, tamanoPagina: 10 });
    const url = String(fetchMock.mock.calls[0]?.[0]);
    expect(url).toContain('tipo=Picking');
    expect(url).toContain('activa=false');
    expect(url).toContain('pagina=2');
    expect(url).not.toContain('almacenId=');
    expect(url).not.toContain('busqueda=');
    expect(url).not.toContain('temperatura=');
  });

  it('llama las rutas de resumen, detalle, creación, actualización y baja', async () => {
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      if (init?.method === 'DELETE') {
        return new Response(null, { status: 204 });
      }
      return Response.json({ id: 9 });
    });
    vi.stubGlobal('fetch', fetchMock);
    await obtenerResumenZonas(1);
    await obtenerZona(4);
    await crearZona(request);
    await actualizarZona(4, request);
    await eliminarZona(4);
    expect(String(fetchMock.mock.calls[0]?.[0])).toContain('/zonas-almacenaje/resumen?almacenId=1');
    expect(String(fetchMock.mock.calls[1]?.[0])).toContain('/zonas-almacenaje/4');
    expect(fetchMock.mock.calls[2]?.[1]?.method).toBe('POST');
    expect(fetchMock.mock.calls[3]?.[1]?.method).toBe('PUT');
    expect(String(fetchMock.mock.calls[3]?.[0])).toContain('/zonas-almacenaje/4');
    expect(fetchMock.mock.calls[4]?.[1]?.method).toBe('DELETE');
    expect(String(fetchMock.mock.calls[4]?.[0])).toContain('/zonas-almacenaje/4');
  });
});
