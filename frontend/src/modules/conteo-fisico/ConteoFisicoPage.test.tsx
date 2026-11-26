import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ConteoFisicoPage } from './ConteoFisicoPage';
import type {
  PaginaTomasInventario,
  ResumenTomasInventario,
  TomaInventarioDetalle,
} from './tipos';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

function prepararApi() {
  const resumen: ResumenTomasInventario = {
    totalTomas: 1,
    abiertas: 1,
    cerradas: 0,
    anuladas: 0,
    lineasConDiferencia: 1,
  };

  const pagina: PaginaTomasInventario = {
    items: [
      {
        id: 1,
        codigo: 'TF-2026-01',
        almacenId: 1,
        almacenCodigo: 'ALM-CENTRAL',
        almacenNombre: 'Almacén Central',
        fechaToma: '2026-11-24T00:00:00Z',
        estado: 'Abierta',
        responsable: 'Luis Torres',
        cantidadProductos: 1,
        lineasConDiferencia: 1,
      },
    ],
    total: 1,
    pagina: 1,
    tamanoPagina: 10,
  };

  let detalleActual: TomaInventarioDetalle = {
    id: 1,
    codigo: 'TF-2026-01',
    almacenId: 1,
    almacenCodigo: 'ALM-CENTRAL',
    almacenNombre: 'Almacén Central',
    fechaToma: '2026-11-24T00:00:00Z',
    estado: 'Abierta',
    responsable: 'Luis Torres',
    observaciones: 'Conteo general de cierre',
    lineas: [
      {
        id: 10,
        productoId: 3,
        productoNombre: 'Motor Eléctrico 2HP',
        stockSistema: 15,
        cantidadContada: 14,
        diferencia: -1,
        observacion: null,
      },
    ],
  };

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const metodo = init?.method ?? 'GET';

    if (url.endsWith('/api/v1/tomas-inventario/resumen') && metodo === 'GET') {
      return respuestaJson(resumen);
    }
    if (url.includes('/api/v1/tomas-inventario?') && metodo === 'GET') {
      return respuestaJson(pagina);
    }
    if (url.endsWith('/api/v1/tomas-inventario/1') && metodo === 'GET') {
      return respuestaJson(detalleActual);
    }
    if (url.endsWith('/api/v1/tomas-inventario') && metodo === 'POST') {
      return respuestaJson(
        { StatusCode: 409, Message: 'Ya existe una toma de inventario con el código TF-2026-01.' },
        409,
      );
    }
    if (url.endsWith('/api/v1/tomas-inventario/1/cerrar') && metodo === 'POST') {
      detalleActual = { ...detalleActual, estado: 'Cerrada' };
      pagina.items[0] = { ...pagina.items[0]!, estado: 'Cerrada' };
      return respuestaJson(detalleActual);
    }
    return respuestaJson({ StatusCode: 404, Message: 'No encontrado' }, 404);
  });

  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock };
}

async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>) {
  render(
    <MemoryRouter>
      <ConteoFisicoPage />
    </MemoryRouter>,
  );

  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'TF-2026-01' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'TF-2026-01' })).toBeInTheDocument();
    expect(screen.getByText('Motor Eléctrico 2HP')).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.filter((call) => String(call[0]).includes('/tomas-inventario?')),
    ).toHaveLength(1);
  });
}

describe('ConteoFisicoPage', () => {
  it('aplica debounce de 300 ms en búsqueda y consulta solo el último término', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicial(fetchMock);

    vi.useFakeTimers();
    fetchMock.mockClear();

    const buscador = screen.getByRole('searchbox', { name: 'Buscar toma de inventario' });
    fireEvent.change(buscador, { target: { value: 'TF' } });
    fireEvent.change(buscador, { target: { value: 'TF-2026' } });

    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(fetchMock).not.toHaveBeenCalled();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
      await Promise.resolve();
      await Promise.resolve();
    });

    const llamadas = fetchMock.mock.calls.filter((call) =>
      String(call[0]).includes('/tomas-inventario?'),
    );
    expect(llamadas).toHaveLength(1);
    expect(String(llamadas[0]?.[0])).toContain('busqueda=TF-2026');
  });

  it('valida localmente nueva toma y conserva los datos ante conflicto 409', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicial(fetchMock);

    fireEvent.click(screen.getByRole('button', { name: 'Nueva toma' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar toma' }));

    expect(
      screen.getByText('El código de la toma de inventario es obligatorio.'),
    ).toBeInTheDocument();

    const inputCodigo = screen.getByLabelText('Código de toma');
    const inputAlmacen = screen.getByLabelText('ID de almacén');
    const inputResponsable = screen.getByLabelText('Responsable');
    const inputProducto = screen.getByLabelText('Producto ID #1');
    const inputCantidad = screen.getByLabelText('Cantidad contada #1');

    fireEvent.change(inputCodigo, { target: { value: 'TF-2026-01' } });
    fireEvent.change(inputAlmacen, { target: { value: '1' } });
    fireEvent.change(inputResponsable, { target: { value: 'Luis Torres' } });
    fireEvent.change(inputProducto, { target: { value: '3' } });
    fireEvent.change(inputCantidad, { target: { value: '14' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar toma' }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(
        'Ya existe una toma de inventario con el código TF-2026-01.',
      );
    });
    expect(inputCodigo).toHaveValue('TF-2026-01');
  });

  it('cierra una toma abierta y oculta los botones de edición/cierre', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicial(fetchMock);

    fireEvent.click(screen.getByRole('button', { name: 'Cerrar toma' }));

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: 'Cerrar toma' })).not.toBeInTheDocument();
      expect(screen.getAllByText('Cerrada').length).toBeGreaterThan(0);
    });
  });
});
