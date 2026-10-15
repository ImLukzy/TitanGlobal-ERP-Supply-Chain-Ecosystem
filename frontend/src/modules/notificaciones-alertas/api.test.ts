import { afterEach, describe, expect, it, vi } from 'vitest';
import { descartarAlerta, listarAlertas, obtenerResumenAlertas, restaurarAlerta } from './api';
import type { FiltrosAlerta } from './tipos';

afterEach(() => vi.unstubAllGlobals());

describe('API de notificaciones y alertas', () => {
  it('A2 lista con ruta y query exactas', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 20 }));
    vi.stubGlobal('fetch', fetchMock);
    const filtros: FiltrosAlerta = { tipo: 'ReclamoVencido', severidad: 'alta', incluirDescartadas: true, pagina: 2, tamanoPagina: 20 };
    await listarAlertas(filtros);
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/alertas?pagina=2&tamanoPagina=20&tipo=ReclamoVencido&severidad=alta&incluirDescartadas=true', expect.objectContaining({ method: 'GET' }));
  });

  it('A2 obtiene el resumen con GET', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ total: 0, altas: 0, medias: 0, bajas: 0, descartadas: 0, porTipo: [] }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerResumenAlertas();
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/alertas/resumen', expect.objectContaining({ method: 'GET' }));
  });

  it('A2 descarta por POST con la clave exacta', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await descartarAlerta('reclamo-vencido:3');
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/alertas/descartar', expect.objectContaining({ method: 'POST', body: '{"clave":"reclamo-vencido:3"}' }));
  });

  it('A2 restaura por POST con la clave exacta', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await restaurarAlerta('reclamo-vencido:8');
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/alertas/restaurar', expect.objectContaining({ method: 'POST', body: '{"clave":"reclamo-vencido:8"}' }));
  });
});
