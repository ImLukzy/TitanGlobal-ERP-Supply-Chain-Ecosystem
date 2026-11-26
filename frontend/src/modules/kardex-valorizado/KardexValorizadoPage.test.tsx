import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { KardexValorizadoPage } from './KardexValorizadoPage';
import type { KardexValorizado, PaginaMovimientos, ResumenKardex, SaldoKardex } from './tipos';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

function prepararApi() {
  const resumen: ResumenKardex = {
    totalMovimientos: 4,
    totalEntradas: 3,
    totalSalidas: 1,
    unidadesEntradas: 150,
    unidadesSalidas: 30,
  };

  const paginaMovimientos: PaginaMovimientos = {
    items: [
      {
        id: 1,
        productoId: 10,
        productoNombre: 'Acero Laminado',
        almacenId: 1,
        almacenCodigo: 'ALM-CENTRAL',
        loteId: null,
        fecha: '2026-11-20T00:00:00Z',
        tipo: 'Entrada',
        cantidad: 50,
        costoUnitario: 120,
        referencia: 'OC-2026-01',
      },
    ],
    total: 1,
    pagina: 1,
    tamanoPagina: 10,
  };

  const kardex: KardexValorizado = {
    productoId: 10,
    productoNombre: 'Acero Laminado',
    almacenId: 1,
    almacenCodigo: 'ALM-CENTRAL',
    metodo: 'PEPS',
    saldoCantidad: 50,
    saldoValor: 6000,
    lineas: [
      {
        movimientoId: 1,
        fecha: '2026-11-20T00:00:00Z',
        tipo: 'Entrada',
        cantidad: 50,
        costoUnitario: 120,
        valorMovimiento: 6000,
        saldoCantidad: 50,
        saldoValor: 6000,
        costoPromedio: 120,
      },
    ],
  };

  const saldos: SaldoKardex[] = [
    {
      productoId: 10,
      productoNombre: 'Acero Laminado',
      almacenId: 1,
      almacenCodigo: 'ALM-CENTRAL',
      entradas: 50,
      salidas: 0,
      saldo: 50,
    },
  ];

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const metodo = init?.method ?? 'GET';

    if (url.endsWith('/api/v1/kardex-valorizado/resumen') && metodo === 'GET') {
      return respuestaJson(resumen);
    }
    if (url.includes('/api/v1/kardex-valorizado/movimientos?') && metodo === 'GET') {
      return respuestaJson(paginaMovimientos);
    }
    if (url.endsWith('/api/v1/kardex-valorizado/movimientos') && metodo === 'POST') {
      return respuestaJson({ StatusCode: 409, Message: 'El almacén ALM-INACTIVO está inactivo.' }, 409);
    }
    if (url.includes('/api/v1/kardex-valorizado/kardex?') && metodo === 'GET') {
      return respuestaJson(kardex);
    }
    if (url.endsWith('/api/v1/kardex-valorizado/saldos') && metodo === 'GET') {
      return respuestaJson(saldos);
    }
    return respuestaJson({ StatusCode: 404, Message: 'No encontrado' }, 404);
  });

  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock };
}

describe('KardexValorizadoPage', () => {
  it('renderiza resumen, movimientos iniciales y valida rango de fechas sin llamar API', async () => {
    const { fetchMock } = prepararApi();
    render(
      <MemoryRouter>
        <KardexValorizadoPage />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(screen.getByText('Acero Laminado')).toBeInTheDocument();
      expect(screen.getByText('Total movimientos')).toBeInTheDocument();
    });

    fireEvent.change(screen.getByLabelText('Fecha desde'), { target: { value: '2026-11-25' } });
    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    fetchMock.mockClear();
    fireEvent.change(screen.getByLabelText('Fecha hasta'), { target: { value: '2026-11-10' } });

    expect(
      screen.getByText('La fecha inicial no puede ser posterior a la final.'),
    ).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('valida localmente nuevo movimiento y conserva valores ante error 409', async () => {
    prepararApi();
    render(
      <MemoryRouter>
        <KardexValorizadoPage />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(screen.getByText('Acero Laminado')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Registrar movimiento' }));
    fireEvent.click(screen.getAllByRole('button', { name: 'Registrar movimiento' })[0]!);

    expect(screen.getByText('El producto es obligatorio.')).toBeInTheDocument();

    const inputProducto = screen.getByLabelText('ID de producto');
    const inputAlmacen = screen.getByLabelText('ID de almacén');
    const inputCosto = screen.getByLabelText('Costo unitario (S/)');
    fireEvent.change(inputProducto, { target: { value: '10' } });
    fireEvent.change(inputAlmacen, { target: { value: '99' } });
    fireEvent.change(inputCosto, { target: { value: '50' } });
    fireEvent.click(screen.getAllByRole('button', { name: 'Registrar movimiento' })[0]!);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(
        'El almacén ALM-INACTIVO está inactivo.',
      );
    });
    expect(inputAlmacen).toHaveValue(99);
  });

  it('calcula hoja de kárdex PEPS y consulta saldos consolidados', async () => {
    prepararApi();
    render(
      <MemoryRouter>
        <KardexValorizadoPage />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(screen.getByText('Acero Laminado')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('tab', { name: 'Kárdex valorizado (PEPS / Promedio)' }));
    fireEvent.change(screen.getByLabelText('ID de producto'), { target: { value: '10' } });
    fireEvent.change(screen.getByLabelText('ID de almacén'), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText('Método de valuación'), { target: { value: 'PEPS' } });
    fireEvent.click(screen.getByRole('button', { name: 'Calcular kárdex' }));

    await waitFor(() => {
      expect(screen.getByText(/Saldo valorizado: S\/ 6000\.00/)).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('tab', { name: 'Saldos consolidados' }));
    await waitFor(() => {
      expect(screen.getByText('Saldos acumulados por almacén')).toBeInTheDocument();
    });
  });
});
