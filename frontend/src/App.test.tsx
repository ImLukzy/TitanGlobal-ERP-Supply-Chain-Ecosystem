import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { getSelectedRole, setSelectedRole } from './api/roleStore';
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
  setSelectedRole(null);
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
    expect(within(comprasSummary.parentElement).getAllByRole('link')).toHaveLength(10);
  });

  it('registra OrdenesCompra y conserva pendientes los demás módulos', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/productos/mas-caro')) {
        return Promise.resolve(new Response(JSON.stringify({ id: 1, nombre: 'Laptop Pro', precio: 2500 }), { status: 200 }));
      }
      if (url.endsWith('/productos/sin-descripcion')) {
        return Promise.resolve(new Response('[]', { status: 200 }));
      }
      if (url.endsWith('/clientes/frecuencia')) {
        return Promise.resolve(new Response('[]', { status: 200 }));
      }
      return Promise.resolve(new Response('{}', { status: 200 }));
    }));
    const { unmount } = renderApp('/modulos/04-ordenes-compra');

    expect(screen.getByRole('heading', { name: '04 · OrdenesCompra' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Nueva orden' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Analítica' })).toBeInTheDocument();
    unmount();
    renderApp('/modulos/01-clientes');
    expect(screen.getByText('Pantalla pendiente')).toBeInTheDocument();
  });

  it('muestra un mensaje de ruta inexistente para un slug que no figura en el catálogo', () => {
    renderApp('/modulos/slug-inexistente');
    expect(screen.getByRole('heading', { name: 'Módulo no encontrado' })).toBeInTheDocument();
  });

  it('Escape cierra el menú y devuelve el foco a su botón', () => {
    renderApp('/modulos/01-clientes');
    const menuButton = screen.getByRole('button', { name: 'Abrir navegación' });
    fireEvent.click(menuButton);
    expect(menuButton).toHaveAttribute('aria-expanded', 'true');

    fireEvent.keyDown(document, { key: 'Escape' });

    expect(menuButton).toHaveAttribute('aria-expanded', 'false');
    expect(document.activeElement).toBe(menuButton);
  });

  it('ignora roles no permitidos en el selector', () => {
    renderApp('/modulos/01-clientes');
    const selector = screen.getByRole('combobox', { name: 'Rol de prueba' });
    fireEvent.change(selector, { target: { value: 'Invitado' } });

    expect(getSelectedRole()).toBeNull();
    expect(selector).toHaveValue('');
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
