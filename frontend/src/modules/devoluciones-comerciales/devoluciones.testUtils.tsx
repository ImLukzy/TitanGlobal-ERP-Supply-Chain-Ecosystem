import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { DevolucionesComercialesPage } from './DevolucionesComercialesPage';
import type { DevolucionDetalle, DevolucionListado } from './tipos';

export const devolucionesMock: DevolucionDetalle[] = [
  {
    id: 1,
    numero: 'DEV-DEV-001',
    fecha: '2026-10-15T08:00:00Z',
    clienteId: 1,
    clienteNombre: 'Acme Industrial',
    ordenCompraId: 1,
    numeroOrden: 'OC-001',
    reclamoNumero: 'REC-DEV-001',
    motivo: 'Defecto',
    estado: 'Recibida',
    unidades: 1,
    total: 2500,
    observaciones: 'Laptop con falla',
    motivoRechazo: null,
    fechaAprobacion: '2026-10-15T09:00:00Z',
    fechaRecepcion: '2026-10-15T10:00:00Z',
    fechaRechazo: null,
    lineas: [
      {
        productoId: 1,
        productoNombre: 'Laptop Pro',
        cantidad: 1,
        precioUnitario: 2500,
        importe: 2500,
      },
    ],
  },
  {
    id: 2,
    numero: 'DEV-DEV-002',
    fecha: '2026-10-14T08:00:00Z',
    clienteId: 1,
    clienteNombre: 'Acme Industrial',
    ordenCompraId: 2,
    numeroOrden: 'OC-002',
    reclamoNumero: null,
    motivo: 'ErrorPedido',
    estado: 'Aprobada',
    unidades: 1,
    total: 900,
    observaciones: 'Monitor equivocado',
    motivoRechazo: null,
    fechaAprobacion: '2026-10-14T09:00:00Z',
    fechaRecepcion: null,
    fechaRechazo: null,
    lineas: [
      {
        productoId: 5,
        productoNombre: 'Monitor 27',
        cantidad: 1,
        precioUnitario: 900,
        importe: 900,
      },
    ],
  },
  {
    id: 3,
    numero: 'DEV-DEV-003',
    fecha: '2026-10-13T08:00:00Z',
    clienteId: 2,
    clienteNombre: 'Beta Comercio',
    ordenCompraId: 5,
    numeroOrden: 'OC-005',
    reclamoNumero: null,
    motivo: 'Arrepentimiento',
    estado: 'Solicitada',
    unidades: 1,
    total: 50,
    observaciones: null,
    motivoRechazo: null,
    fechaAprobacion: null,
    fechaRecepcion: null,
    fechaRechazo: null,
    lineas: [
      {
        productoId: 3,
        productoNombre: 'Mouse Óptico',
        cantidad: 1,
        precioUnitario: 50,
        importe: 50,
      },
    ],
  },
  {
    id: 4,
    numero: 'DEV-DEV-004',
    fecha: '2026-10-12T08:00:00Z',
    clienteId: 4,
    clienteNombre: 'Delta Logística',
    ordenCompraId: 8,
    numeroOrden: 'OC-008',
    reclamoNumero: null,
    motivo: 'Otro',
    estado: 'Rechazada',
    unidades: 1,
    total: 800,
    observaciones: 'Consulta de garantía',
    motivoRechazo: 'Fuera del plazo de devolución',
    fechaAprobacion: null,
    fechaRecepcion: null,
    fechaRechazo: '2026-10-12T09:00:00Z',
    lineas: [
      {
        productoId: 9,
        productoNombre: 'Silla Ejecutiva',
        cantidad: 1,
        precioUnitario: 800,
        importe: 800,
      },
    ],
  },
];

export interface OpcionesApi {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorCrear?: boolean;
  errorAccion?: boolean;
}

export interface LlamadaDevolucion {
  url: string;
  init: RequestInit;
}

function listado(item: DevolucionDetalle): DevolucionListado {
  return {
    id: item.id,
    numero: item.numero,
    fecha: item.fecha,
    clienteId: item.clienteId,
    clienteNombre: item.clienteNombre,
    ordenCompraId: item.ordenCompraId,
    numeroOrden: item.numeroOrden,
    reclamoNumero: item.reclamoNumero,
    motivo: item.motivo,
    estado: item.estado,
    unidades: item.unidades,
    total: item.total,
  };
}

export function prepararApiDevoluciones(opciones: OpcionesApi = {}) {
  const llamadas: LlamadaDevolucion[] = [];
  const datos = devolucionesMock.map((item) => ({ ...item, lineas: [...item.lineas] }));
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });

    if (parsed.pathname.endsWith('/devoluciones-comerciales/resumen')) {
      return Response.json({
        total: 4,
        solicitadas: 1,
        aprobadas: 1,
        recibidas: 1,
        rechazadas: 1,
        montoReembolsado: 2500,
        porMotivo: [
          { motivo: 'Defecto', devoluciones: 1, unidades: 1 },
          { motivo: 'ErrorPedido', devoluciones: 1, unidades: 1 },
          { motivo: 'Arrepentimiento', devoluciones: 1, unidades: 1 },
          { motivo: 'Otro', devoluciones: 1, unidades: 1 },
        ],
        tasaDevolucion: 10,
      });
    }
    if (parsed.pathname.endsWith('/clientes')) {
      return Response.json({
        items: [
          { id: 1, nombre: 'Acme Industrial' },
          { id: 2, nombre: 'Beta Comercio' },
          { id: 3, nombre: 'Gamma Manufactura' },
          { id: 4, nombre: 'Delta Logística' },
        ],
      });
    }
    if (parsed.pathname.endsWith('/reclamos-postventa')) {
      return Response.json({
        items: [
          { id: 1, numero: 'REC-DEV-001', ordenCompraId: 1 },
          { id: 2, numero: 'REC-DEV-002', ordenCompraId: 2 },
        ],
      });
    }
    if (parsed.pathname.endsWith('/devoluciones-comerciales') && metodo === 'GET') {
      if (opciones.errorLista) {
        return Response.json(
          { StatusCode: 500, Message: 'Falló el listado de devoluciones.' },
          { status: 500 },
        );
      }
      const estado = parsed.searchParams.get('estado');
      const motivo = parsed.searchParams.get('motivo');
      const clienteId = parsed.searchParams.get('clienteId');
      const query = parsed.searchParams.get('busqueda')?.toLowerCase() ?? '';
      const items = opciones.listaVacia
        ? []
        : datos.filter((item) => (
          (!estado || item.estado === estado)
          && (!motivo || item.motivo === motivo)
          && (!clienteId || item.clienteId === Number(clienteId))
          && (!query || `${item.numero} ${item.numeroOrden} ${item.clienteNombre}`.toLowerCase().includes(query))
        ));
      return Response.json({
        items: items.map(listado),
        total: items.length,
        pagina: Number(parsed.searchParams.get('pagina') ?? 1),
        tamanoPagina: 10,
      });
    }
    if (parsed.pathname.endsWith('/devoluciones-comerciales') && metodo === 'POST') {
      if (opciones.errorCrear) {
        return Response.json(
          {
            StatusCode: 409,
            Message: 'Solo quedan 2 unidades de Mouse Óptico por devolver en la orden OC-001.',
          },
          { status: 409 },
        );
      }
      return Response.json({ id: 9, numero: 'DEV-000009' });
    }
    if (parsed.pathname.endsWith('/disponible')) {
      const ordenId = Number(parsed.pathname.match(/ordenes\/(\d+)/)?.[1]);
      const disponibles = ordenId === 1
        ? [
          { productoId: 1, productoNombre: 'Laptop Pro', comprado: 1, devuelto: 1, disponible: 0, precioUnitario: 2500 },
          { productoId: 3, productoNombre: 'Mouse Óptico', comprado: 2, devuelto: 0, disponible: 2, precioUnitario: 50 },
        ]
        : [{ productoId: 3, productoNombre: 'Mouse Óptico', comprado: 1, devuelto: 1, disponible: 0, precioUnitario: 50 }];
      return Response.json(disponibles);
    }
    if (/\/ordenes-compra\/clientes\/\d+\/jerarquia$/.test(parsed.pathname)) {
      const clienteId = Number(parsed.pathname.match(/clientes\/(\d+)/)?.[1]);
      const orden = clienteId === 1
        ? { ordenId: 1, numeroOrden: 'OC-001' }
        : { ordenId: 5, numeroOrden: 'OC-005' };
      return Response.json([{
        ...orden,
        fecha: '2026-01-02T00:00:00Z',
        nombreCliente: 'Cliente',
        detalles: [],
      }]);
    }
    const accion = parsed.pathname.match(/devoluciones-comerciales\/(\d+)\/(aprobar|recibir|rechazar)$/);
    if (accion && opciones.errorAccion) {
      return Response.json(
        { StatusCode: 409, Message: 'La devolución DEV-DEV-002 está aprobada.' },
        { status: 409 },
      );
    }
    if (accion) {
      const detalle = datos[Number(accion[1]) - 1];
      if (detalle) {
        if (accion[2] === 'aprobar') {
          detalle.estado = 'Aprobada';
        }
        if (accion[2] === 'recibir') {
          detalle.estado = 'Recibida';
        }
        if (accion[2] === 'rechazar') {
          detalle.estado = 'Rechazada';
        }
        return Response.json(detalle);
      }
    }
    if (metodo === 'PUT') {
      return Response.json({ id: 3, numero: 'DEV-DEV-003' });
    }
    if (metodo === 'DELETE') {
      return new Response(null, { status: 204 });
    }
    const detalle = datos.find((item) => parsed.pathname.endsWith(`/devoluciones-comerciales/${item.id}`));
    if (detalle) {
      return Response.json(detalle);
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

export function renderDevoluciones(initialEntries = ['/modulos/10-devoluciones-comerciales']) {
  return render(
    <MemoryRouter initialEntries={initialEntries}>
      <DevolucionesComercialesPage />
    </MemoryRouter>,
  );
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await screen.findByRole('button', { name: 'DEV-DEV-001' });
  await screen.findByText('Devoluciones por motivo');
  await screen.findByRole('combobox', { name: 'Cliente' });
  await waitFor(() => {
    const listados = fetchMock.mock.calls.filter((call) => (
      String(call[0]).includes('/devoluciones-comerciales?')
    ));
    expect(listados).toHaveLength(1);
  });
}

export function limpiarDevoluciones(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
