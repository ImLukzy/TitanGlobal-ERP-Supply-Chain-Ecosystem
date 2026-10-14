import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { AppRoutes } from './App';
import { RoleProvider } from './layout/RoleContext';
import { dominios } from './modules/catalogo';
import { MemoryRouter } from 'react-router';

function renderApp(path = '/') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <RoleProvider><AppRoutes /></RoleProvider>
    </MemoryRouter>,
  );
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  window.localStorage.clear();
});

describe('navegación', () => {
  it('muestra los ocho dominios y abre sus diez módulos', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('offline')));
    renderApp();

    dominios.forEach((dominio) => {
      expect(screen.getAllByText(dominio.nombre).length).toBeGreaterThan(0);
    });
    const comprasSummary = screen.getAllByText('Compras y Abastecimiento Global')
      .find((elemento) => elemento.closest('summary') !== null)
      ?.closest('summary') ?? null;
    if (comprasSummary === null || comprasSummary.parentElement === null) {
      throw new Error('No se encontró el resumen del dominio de compras.');
    }
    fireEvent.click(comprasSummary);
    expect(screen.getByText('Proveedores')).toBeInTheDocument();
    expect(screen.getByText('AcuerdosAbastecimiento')).toBeInTheDocument();
    expect(within(comprasSummary.parentElement as HTMLElement).getAllByRole('link')).toHaveLength(10);
  });

  it('muestra la página pendiente para OrdenesCompra y un mensaje para slug inválido', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('offline')));
    const { unmount } = renderApp('/modulos/04-ordenes-compra');

    expect(screen.getByRole('heading', { name: '04 · OrdenesCompra' })).toBeInTheDocument();
    expect(screen.getByText('Pantalla pendiente')).toBeInTheDocument();
    unmount();
    renderApp('/modulos/slug-inexistente');
    expect(screen.getByRole('heading', { name: 'Módulo no encontrado' })).toBeInTheDocument();
  });
});

describe('portada', () => {
  it('informa que la API está en línea cuando salud responde', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 200 })));
    renderApp();

    expect(await screen.findByText('API en línea')).toBeInTheDocument();
  });

  it('muestra API no disponible y el mensaje de error cuando falla salud', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('offline')));
    renderApp();

    expect(await screen.findByText('API no disponible')).toBeInTheDocument();
    expect(screen.getByText('No se pudo conectar con la API.')).toBeInTheDocument();
  });
});
