import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { vi } from 'vitest';
import { CotizacionesPage } from './CotizacionesPage';

const cotizacion = {
  id: 1,
  numero: 'COT-DEV-001',
  fecha: '2026-01-02T00:00:00Z',
  clienteId: 1,
  clienteNombre: 'Acme Industrial',
  fechaValidez: '2027-01-02T00:00:00Z',
  estado: 'Emitida',
  vencida: false,
  total: 2312.5,
  subtotal: 2500,
  descuento: 187.5,
  observaciones: 'Precios válidos por 30 días.',
  motivoRechazo: null,
  fechaDecision: null,
  ordenCompraId: null,
  ordenCompraNumero: null,
  lineas: [
    {
      productoId: 1,
      productoNombre: 'Laptop Pro',
      cantidad: 1,
      precioUnitario: 2500,
      descuentoPorcentaje: 7.5,
      subtotal: 2500,
      descuento: 187.5,
      importe: 2312.5,
    },
  ],
};

export const cotizacionesDetalleMock = [
  cotizacion,
  {
    ...cotizacion,
    id: 2,
    numero: 'COT-DEV-002',
    clienteId: 2,
    clienteNombre: 'Beta Comercio',
    fechaValidez: '2026-09-30T00:00:00Z',
    vencida: true,
    total: 900,
    subtotal: 900,
    descuento: 0,
    lineas: [
      {
        productoId: 2,
        productoNombre: 'Monitor 27',
        cantidad: 1,
        precioUnitario: 900,
        descuentoPorcentaje: 0,
        subtotal: 900,
        descuento: 0,
        importe: 900,
      },
    ],
  },
  {
    ...cotizacion,
    id: 3,
    numero: 'COT-DEV-003',
    estado: 'Rechazada',
    motivoRechazo: 'El cliente eligió otra alternativa.',
  },
  {
    ...cotizacion,
    id: 4,
    numero: 'COT-DEV-004',
    estado: 'Aceptada',
    total: 900,
    subtotal: 900,
    descuento: 0,
    lineas: [
      {
        productoId: 2,
        productoNombre: 'Monitor 27',
        cantidad: 1,
        precioUnitario: 900,
        descuentoPorcentaje: 0,
        subtotal: 900,
        descuento: 0,
        importe: 900,
      },
    ],
  },
];

const fila = {
  id: 1,
  numero: 'COT-DEV-001',
  fecha: '2026-01-02T00:00:00Z',
  clienteId: 1,
  clienteNombre: 'Acme Industrial',
  fechaValidez: '2027-01-02T00:00:00Z',
  estado: 'Emitida',
  vencida: false,
  total: 2312.5,
  ordenCompraNumero: null,
};

const filas = [
  fila,
  {
    ...fila,
    id: 2,
    numero: 'COT-DEV-002',
    clienteId: 2,
    clienteNombre: 'Beta Comercio',
    fechaValidez: '2026-09-30T00:00:00Z',
    vencida: true,
    total: 900,
  },
  {
    ...fila,
    id: 3,
    numero: 'COT-DEV-003',
    clienteId: 3,
    clienteNombre: 'Gamma Manufactura',
    estado: 'Rechazada',
  },
  {
    ...fila,
    id: 4,
    numero: 'COT-DEV-004',
    estado: 'Aceptada',
    total: 900,
  },
];

export function prepararFetch(
  opciones: { listaVacia?: boolean; errorLista?: boolean } = {},
): ReturnType<typeof vi.fn> {
  let aceptada = false;
  const fetchMock = vi.fn(
    async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes('/cotizaciones/resumen')) {
        return Response.json({
          total: 4,
          emitidas: 2,
          vencidas: 1,
          aceptadas: 1,
          rechazadas: 1,
          montoVigente: 2312.5,
          montoAceptado: 900,
          tasaAceptacion: 50,
          topClientes: [
            {
              clienteId: 2,
              nombre: 'Beta Comercio',
              cotizaciones: 1,
              montoAceptado: 900,
            },
          ],
        });
      }
      if (url.includes('/clientes')) {
        return Response.json({
          items: [
            { id: 1, nombre: 'Acme Industrial' },
            { id: 2, nombre: 'Beta Comercio' },
          ],
        });
      }
      if (url.includes('/productos')) {
        return Response.json({
          items: [
            { id: 1, nombre: 'Laptop Pro', precio: 2500 },
            { id: 2, nombre: 'Monitor 27', precio: 900 },
            { id: 3, nombre: 'Mouse Óptico', precio: 50 },
          ],
        });
      }
      const cotizacionDetalle = cotizacionesDetalleMock.find((item) =>
        url.endsWith(`/cotizaciones/${item.id}`),
      );
      if (cotizacionDetalle) {
        return Response.json(
          aceptada && cotizacionDetalle.id === 1
            ? {
                ...cotizacionDetalle,
                estado: 'Aceptada',
                ordenCompraId: 7,
                ordenCompraNumero: 'OC-COT-000001',
              }
            : cotizacionDetalle,
        );
      }
      if (url.endsWith('/cotizaciones/1/aceptar')) {
        aceptada = true;
        return Response.json({
          ...cotizacionesDetalleMock[0],
          estado: 'Aceptada',
          ordenCompraId: 7,
          ordenCompraNumero: 'OC-COT-000001',
        });
      }
      if (url.endsWith('/cotizaciones/1/rechazar')) {
        return Response.json(
          {
            StatusCode: 409,
            Message: 'La cotización COT-DEV-001 ya está aceptada.',
          },
          { status: 409 },
        );
      }
      if (url === '/api/v1/cotizaciones' && init?.method === 'POST') {
        return Response.json(
          {
            StatusCode: 400,
            Message: 'La fecha de validez no puede ser anterior a hoy.',
          },
          { status: 400 },
        );
      }
      if (url.includes('/cotizaciones?')) {
        if (opciones.errorLista) {
          return Response.json(
            {
              StatusCode: 503,
              Message: 'Servicio temporalmente no disponible.',
            },
            { status: 503 },
          );
        }
        if (opciones.listaVacia) {
          return Response.json({
            items: [],
            total: 0,
            pagina: 1,
            tamanoPagina: 10,
          });
        }
        return Response.json({
          items: filas,
          total: filas.length,
          pagina: 1,
          tamanoPagina: 10,
        });
      }
      if (init?.method === 'DELETE') {
        return new Response(null, { status: 204 });
      }
      return Response.json({ id: 2, numero: 'COT-000002' });
    },
  );
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

export function montar(): void {
  render(
    <MemoryRouter>
      <CotizacionesPage />
    </MemoryRouter>,
  );
}
