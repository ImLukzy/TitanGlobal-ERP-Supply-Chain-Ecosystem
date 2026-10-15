import { act, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import type { ReactNode } from 'react';
import { expect, vi } from 'vitest';
import { SolicitudesCompraPage } from './SolicitudesCompraPage';
import type { PaginaSolicitudes, ResumenSolicitudes, SolicitudDetalle, SolicitudListado } from './tipos';

export interface OpcionesMock {
  listaVacia?: boolean;
  paginado?: boolean;
  errorLista?: boolean;
  errorCrear?: boolean;
  errorDecidir?: boolean;
  errorBaja?: boolean;
}

export interface LlamadaApi {
  url: string;
  init: RequestInit;
}

export const solicitudesBase: SolicitudDetalle[] = [
  {
    id: 1,
    numero: 'SC-000001',
    fecha: '2026-01-02T00:00:00Z',
    solicitante: 'Ana Torres',
    proveedorId: 1,
    proveedorNombre: 'Andes Suministros S.A.C.',
    estado: 'Pendiente',
    totalLineas: 2,
    montoEstimado: 3400,
    observaciones: 'Entrega para la planta principal',
    motivoRechazo: null,
    fechaDecision: null,
    lineas: [
      { productoId: 1, productoNombre: 'Laptop Pro', cantidad: 1, precioEstimado: 2500, subtotal: 2500 },
      { productoId: 2, productoNombre: 'Monitor 27', cantidad: 1, precioEstimado: 900, subtotal: 900 },
    ],
  },
  {
    id: 2,
    numero: 'SC-000002',
    fecha: '2026-01-04T00:00:00Z',
    solicitante: 'Luis Paredes',
    proveedorId: null,
    proveedorNombre: null,
    estado: 'Pendiente',
    totalLineas: 1,
    montoEstimado: 250,
    observaciones: null,
    motivoRechazo: null,
    fechaDecision: null,
    lineas: [{ productoId: 3, productoNombre: 'Mouse Óptico', cantidad: 5, precioEstimado: 50, subtotal: 250 }],
  },
  {
    id: 3,
    numero: 'SC-000003',
    fecha: '2026-01-06T00:00:00Z',
    solicitante: 'Rosa Salazar',
    proveedorId: 2,
    proveedorNombre: 'Cobre Austral SpA',
    estado: 'Aprobada',
    totalLineas: 1,
    montoEstimado: 1800,
    observaciones: null,
    motivoRechazo: null,
    fechaDecision: '2026-01-07T00:00:00Z',
    lineas: [{ productoId: 2, productoNombre: 'Monitor 27', cantidad: 2, precioEstimado: 900, subtotal: 1800 }],
  },
  {
    id: 4,
    numero: 'SC-000004',
    fecha: '2026-01-08T00:00:00Z',
    solicitante: 'Pablo Núñez',
    proveedorId: 1,
    proveedorNombre: 'Andes Suministros S.A.C.',
    estado: 'Aprobada',
    totalLineas: 1,
    montoEstimado: 500,
    observaciones: null,
    motivoRechazo: null,
    fechaDecision: '2026-01-09T00:00:00Z',
    lineas: [{ productoId: 3, productoNombre: 'Mouse Óptico', cantidad: 10, precioEstimado: 50, subtotal: 500 }],
  },
  {
    id: 5,
    numero: 'SC-000005',
    fecha: '2026-01-10T00:00:00Z',
    solicitante: 'Elena Cruz',
    proveedorId: 3,
    proveedorNombre: 'Ningbo Components Ltd.',
    estado: 'Rechazada',
    totalLineas: 1,
    montoEstimado: 300,
    observaciones: 'Requiere revisión',
    motivoRechazo: 'El presupuesto del trimestre ya fue asignado.',
    fechaDecision: '2026-01-11T00:00:00Z',
    lineas: [{ productoId: 3, productoNombre: 'Mouse Óptico', cantidad: 6, precioEstimado: 50, subtotal: 300 }],
  },
];

const resumenBase: ResumenSolicitudes = {
  total: 5,
  pendientes: 2,
  aprobadas: 2,
  rechazadas: 1,
  montoPendiente: 3650,
  montoAprobado: 2300,
  topProductosSolicitados: [
    { productoId: 3, nombre: 'Mouse Óptico', unidades: 21 },
    { productoId: 2, nombre: 'Monitor 27', unidades: 3 },
    { productoId: 1, nombre: 'Laptop Pro', unidades: 1 },
  ],
};

export function prepararApi(opciones: OpcionesMock = {}) {
  const solicitudes = structuredClone(solicitudesBase);
  const llamadas: LlamadaApi[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });
    if (url.endsWith('/solicitudes-compra/resumen')) {
      const pendientes = solicitudes.filter((item) => item.estado === 'Pendiente');
      const aprobadas = solicitudes.filter((item) => item.estado === 'Aprobada');
      const rechazadas = solicitudes.filter((item) => item.estado === 'Rechazada');
      return Response.json({
        ...resumenBase,
        total: solicitudes.length,
        pendientes: pendientes.length,
        aprobadas: aprobadas.length,
        rechazadas: rechazadas.length,
        montoPendiente: pendientes.reduce((suma, item) => suma + item.montoEstimado, 0),
        montoAprobado: aprobadas.reduce((suma, item) => suma + item.montoEstimado, 0),
      });
    }
    if (url.endsWith('/proveedores?activo=true&tamanoPagina=100')) {
      return Response.json({ items: [
        { id: 1, razonSocial: 'Andes Suministros S.A.C.' },
        { id: 2, razonSocial: 'Cobre Austral SpA' },
        { id: 3, razonSocial: 'Ningbo Components Ltd.' },
      ] });
    }
    if (url.endsWith('/productos?tamanoPagina=100')) {
      return Response.json({ items: [
        { id: 1, nombre: 'Laptop Pro', precio: 2500 },
        { id: 2, nombre: 'Monitor 27', precio: 900 },
        { id: 3, nombre: 'Mouse Óptico', precio: 50 },
      ] });
    }
    if (url.includes('/solicitudes-compra?') && metodo === 'GET') {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'No se pudo cargar la lista de solicitudes.' }, { status: 500 });
      }
      const pagina: PaginaSolicitudes = {
        items: opciones.listaVacia ? [] : [...solicitudes].reverse().map((item): SolicitudListado => ({
          id: item.id,
          numero: item.numero,
          fecha: item.fecha,
          solicitante: item.solicitante,
          proveedorId: item.proveedorId,
          proveedorNombre: item.proveedorNombre,
          estado: item.estado,
          totalLineas: item.totalLineas,
          montoEstimado: item.montoEstimado,
        })),
        total: opciones.listaVacia ? 0 : opciones.paginado ? 15 : solicitudes.length,
        pagina: 1,
        tamanoPagina: 10,
      };
      return Response.json(pagina);
    }
    if (url.endsWith('/solicitudes-compra') && metodo === 'POST') {
      if (opciones.errorCrear) {
        return Response.json({ StatusCode: 400, Message: 'El solicitante no puede superar 100 caracteres.' }, { status: 400 });
      }
      const request = JSON.parse(String(init.body)) as {
        solicitante: string;
        proveedorId?: number;
        observaciones?: string;
        lineas: Array<{ productoId: number; cantidad: number; precioEstimado: number }>;
      };
      const productos = [
        { id: 1, nombre: 'Laptop Pro' },
        { id: 2, nombre: 'Monitor 27' },
        { id: 3, nombre: 'Mouse Óptico' },
      ];
      const proveedor = [
        { id: 1, nombre: 'Andes Suministros S.A.C.' },
        { id: 2, nombre: 'Cobre Austral SpA' },
        { id: 3, nombre: 'Ningbo Components Ltd.' },
      ].find((item) => item.id === request.proveedorId);
      const nueva: SolicitudDetalle = {
        id: 6,
        numero: 'SC-000006',
        fecha: '2026-01-12T00:00:00Z',
        solicitante: request.solicitante,
        proveedorId: request.proveedorId ?? null,
        proveedorNombre: proveedor?.nombre ?? null,
        estado: 'Pendiente',
        totalLineas: request.lineas.length,
        montoEstimado: request.lineas.reduce((suma, linea) => suma + linea.cantidad * linea.precioEstimado, 0),
        observaciones: request.observaciones ?? null,
        motivoRechazo: null,
        fechaDecision: null,
        lineas: request.lineas.map((linea) => ({
          ...linea,
          productoNombre: productos.find((item) => item.id === linea.productoId)?.nombre ?? 'Producto',
          subtotal: linea.cantidad * linea.precioEstimado,
        })),
      };
      solicitudes.push(nueva);
      return Response.json({ id: 6, numero: 'SC-000006' });
    }
    if (url.endsWith('/solicitudes-compra/1/aprobar') && metodo === 'POST') {
      if (opciones.errorDecidir) {
        return Response.json({ StatusCode: 409, Message: 'La solicitud SC-000001 ya está aprobada.' }, { status: 409 });
      }
      const actual = solicitudes.find((item) => item.id === 1);
      if (actual) {
        actual.estado = 'Aprobada';
        actual.fechaDecision = '2026-01-12T00:00:00Z';
      }
      return Response.json(solicitudes[0]);
    }
    if (url.endsWith('/solicitudes-compra/1/rechazar') && metodo === 'POST') {
      const actual = solicitudes.find((item) => item.id === 1);
      if (actual) {
        actual.estado = 'Rechazada';
        actual.motivoRechazo = JSON.parse(String(init.body))?.motivo ?? null;
        actual.fechaDecision = '2026-01-12T00:00:00Z';
      }
      return Response.json(actual);
    }
    if (url.endsWith('/solicitudes-compra/1') && metodo === 'DELETE') {
      if (opciones.errorBaja) {
        return Response.json({ StatusCode: 409, Message: 'La solicitud SC-000001 ya está aprobada.' }, { status: 409 });
      }
      return new Response(null, { status: 204 });
    }
    if (url.endsWith('/solicitudes-compra/1') && metodo === 'PUT') {
      return Response.json({ id: 1, numero: 'SC-000001' });
    }
    const id = Number(url.slice(url.lastIndexOf('/') + 1));
    const solicitud = solicitudes.find((item) => item.id === id);
    if (solicitud) {
      return Response.json(solicitud);
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

export function renderPagina(ui: ReactNode = <SolicitudesCompraPage />) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>, llamadas: LlamadaApi[] = []): Promise<void> {
  await screen.findByRole('button', { name: 'SC-000005' });
  await screen.findByRole('option', { name: 'Andes Suministros S.A.C.' });
  await waitFor(() => {
    expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/productos?tamanoPagina=100'))).toBe(true);
  });
  vi.useFakeTimers();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
  await waitFor(() => {
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/solicitudes-compra?'))).toHaveLength(2);
  });
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
  });
  fetchMock.mockClear();
  llamadas.splice(0, llamadas.length);
}
