import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { Analitica } from './Analitica';

function jsonResponse(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), { status });
}

function obtenerSeccion(titulo: string): HTMLElement {
  const section = screen.getByRole('heading', { name: titulo }).closest('section');
  if (section === null) {
    throw new Error(`No se encontró la sección ${titulo}.`);
  }
  return section;
}

function stubAnaliticas(respuestas: Record<string, Response>): void {
  vi.stubGlobal('fetch', vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    return Promise.resolve(respuestas[url] ?? new Response('[]', { status: 200 }));
  }));
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe('analítica de OrdenesCompra', () => {
  it('carga los patrones 5, 8 y 9 al abrir y presenta jerarquía con subtotales', async () => {
    const ordenes = [{
      ordenId: 1,
      numeroOrden: 'OC-001',
      fecha: '2026-01-02T00:00:00Z',
      nombreCliente: 'Acme Industrial',
      detalles: [{ productoId: 1, nombreProducto: 'Laptop Pro', cantidad: 1, precioUnitario: 2500 }],
    }];
    const respuestas: Record<string, Response> = {
      '/api/v1/ordenes-compra/productos/mas-caro': jsonResponse({ id: 1, nombre: 'Laptop Pro', precio: 2500 }),
      '/api/v1/ordenes-compra/productos/sin-descripcion': jsonResponse([{ id: 3, nombre: 'Mouse', precio: 80 }]),
      '/api/v1/ordenes-compra/clientes/frecuencia': jsonResponse([{ clienteId: 1, totalOrdenes: 4 }, { clienteId: 2, totalOrdenes: 2 }]),
      '/api/v1/ordenes-compra/clientes/1/jerarquia': jsonResponse(ordenes),
    };
    stubAnaliticas(respuestas);
    render(<Analitica />);

    expect(await within(obtenerSeccion('Producto más caro')).findByText('Laptop Pro')).toBeInTheDocument();
    expect(await within(obtenerSeccion('Productos sin descripción')).findByText('Mouse')).toBeInTheDocument();
    expect(await within(obtenerSeccion('Frecuencia de clientes')).findByText('4')).toBeInTheDocument();
    const seccionJerarquia = obtenerSeccion('Jerarquía de órdenes');
    fireEvent.click(within(seccionJerarquia).getByRole('button', { name: 'Consultar' }));

    const ordenTitulo = await within(seccionJerarquia).findByText('OC-001');
    const ordenDetails = ordenTitulo.closest('details');
    expect(ordenDetails).toHaveAttribute('open');
    expect(within(seccionJerarquia).getByText(/Acme Industrial/)).toBeInTheDocument();
    expect(within(seccionJerarquia).getByText('Laptop Pro')).toBeInTheDocument();
    expect(seccionJerarquia.textContent).toContain('2,500.00');
  });

  it('trata el producto más caro vacío como Sin resultados', async () => {
    stubAnaliticas({ '/api/v1/ordenes-compra/productos/mas-caro': new Response(null, { status: 200 }) });
    render(<Analitica />);

    expect(await within(obtenerSeccion('Producto más caro')).findByText('Sin resultados')).toBeInTheDocument();
  });

  it('muestra el error 500 de una tarjeta y conserva los resultados de las demás', async () => {
    stubAnaliticas({
      '/api/v1/ordenes-compra/productos/mas-caro': jsonResponse({ StatusCode: 500, Message: 'Falla de consulta' }, 500),
      '/api/v1/ordenes-compra/productos/sin-descripcion': jsonResponse([{ id: 5, nombre: 'Teclado', precio: 140 }]),
    });
    render(<Analitica />);

    expect(await within(obtenerSeccion('Producto más caro')).findByText('Falla de consulta')).toBeInTheDocument();
    expect(await within(obtenerSeccion('Productos sin descripción')).findByText('Teclado')).toBeInTheDocument();
  });
});
