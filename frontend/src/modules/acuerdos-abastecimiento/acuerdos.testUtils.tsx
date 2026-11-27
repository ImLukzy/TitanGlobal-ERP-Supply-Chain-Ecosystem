import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router';
import { vi } from 'vitest';
import type { AcuerdoDetalle, LineaAcuerdo, ResumenAcuerdos } from './tipos';

export const proveedores = [
  { id: 1, razonSocial: 'Andes Suministros S.A.C.' },
  { id: 2, razonSocial: 'Cobre Austral SpA' },
];

export const productos = [
  { id: 1, nombre: 'Cobre refinado' },
  { id: 2, nombre: 'Lingote de zinc' },
  { id: 3, nombre: 'Resina epóxica' },
];

function linea(id: number, productoId: number, productoNombre: string, precio: number, comprometida: number, consumida: number): LineaAcuerdo {
  return { id, productoId, productoNombre, precioAcordado: precio, cantidadComprometida: comprometida, cantidadConsumida: consumida, saldo: comprometida - consumida };
}

function acuerdo(
  id: number, numero: string, proveedorId: number, estado: AcuerdoDetalle['estado'], fechaInicio: string, fechaFin: string,
  montoMaximo: number, lineas: LineaAcuerdo[],
): AcuerdoDetalle {
  const comprometido = lineas.reduce((suma, l) => suma + l.precioAcordado * l.cantidadComprometida, 0);
  const consumido = lineas.reduce((suma, l) => suma + l.precioAcordado * l.cantidadConsumida, 0);
  return {
    id, numero, proveedorId, proveedorNombre: proveedores.find((p) => p.id === proveedorId)?.razonSocial ?? '',
    descripcion: id === 1 ? 'Marco anual de cobre.' : null, estado, fechaInicio, fechaFin, montoMaximo,
    montoComprometido: comprometido, montoConsumido: consumido, fechaRegistro: '2026-01-02T12:00:00Z', lineas,
  };
}

export const acuerdos: AcuerdoDetalle[] = [
  acuerdo(1, 'AM-2026-001', 1, 'Vigente', '2026-01-01T12:00:00Z', '2099-12-31T12:00:00Z', 100000, [
    linea(1, 1, 'Cobre refinado', 50, 1000, 400),
    linea(2, 2, 'Lingote de zinc', 80, 500, 0),
  ]),
  acuerdo(2, 'AM-2026-002', 2, 'Vigente', '2026-03-01T12:00:00Z', '2099-12-31T12:00:00Z', 60000, [linea(3, 3, 'Resina epóxica', 120, 300, 300)]),
  acuerdo(3, 'AM-2026-003', 1, 'Suspendido', '2026-06-01T12:00:00Z', '2099-12-31T12:00:00Z', 40000, [linea(4, 1, 'Cobre refinado', 55, 200, 50)]),
  acuerdo(4, 'AM-2026-004', 2, 'Cancelado', '2026-01-01T12:00:00Z', '2026-06-30T12:00:00Z', 25000, [linea(5, 2, 'Lingote de zinc', 30, 500, 100)]),
  acuerdo(5, 'AM-2026-005', 1, 'Vigente', '2026-01-01T12:00:00Z', '2026-10-31T12:00:00Z', 20000, [linea(6, 2, 'Lingote de zinc', 80, 100, 0)]),
  acuerdo(6, 'AM-2026-006', 2, 'Vigente', '2099-01-01T12:00:00Z', '2099-12-31T12:00:00Z', 30000, []),
];

export const resumen: ResumenAcuerdos = {
  totalAcuerdos: 4,
  montoMaximoVigente: 160000,
  montoConsumido: 61750,
  porEstado: [
    { estado: 'Vigente', total: 2 },
    { estado: 'Suspendido', total: 1 },
    { estado: 'Cancelado', total: 1 },
  ],
};

interface ApiOpciones {
  vacio?: boolean;
  errorLista?: boolean;
  errorCrear?: boolean;
  errorLinea?: boolean;
  errorAccion?: boolean;
  errorBaja?: boolean;
}

function paraListado(a: AcuerdoDetalle) {
  return {
    id: a.id, numero: a.numero, proveedorNombre: a.proveedorNombre, estado: a.estado, fechaInicio: a.fechaInicio,
    fechaFin: a.fechaFin, montoMaximo: a.montoMaximo, montoComprometido: a.montoComprometido, montoConsumido: a.montoConsumido,
  };
}

export function prepararApi(opciones: ApiOpciones = {}) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const metodo = init.method ?? 'GET';
    if (url.endsWith('/acuerdos-abastecimiento/resumen')) {
      return Response.json(resumen);
    }
    if (url.includes('/proveedores?')) {
      return Response.json({ items: proveedores });
    }
    if (url.includes('/productos?')) {
      return Response.json({ items: productos });
    }
    if (metodo === 'POST' && url.endsWith('/acuerdos-abastecimiento')) {
      return opciones.errorCrear
        ? Response.json({ StatusCode: 409, Message: 'Ya existe un acuerdo con el número AM-2026-001.' }, { status: 409 })
        : Response.json({ id: 7 });
    }
    if (metodo === 'POST' && url.endsWith('/lineas') && opciones.errorLinea) {
      return Response.json({ StatusCode: 400, Message: 'El compromiso de las líneas supera el monto máximo del acuerdo.' }, { status: 400 });
    }
    if (metodo === 'POST' || metodo === 'PUT') {
      return opciones.errorAccion
        ? Response.json({ StatusCode: 409, Message: 'El acuerdo 1 no está vigente.' }, { status: 409 })
        : Response.json({ id: Number(/acuerdos-abastecimiento\/(\d+)/.exec(url)?.[1] ?? 0) });
    }
    if (metodo === 'DELETE') {
      return opciones.errorBaja
        ? Response.json({ StatusCode: 409, Message: 'El acuerdo 5 tiene consumos y no se puede eliminar; cancélelo.' }, { status: 409 })
        : new Response(null, { status: 204 });
    }
    const detalle = /\/acuerdos-abastecimiento\/(\d+)$/.exec(url);
    if (detalle !== null) {
      const encontrado = acuerdos.find((a) => a.id === Number(detalle[1]));
      return encontrado === undefined ? Response.json({ StatusCode: 404, Message: 'El acuerdo no existe.' }, { status: 404 }) : Response.json(encontrado);
    }
    if (url.includes('/acuerdos-abastecimiento?')) {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'No se pudieron cargar los acuerdos.' }, { status: 500 });
      }
      if (opciones.vacio) {
        return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
      }
      return Response.json({ items: acuerdos.map(paraListado), total: acuerdos.length, pagina: 1, tamanoPagina: 10 });
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock };
}

export function renderEnRouter(elemento: ReactElement, initialEntries = ['/modulos/20-acuerdos-abastecimiento']) {
  return render(<MemoryRouter initialEntries={initialEntries}>{elemento}</MemoryRouter>);
}

export async function esperarCargaInicial(): Promise<void> {
  await screen.findByRole('button', { name: 'AM-2026-002' }, { timeout: 5000 });
  await screen.findByText(/160,000\.00/, {}, { timeout: 5000 });
  await screen.findByRole('option', { name: 'Cobre Austral SpA' }, { timeout: 5000 });
}

export async function seleccionar(numero: string): Promise<void> {
  fireEvent.click(await screen.findByRole('button', { name: numero }));
  await screen.findByRole('heading', { name: numero });
}

export function cambiarValor(etiqueta: string, valor: string): void {
  fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } });
}

export function llamadasLista(fetchMock: ReturnType<typeof prepararApi>['fetchMock']): string[] {
  return fetchMock.mock.calls.map((call) => String(call[0])).filter((url) => url.includes('/acuerdos-abastecimiento?'));
}

export function llamadasMetodo(fetchMock: ReturnType<typeof prepararApi>['fetchMock'], metodo: string): Array<[string, RequestInit | undefined]> {
  return fetchMock.mock.calls.filter((call) => call[1]?.method === metodo).map((call) => [String(call[0]), call[1]]);
}
