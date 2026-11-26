import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { limpiarSesion, setSesion } from '../../api/sesionStore';
import { SesionProvider } from '../../layout/SesionContext';
import { formateadorMoneda } from '../../lib/formato';
import type { EmpleadoListado } from '../empleados/tipos';
import { AdelantosPrestamosPage } from './AdelantosPrestamosPage';
import type { CapacidadPrestamo, CuotaPrestamo, PrestamoDetalle, PrestamoListado } from './tipos';

// El formateador usa espacios de no separación; las pruebas comparan con espacios normales.
export function moneda(importe: number): string {
  return formateadorMoneda.format(importe).replace(/\u00a0/g, ' ');
}

export interface LlamadaPrestamos {
  url: string;
  metodo: string;
  cuerpo: unknown;
}

export type AccionPrestamo = 'registrar' | 'anulacion';

export interface OpcionesApiPrestamos {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorResumen?: boolean;
  errorDetalle?: boolean;
  errorCapacidad?: boolean;
  totalEmpleados?: number;
  totalListado?: number;
  errorEscritura?: { accion: AccionPrestamo; status: number; mensaje: string };
}

function cuota(id: number, numero: number, mes: number, monto: number, descontado: number): CuotaPrestamo {
  return {
    id,
    numeroCuota: numero,
    anio: 2026,
    mes,
    monto,
    montoDescontado: descontado,
    estado: descontado >= monto ? 'Descontada' : 'Pendiente',
  };
}

const base = {
  motivo: 'Gastos médicos',
  primerAnio: 2026,
  primerMes: 9,
  fechaAnulacion: null,
  motivoAnulacion: null,
};

export const prestamosMock: PrestamoDetalle[] = [
  {
    ...base,
    id: 1,
    numero: 'PRE-000001',
    empleadoId: 1,
    empleadoLegajo: 'EMP-000001',
    empleadoNombre: 'Ana Torres Vega',
    tipo: 'Prestamo',
    monto: 6000,
    numeroCuotas: 6,
    fechaOtorgamiento: '2026-09-01',
    saldo: 4000,
    estado: 'Activo',
    cuotas: [
      cuota(1, 1, 9, 1000, 1000),
      cuota(2, 2, 10, 1000, 1000),
      cuota(3, 3, 11, 1000, 0),
      cuota(4, 4, 12, 1000, 0),
      cuota(5, 5, 1, 1000, 0),
      cuota(6, 6, 2, 1000, 0),
    ],
  },
  {
    ...base,
    id: 2,
    numero: 'PRE-000002',
    empleadoId: 2,
    empleadoLegajo: 'EMP-000002',
    empleadoNombre: 'Luis Rojas Mendoza',
    tipo: 'Adelanto',
    monto: 800,
    numeroCuotas: 1,
    fechaOtorgamiento: '2026-08-10',
    primerMes: 8,
    saldo: 0,
    estado: 'Cancelado',
    cuotas: [cuota(7, 1, 8, 800, 800)],
  },
  {
    ...base,
    id: 3,
    numero: 'PRE-000003',
    empleadoId: 3,
    empleadoLegajo: 'EMP-000003',
    empleadoNombre: 'Maria Salazar Quispe',
    tipo: 'Prestamo',
    monto: 1200,
    numeroCuotas: 3,
    fechaOtorgamiento: '2026-07-01',
    primerMes: 7,
    saldo: 0,
    estado: 'Anulado',
    fechaAnulacion: '2026-07-05',
    motivoAnulacion: 'Error de carga',
    cuotas: [cuota(8, 1, 7, 400, 0), cuota(9, 2, 8, 400, 0), cuota(10, 3, 9, 400, 0)],
  },
  {
    ...base,
    id: 4,
    numero: 'PRE-000004',
    empleadoId: 1,
    empleadoLegajo: 'EMP-000001',
    empleadoNombre: 'Ana Torres Vega',
    tipo: 'Adelanto',
    monto: 500,
    numeroCuotas: 1,
    fechaOtorgamiento: '2026-11-02',
    primerMes: 11,
    saldo: 500,
    estado: 'Activo',
    cuotas: [cuota(11, 1, 11, 500, 0)],
  },
];

const empleadosActivos: EmpleadoListado[] = [
  { id: 1, legajo: 'EMP-000001', tipoDocumento: 'DNI', numeroDocumento: '10000001', nombres: 'Ana', apellidos: 'Torres Vega', cargo: 'Gerente de Ventas', area: 'Ventas', fechaIngreso: '2021-03-01', estado: 'Activo' },
  { id: 2, legajo: 'EMP-000002', tipoDocumento: 'DNI', numeroDocumento: '10000002', nombres: 'Luis', apellidos: 'Rojas Mendoza', cargo: 'Analista de Compras', area: 'Compras', fechaIngreso: '2019-08-16', estado: 'Activo' },
  { id: 3, legajo: 'EMP-000003', tipoDocumento: 'CE', numeroDocumento: '004512367', nombres: 'Maria', apellidos: 'Salazar Quispe', cargo: 'Almacenero', area: 'Almacen', fechaIngreso: '2024-01-08', estado: 'Activo' },
];

// Empleado 1: sueldo 9500 (máximo 2479.50). Empleado 2: sueldo 4200 (máximo 1096.20) con 1000 ya comprometidos en 02/2030. Empleado 3: sin contrato.
function capacidadDe(empleadoId: number, fecha: string): CapacidadPrestamo {
  if (empleadoId === 2) {
    return {
      empleadoId,
      fecha,
      sueldoVigente: 4200,
      netoEstimado: 3654,
      cuotaMaxima: 1096.2,
      cuotasComprometidasPorMes: [{ anio: 2030, mes: 2, monto: 1000 }],
    };
  }
  if (empleadoId === 3) {
    return { empleadoId, fecha, sueldoVigente: 0, netoEstimado: 0, cuotaMaxima: 0, cuotasComprometidasPorMes: [] };
  }
  return { empleadoId, fecha, sueldoVigente: 9500, netoEstimado: 8265, cuotaMaxima: 2479.5, cuotasComprometidasPorMes: [] };
}

function listado(prestamo: PrestamoDetalle): PrestamoListado {
  return {
    id: prestamo.id,
    numero: prestamo.numero,
    empleadoId: prestamo.empleadoId,
    empleadoLegajo: prestamo.empleadoLegajo,
    empleadoNombre: prestamo.empleadoNombre,
    tipo: prestamo.tipo,
    monto: prestamo.monto,
    numeroCuotas: prestamo.numeroCuotas,
    fechaOtorgamiento: prestamo.fechaOtorgamiento,
    saldo: prestamo.saldo,
    estado: prestamo.estado,
  };
}

function error(status: number, mensaje: string): Response {
  return Response.json({ StatusCode: status, Message: mensaje }, { status });
}

function respuestaLista(parsed: URL, opciones: OpcionesApiPrestamos): Response {
  if (opciones.errorLista) {
    return error(500, 'No se pudo cargar la lista de préstamos.');
  }
  if (opciones.listaVacia) {
    return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
  }
  const busqueda = parsed.searchParams.get('busqueda')?.toLowerCase() ?? '';
  const tipo = parsed.searchParams.get('tipo');
  const estado = parsed.searchParams.get('estado');
  const pagina = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  const filtrados = prestamosMock.filter((prestamo) => {
    const coincideBusqueda = busqueda === ''
      || [prestamo.numero, prestamo.empleadoNombre, prestamo.empleadoLegajo]
        .some((campo) => campo.toLowerCase().includes(busqueda));
    return coincideBusqueda && (tipo === null || prestamo.tipo === tipo) && (estado === null || prestamo.estado === estado);
  });
  const inicio = (pagina - 1) * tamano;
  return Response.json({
    items: filtrados.slice(inicio, inicio + tamano).map(listado),
    total: opciones.totalListado ?? filtrados.length,
    pagina,
    tamanoPagina: tamano,
  });
}

function respuestaDetalle(id: number, opciones: OpcionesApiPrestamos): Response {
  if (opciones.errorDetalle) {
    return error(500, 'No se pudo cargar el detalle del préstamo.');
  }
  const prestamo = prestamosMock.find((item) => item.id === id);
  if (prestamo === undefined) {
    return error(404, `El préstamo ${id} no existe.`);
  }
  return Response.json(prestamo);
}

function respuestaMock(parsed: URL, metodo: string, opciones: OpcionesApiPrestamos): Response {
  const ruta = parsed.pathname;
  if (ruta.endsWith('/empleados')) {
    return Response.json({
      items: empleadosActivos,
      total: opciones.totalEmpleados ?? empleadosActivos.length,
      pagina: 1,
      tamanoPagina: 100,
    });
  }
  if (ruta.endsWith('/prestamos/resumen')) {
    if (opciones.errorResumen) {
      return error(500, 'No se pudo cargar el resumen de préstamos.');
    }
    return Response.json({ activos: 2, saldoPendienteTotal: 4500, otorgadoMes: 500, cuotasDelMes: 1500 });
  }
  const capacidad = /\/prestamos\/capacidad\/(\d+)$/.exec(ruta);
  if (capacidad) {
    if (opciones.errorCapacidad) {
      return error(404, `El empleado ${capacidad[1]} no existe.`);
    }
    return Response.json(capacidadDe(Number(capacidad[1]), parsed.searchParams.get('fecha') ?? '2026-11-22'));
  }
  const anulacion = /\/prestamos\/(\d+)\/anulacion$/.exec(ruta);
  if (anulacion) {
    if (opciones.errorEscritura?.accion === 'anulacion') {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json({ id: Number(anulacion[1]) });
  }
  const individual = /\/prestamos\/(\d+)$/.exec(ruta);
  if (individual) {
    return respuestaDetalle(Number(individual[1]), opciones);
  }
  if (ruta.endsWith('/prestamos') && metodo === 'POST') {
    if (opciones.errorEscritura?.accion === 'registrar') {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json({ id: 5, numero: 'PRE-000005' });
  }
  if (ruta.endsWith('/prestamos')) {
    return respuestaLista(parsed, opciones);
  }
  return error(404, 'Ruta no prevista en la prueba.');
}

export function prepararApiPrestamos(opciones: OpcionesApiPrestamos = {}) {
  const llamadas: LlamadaPrestamos[] = [];
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

// Los roles viajan en la sesión: sin RRHH ni ADMIN, «Nuevo préstamo» y «Anular» quedan deshabilitados.
export function renderPrestamos(roles: string[] = ['RRHH'], path = '/modulos/67-adelantos-prestamos') {
  setSesion({
    token: 'token-de-prueba',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    usuario: { id: 1, nombreUsuario: 'rrhh', nombreCompleto: 'Usuario de prueba' },
    roles,
  });
  return render(
    <SesionProvider>
      <MemoryRouter initialEntries={[path]}>
        <AdelantosPrestamosPage />
      </MemoryRouter>
    </SesionProvider>,
  );
}

export function urlsDeListado(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.includes('/prestamos?'));
}

export function urlsDeResumen(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.endsWith('/prestamos/resumen'));
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando préstamos…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de préstamos…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'PRE-000001' })).toBeInTheDocument();
    expect(urlsDeListado(fetchMock)).toHaveLength(1);
    expect(urlsDeResumen(fetchMock)).toHaveLength(1);
  });
}

// Monta la pantalla con temporizadores falsos para que el temporizador de búsqueda (300 ms) se resuelva sin esperas reales.
export async function montarPrestamosCargados(
  fetchMock: ReturnType<typeof vi.fn>,
  roles: string[] = ['RRHH'],
): Promise<void> {
  vi.useFakeTimers();
  renderPrestamos(roles);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock);
}

// Para listas vacías o con error no hay filas que esperar: solo se espera el fin del temporizador.
export async function montarSinFilas(roles: string[] = ['RRHH']): Promise<void> {
  vi.useFakeTimers();
  renderPrestamos(roles);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
}

export function limpiarPrestamos(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  limpiarSesion();
}
