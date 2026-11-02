import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { EmpleadosPage } from './EmpleadosPage';
import type { EmpleadoDetalle, EmpleadoListado } from './tipos';

export interface LlamadaEmpleados {
  url: string;
  metodo: string;
  cuerpo: unknown;
}

export interface OpcionesApiEmpleados {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorResumen?: boolean;
  errorDetalle?: boolean;
  errorGuardar?: 'duplicado' | 'invalido';
  errorCese?: 'yaCesado' | 'fecha';
  totalListado?: number;
  idCreado?: number;
}

// Los empleados replican el seed de la spec 47 §4 (tres Activo y un Cesado).
const empleadosMock: EmpleadoDetalle[] = [
  {
    id: 1,
    legajo: 'EMP-000001',
    tipoDocumento: 'DNI',
    numeroDocumento: '10000001',
    nombres: 'Ana',
    apellidos: 'Torres Vega',
    fechaNacimiento: '1990-05-14',
    fechaIngreso: '2021-03-01',
    cargo: 'Gerente de Ventas',
    area: 'Ventas',
    email: 'ana.torres@titanglobal.example',
    telefono: '999000001',
    estado: 'Activo',
    fechaCese: null,
    motivoCese: null,
  },
  {
    id: 2,
    legajo: 'EMP-000002',
    tipoDocumento: 'DNI',
    numeroDocumento: '10000002',
    nombres: 'Luis',
    apellidos: 'Rojas Mendoza',
    fechaNacimiento: '1988-11-02',
    fechaIngreso: '2019-08-16',
    cargo: 'Analista de Compras',
    area: 'Compras',
    email: 'luis.rojas@titanglobal.example',
    telefono: '999000002',
    estado: 'Activo',
    fechaCese: null,
    motivoCese: null,
  },
  {
    id: 3,
    legajo: 'EMP-000003',
    tipoDocumento: 'CE',
    numeroDocumento: '004512367',
    nombres: 'Maria',
    apellidos: 'Salazar Quispe',
    fechaNacimiento: '1995-02-20',
    fechaIngreso: '2024-01-08',
    cargo: 'Almacenero',
    area: 'Almacen',
    email: 'maria.salazar@titanglobal.example',
    telefono: '999000003',
    estado: 'Activo',
    fechaCese: null,
    motivoCese: null,
  },
  {
    id: 4,
    legajo: 'EMP-000004',
    tipoDocumento: 'Pasaporte',
    numeroDocumento: 'P1234567',
    nombres: 'Carlos',
    apellidos: 'Diaz Soto',
    fechaNacimiento: '1982-07-30',
    fechaIngreso: '2018-05-02',
    cargo: 'Auxiliar de Logistica',
    area: 'Logistica',
    email: null,
    telefono: null,
    estado: 'Cesado',
    fechaCese: '2026-09-30',
    motivoCese: 'Renuncia voluntaria',
  },
];

function listado(empleado: EmpleadoDetalle): EmpleadoListado {
  return {
    id: empleado.id,
    legajo: empleado.legajo,
    tipoDocumento: empleado.tipoDocumento,
    numeroDocumento: empleado.numeroDocumento,
    nombres: empleado.nombres,
    apellidos: empleado.apellidos,
    cargo: empleado.cargo,
    area: empleado.area,
    fechaIngreso: empleado.fechaIngreso,
    estado: empleado.estado,
  };
}

function respuestaLista(parsed: URL, opciones: OpcionesApiEmpleados): Response {
  if (opciones.errorLista) {
    return Response.json({ StatusCode: 500, Message: 'No se pudo cargar la lista de empleados.' }, { status: 500 });
  }
  if (opciones.listaVacia) {
    return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
  }
  const busqueda = parsed.searchParams.get('busqueda')?.toLowerCase() ?? '';
  const estado = parsed.searchParams.get('estado');
  const pagina = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  const filtrados = empleadosMock.filter((empleado) => {
    const coincideBusqueda = busqueda === ''
      || [empleado.legajo, empleado.nombres, empleado.apellidos, empleado.numeroDocumento]
        .some((campo) => campo.toLowerCase().includes(busqueda));
    const coincideEstado = estado === null || empleado.estado === estado;
    return coincideBusqueda && coincideEstado;
  });
  const inicio = (pagina - 1) * tamano;
  return Response.json({
    items: filtrados.slice(inicio, inicio + tamano).map(listado),
    total: opciones.totalListado ?? filtrados.length,
    pagina,
    tamanoPagina: tamano,
  });
}

function respuestaConteo(parsed: URL, opciones: OpcionesApiEmpleados): Response {
  if (opciones.errorResumen) {
    return Response.json({ StatusCode: 500, Message: 'No se pudo cargar el resumen de empleados.' }, { status: 500 });
  }
  return respuestaLista(parsed, {});
}


function respuestaGuardar(id: number, opciones: OpcionesApiEmpleados): Response {
  if (opciones.errorGuardar === 'duplicado') {
    return Response.json({ StatusCode: 409, Message: 'Ya existe un empleado con el documento DNI 10000001.' }, { status: 409 });
  }
  if (opciones.errorGuardar === 'invalido') {
    return Response.json({ StatusCode: 400, Message: 'El email no tiene un formato válido.' }, { status: 400 });
  }
  return Response.json({ id });
}

function respuestaCese(id: number, opciones: OpcionesApiEmpleados): Response {
  if (opciones.errorCese === 'yaCesado') {
    return Response.json({ StatusCode: 409, Message: `El empleado ${id} ya está cesado.` }, { status: 409 });
  }
  if (opciones.errorCese === 'fecha') {
    return Response.json({ StatusCode: 400, Message: 'La fecha de cese no puede ser anterior a la fecha de ingreso.' }, { status: 400 });
  }
  return Response.json({ id });
}

function respuestaDetalle(id: number, opciones: OpcionesApiEmpleados): Response {
  if (opciones.errorDetalle) {
    return Response.json({ StatusCode: 500, Message: 'No se pudo cargar el detalle del empleado.' }, { status: 500 });
  }
  const empleado = empleadosMock.find((item) => item.id === id);
  if (empleado === undefined) {
    return Response.json({ StatusCode: 404, Message: `El empleado ${id} no existe.` }, { status: 404 });
  }
  return Response.json(empleado);
}

function respuestaMock(parsed: URL, metodo: string, opciones: OpcionesApiEmpleados): Response {
  const ruta = parsed.pathname;
  const cese = /\/empleados\/(\d+)\/cese$/.exec(ruta);
  if (cese) {
    return respuestaCese(Number(cese[1]), opciones);
  }
  const individual = /\/empleados\/(\d+)$/.exec(ruta);
  if (individual && metodo === 'PUT') {
    return respuestaGuardar(Number(individual[1]), opciones);
  }
  if (individual) {
    return respuestaDetalle(Number(individual[1]), opciones);
  }
  if (ruta.endsWith('/empleados') && metodo === 'POST') {
    return respuestaGuardar(opciones.idCreado ?? 9, opciones);
  }
  if (ruta.endsWith('/empleados') && parsed.searchParams.get('tamanoPagina') === '1') {
    return respuestaConteo(parsed, opciones);
  }
  if (ruta.endsWith('/empleados')) {
    return respuestaLista(parsed, opciones);
  }
  return Response.json({ StatusCode: 404, Message: 'Ruta no prevista en la prueba.' }, { status: 404 });
}

export function prepararApiEmpleados(opciones: OpcionesApiEmpleados = {}) {
  const llamadas: LlamadaEmpleados[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    const cuerpo = typeof init.body === 'string' ? JSON.parse(init.body) as unknown : undefined;
    llamadas.push({ url, metodo, cuerpo });
    return respuestaMock(parsed, metodo, opciones);
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

export function renderEmpleados(path = '/modulos/61-empleados') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <EmpleadosPage />
    </MemoryRouter>,
  );
}

// Los conteos del resumen son listados con tamanoPagina=1; el listado de la tabla usa tamanoPagina=10.
export function urlsDeListado(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.includes('/empleados?') && url.endsWith('tamanoPagina=10'));
}

export function urlsDeConteo(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.includes('/empleados?') && url.endsWith('tamanoPagina=1'));
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando empleados…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de empleados…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'EMP-000001' })).toBeInTheDocument();
    expect(urlsDeListado(fetchMock)).toHaveLength(1);
    expect(urlsDeConteo(fetchMock)).toHaveLength(2);
  });
}

// Monta la pantalla con temporizadores falsos para que el temporizador de búsqueda (300 ms) se resuelva sin esperas reales.
export async function montarEmpleadosCargados(fetchMock: ReturnType<typeof vi.fn>, path?: string): Promise<void> {
  vi.useFakeTimers();
  renderEmpleados(path);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock);
}

export function limpiarEmpleados(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
