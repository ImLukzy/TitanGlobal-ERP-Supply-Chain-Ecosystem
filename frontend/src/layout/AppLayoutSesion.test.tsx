import { afterEach, describe, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { getSesion, limpiarSesion, setSesion } from '../api/sesionStore';
import { AppLayout } from './AppLayout';
import { RoleProvider } from './RoleContext';
import { SesionProvider } from './SesionContext';

afterEach(() => {
  cleanup();
  limpiarSesion();
  vi.unstubAllGlobals();
});

function renderCabecera(conSesionProvider: boolean): void {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ altas: 0 })));
  const contenido = (
    <MemoryRouter>
      <RoleProvider>
        <Routes>
          <Route element={<AppLayout />}>
            <Route index element={<h1>Contenido</h1>} />
          </Route>
        </Routes>
      </RoleProvider>
    </MemoryRouter>
  );
  render(conSesionProvider ? <SesionProvider>{contenido}</SesionProvider> : contenido);
}

describe('barra superior con sesión', () => {
  it('muestra el nombre y cierra la sesión', async () => {
    setSesion({
      token: 'tok',
      expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
      usuario: { id: 1, nombreUsuario: 'demo', nombreCompleto: 'Usuario de demostración' },
      roles: [],
    });
    renderCabecera(true);
    expect(await screen.findByText('Usuario de demostración')).toBeInTheDocument();
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }));
    });
    expect(getSesion()).toBeNull();
    expect(screen.queryByRole('button', { name: 'Cerrar sesión' })).not.toBeInTheDocument();
  });

  it('sin proveedor de sesión no muestra el botón', async () => {
    renderCabecera(false);
    expect(await screen.findByRole('heading', { name: 'Contenido' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Cerrar sesión' })).not.toBeInTheDocument();
  });
});
