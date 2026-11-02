import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarRol,
  crearRol,
  eliminarRol,
  guardarPermisosRol,
  listarRoles,
  obtenerMatrizPermisos,
  obtenerPermisosRol,
  obtenerRol,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de roles y permisos', () => {
  it('lista roles con filtros exactos', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarRoles({ busqueda: 'Ventas', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/roles?busqueda=Ventas&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite filtros vacíos y undefined', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarRoles({ busqueda: '', pagina: 1, tamanoPagina: 20 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/roles?pagina=1&tamanoPagina=20');
  });

  it('obtiene el rol por id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 3 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerRol(3);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/roles/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea roles con POST y el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 6 }));
    vi.stubGlobal('fetch', fetchMock);
    const request = { codigo: 'AUDITOR', nombre: 'Auditor', descripcion: 'Revisa registros' };
    await crearRol(request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/roles');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(request) });
  });

  it('actualiza el rol con PUT e id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 3 }));
    vi.stubGlobal('fetch', fetchMock);
    const request = { codigo: 'COMPRAS', nombre: 'Compras global' };
    await actualizarRol(3, request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/roles/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(request) });
  });

  it('elimina el rol con DELETE e id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await eliminarRol(6);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/roles/6');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('obtiene los permisos de un rol', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson([]));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerPermisosRol(2);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/roles/2/permisos');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('guarda los permisos con PUT y el conjunto completo', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson([]));
    vi.stubGlobal('fetch', fetchMock);
    const request = {
      permisos: [
        { moduloNumero: 1, puedeLeer: true, puedeEscribir: true },
        { moduloNumero: 21, puedeLeer: true, puedeEscribir: false },
      ],
    };
    await guardarPermisosRol(2, request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/roles/2/permisos');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(request) });
  });

  it('obtiene la matriz de permisos', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ roles: [], modulos: [] }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerMatrizPermisos();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/roles/matriz');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });
});
