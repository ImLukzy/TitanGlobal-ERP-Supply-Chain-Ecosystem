import { render, screen, waitFor } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { AlmacenesPage } from './AlmacenesPage';
import type {
  AlmacenDetalle,
  AlmacenListado,
  GuardarAlmacenRequest,
  ResumenAlmacenes,
} from './tipos';

export const almacenesBase: AlmacenDetalle[] = [
  {
    id: 1,
    codigo: 'ALM-LIM-01',
    nombre: 'Central Lima',
    tipo: 'Central',
    direccion: 'Av. Argentina 1200',
    ciudad: 'Lima',
    capacidadM3: 500,
    responsable: 'María Torres',
    activo: true,
  },
  {
    id: 2,
    codigo: 'ALM-LIM-02',
    nombre: 'Regional Lima',
    tipo: 'Regional',
    direccion: 'Av. Separadora Industrial 500',
    ciudad: 'Lima',
    capacidadM3: 200,
    responsable: 'Luis Paredes',
    activo: true,
  },
  {
    id: 3,
    codigo: 'ALM-ARE-01',
    nombre: 'Regional Arequipa',
    tipo: 'Regional',
    direccion: 'Av. Ejército 800',
    ciudad: 'Arequipa',
    capacidadM3: 150,
    responsable: 'Ana Salas',
    activo: true,
  },
  {
    id: 4,
    codigo: 'ALM-TRU-01',
    nombre: 'Regional Trujillo',
    tipo: 'Regional',
    direccion: 'Av. América Sur 900',
    ciudad: 'Trujillo',
    capacidadM3: 100,
    responsable: 'Jorge Ríos',
    activo: false,
  },
  {
    id: 5,
    codigo: 'ALM-CAL-TR',
    nombre: 'Transito Callao',
    tipo: 'Transito',
    direccion: 'Av. Néstor Gambetta 1500',
    ciudad: 'Callao',
    capacidadM3: 75,
    responsable: null,
    activo: true,
  },
];

export interface OpcionesApiAlmacenes {
  vacio?: boolean;
  errorLista?: boolean;
  errorGuardarCodigo?: boolean;
  errorGuardar400?: boolean;
  errorEliminarConZonas?: boolean;
}

export interface LlamadaApiAlmacen {
  url: string;
  init: RequestInit;
}

function resumenDe(filas: AlmacenDetalle[]): ResumenAlmacenes {
  const activos = filas.filter((almacen) => almacen.activo);
  const tipos = ['Central', 'Regional', 'Transito'] as const;
  const porTipo = tipos.flatMap((tipo) => {
    const coincidencias = filas.filter((almacen) => almacen.tipo === tipo);
    if (coincidencias.length === 0) {
      return [];
    }
    return [{
      tipo,
      totalAlmacenes: coincidencias.length,
      capacidadM3: coincidencias.reduce((suma, almacen) => suma + almacen.capacidadM3, 0),
    }];
  });
  const ciudades = [...new Set(filas.map((almacen) => almacen.ciudad))];
  const porCiudad = ciudades.map((ciudad) => {
    const coincidencias = filas.filter((almacen) => almacen.ciudad === ciudad);
    return {
      ciudad,
      totalAlmacenes: coincidencias.length,
      activos: coincidencias.filter((almacen) => almacen.activo).length,
    };
  }).sort((a, b) => b.totalAlmacenes - a.totalAlmacenes || a.ciudad.localeCompare(b.ciudad));
  return {
    totalAlmacenes: filas.length,
    activos: activos.length,
    inactivos: filas.length - activos.length,
    capacidadTotalM3: filas.reduce((suma, almacen) => suma + almacen.capacidadM3, 0),
    capacidadActivaM3: activos.reduce((suma, almacen) => suma + almacen.capacidadM3, 0),
    porTipo,
    porCiudad,
  };
}

function aListado(almacen: AlmacenDetalle): AlmacenListado {
  return {
    id: almacen.id,
    codigo: almacen.codigo,
    nombre: almacen.nombre,
    tipo: almacen.tipo,
    ciudad: almacen.ciudad,
    capacidadM3: almacen.capacidadM3,
    activo: almacen.activo,
  };
}

export function prepararApiAlmacenes(opciones: OpcionesApiAlmacenes = {}) {
  const filas = structuredClone(almacenesBase);
  const llamadas: LlamadaApiAlmacen[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });
    const parsed = new URL(url, 'http://localhost');

    if (url.endsWith('/almacenes/ciudades')) {
      return Response.json([...new Set(filas.map((almacen) => almacen.ciudad))].sort());
    }
    if (url.endsWith('/almacenes/resumen')) {
      return Response.json(resumenDe(filas));
    }
    if (metodo === 'POST' && url.endsWith('/almacenes')) {
      const request = JSON.parse(String(init.body)) as GuardarAlmacenRequest;
      if (opciones.errorGuardar400) {
        return Response.json({
          StatusCode: 400,
          Message: 'El nombre del almacén no puede superar 200 caracteres.',
        }, { status: 400 });
      }
      if (opciones.errorGuardarCodigo || filas.some((almacen) => almacen.codigo === request.codigo)) {
        return Response.json({
          StatusCode: 409,
          Message: `Ya existe un almacén con el código ${request.codigo}.`,
        }, { status: 409 });
      }
      const almacen: AlmacenDetalle = { id: 6, ...request, responsable: request.responsable ?? null };
      filas.push(almacen);
      return Response.json({ id: almacen.id });
    }
    if (metodo === 'PUT') {
      const request = JSON.parse(String(init.body)) as GuardarAlmacenRequest;
      const id = Number(parsed.pathname.split('/').at(-1));
      const index = filas.findIndex((almacen) => almacen.id === id);
      if (index < 0) {
        return Response.json({ StatusCode: 404, Message: `El almacén ${id} no existe.` }, { status: 404 });
      }
      filas[index] = { id, ...request, responsable: request.responsable ?? null };
      return Response.json({ id });
    }
    if (metodo === 'DELETE') {
      const id = Number(parsed.pathname.split('/').at(-1));
      if (opciones.errorEliminarConZonas) {
        return Response.json({
          StatusCode: 409,
          Message: `No se puede eliminar el almacén ${id} porque tiene zonas.`,
        }, { status: 409 });
      }
      const index = filas.findIndex((almacen) => almacen.id === id);
      if (index < 0) {
        return Response.json({ StatusCode: 404, Message: `El almacén ${id} no existe.` }, { status: 404 });
      }
      filas.splice(index, 1);
      return new Response(null, { status: 204 });
    }
    if (/\/almacenes\/\d+$/.test(parsed.pathname)) {
      const id = Number(parsed.pathname.split('/').at(-1));
      const almacen = filas.find((item) => item.id === id);
      if (almacen === undefined) {
        return Response.json({ StatusCode: 404, Message: `El almacén ${id} no existe.` }, { status: 404 });
      }
      return Response.json(almacen);
    }
    if (url.includes('/almacenes?')) {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'No se pudo cargar el listado de almacenes.' }, { status: 500 });
      }
      if (opciones.vacio) {
        return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
      }
      const busqueda = (parsed.searchParams.get('busqueda') ?? '').toLocaleLowerCase();
      const ciudad = (parsed.searchParams.get('ciudad') ?? '').toLocaleLowerCase();
      const tipo = parsed.searchParams.get('tipo') ?? '';
      const activo = parsed.searchParams.get('activo');
      const pagina = Number(parsed.searchParams.get('pagina') ?? '1');
      const coincidencias = filas.filter((almacen) => (
        (busqueda === '' || almacen.codigo.toLocaleLowerCase().includes(busqueda) || almacen.nombre.toLocaleLowerCase().includes(busqueda))
        && (ciudad === '' || almacen.ciudad.toLocaleLowerCase() === ciudad)
        && (tipo === '' || almacen.tipo === tipo)
        && (activo === null || String(almacen.activo) === activo)
      )).sort((a, b) => a.codigo.localeCompare(b.codigo) || a.id - b.id);
      const items = coincidencias.slice((pagina - 1) * 10, pagina * 10).map(aListado);
      return Response.json({ items, total: coincidencias.length, pagina, tamanoPagina: 10 });
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas, filas };
}

export function renderAlmacenes(elemento: ReactElement = <AlmacenesPage />, initialEntries = ['/modulos/23-almacenes']) {
  return render(<MemoryRouter initialEntries={initialEntries}>{elemento}</MemoryRouter>);
}

export async function esperarCargaAlmacenes(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await screen.findByRole('button', { name: 'ALM-LIM-01' });
  await screen.findByRole('combobox', { name: 'Ciudad' });
  await screen.findByRole('button', { name: 'Editar almacén' });
  await waitFor(() => {
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/almacenes?'))).toHaveLength(1);
  });
}
