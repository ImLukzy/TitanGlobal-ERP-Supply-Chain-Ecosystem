import { cleanup, render, screen, waitFor } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import type { ClienteClasificado, GuardarSegmentoRequest, ResumenSegmentacion, SegmentoDetalle, SegmentoListado } from './tipos';
import { SegmentacionClientesPage } from './SegmentacionClientesPage';

export const segmentosSeed: SegmentoDetalle[] = [
  { id: 1, codigo: 'VIP', nombre: 'Clientes VIP', descripcion: 'Mayor valor y frecuencia.', prioridad: 1, montoMinimo: 5000, ordenesMinimas: 3, activo: true, clientes: 1 },
  { id: 2, codigo: 'FRECUENTE', nombre: 'Clientes frecuentes', descripcion: 'Compras frecuentes.', prioridad: 2, montoMinimo: 0, ordenesMinimas: 2, activo: true, clientes: 1 },
  { id: 3, codigo: 'OCASIONAL', nombre: 'Clientes ocasionales', descripcion: 'Clientes con compras ocasionales.', prioridad: 3, montoMinimo: 0, ordenesMinimas: 1, activo: true, clientes: 2 },
  { id: 4, codigo: 'PREMIUM-OLD', nombre: 'Premium anterior', descripcion: 'Segmento histórico inactivo.', prioridad: 4, montoMinimo: 1000, ordenesMinimas: 0, activo: false, clientes: 0 },
];

export const clasificacionSeed: ClienteClasificado[] = [
  { clienteId: 1, nombre: 'Acme Industrial', ruc: '20111111111', totalOrdenes: 4, montoTotal: 10000, segmentoId: 1, segmentoCodigo: 'VIP', segmentoNombre: 'Clientes VIP' },
  { clienteId: 2, nombre: 'Beta Comercio', ruc: '20222222222', totalOrdenes: 2, montoTotal: 2000, segmentoId: 2, segmentoCodigo: 'FRECUENTE', segmentoNombre: 'Clientes frecuentes' },
  { clienteId: 4, nombre: 'Delta Logística', ruc: '20444444444', totalOrdenes: 1, montoTotal: 750, segmentoId: 3, segmentoCodigo: 'OCASIONAL', segmentoNombre: 'Clientes ocasionales' },
  { clienteId: 3, nombre: 'Gamma Manufactura', ruc: '20333333333', totalOrdenes: 1, montoTotal: 500, segmentoId: 3, segmentoCodigo: 'OCASIONAL', segmentoNombre: 'Clientes ocasionales' },
  { clienteId: 5, nombre: 'Cliente sin órdenes', ruc: '20555555555', totalOrdenes: 0, montoTotal: 0, segmentoId: null, segmentoCodigo: null, segmentoNombre: null },
];

export interface OpcionesMockSegmentacion {
  vacio?: boolean;
  errorLista?: boolean;
  errorClasificacion?: boolean;
  errorCodigo?: boolean;
  errorPrioridad?: boolean;
}

export interface LlamadaMockSegmentacion {
  url: string;
  init: RequestInit;
}

function crearListado(segmento: SegmentoDetalle): SegmentoListado {
  const { descripcion, ...listado } = segmento;
  void descripcion;
  return listado;
}

function crearResumen(segmentos: SegmentoDetalle[]): ResumenSegmentacion {
  return {
    totalClientes: clasificacionSeed.length,
    clientesSegmentados: clasificacionSeed.filter((cliente) => cliente.segmentoId !== null).length,
    clientesSinSegmento: clasificacionSeed.filter((cliente) => cliente.segmentoId === null).length,
    porSegmento: segmentos.filter((segmento) => segmento.activo).map((segmento) => {
      const clientes = clasificacionSeed.filter((cliente) => cliente.segmentoId === segmento.id);
      return {
        segmentoId: segmento.id,
        codigo: segmento.codigo,
        nombre: segmento.nombre,
        clientes: clientes.length,
        montoTotal: clientes.reduce((total, cliente) => total + cliente.montoTotal, 0),
      };
    }).sort((a, b) => a.segmentoId - b.segmentoId),
  };
}

function pagina<T>(items: T[], numero: number, tamano: number) {
  const inicio = (numero - 1) * tamano;
  return { items: items.slice(inicio, inicio + tamano), total: items.length, pagina: numero, tamanoPagina: tamano };
}

export function prepararApiSegmentacion(opciones: OpcionesMockSegmentacion = {}) {
  const segmentos = structuredClone(segmentosSeed);
  const llamadas: LlamadaMockSegmentacion[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });

    if (parsed.pathname.endsWith('/segmentos-clientes/resumen')) {
      return Response.json(crearResumen(segmentos));
    }
    if (metodo === 'POST' && parsed.pathname.endsWith('/segmentos-clientes')) {
      const request = JSON.parse(String(init.body)) as GuardarSegmentoRequest;
      if (opciones.errorCodigo || segmentos.some((segmento) => segmento.codigo === request.codigo)) {
        return Response.json({ StatusCode: 409, Message: `Ya existe un segmento con el código ${request.codigo}.` }, { status: 409 });
      }
      if (opciones.errorPrioridad || segmentos.some((segmento) => segmento.prioridad === request.prioridad)) {
        return Response.json({ StatusCode: 409, Message: `Ya existe un segmento con la prioridad ${request.prioridad}.` }, { status: 409 });
      }
      const segmento: SegmentoDetalle = { id: 5, ...request, clientes: 0 };
      segmentos.push(segmento);
      return Response.json({ id: segmento.id });
    }
    if (metodo === 'PUT') {
      const id = Number(parsed.pathname.split('/').at(-1));
      const request = JSON.parse(String(init.body)) as GuardarSegmentoRequest;
      const index = segmentos.findIndex((segmento) => segmento.id === id);
      if (index < 0) {
        return Response.json({ StatusCode: 404, Message: `El segmento ${id} no existe.` }, { status: 404 });
      }
      if ((opciones.errorCodigo && request.codigo !== 'VIP') || segmentos.some((segmento) => segmento.id !== id && segmento.codigo === request.codigo)) {
        return Response.json({ StatusCode: 409, Message: `Ya existe un segmento con el código ${request.codigo}.` }, { status: 409 });
      }
      if ((opciones.errorPrioridad && request.prioridad !== 1) || segmentos.some((segmento) => segmento.id !== id && segmento.prioridad === request.prioridad)) {
        return Response.json({ StatusCode: 409, Message: `Ya existe un segmento con la prioridad ${request.prioridad}.` }, { status: 409 });
      }
      segmentos[index] = { id, ...request, clientes: segmentos[index]?.clientes ?? 0 };
      return Response.json({ id });
    }
    if (metodo === 'DELETE') {
      const id = Number(parsed.pathname.split('/').at(-1));
      const index = segmentos.findIndex((segmento) => segmento.id === id);
      if (index < 0) {
        return Response.json({ StatusCode: 404, Message: `El segmento ${id} no existe.` }, { status: 404 });
      }
      segmentos.splice(index, 1);
      return new Response(null, { status: 204 });
    }
    if (parsed.pathname.endsWith('/segmentos-clientes/clasificacion')) {
      if (opciones.errorClasificacion) {
        return Response.json({ StatusCode: 500, Message: 'No se pudo cargar la clasificación de clientes.' }, { status: 500 });
      }
      let clientes = clasificacionSeed;
      const segmentoId = parsed.searchParams.get('segmentoId');
      if (segmentoId !== null) {
        clientes = clientes.filter((cliente) => cliente.segmentoId === Number(segmentoId));
      }
      if (parsed.searchParams.get('sinSegmento') === 'true') {
        clientes = clientes.filter((cliente) => cliente.segmentoId === null);
      }
      const busqueda = (parsed.searchParams.get('busqueda') ?? '').toLocaleLowerCase();
      clientes = clientes.filter((cliente) => `${cliente.nombre} ${cliente.ruc}`.toLocaleLowerCase().includes(busqueda));
      const numero = Number(parsed.searchParams.get('pagina') ?? 1);
      const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? 10);
      return Response.json(pagina(clientes, numero, tamano));
    }
    const pathMatch = parsed.pathname.match(/\/segmentos-clientes\/(\d+)(?:\/(clientes))?$/);
    if (pathMatch !== null) {
      const id = Number(pathMatch[1]);
      const segmento = segmentos.find((item) => item.id === id);
      if (segmento === undefined) {
        return Response.json({ StatusCode: 404, Message: `El segmento ${id} no existe.` }, { status: 404 });
      }
      if (pathMatch[2] === 'clientes') {
        const clientes = clasificacionSeed.filter((cliente) => cliente.segmentoId === id);
        const numero = Number(parsed.searchParams.get('pagina') ?? 1);
        const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? 10);
        return Response.json(pagina(clientes, numero, tamano));
      }
      return Response.json(segmento);
    }
    if (parsed.pathname.endsWith('/segmentos-clientes')) {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'No se pudo cargar la lista de segmentos.' }, { status: 500 });
      }
      if (opciones.vacio) {
        return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
      }
      let items = segmentos;
      const busqueda = (parsed.searchParams.get('busqueda') ?? '').toLocaleLowerCase();
      items = items.filter((segmento) => `${segmento.codigo} ${segmento.nombre}`.toLocaleLowerCase().includes(busqueda));
      const activo = parsed.searchParams.get('activo');
      if (activo !== null) {
        items = items.filter((segmento) => String(segmento.activo) === activo);
      }
      const itemsListado = items.sort((a, b) => a.prioridad - b.prioridad).map(crearListado);
      const numero = Number(parsed.searchParams.get('pagina') ?? 1);
      const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? 10);
      return Response.json(pagina(itemsListado, numero, tamano));
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas, segmentos };
}

export function renderSegmentacion(elemento: ReactElement = <SegmentacionClientesPage />, initialEntries = ['/modulos/02-segmentacion-clientes']) {
  return render(<MemoryRouter initialEntries={initialEntries}>{elemento}</MemoryRouter>);
}

export async function esperarCargaSegmentos(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await screen.findByRole('button', { name: 'VIP' });
  await screen.findByRole('button', { name: 'Editar segmento' });
  await screen.findByRole('heading', { level: 2, name: 'Clientes VIP' });
  await waitFor(() => {
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/segmentos-clientes?'))).toHaveLength(1);
  });
}

export function limpiarSegmentacion(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
