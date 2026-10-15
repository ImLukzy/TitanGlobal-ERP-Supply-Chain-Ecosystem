import { cleanup, render, screen, waitFor } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import type { AlmacenListado } from '../almacenes/tipos';
import type { GuardarZonaRequest, ResumenZonas, ZonaDetalle, ZonaListado } from './tipos';
import { ZonasAlmacenajePage } from './ZonasAlmacenajePage';

export const almacenesSeed: AlmacenListado[] = [
  { id: 1, codigo: 'ALM-LIM-01', nombre: 'Central Lima', tipo: 'Central', ciudad: 'Lima', capacidadM3: 500, activo: true },
  { id: 3, codigo: 'ALM-ARE-01', nombre: 'Regional Arequipa', tipo: 'Regional', ciudad: 'Arequipa', capacidadM3: 150, activo: true },
  { id: 4, codigo: 'ALM-TRU-01', nombre: 'Regional Trujillo', tipo: 'Regional', ciudad: 'Trujillo', capacidadM3: 100, activo: false },
  { id: 5, codigo: 'ALM-CAL-TR', nombre: 'Transito Callao', tipo: 'Transito', ciudad: 'Callao', capacidadM3: 75, activo: true },
];

export const zonasSeed: ZonaDetalle[] = [
  { id: 1, almacenId: 1, almacenCodigo: 'ALM-LIM-01', almacenNombre: 'Central Lima', codigo: 'REC-01', nombre: 'Recepción principal', tipo: 'Recepcion', temperatura: 'Ambiente', capacidadM3: 50, activa: true },
  { id: 2, almacenId: 1, almacenCodigo: 'ALM-LIM-01', almacenNombre: 'Central Lima', codigo: 'ALM-A', nombre: 'Almacenamiento A', tipo: 'Almacenamiento', temperatura: 'Ambiente', capacidadM3: 300, activa: true },
  { id: 3, almacenId: 1, almacenCodigo: 'ALM-LIM-01', almacenNombre: 'Central Lima', codigo: 'PIC-01', nombre: 'Picking principal', tipo: 'Picking', temperatura: 'Ambiente', capacidadM3: 60, activa: true },
  { id: 4, almacenId: 1, almacenCodigo: 'ALM-LIM-01', almacenNombre: 'Central Lima', codigo: 'DES-01', nombre: 'Despacho principal', tipo: 'Despacho', temperatura: 'Ambiente', capacidadM3: 40, activa: true },
  { id: 5, almacenId: 3, almacenCodigo: 'ALM-ARE-01', almacenNombre: 'Regional Arequipa', codigo: 'ALM-A', nombre: 'Almacenamiento Arequipa', tipo: 'Almacenamiento', temperatura: 'Ambiente', capacidadM3: 100, activa: true },
  { id: 6, almacenId: 3, almacenCodigo: 'ALM-ARE-01', almacenNombre: 'Regional Arequipa', codigo: 'FRIO-01', nombre: 'Cámara refrigerada', tipo: 'Almacenamiento', temperatura: 'Refrigerada', capacidadM3: 30, activa: true },
  { id: 7, almacenId: 5, almacenCodigo: 'ALM-CAL-TR', almacenNombre: 'Transito Callao', codigo: 'TRAN-01', nombre: 'Tránsito principal', tipo: 'Recepcion', temperatura: 'Ambiente', capacidadM3: 60, activa: true },
  { id: 8, almacenId: 5, almacenCodigo: 'ALM-CAL-TR', almacenNombre: 'Transito Callao', codigo: 'CUA-01', nombre: 'Cuarentena', tipo: 'Cuarentena', temperatura: 'Ambiente', capacidadM3: 20, activa: false },
];

export interface OpcionMockZonas {
  vacio?: boolean;
  errorLista?: boolean;
}

export interface LlamadaMockZona {
  url: string;
  init: RequestInit;
}

function obtenerListado(zona: ZonaDetalle): ZonaListado {
  return {
    id: zona.id,
    almacenId: zona.almacenId,
    almacenCodigo: zona.almacenCodigo,
    codigo: zona.codigo,
    nombre: zona.nombre,
    tipo: zona.tipo,
    temperatura: zona.temperatura,
    capacidadM3: zona.capacidadM3,
    activa: zona.activa,
  };
}

function crearResumen(zonas: ZonaDetalle[], filtroAlmacen: number | null): ResumenZonas {
  const zonasFiltradas = filtroAlmacen === null ? zonas : zonas.filter((zona) => zona.almacenId === filtroAlmacen);
  const tipos = ['Recepcion', 'Almacenamiento', 'Picking', 'Despacho', 'Cuarentena'] as const;
  const porTipo = tipos.flatMap((tipo) => {
    const coincidencias = zonasFiltradas.filter((zona) => zona.tipo === tipo);
    if (coincidencias.length === 0) {
      return [];
    }
    return [{
      tipo,
      totalZonas: coincidencias.length,
      capacidadM3: coincidencias.reduce((total, zona) => total + zona.capacidadM3, 0),
    }];
  });
  const almacenes = filtroAlmacen === null ? almacenesSeed : almacenesSeed.filter((almacen) => almacen.id === filtroAlmacen);
  const ocupacion = almacenes.filter((almacen) => zonas.some((zona) => zona.almacenId === almacen.id)).map((almacen) => {
    const asignada = zonas.filter((zona) => zona.almacenId === almacen.id && zona.activa)
      .reduce((total, zona) => total + zona.capacidadM3, 0);
    return {
      almacenId: almacen.id,
      almacenCodigo: almacen.codigo,
      capacidadAlmacenM3: almacen.capacidadM3,
      capacidadAsignadaM3: asignada,
      porcentajeAsignado: almacen.capacidadM3 === 0 ? 0 : Number(((asignada / almacen.capacidadM3) * 100).toFixed(2)),
    };
  });
  return {
    totalZonas: zonasFiltradas.length,
    activas: zonasFiltradas.filter((zona) => zona.activa).length,
    porTipo,
    ocupacion,
  };
}

export function prepararApiZonas(opciones: OpcionMockZonas = {}) {
  const zonas = structuredClone(zonasSeed);
  const llamadas: LlamadaMockZona[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });

    if (url.includes('/almacenes?')) {
      const activos = parsed.searchParams.get('activo');
      const items = almacenesSeed.filter((almacen) => activos !== 'true' || almacen.activo);
      return Response.json({ items, total: items.length, pagina: 1, tamanoPagina: 100 });
    }
    if (parsed.pathname.endsWith('/zonas-almacenaje/resumen')) {
      const valorId = parsed.searchParams.get('almacenId');
      return Response.json(crearResumen(zonas, valorId === null ? null : Number(valorId)));
    }
    if (metodo === 'POST' && parsed.pathname.endsWith('/zonas-almacenaje')) {
      const request = JSON.parse(String(init.body)) as GuardarZonaRequest;
      const almacen = almacenesSeed.find((item) => item.id === request.almacenId);
      if (almacen === undefined) {
        return Response.json({ StatusCode: 400, Message: `El almacén ${request.almacenId} no existe.` }, { status: 400 });
      }
      if (!almacen.activo) {
        return Response.json({ StatusCode: 409, Message: `El almacén ${almacen.codigo} está inactivo.` }, { status: 409 });
      }
      if (zonas.some((zona) => zona.almacenId === request.almacenId && zona.codigo === request.codigo)) {
        return Response.json({ StatusCode: 409, Message: `Ya existe la zona ${request.codigo} en el almacén ${almacen.codigo}.` }, { status: 409 });
      }
      const asignada = zonas.filter((zona) => zona.almacenId === request.almacenId && zona.activa)
        .reduce((total, zona) => total + zona.capacidadM3, 0);
      if (request.activa && asignada + request.capacidadM3 > almacen.capacidadM3) {
        return Response.json({
          StatusCode: 409,
          Message: `La capacidad de las zonas activas (${(asignada + request.capacidadM3).toFixed(2)} m³) superaría la del almacén ${almacen.codigo} (${almacen.capacidadM3.toFixed(2)} m³).`,
        }, { status: 409 });
      }
      const zona: ZonaDetalle = { id: 9, ...request, almacenCodigo: almacen.codigo, almacenNombre: almacen.nombre };
      zonas.push(zona);
      return Response.json({ id: zona.id });
    }
    if (metodo === 'PUT') {
      const request = JSON.parse(String(init.body)) as GuardarZonaRequest;
      const id = Number(parsed.pathname.split('/').at(-1));
      const index = zonas.findIndex((zona) => zona.id === id);
      const almacen = almacenesSeed.find((item) => item.id === request.almacenId);
      if (index < 0) {
        return Response.json({ StatusCode: 404, Message: `La zona ${id} no existe.` }, { status: 404 });
      }
      if (almacen === undefined) {
        return Response.json({ StatusCode: 400, Message: `El almacén ${request.almacenId} no existe.` }, { status: 400 });
      }
      if (zonas.some((zona) => zona.id !== id && zona.almacenId === request.almacenId && zona.codigo === request.codigo)) {
        return Response.json({ StatusCode: 409, Message: `Ya existe la zona ${request.codigo} en el almacén ${almacen.codigo}.` }, { status: 409 });
      }
      const asignada = zonas.filter((zona) => zona.id !== id && zona.almacenId === request.almacenId && zona.activa)
        .reduce((total, zona) => total + zona.capacidadM3, 0);
      if (request.activa && asignada + request.capacidadM3 > almacen.capacidadM3) {
        return Response.json({
          StatusCode: 409,
          Message: `La capacidad de las zonas activas (${(asignada + request.capacidadM3).toFixed(2)} m³) superaría la del almacén ${almacen.codigo} (${almacen.capacidadM3.toFixed(2)} m³).`,
        }, { status: 409 });
      }
      zonas[index] = { id, ...request, almacenCodigo: almacen.codigo, almacenNombre: almacen.nombre };
      return Response.json({ id });
    }
    if (metodo === 'DELETE') {
      const id = Number(parsed.pathname.split('/').at(-1));
      const index = zonas.findIndex((zona) => zona.id === id);
      if (index < 0) {
        return Response.json({ StatusCode: 404, Message: `La zona ${id} no existe.` }, { status: 404 });
      }
      zonas.splice(index, 1);
      return new Response(null, { status: 204 });
    }
    if (/\/zonas-almacenaje\/\d+$/.test(parsed.pathname)) {
      const id = Number(parsed.pathname.split('/').at(-1));
      const zona = zonas.find((item) => item.id === id);
      if (zona === undefined) {
        return Response.json({ StatusCode: 404, Message: `La zona ${id} no existe.` }, { status: 404 });
      }
      return Response.json(zona);
    }
    if (parsed.pathname.endsWith('/zonas-almacenaje')) {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'No se pudo cargar el listado de zonas.' }, { status: 500 });
      }
      const almacenId = parsed.searchParams.get('almacenId');
      const busqueda = (parsed.searchParams.get('busqueda') ?? '').toLocaleLowerCase();
      const tipo = parsed.searchParams.get('tipo') ?? '';
      const temperatura = parsed.searchParams.get('temperatura') ?? '';
      const activa = parsed.searchParams.get('activa');
      const pagina = Number(parsed.searchParams.get('pagina') ?? '1');
      const coincidencias = opciones.vacio ? [] : zonas.filter((zona) => (
        (almacenId === null || String(zona.almacenId) === almacenId)
        && (busqueda === '' || zona.codigo.toLocaleLowerCase().includes(busqueda) || zona.nombre.toLocaleLowerCase().includes(busqueda))
        && (tipo === '' || zona.tipo === tipo)
        && (temperatura === '' || zona.temperatura === temperatura)
        && (activa === null || String(zona.activa) === activa)
      )).sort((a, b) => a.almacenCodigo.localeCompare(b.almacenCodigo) || a.codigo.localeCompare(b.codigo) || a.id - b.id);
      const items = coincidencias.slice((pagina - 1) * 10, pagina * 10).map(obtenerListado);
      return Response.json({ items, total: coincidencias.length, pagina, tamanoPagina: 10 });
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas, zonas };
}

export function renderZonas(elemento: ReactElement = <ZonasAlmacenajePage />, initialEntries = ['/modulos/24-zonas-almacenaje']) {
  return render(<MemoryRouter initialEntries={initialEntries}>{elemento}</MemoryRouter>);
}

export async function esperarCargaZonas(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await screen.findByRole('button', { name: 'REC-01' });
  await screen.findByRole('button', { name: 'Editar zona' });
  await screen.findByText('Casi lleno');
  await waitFor(() => {
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/zonas-almacenaje?')).length).toBe(1);
  });
}

export function limpiarZonas(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
