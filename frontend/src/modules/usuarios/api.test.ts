import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarUsuario,
  crearUsuario,
  eliminarUsuario,
  listarRolesUsuarios,
  listarUsuarios,
  obtenerPermisosUsuario,
  obtenerResumenUsuarios,
  obtenerUsuario,
} from './api';
import type { GuardarUsuarioRequest } from './tipos';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

const cuerpoUsuario: GuardarUsuarioRequest = {
  nombreUsuario: 'ana.nueva',
  nombreCompleto: 'Ana Nueva',
  email: 'ana.nueva@titanglobal.example',
  activo: true,
  rolIds: [2, 5],
};

describe('API de usuarios', () => {
  it('lista usuarios con filtros exactos en la query', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarUsuarios({ busqueda: 'ana', rolId: 2, activo: false, pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/usuarios?busqueda=ana&rolId=2&activo=false&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite los filtros vacíos y undefined al listar', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarUsuarios({ busqueda: '', rolId: '', activo: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/usuarios?pagina=1&tamanoPagina=10');
  });

  it('obtiene el usuario por id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 4 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerUsuario(4);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/usuarios/4');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea usuarios con POST y el cuerpo exacto incluido rolIds', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 9 }));
    vi.stubGlobal('fetch', fetchMock);
    await crearUsuario(cuerpoUsuario);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/usuarios');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpoUsuario) });
  });

  it('actualiza el usuario con PUT, su id y el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 4 }));
    vi.stubGlobal('fetch', fetchMock);
    await actualizarUsuario(4, cuerpoUsuario);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/usuarios/4');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(cuerpoUsuario) });
  });

  it('elimina el usuario con DELETE e id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await expect(eliminarUsuario(5)).resolves.toBeUndefined();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/usuarios/5');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('lee los permisos efectivos del usuario', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson([]));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerPermisosUsuario(4);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/usuarios/4/permisos');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('lee el resumen de usuarios', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ total: 5 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerResumenUsuarios();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/usuarios/resumen');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('lee los roles del módulo 72 con tamanoPagina=100 y los reduce a id, código y nombre', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({
      items: [{ id: 2, codigo: 'VENTAS', nombre: 'Ventas', esSistema: false, modulosConLectura: 11, modulosConEscritura: 10 }],
      total: 1,
      pagina: 1,
      tamanoPagina: 100,
    }));
    vi.stubGlobal('fetch', fetchMock);
    const roles = await listarRolesUsuarios();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/roles?tamanoPagina=100');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
    expect(roles).toEqual([{ id: 2, codigo: 'VENTAS', nombre: 'Ventas' }]);
  });

  it('propaga el mensaje literal del backend cuando responde 409', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ StatusCode: 409, Message: 'Ya existe el usuario ana.ventas.' }, 409));
    vi.stubGlobal('fetch', fetchMock);
    await expect(crearUsuario(cuerpoUsuario)).rejects.toMatchObject({
      statusCode: 409,
      message: 'Ya existe el usuario ana.ventas.',
    });
  });
});
