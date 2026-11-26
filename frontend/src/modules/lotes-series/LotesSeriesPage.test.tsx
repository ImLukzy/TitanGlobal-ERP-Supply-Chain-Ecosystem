import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { LotesSeriesPage } from './LotesSeriesPage';
import type { LoteDetalle, PaginaLotes, PaginaSeries, ResumenLotes } from './tipos';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

function prepararApi() {
  const resumen: ResumenLotes = {
    totalLotes: 2,
    disponibles: 1,
    enCuarentena: 1,
    bloqueados: 0,
    vencidos: 0,
    porVencer: 1,
  };

  const paginaLotes: PaginaLotes = {
    items: [
      {
        id: 1,
        productoId: 101,
        productoNombre: 'Taladro Industrial',
        codigo: 'LOT-2026-A1',
        fechaVencimiento: '2026-12-31T00:00:00Z',
        cantidad: 25,
        estado: 'Disponible',
        vencido: false,
      },
    ],
    total: 1,
    pagina: 1,
    tamanoPagina: 10,
  };

  const detalleLote: LoteDetalle = {
    id: 1,
    productoId: 101,
    productoNombre: 'Taladro Industrial',
    codigo: 'LOT-2026-A1',
    fechaFabricacion: '2026-01-15T00:00:00Z',
    fechaVencimiento: '2026-12-31T00:00:00Z',
    cantidad: 25,
    estado: 'Disponible',
    vencido: false,
    totalSeries: 2,
  };

  const paginaSeries: PaginaSeries = {
    items: [
      {
        id: 11,
        productoId: 101,
        loteId: 1,
        numeroSerie: 'SN-TAL-001',
        estado: 'Disponible',
      },
    ],
    total: 1,
    pagina: 1,
    tamanoPagina: 10,
  };

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const metodo = init?.method ?? 'GET';

    if (url.endsWith('/api/v1/lotes-series/lotes/resumen') && metodo === 'GET') {
      return respuestaJson(resumen);
    }
    if (url.includes('/api/v1/lotes-series/lotes?') && metodo === 'GET') {
      return respuestaJson(paginaLotes);
    }
    if (url.endsWith('/api/v1/lotes-series/lotes/1') && metodo === 'GET') {
      return respuestaJson(detalleLote);
    }
    if (url.endsWith('/api/v1/lotes-series/lotes') && metodo === 'POST') {
      return respuestaJson(
        { StatusCode: 409, Message: 'Ya existe el lote LOT-2026-A1 para el producto 101.' },
        409,
      );
    }
    if (url.includes('/api/v1/lotes-series/series?') && metodo === 'GET') {
      return respuestaJson(paginaSeries);
    }
    if (url.endsWith('/api/v1/lotes-series/series/11/estado') && metodo === 'PUT') {
      paginaSeries.items[0] = { ...paginaSeries.items[0]!, estado: 'Asignada' };
      return respuestaJson({ id: 11 });
    }
    return respuestaJson({ StatusCode: 404, Message: 'No encontrado' }, 404);
  });

  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock };
}

async function esperarCargaInicialLotes(fetchMock: ReturnType<typeof vi.fn>) {
  vi.useFakeTimers();
  render(
    <MemoryRouter>
      <LotesSeriesPage />
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
    expect(screen.getByRole('button', { name: 'LOT-2026-A1' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'LOT-2026-A1' })).toBeInTheDocument();
    expect(screen.getByText('Total de lotes')).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.filter((call) => String(call[0]).includes('/lotes-series/lotes?')),
    ).toHaveLength(1);
  });
}

describe('LotesSeriesPage', () => {
  it('aplica debounce de 300 ms en búsqueda de lotes y consulta solo el último código', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialLotes(fetchMock);

    vi.useFakeTimers();
    fetchMock.mockClear();

    const buscador = screen.getByRole('searchbox', { name: 'Buscar lote por código' });
    fireEvent.change(buscador, { target: { value: 'LOT' } });
    fireEvent.change(buscador, { target: { value: 'LOT-2026' } });

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
      String(call[0]).includes('/lotes-series/lotes?'),
    );
    expect(llamadas).toHaveLength(1);
    expect(String(llamadas[0]?.[0])).toContain('busqueda=LOT-2026');
  });

  it('valida localmente nuevo lote y conserva campos ante conflicto 409', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialLotes(fetchMock);

    fireEvent.click(screen.getByRole('button', { name: 'Nuevo lote' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar lote' }));

    expect(screen.getByText('El producto es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El código del lote es obligatorio.')).toBeInTheDocument();

    const inputProducto = screen.getByLabelText('ID de producto');
    const inputCodigo = screen.getByLabelText('Código de lote');
    fireEvent.change(inputProducto, { target: { value: '101' } });
    fireEvent.change(inputCodigo, { target: { value: 'LOT-2026-A1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar lote' }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(
        'Ya existe el lote LOT-2026-A1 para el producto 101.',
      );
    });
    expect(inputCodigo).toHaveValue('LOT-2026-A1');
  });

  it('cambia a la pestaña de Números de serie y actualiza el estado de una serie', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialLotes(fetchMock);

    fireEvent.click(screen.getByRole('tab', { name: 'Números de serie' }));

    await waitFor(() => {
      expect(screen.getByText('SN-TAL-001')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Asignar' }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Liberar' })).toBeInTheDocument();
    });
  });
});
