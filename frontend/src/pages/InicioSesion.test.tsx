import { afterEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { getSesion, limpiarSesion } from '../api/sesionStore';
import { SesionProvider } from '../layout/SesionContext';
import { InicioSesion } from './InicioSesion';

afterEach(() => {
  cleanup();
  limpiarSesion();
  vi.unstubAllGlobals();
});

function renderLogin(): void {
  render(
    <SesionProvider>
      <InicioSesion />
    </SesionProvider>,
  );
}

function escribir(usuario: string, contrasena: string): void {
  fireEvent.change(screen.getByLabelText('Usuario'), { target: { value: usuario } });
  fireEvent.change(screen.getByLabelText('Contraseña'), { target: { value: contrasena } });
}

describe('pantalla de inicio de sesión', () => {
  it('muestra el aviso de demostración', () => {
    renderLogin();
    expect(screen.getByText('Usuario de demostración: invitado · Contraseña: Demo2026!')).toBeInTheDocument();
  });

  it('con campos vacíos muestra el mensaje y no llama a la API', () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    renderLogin();
    fireEvent.click(screen.getByRole('button', { name: 'Iniciar sesión' }));
    expect(screen.getByRole('alert')).toHaveTextContent('El usuario y la contraseña son obligatorios.');
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('con credenciales correctas guarda la sesión', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({
      token: 'tok',
      expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
      usuario: { id: 2, nombreUsuario: 'demo', nombreCompleto: 'Usuario de demostración' },
      roles: [],
    }));
    vi.stubGlobal('fetch', fetchMock);
    renderLogin();
    escribir('demo', 'Demo2026!');
    fireEvent.click(screen.getByRole('button', { name: 'Iniciar sesión' }));

    await waitFor(() => expect(getSesion()?.token).toBe('tok'));
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/autenticacion/login');
    expect(fetchMock.mock.calls[0][1]?.body).toBe('{"nombreUsuario":"demo","contrasena":"Demo2026!"}');
  });

  it('muestra el mensaje de la API si las credenciales fallan', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ StatusCode: 401, Message: 'Usuario o contraseña incorrectos.' }), { status: 401 }),
    ));
    renderLogin();
    escribir('demo', 'mala');
    fireEvent.click(screen.getByRole('button', { name: 'Iniciar sesión' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Usuario o contraseña incorrectos.');
    expect(getSesion()).toBeNull();
    expect(screen.getByRole('button', { name: 'Iniciar sesión' })).toBeEnabled();
  });
});
