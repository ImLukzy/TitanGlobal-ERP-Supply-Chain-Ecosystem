import { afterEach, describe, expect, it, vi } from 'vitest';
import { apiGet, apiPost } from './client';
import { setSelectedRole } from './roleStore';
import { getSesion, limpiarSesion, setSesion } from './sesionStore';

afterEach(() => {
  limpiarSesion();
  setSelectedRole(null);
  vi.unstubAllGlobals();
});

function guardarSesionDePrueba(): void {
  setSesion({
    token: 'abc.def.ghi',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    usuario: { id: 7, nombreUsuario: 'maria', nombreCompleto: 'María Pérez' },
    roles: [],
  });
}

describe('cliente HTTP con sesión', () => {
  it('envía Authorization y X-Usuario de la sesión', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);
    guardarSesionDePrueba();

    await apiGet('/clientes');
    const cabeceras = fetchMock.mock.calls[0][1]?.headers as Headers;
    expect(cabeceras.get('Authorization')).toBe('Bearer abc.def.ghi');
    expect(cabeceras.get('X-Usuario')).toBe('maria');
  });

  it('sin sesión no envía Authorization y mantiene X-Usuario por defecto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    await apiGet('/clientes');
    const cabeceras = fetchMock.mock.calls[0][1]?.headers as Headers;
    expect(cabeceras.has('Authorization')).toBe(false);
    expect(cabeceras.get('X-Usuario')).toBe('demo');
  });

  it('el login no envía el rol simulado, para poder entrar aunque se eligiera Bloqueado', async () => {
    const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(new Response('{}', { status: 200 })));
    vi.stubGlobal('fetch', fetchMock);
    setSelectedRole('Bloqueado');

    await apiPost('/autenticacion/login', { nombreUsuario: 'admin', contrasena: 'x' });
    await apiGet('/clientes');
    const cabecerasLogin = fetchMock.mock.calls[0][1]?.headers as Headers;
    const cabecerasClientes = fetchMock.mock.calls[1][1]?.headers as Headers;
    expect(cabecerasLogin.has('X-User-Role')).toBe(false);
    expect(cabecerasClientes.get('X-User-Role')).toBe('Bloqueado');
  });

  it('un 401 fuera del login borra la sesión y lanza ApiError', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ StatusCode: 401, Message: 'Debe iniciar sesión para acceder a este recurso.' }), { status: 401 }),
    ));
    guardarSesionDePrueba();

    await expect(apiGet('/clientes')).rejects.toMatchObject({ statusCode: 401, message: 'Debe iniciar sesión para acceder a este recurso.' });
    expect(getSesion()).toBeNull();
  });

  it('un 401 del login no borra la sesión', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ StatusCode: 401, Message: 'Usuario o contraseña incorrectos.' }), { status: 401 }),
    ));
    guardarSesionDePrueba();

    await expect(apiPost('/autenticacion/login', { nombreUsuario: 'x', contrasena: 'y' })).rejects.toMatchObject({ statusCode: 401 });
    expect(getSesion()).not.toBeNull();
  });
});
