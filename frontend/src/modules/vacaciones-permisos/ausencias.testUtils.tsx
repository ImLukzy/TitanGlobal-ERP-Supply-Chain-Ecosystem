import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { limpiarSesion, setSesion } from '../../api/sesionStore';
import { SesionProvider } from '../../layout/SesionContext';
import type { EmpleadoListado } from '../empleados/tipos';
import type { AusenciaDetalle, AusenciaListado, SaldoVacacional } from './tipos';
import { VacacionesPermisosPage } from './VacacionesPermisosPage';

export interface LlamadaAusencias {
  url: string;
  metodo: string;
  cuerpo: unknown;
}

export type AccionAusencia = 'registrar' | 'aprobacion' | 'rechazo' | 'cancelacion';

export interface OpcionesApiAusencias {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorResumen?: boolean;
  errorDetalle?: boolean;
  errorSaldo?: boolean;
  totalEmpleados?: number;
  totalListado?: number;
  errorEscritura?: { accion: AccionAusencia; status: number; mensaje: string };
}

const base = {
  fechaSolicitud: '2026-01-10',
  motivo: null,
  decididoPor: null,
  fechaDecision: null,
  motivoRechazo: null,
  fechaCancelacion: null,
};

// Las fechas del mock son muy anteriores (2020) o muy posteriores (2099) a hoy: «ya empezó» y «no ha empezado» no dependen del reloj.
export const ausenciasMock: AusenciaDetalle[] = [
  {
    ...base,
    id: 1,
    numero: 'AUS-000001',
    empleadoId: 1,
    empleadoLegajo: 'EMP-000001',
    empleadoNombre: 'Ana Torres Vega',
    tipo: 'Vacaciones',
    fechaInicio: '2020-02-03',
    fechaFin: '2020-02-14',
    dias: 12,
    estado: 'Aprobada',
    decididoPor: 'rrhh',
    fechaDecision: '2020-01-20T15:00:00Z',
  },
  {
    ...base,
    id: 2,
    numero: 'AUS-000002',
    empleadoId: 1,
    empleadoLegajo: 'EMP-000001',
    empleadoNombre: 'Ana Torres Vega',
    tipo: 'Vacaciones',
    fechaInicio: '2099-01-04',
    fechaFin: '2099-01-15',
    dias: 12,
    estado: 'Aprobada',
    decididoPor: 'rrhh',
    fechaDecision: '2026-10-01T15:00:00Z',
  },
  {
    ...base,
    id: 3,
    numero: 'AUS-000003',
    empleadoId: 1,
    empleadoLegajo: 'EMP-000001',
    empleadoNombre: 'Ana Torres Vega',
    tipo: 'PermisoConGoce',
    fechaInicio: '2099-03-10',
    fechaFin: '2099-03-11',
    dias: 2,
    estado: 'Pendiente',
    motivo: 'Trámite personal',
  },
  {
    ...base,
    id: 4,
    numero: 'AUS-000004',
    empleadoId: 2,
    empleadoLegajo: 'EMP-000002',
    empleadoNombre: 'Luis Rojas Mendoza',
    tipo: 'DescansoMedico',
    fechaInicio: '2020-05-01',
    fechaFin: '2020-05-03',
    dias: 3,
    estado: 'Aprobada',
    motivo: 'Cirugía menor',
    decididoPor: 'admin',
    fechaDecision: '2020-05-01T12:00:00Z',
  },
  {
    ...base,
    id: 5,
    numero: 'AUS-000005',
    empleadoId: 3,
    empleadoLegajo: 'EMP-000003',
    empleadoNombre: 'Maria Salazar Quispe',
    tipo: 'Vacaciones',
    fechaInicio: '2099-06-01',
    fechaFin: '2099-06-10',
    dias: 10,
    estado: 'Rechazada',
    decididoPor: 'rrhh',
    fechaDecision: '2026-10-05T15:00:00Z',
    motivoRechazo: 'Periodo de cierre contable',
  },
  {
    ...base,
    id: 6,
    numero: 'AUS-000006',
    empleadoId: 3,
    empleadoLegajo: 'EMP-000003',
    empleadoNombre: 'Maria Salazar Quispe',
    tipo: 'PermisoSinGoce',
    fechaInicio: '2099-07-01',
    fechaFin: '2099-07-05',
    dias: 5,
    estado: 'Cancelada',
    motivo: 'Viaje familiar',
    fechaCancelacion: '2026-10-20',
  },
  {
    ...base,
    id: 7,
    numero: 'AUS-000007',
    empleadoId: 2,
    empleadoLegajo: 'EMP-000002',
    empleadoNombre: 'Luis Rojas Mendoza',
    tipo: 'Vacaciones',
    fechaInicio: '2099-09-01',
    fechaFin: '2099-09-10',
    dias: 10,
    estado: 'Pendiente',
  },
];

export const saldoMock: SaldoVacacional = {
  empleadoId: 1,
  fechaIngreso: '2021-03-01',
  fecha: '2099-01-04',
  mesesServicio: 70,
  diasDevengados: 30,
  diasGozados: 12,
  diasProgramados: 12,
  saldo: 6.5,
};

const empleadosActivos: EmpleadoListado[] = [
  { id: 1, legajo: 'EMP-000001', tipoDocumento: 'DNI', numeroDocumento: '10000001', nombres: 'Ana', apellidos: 'Torres Vega', cargo: 'Gerente de Ventas', area: 'Ventas', fechaIngreso: '2021-03-01', estado: 'Activo' },
  { id: 2, legajo: 'EMP-000002', tipoDocumento: 'DNI', numeroDocumento: '10000002', nombres: 'Luis', apellidos: 'Rojas Mendoza', cargo: 'Analista de Compras', area: 'Compras', fechaIngreso: '2019-08-16', estado: 'Activo' },
  { id: 3, legajo: 'EMP-000003', tipoDocumento: 'CE', numeroDocumento: '004512367', nombres: 'Maria', apellidos: 'Salazar Quispe', cargo: 'Almacenero', area: 'Almacen', fechaIngreso: '2024-01-08', estado: 'Activo' },
];

function listado(ausencia: AusenciaDetalle): AusenciaListado {
  return {
    id: ausencia.id,
    numero: ausencia.numero,
    empleadoId: ausencia.empleadoId,
    empleadoLegajo: ausencia.empleadoLegajo,
    empleadoNombre: ausencia.empleadoNombre,
    tipo: ausencia.tipo,
    fechaInicio: ausencia.fechaInicio,
    fechaFin: ausencia.fechaFin,
    dias: ausencia.dias,
    estado: ausencia.estado,
    fechaSolicitud: ausencia.fechaSolicitud,
  };
}

function error(status: number, mensaje: string): Response {
  return Response.json({ StatusCode: status, Message: mensaje }, { status });
}

function respuestaLista(parsed: URL, opciones: OpcionesApiAusencias): Response {
  if (opciones.errorLista) {
    return error(500, 'No se pudo cargar la lista de ausencias.');
  }
  if (opciones.listaVacia) {
    return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
  }
  const busqueda = parsed.searchParams.get('busqueda')?.toLowerCase() ?? '';
  const tipo = parsed.searchParams.get('tipo');
  const estado = parsed.searchParams.get('estado');
  const pagina = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  const filtrados = ausenciasMock.filter((ausencia) => {
    const coincideBusqueda = busqueda === ''
      || [ausencia.numero, ausencia.empleadoNombre, ausencia.empleadoLegajo]
        .some((campo) => campo.toLowerCase().includes(busqueda));
    return coincideBusqueda && (tipo === null || ausencia.tipo === tipo) && (estado === null || ausencia.estado === estado);
  });
  const inicio = (pagina - 1) * tamano;
  return Response.json({
    items: filtrados.slice(inicio, inicio + tamano).map(listado),
    total: opciones.totalListado ?? filtrados.length,
    pagina,
    tamanoPagina: tamano,
  });
}

function respuestaDetalle(id: number, opciones: OpcionesApiAusencias): Response {
  if (opciones.errorDetalle) {
    return error(500, 'No se pudo cargar el detalle de la ausencia.');
  }
  const ausencia = ausenciasMock.find((item) => item.id === id);
  if (ausencia === undefined) {
    return error(404, `La ausencia ${id} no existe.`);
  }
  return Response.json(ausencia);
}

function respuestaSaldo(parsed: URL, empleadoId: number, opciones: OpcionesApiAusencias): Response {
  if (opciones.errorSaldo) {
    return error(404, `El empleado ${empleadoId} no existe.`);
  }
  const fecha = parsed.searchParams.get('fecha') ?? '2026-11-22';
  return Response.json({ ...saldoMock, empleadoId, fecha });
}

function respuestaMock(parsed: URL, metodo: string, opciones: OpcionesApiAusencias): Response {
  const ruta = parsed.pathname;
  if (ruta.endsWith('/empleados')) {
    return Response.json({
      items: empleadosActivos,
      total: opciones.totalEmpleados ?? empleadosActivos.length,
      pagina: 1,
      tamanoPagina: 100,
    });
  }
  if (ruta.endsWith('/ausencias/resumen')) {
    if (opciones.errorResumen) {
      return error(500, 'No se pudo cargar el resumen de ausencias.');
    }
    return Response.json({ pendientes: 2, ausentesHoy: 1, vacacionesHoy: 0, descansosMedicosHoy: 1, aprobadasMes: 3 });
  }
  const saldo = /\/ausencias\/saldo-vacacional\/(\d+)$/.exec(ruta);
  if (saldo) {
    return respuestaSaldo(parsed, Number(saldo[1]), opciones);
  }
  const accion = /\/ausencias\/(\d+)\/(aprobacion|rechazo|cancelacion)$/.exec(ruta);
  if (accion) {
    if (opciones.errorEscritura?.accion === accion[2]) {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json({ id: Number(accion[1]) });
  }
  const individual = /\/ausencias\/(\d+)$/.exec(ruta);
  if (individual) {
    return respuestaDetalle(Number(individual[1]), opciones);
  }
  if (ruta.endsWith('/ausencias') && metodo === 'POST') {
    if (opciones.errorEscritura?.accion === 'registrar') {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json({ id: 8, numero: 'AUS-000008' });
  }
  if (ruta.endsWith('/ausencias')) {
    return respuestaLista(parsed, opciones);
  }
  return error(404, 'Ruta no prevista en la prueba.');
}

export function prepararApiAusencias(opciones: OpcionesApiAusencias = {}) {
  const llamadas: LlamadaAusencias[] = [];
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

// Los roles viajan en la sesión: sin RRHH ni ADMIN, Aprobar y Rechazar quedan deshabilitados.
export function renderAusencias(roles: string[] = ['RRHH'], path = '/modulos/65-vacaciones-permisos') {
  setSesion({
    token: 'token-de-prueba',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    usuario: { id: 1, nombreUsuario: 'rrhh', nombreCompleto: 'Usuario de prueba' },
    roles,
  });
  return render(
    <SesionProvider>
      <MemoryRouter initialEntries={[path]}>
        <VacacionesPermisosPage />
      </MemoryRouter>
    </SesionProvider>,
  );
}

export function urlsDeListado(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.includes('/ausencias?'));
}

export function urlsDeResumen(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.endsWith('/ausencias/resumen'));
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando ausencias…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de ausencias…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'AUS-000001' })).toBeInTheDocument();
    expect(urlsDeListado(fetchMock)).toHaveLength(1);
    expect(urlsDeResumen(fetchMock)).toHaveLength(1);
  });
}

// Monta la pantalla con temporizadores falsos para que el temporizador de búsqueda (300 ms) se resuelva sin esperas reales.
export async function montarAusenciasCargadas(
  fetchMock: ReturnType<typeof vi.fn>,
  roles: string[] = ['RRHH'],
): Promise<void> {
  vi.useFakeTimers();
  renderAusencias(roles);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock);
}

// Para listas vacías o con error no hay filas que esperar: solo se espera el fin del temporizador.
export async function montarSinFilas(roles: string[] = ['RRHH']): Promise<void> {
  vi.useFakeTimers();
  renderAusencias(roles);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
}

export function limpiarAusencias(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  limpiarSesion();
}
