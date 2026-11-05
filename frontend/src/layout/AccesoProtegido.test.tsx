import { afterEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { limpiarSesion, setSesion } from '../api/sesionStore';
import { AccesoProtegido } from './AccesoProtegido';
import { SesionProvider } from './SesionContext';

afterEach(() => {
  cleanup();
  limpiarSesion();
});

function renderProtegido(): void {
  render(
    <SesionProvider>
      <AccesoProtegido>
        <p>Contenido privado</p>
      </AccesoProtegido>
    </SesionProvider>,
  );
}

describe('AccesoProtegido', () => {
  it('sin sesión muestra el inicio de sesión', () => {
    renderProtegido();
    expect(screen.getByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
    expect(screen.queryByText('Contenido privado')).not.toBeInTheDocument();
  });

  it('con sesión muestra los hijos', () => {
    setSesion({
      token: 'tok',
      expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
      usuario: { id: 1, nombreUsuario: 'demo', nombreCompleto: 'Demo' },
      roles: [],
    });
    renderProtegido();
    expect(screen.getByText('Contenido privado')).toBeInTheDocument();
  });
});
