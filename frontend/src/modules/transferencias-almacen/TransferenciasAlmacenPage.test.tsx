import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { TransferenciasAlmacenPage } from './TransferenciasAlmacenPage';
import type {
  PaginaTransferencias,
  ResumenTransferencias,
  TransferenciaDetalle,
} from './tipos';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

function prepararApi() {
  const resumen: ResumenTransferencias = {
    totalTransferencias: 2,
    unidadesTransferidas: 40,
    valorTransferido: 2000,
  };

  const pagina: PaginaTransferencias = {
    items: [
      {
        id: 1,
        productoId: 5,
        productoNombre: 'Válvula Hidráulica',
        almacenOrigenId: 1,
        almacenOrigenCodigo: 'ALM-NORTE',
        almacenDestinoId: 2,
        almacenDestinoCodigo: 'ALM-SUR',
        cantidad: 20,
        costoUnitario: 50,
        valorTotal: 1000,
        fecha: '2026-11-22T00:00:00Z',
      },
    ],
    total: 1,
    pagina: 1,
    tamanoPagina: 10,
  };

  const detalle: TransferenciaDetalle = {
    ...pagina.items[0]!,
    observaciones: 'Reposición mensual',
    movimientoSalidaId: 91,
    movimientoEntradaId: 92,
  };

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const metodo = init?.method ?? 'GET';

    if (url.endsWith('/api/v1/transferencias-almacen/resumen') && metodo === 'GET') {
      return respuestaJson(resumen);
    }
    if (url.includes('/api/v1/transferencias-almacen?') && metodo === 'GET') {
      return respuestaJson(pagina);
    }
    if (url.endsWith('/api/v1/transferencias-almacen/1') && metodo === 'GET') {
      return respuestaJson(detalle);
    }
    if (url.endsWith('/api/v1/transferencias-almacen') && metodo === 'POST') {
      return respuestaJson(
        { StatusCode: 409, Message: 'Stock insuficiente en el almacén de origen.' },
        409,
      );
    }
    return respuestaJson({ StatusCode: 404, Message: 'No encontrado' }, 404);
  });

  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock };
}

describe('TransferenciasAlmacenPage', () => {
  it('renderiza resumen, tabla de transferencias y ficha lateral con movimientos de kárdex', async () => {
    prepararApi();
    render(
      <MemoryRouter>
        <TransferenciasAlmacenPage />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(screen.getByRole('button', { name: '#1' })).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: 'Transferencia #1' })).toBeInTheDocument();
      expect(screen.getByText('Salida #91 · Entrada #92')).toBeInTheDocument();
      expect(screen.getByText('Reposición mensual')).toBeInTheDocument();
    });
  });

  it('valida almacenes distintos localmente y conserva valores ante error 409 del servidor', async () => {
    prepararApi();
    render(
      <MemoryRouter>
        <TransferenciasAlmacenPage />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(screen.getByRole('button', { name: '#1' })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Nueva transferencia' }));

    const inputProducto = screen.getByLabelText('ID de producto');
    const inputOrigen = screen.getByLabelText('ID de almacén origen');
    const inputDestino = screen.getByLabelText('ID de almacén destino');
    const inputCantidad = screen.getByLabelText('Cantidad');

    fireEvent.change(inputProducto, { target: { value: '5' } });
    fireEvent.change(inputOrigen, { target: { value: '1' } });
    fireEvent.change(inputDestino, { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar transferencia' }));

    expect(
      screen.getByText('El almacén de origen y el de destino deben ser distintos.'),
    ).toBeInTheDocument();

    fireEvent.change(inputDestino, { target: { value: '2' } });
    fireEvent.change(inputCantidad, { target: { value: '500' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar transferencia' }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(
        'Stock insuficiente en el almacén de origen.',
      );
    });
    expect(inputCantidad).toHaveValue(500);
  });
});
