import { afterEach, describe, expect, it, vi } from 'vitest';
import { configurarUsuarioSolicitud } from '../../api/client';
import { listarAuditoria, listarEntidadesAuditoria, obtenerRegistroAuditoria, obtenerResumenAuditoria } from './api';

const paginaVacia = { items: [], total: 0, pagina: 1, tamanoPagina: 20 };
const resumenVacio = { total: 0, altas: 0, modificaciones: 0, bajas: 0, porEntidad: [], ultimos7Dias: [] };

afterEach(() => {
  configurarUsuarioSolicitud('demo');
  vi.unstubAllGlobals();
});

describe('API de auditoría', () => {
  it('lista con el orden contractual de filtros y paginación', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json(paginaVacia));
    vi.stubGlobal('fetch', fetchMock);
    await listarAuditoria({ entidad: 'Cliente', entidadId: '3', accion: 'Modificacion', usuario: 'demo', fechaDesde: '2026-01-01', fechaHasta: '2026-01-31', pagina: 2, tamanoPagina: 20 });
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/auditoria?entidad=Cliente&entidadId=3&accion=Modificacion&usuario=demo&fechaDesde=2026-01-01&fechaHasta=2026-01-31&pagina=2&tamanoPagina=20');
    expect((fetchMock.mock.calls[0][1]?.headers as Headers).get('X-Usuario')).toBe('demo');
  });

  it('obtiene un registro por id con la ruta exacta', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ id: 42 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerRegistroAuditoria(42);
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/auditoria/42');
  });

  it('obtiene las entidades de auditoría', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json(['Cliente', 'Producto']));
    vi.stubGlobal('fetch', fetchMock);
    await listarEntidadesAuditoria();
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/auditoria/entidades');
  });

  it('obtiene el resumen de auditoría', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json(resumenVacio));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerResumenAuditoria();
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/auditoria/resumen');
  });
});
