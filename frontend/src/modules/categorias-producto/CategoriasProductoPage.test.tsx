import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CategoriasProductoPage } from './CategoriasProductoPage';
import type { CategoriaProducto, ResumenCategoriasProducto } from './tipos';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

function prepararApi() {
  const categorias: CategoriaProducto[] = [
    {
      id: 1,
      nombre: 'Electrónica',
      descripcion: 'Componentes y dispositivos electrónicos',
      activa: true,
    },
    {
      id: 2,
      nombre: 'Ferretería',
      descripcion: 'Herramientas y fijaciones',
      activa: true,
    },
  ];

  const resumen: ResumenCategoriasProducto = {
    totalCategorias: 3,
    categoriasActivas: 2,
    categoriasInactivas: 1,
  };

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const metodo = init?.method ?? 'GET';

    if (url.endsWith('/api/v1/categorias-producto/resumen') && metodo === 'GET') {
      return respuestaJson(resumen);
    }
    if (url.includes('/api/v1/categorias-producto?') && metodo === 'GET') {
      const params = new URL(url, 'http://localhost').searchParams;
      const buscar = (params.get('buscar') ?? '').toLowerCase();
      const incluir = params.get('incluirInactivas') === 'true';
      const filtradas = categorias.filter(
        (item) => (incluir || item.activa) && item.nombre.toLowerCase().includes(buscar),
      );
      return respuestaJson(filtradas);
    }
    if (url.endsWith('/api/v1/categorias-producto/1') && metodo === 'GET') {
      return respuestaJson(categorias[0]);
    }
    if (url.endsWith('/api/v1/categorias-producto/2') && metodo === 'GET') {
      return respuestaJson(categorias[1]);
    }
    if (url.endsWith('/api/v1/categorias-producto') && metodo === 'POST') {
      return respuestaJson({ StatusCode: 409, Message: 'Ya existe una categoría con el nombre Electrónica.' }, 409);
    }
    if (url.endsWith('/api/v1/categorias-producto/1') && metodo === 'DELETE') {
      categorias[0] = { ...categorias[0]!, activa: false };
      return new Response(null, { status: 204 });
    }
    return respuestaJson({ StatusCode: 404, Message: 'No encontrado' }, 404);
  });

  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock };
}

async function esperarCargaInicialCategorias(fetchMock: ReturnType<typeof vi.fn>) {
  vi.useFakeTimers();
  render(
    <MemoryRouter>
      <CategoriasProductoPage />
    </MemoryRouter>,
  );

  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();

  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'Electrónica' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Electrónica' })).toBeInTheDocument();
    expect(screen.getByText('Total de categorías')).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.filter((call) => String(call[0]).includes('/categorias-producto?')),
    ).toHaveLength(1);
  });
}

describe('CategoriasProductoPage', () => {
  it('aplica la búsqueda con debounce de 300 ms y consulta solo el término final', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialCategorias(fetchMock);

    vi.useFakeTimers();
    fetchMock.mockClear();

    const buscador = screen.getByRole('searchbox', { name: 'Buscar categoría por nombre' });
    fireEvent.change(buscador, { target: { value: 'F' } });
    fireEvent.change(buscador, { target: { value: 'Ferr' } });

    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(fetchMock).not.toHaveBeenCalled();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
      await Promise.resolve();
      await Promise.resolve();
    });

    const llamadasListado = fetchMock.mock.calls.filter((call) =>
      String(call[0]).includes('/categorias-producto?'),
    );
    expect(llamadasListado).toHaveLength(1);
    expect(String(llamadasListado[0]?.[0])).toContain('buscar=Ferr');
  });

  it('valida localmente el formulario y conserva datos ante conflicto 409', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialCategorias(fetchMock);

    fireEvent.click(screen.getByRole('button', { name: 'Nueva categoría' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar categoría' }));

    expect(screen.getByText('El nombre de la categoría es obligatorio.')).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.filter(
        (call) => String(call[0]).endsWith('/categorias-producto') && call[1]?.method === 'POST',
      ),
    ).toHaveLength(0);

    const inputNombre = screen.getByLabelText('Nombre');
    const inputDescripcion = screen.getByLabelText('Descripción');
    fireEvent.change(inputNombre, { target: { value: 'Electrónica' } });
    fireEvent.change(inputDescripcion, { target: { value: 'Duplicada' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar categoría' }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(
        'Ya existe una categoría con el nombre Electrónica.',
      );
    });
    expect(inputNombre).toHaveValue('Electrónica');
    expect(inputDescripcion).toHaveValue('Duplicada');
  });

  it('cierra el diálogo de baja con Escape devolviendo el foco al botón Desactivar', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialCategorias(fetchMock);

    const botonDesactivar = screen.getByRole('button', { name: 'Desactivar' });
    fireEvent.click(botonDesactivar);

    expect(screen.getByRole('dialog', { name: 'Eliminar categoría' })).toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Escape' });

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(
      fetchMock.mock.calls.filter((call) => call[1]?.method === 'DELETE'),
    ).toHaveLength(0);
  });
});
