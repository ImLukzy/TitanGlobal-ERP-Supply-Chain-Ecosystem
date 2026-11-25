import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CategoriasProductoPage } from './CategoriasProductoPage';

describe('CategoriasProductoPage', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('muestra resumen, listado, creación y reactivación de categorías', async () => {
    const categorias = [
      { id: 1, nombre: 'Hardware', descripcion: 'Equipos y periféricos', activa: true },
      { id: 2, nombre: 'Obsoletos', descripcion: 'Línea descontinuada', activa: false },
    ];

    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const metodo = init?.method ?? 'GET';

      if (url.startsWith('/api/v1/categorias-producto/resumen')) {
        return new Response(
          JSON.stringify({ totalCategorias: 2, categoriasActivas: 1, categoriasInactivas: 1 }),
          { status: 200, headers: { 'Content-Type': 'application/json' } },
        );
      }

      if (url === '/api/v1/categorias-producto/1' && metodo === 'GET') {
        return new Response(JSON.stringify(categorias[0]), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        });
      }

      if (url === '/api/v1/categorias-producto/2/reactivar' && metodo === 'POST') {
        categorias[1].activa = true;
        return new Response(null, { status: 204 });
      }

      if (url === '/api/v1/categorias-producto' && metodo === 'POST') {
        categorias.push({ id: 3, nombre: 'Software', descripcion: 'Licencias', activa: true });
        return new Response(JSON.stringify({ id: 3 }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        });
      }

      if (url.startsWith('/api/v1/categorias-producto')) {
        return new Response(JSON.stringify(categorias), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        });
      }

      return new Response(null, { status: 404 });
    });

    vi.stubGlobal('fetch', fetchMock);

    render(
      <MemoryRouter>
        <CategoriasProductoPage />
      </MemoryRouter>,
    );

    expect(await screen.findByRole('heading', { level: 1, name: /22 · Categor/i })).toBeInTheDocument();
    expect(await screen.findByText('Equipos y periféricos')).toBeInTheDocument();
    expect(screen.getByText('Obsoletos')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Reactivar' }));
    await waitFor(() => {
      expect(screen.getByText(/Categoría "Obsoletos" reactivada correctamente\./)).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Nueva categoría' }));
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Software' } });
    fireEvent.change(screen.getByLabelText('Descripción'), { target: { value: 'Licencias' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar categoría' }));

    await waitFor(() => {
      expect(screen.getByText(/Categoría "Software" registrada correctamente\./)).toBeInTheDocument();
    });
  });
});
