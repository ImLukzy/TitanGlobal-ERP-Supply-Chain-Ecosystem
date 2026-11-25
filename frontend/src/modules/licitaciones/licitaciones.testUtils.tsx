import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router';
import { vi } from 'vitest';
import type { LicitacionDetalle, OfertaLicitacion, ResumenLicitaciones } from './tipos';

export const proveedores = [
  { id: 1, razonSocial: 'Andes Suministros S.A.C.' },
  { id: 2, razonSocial: 'Cobre Austral SpA' },
];

function oferta(id: number, proveedorId: number, proveedorNombre: string, monto: number, esGanadora = false): OfertaLicitacion {
  return { id, proveedorId, proveedorNombre, monto, plazoEntregaDias: 15, observaciones: id === 1 ? 'Entrega en almacén central.' : null, fechaRegistro: '2026-11-02T12:00:00Z', esGanadora };
}

function base(id: number, numero: string, titulo: string, estado: LicitacionDetalle['estado'], tipo: LicitacionDetalle['tipo'], fechaCierre: string, ofertas: OfertaLicitacion[]): LicitacionDetalle {
  return {
    id, numero, titulo, descripcion: id === 1 ? 'Compra de cobre refinado.' : null, tipo, estado,
    presupuestoReferencial: 30000, fechaCierre, proveedorAdjudicadoId: null, proveedorAdjudicadoNombre: null,
    montoAdjudicado: null, fechaRegistro: '2026-11-01T12:00:00Z', ofertas,
  };
}

export const licitaciones: LicitacionDetalle[] = [
  base(1, 'LIC-2026-001', 'Cobre refinado', 'Abierta', 'ConcursoPrecios', '2099-12-31T17:00:00Z', []),
  base(2, 'LIC-2026-002', 'Empaques industriales', 'Abierta', 'SubastaInversa', '2099-12-31T17:00:00Z', [
    oferta(1, 1, 'Andes Suministros S.A.C.', 25000),
    oferta(2, 2, 'Cobre Austral SpA', 27000),
  ]),
  { ...base(3, 'LIC-2026-003', 'Pallets certificados', 'Adjudicada', 'ConcursoPrecios', '2026-10-20T17:00:00Z', [oferta(3, 1, 'Andes Suministros S.A.C.', 28000, true)]), proveedorAdjudicadoId: 1, proveedorAdjudicadoNombre: 'Andes Suministros S.A.C.', montoAdjudicado: 28000 },
  base(4, 'LIC-2026-004', 'Resinas plásticas', 'Cerrada', 'ConcursoPrecios', '2026-10-25T17:00:00Z', [
    oferta(4, 1, 'Andes Suministros S.A.C.', 21000),
    oferta(5, 2, 'Cobre Austral SpA', 19500),
  ]),
  base(5, 'LIC-2026-005', 'Cajas de cartón', 'Cancelada', 'ConcursoPrecios', '2026-10-30T17:00:00Z', []),
  base(6, 'LIC-2026-006', 'Tarimas de madera', 'Abierta', 'ConcursoPrecios', '2026-11-01T17:00:00Z', []),
];

export const resumen: ResumenLicitaciones = {
  totalLicitaciones: 5,
  totalOfertas: 7,
  presupuestoAbierto: 70000,
  montoAdjudicado: 28000,
  porEstado: [
    { estado: 'Abierta', total: 2 },
    { estado: 'Cerrada', total: 1 },
    { estado: 'Adjudicada', total: 1 },
    { estado: 'Cancelada', total: 1 },
  ],
};

interface ApiOpciones {
  vacio?: boolean;
  errorLista?: boolean;
  errorCrear?: boolean;
  errorAccion?: boolean;
  errorBaja?: boolean;
  totalFilas?: number;
}

function paraListado(l: LicitacionDetalle) {
  return { id: l.id, numero: l.numero, titulo: l.titulo, tipo: l.tipo, estado: l.estado, presupuestoReferencial: l.presupuestoReferencial, fechaCierre: l.fechaCierre, totalOfertas: l.ofertas.length };
}

export function prepararApi(opciones: ApiOpciones = {}) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const metodo = init.method ?? 'GET';
    if (url.endsWith('/licitaciones/resumen')) {
      return Response.json(resumen);
    }
    if (url.includes('/proveedores?')) {
      return Response.json({ items: proveedores });
    }
    if (metodo === 'POST' && url.endsWith('/licitaciones')) {
      if (opciones.errorCrear) {
        return Response.json({ StatusCode: 409, Message: 'Ya existe una licitación con el número LIC-2026-001.' }, { status: 409 });
      }
      return Response.json({ id: 7 });
    }
    if (metodo === 'POST' || metodo === 'PUT') {
      if (opciones.errorAccion) {
        return Response.json({ StatusCode: 409, Message: 'La licitación 2 no está abierta.' }, { status: 409 });
      }
      return Response.json({ id: Number(/licitaciones\/(\d+)/.exec(url)?.[1] ?? 0) });
    }
    if (metodo === 'DELETE') {
      if (opciones.errorBaja) {
        return Response.json({ StatusCode: 409, Message: 'La licitación 1 tiene ofertas y no se puede eliminar; cancélela.' }, { status: 409 });
      }
      return new Response(null, { status: 204 });
    }
    const detalle = /\/licitaciones\/(\d+)$/.exec(url);
    if (detalle !== null) {
      const encontrada = licitaciones.find((l) => l.id === Number(detalle[1]));
      return encontrada === undefined ? Response.json({ StatusCode: 404, Message: 'La licitación no existe.' }, { status: 404 }) : Response.json(encontrada);
    }
    if (url.includes('/licitaciones?')) {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'No se pudieron cargar las licitaciones.' }, { status: 500 });
      }
      if (opciones.vacio) {
        return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
      }
      const pagina = Number(new URL(url, 'http://local').searchParams.get('pagina') ?? '1');
      return Response.json({ items: licitaciones.map(paraListado), total: opciones.totalFilas ?? licitaciones.length, pagina, tamanoPagina: 10 });
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock };
}

export function renderEnRouter(elemento: ReactElement, initialEntries = ['/modulos/14-licitaciones']) {
  return render(<MemoryRouter initialEntries={initialEntries}>{elemento}</MemoryRouter>);
}

export async function esperarCargaInicial(): Promise<void> {
  await screen.findByRole('button', { name: 'LIC-2026-002' }, { timeout: 5000 });
  await screen.findByText(/70,000\.00/, {}, { timeout: 5000 });
}

export async function seleccionar(numero: string): Promise<void> {
  fireEvent.click(await screen.findByRole('button', { name: numero }));
  await screen.findByRole('heading', { name: numero });
}

export function cambiarValor(etiqueta: string, valor: string): void {
  fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } });
}

export function llamadasLista(fetchMock: ReturnType<typeof prepararApi>['fetchMock']): string[] {
  return fetchMock.mock.calls.map((call) => String(call[0])).filter((url) => url.includes('/licitaciones?'));
}

export function llamadasMetodo(fetchMock: ReturnType<typeof prepararApi>['fetchMock'], metodo: string): Array<[string, RequestInit | undefined]> {
  return fetchMock.mock.calls.filter((call) => call[1]?.method === metodo).map((call) => [String(call[0]), call[1]]);
}
