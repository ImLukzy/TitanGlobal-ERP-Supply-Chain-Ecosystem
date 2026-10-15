import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router';
import { vi } from 'vitest';
import type { OrdenAbastecimientoDetalle, ResumenOrdenesAbastecimiento } from './tipos';

export const proveedores = [
  { id: 1, razonSocial: 'Andes Suministros S.A.C.' },
  { id: 2, razonSocial: 'Cobre Austral SpA' },
];

export const productos = [
  { id: 1, nombre: 'Laptop Pro', precio: 2500 },
  { id: 2, nombre: 'Monitor 27', precio: 900 },
  { id: 3, nombre: 'Mouse Óptico', precio: 50 },
];

export const ordenes: OrdenAbastecimientoDetalle[] = [
  {
    id: 1,
    numero: 'OA-000001',
    fecha: '2026-10-10T00:00:00Z',
    proveedorId: 1,
    proveedorNombre: 'Andes Suministros S.A.C.',
    solicitudCompraId: 3,
    solicitudNumero: 'SC-000003',
    estado: 'Emitida',
    fechaEntregaEstimada: '2026-10-25T00:00:00Z',
    fechaRecepcion: null,
    atrasada: false,
    observaciones: 'Entrega para la planta principal',
    motivoAnulacion: null,
    montoTotal: 3400,
    lineas: [
      { productoId: 1, productoNombre: 'Laptop Pro', cantidad: 1, precioUnitario: 2500, subtotal: 2500 },
      { productoId: 2, productoNombre: 'Monitor 27', cantidad: 1, precioUnitario: 900, subtotal: 900 },
    ],
  },
  {
    id: 2,
    numero: 'OA-000002',
    fecha: '2026-10-08T00:00:00Z',
    proveedorId: 2,
    proveedorNombre: 'Cobre Austral SpA',
    solicitudCompraId: null,
    solicitudNumero: null,
    estado: 'Emitida',
    fechaEntregaEstimada: '2026-10-10T00:00:00Z',
    fechaRecepcion: null,
    atrasada: true,
    observaciones: null,
    motivoAnulacion: null,
    montoTotal: 1000,
    lineas: [
      { productoId: 2, productoNombre: 'Monitor 27', cantidad: 1, precioUnitario: 900, subtotal: 900 },
      { productoId: 3, productoNombre: 'Mouse Óptico', cantidad: 2, precioUnitario: 50, subtotal: 100 },
    ],
  },
  {
    id: 3,
    numero: 'OA-000003',
    fecha: '2026-10-06T00:00:00Z',
    proveedorId: 1,
    proveedorNombre: 'Andes Suministros S.A.C.',
    solicitudCompraId: null,
    solicitudNumero: null,
    estado: 'Recibida',
    fechaEntregaEstimada: '2026-10-08T00:00:00Z',
    fechaRecepcion: '2026-10-09T00:00:00Z',
    atrasada: false,
    observaciones: null,
    motivoAnulacion: null,
    montoTotal: 2500,
    lineas: [{ productoId: 1, productoNombre: 'Laptop Pro', cantidad: 1, precioUnitario: 2500, subtotal: 2500 }],
  },
  {
    id: 4,
    numero: 'OA-000004',
    fecha: '2026-10-04T00:00:00Z',
    proveedorId: 2,
    proveedorNombre: 'Cobre Austral SpA',
    solicitudCompraId: null,
    solicitudNumero: null,
    estado: 'Anulada',
    fechaEntregaEstimada: '2026-10-20T00:00:00Z',
    fechaRecepcion: null,
    atrasada: false,
    observaciones: null,
    motivoAnulacion: 'Cambio de planificación de compra.',
    montoTotal: 450,
    lineas: [{ productoId: 3, productoNombre: 'Mouse Óptico', cantidad: 9, precioUnitario: 50, subtotal: 450 }],
  },
];

const resumen: ResumenOrdenesAbastecimiento = {
  total: 4,
  emitidas: 2,
  recibidas: 1,
  anuladas: 1,
  atrasadas: 1,
  montoPorRecibir: 4400,
  montoRecibido: 2500,
  topProveedores: [
    { proveedorId: 1, razonSocial: 'Andes Suministros S.A.C.', ordenes: 2, monto: 5900 },
    { proveedorId: 2, razonSocial: 'Cobre Austral SpA', ordenes: 1, monto: 1000 },
  ],
};

interface ApiOpciones {
  vacio?: boolean;
  errorLista?: boolean;
  errorCrear?: boolean;
  errorGenerar?: boolean;
  errorAnular?: boolean;
  solicitudEstado?: 'Pendiente' | 'Aprobada';
  totalFilas?: number;
}

export function prepararApi(opciones: ApiOpciones = {}) {
  const llamadas: { url: string; init: RequestInit }[] = [];
  const filas: OrdenAbastecimientoDetalle[] = ordenes.map((orden) => ({
    ...orden,
    lineas: orden.lineas.map((linea) => ({ ...linea })),
  }));
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    llamadas.push({ url, init });
    if (url.endsWith('/ordenes-abastecimiento/resumen')) {
      return Response.json(resumen);
    }
    if (url.endsWith('/proveedores?activo=true&tamanoPagina=100')) {
      return Response.json({ items: proveedores });
    }
    if (url.endsWith('/productos?tamanoPagina=100')) {
      return Response.json({ items: productos });
    }
    if (url.includes('/ordenes-abastecimiento/desde-solicitud/')) {
      if (opciones.errorGenerar) {
        return Response.json({ StatusCode: 409, Message: 'La solicitud SC-000003 ya tiene la orden OA-000001.' }, { status: 409 });
      }
      const cuerpo = JSON.parse(String(init.body)) as { proveedorId: number; fechaEntregaEstimada: string; observaciones?: string };
      const proveedor = proveedores.find((item) => item.id === cuerpo.proveedorId) ?? proveedores[0]!;
      const generada: OrdenAbastecimientoDetalle = {
        id: 5,
        numero: 'OA-000005',
        fecha: '2026-10-14T00:00:00Z',
        proveedorId: proveedor.id,
        proveedorNombre: proveedor.razonSocial,
        solicitudCompraId: 3,
        solicitudNumero: 'SC-000003',
        estado: 'Emitida',
        fechaEntregaEstimada: `${cuerpo.fechaEntregaEstimada}T00:00:00Z`,
        fechaRecepcion: null,
        atrasada: false,
        observaciones: cuerpo.observaciones ?? null,
        motivoAnulacion: null,
        montoTotal: 1800,
        lineas: [{ productoId: 2, productoNombre: 'Monitor 27', cantidad: 2, precioUnitario: 900, subtotal: 1800 }],
      };
      filas.unshift(generada);
      return Response.json({ id: 5, numero: 'OA-000005' });
    }
    if (init.method === 'POST' && url.endsWith('/ordenes-abastecimiento')) {
      if (opciones.errorCrear) {
        return Response.json({ StatusCode: 400, Message: 'El proveedor 1 está inactivo.' }, { status: 400 });
      }
      return Response.json({ id: 5, numero: 'OA-000005' });
    }
    if (init.method === 'POST' && url.endsWith('/anular')) {
      if (opciones.errorAnular) {
        return Response.json({ StatusCode: 409, Message: 'La orden OA-000002 ya está anulada.' }, { status: 409 });
      }
      const destino = filas.find((orden) => url.includes(`/ordenes-abastecimiento/${orden.id}/anular`));
      if (destino !== undefined) {
        destino.estado = 'Anulada';
        destino.motivoAnulacion = JSON.parse(String(init.body)).motivo as string;
        destino.atrasada = false;
        return Response.json(destino);
      }
    }
    if (init.method === 'POST' && url.endsWith('/recibir')) {
      const destino = filas.find((orden) => url.includes(`/ordenes-abastecimiento/${orden.id}/recibir`));
      if (destino !== undefined) {
        destino.estado = 'Recibida';
        destino.fechaRecepcion = '2026-10-14T00:00:00Z';
        destino.atrasada = false;
        return Response.json(destino);
      }
    }
    if (init.method === 'PUT') {
      const destino = filas.find((orden) => url.endsWith(`/ordenes-abastecimiento/${orden.id}`));
      return Response.json({ id: destino?.id ?? 1, numero: destino?.numero ?? 'OA-000001' });
    }
    if (url.includes('/ordenes-abastecimiento?')) {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'No se pudieron cargar las órdenes.' }, { status: 500 });
      }
      if (opciones.vacio) {
        return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
      }
      const pagina = Number(new URL(url, 'http://localhost').searchParams.get('pagina') ?? '1');
      const filasDePagina = filas.slice((pagina - 1) * 10, pagina * 10);
      const filasVisibles = filasDePagina.length > 0 ? filasDePagina : opciones.totalFilas ? filas.slice(0, 10) : [];
      const porPagina = filasVisibles.map((orden) => ({
        id: orden.id,
        numero: orden.numero,
        fecha: orden.fecha,
        proveedorId: orden.proveedorId,
        proveedorNombre: orden.proveedorNombre,
        solicitudNumero: orden.solicitudNumero,
        estado: orden.estado,
        fechaEntregaEstimada: orden.fechaEntregaEstimada,
        atrasada: orden.atrasada,
        montoTotal: orden.montoTotal,
      }));
      return Response.json({ items: porPagina, total: opciones.totalFilas ?? filas.length, pagina, tamanoPagina: 10 });
    }
    if (/\/ordenes-abastecimiento\/\d+$/.test(url)) {
      const id = Number(url.split('/').at(-1));
      const detalle = filas.find((orden) => orden.id === id);
      return detalle ? Response.json(detalle) : Response.json({ StatusCode: 404, Message: `La orden de abastecimiento ${id} no existe.` }, { status: 404 });
    }
    if (url.includes('/solicitudes-compra?')) {
      return Response.json({
        items: [{
          id: 3,
          numero: 'SC-000003',
          fecha: '2026-10-01T00:00:00Z',
          solicitante: 'Rosa Salazar',
          proveedorId: 1,
          proveedorNombre: 'Andes Suministros S.A.C.',
          estado: opciones.solicitudEstado ?? 'Aprobada',
          totalLineas: 1,
          montoEstimado: 1800,
        }],
        total: 1,
        pagina: 1,
        tamanoPagina: 10,
      });
    }
    if (url.endsWith('/solicitudes-compra/resumen')) {
      return Response.json({
        total: 1,
        pendientes: 0,
        aprobadas: 1,
        rechazadas: 0,
        montoPendiente: 0,
        montoAprobado: 1800,
        topProductosSolicitados: [],
      });
    }
    if (url.endsWith('/solicitudes-compra/3')) {
      return Response.json({
        id: 3,
        numero: 'SC-000003',
        fecha: '2026-10-01T00:00:00Z',
        solicitante: 'Rosa Salazar',
        proveedorId: 1,
        proveedorNombre: 'Andes Suministros S.A.C.',
        estado: opciones.solicitudEstado ?? 'Aprobada',
        totalLineas: 1,
        montoEstimado: 1800,
        observaciones: null,
        motivoRechazo: null,
        fechaDecision: null,
        lineas: [{ productoId: 2, productoNombre: 'Monitor 27', cantidad: 2, precioEstimado: 900, subtotal: 1800 }],
      });
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

export function renderEnRouter(elemento: ReactElement, initialEntries = ['/modulos/15-ordenes-abastecimiento']) {
  return render(<MemoryRouter initialEntries={initialEntries}>{elemento}</MemoryRouter>);
}

export async function esperarCargaInicial(): Promise<void> {
  await screen.findByRole('button', { name: 'OA-000001' }, { timeout: 5000 });
  await screen.findByText(/5,900\.00/, {}, { timeout: 5000 });
  await screen.findByRole('option', { name: 'Cobre Austral SpA' }, { timeout: 5000 });
}

export function cambiarValor(etiqueta: string, valor: string): void {
  fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } });
}
