import { afterEach, describe, expect, it, vi } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { AppLayout } from './AppLayout';
import { RoleProvider } from './RoleContext';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderCabecera(): void {
  render(
    <MemoryRouter>
      <RoleProvider>
        <Routes>
          <Route element={<AppLayout />}>
            <Route index element={<h1>Contenido disponible</h1>} />
          </Route>
        </Routes>
      </RoleProvider>
    </MemoryRouter>,
  );
}

describe('indicador de alertas en la cabecera', () => {
  it('A5 carga una vez el resumen y enlaza las alertas altas', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ altas: 3 }));
    vi.stubGlobal('fetch', fetchMock);
    renderCabecera();
    const indicador = await screen.findByRole('link', { name: '3 alertas altas' });
    expect(indicador).toHaveAttribute('href', '/modulos/78-notificaciones-alertas?severidad=alta');
    expect(fetchMock.mock.calls.filter((llamada) => String(llamada[0]) === '/api/v1/alertas/resumen')).toHaveLength(1);
  });

  it('A5 muestra el acceso sin cifra cuando no hay alertas altas', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ altas: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    renderCabecera();
    expect(await screen.findByRole('link', { name: 'Alertas, sin alertas altas' })).toHaveTextContent('Alertas');
    expect(screen.queryByText(/\d+ alertas altas/)).not.toBeInTheDocument();
  });

  it('A5 actualiza el indicador cuando se anuncia un refresco del resumen', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json({ altas: 1 }))
      .mockResolvedValue(Response.json({ altas: 2 }));
    vi.stubGlobal('fetch', fetchMock);
    renderCabecera();
    await screen.findByRole('link', { name: '1 alerta alta' });
    window.dispatchEvent(new Event('alertas-resumen-refrescar'));
    expect(await screen.findByRole('link', { name: '2 alertas altas' })).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((llamada) => String(llamada[0]) === '/api/v1/alertas/resumen')).toHaveLength(2);
  });

  it('A5 conserva operativa la cabecera si falla el resumen', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('offline')));
    renderCabecera();
    expect(await screen.findByRole('heading', { name: 'Contenido disponible' })).toBeInTheDocument();
    expect(screen.getByRole('combobox', { name: 'Rol de prueba' })).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByRole('link', { name: /alerta/ })).not.toBeInTheDocument());
  });
});
