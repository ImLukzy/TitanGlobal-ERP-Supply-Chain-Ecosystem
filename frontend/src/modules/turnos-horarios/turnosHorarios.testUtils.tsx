import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { limpiarSesion, setSesion } from '../../api/sesionStore';
import { SesionProvider } from '../../layout/SesionContext';
import type { EmpleadoListado } from '../empleados/tipos';
import { sumarDias } from './presentacion';
import type { AsignacionTurnoListado, DiaProgramado, ResumenTurnos, TurnoDetalle, TurnoListado } from './tipos';
import { TurnosHorariosPage } from './TurnosHorariosPage';

export interface LlamadaTurnos {
  url: string;
  metodo: string;
  cuerpo: unknown;
}

export type AccionEscrituraTurnos =
  | 'crear-turno'
  | 'editar-turno'
  | 'desactivar-turno'
  | 'activar-turno'
  | 'crear-asignacion'
  | 'finalizar-asignacion'
  | 'eliminar-asignacion';

export interface OpcionesApiTurnos {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorResumen?: boolean;
  errorProgramacion?: boolean;
  totalEmpleados?: number;
  totalListado?: number;
  errorEscritura?: { accion: AccionEscrituraTurnos; status: number; mensaje: string };
}

// Los turnos replican el seed de la spec backend del 64 (DIA, NOCHE, ROT4X2, SABADO inactivo) y añaden TARDE sin asignaciones para editar.
export const turnosMock: TurnoDetalle[] = [
  {
    id: 1,
    codigo: 'DIA',
    nombre: 'Turno dia',
    tipo: 'Fijo',
    horaEntrada: '08:00:00',
    horaSalida: '17:00:00',
    cruzaMedianoche: false,
    toleranciaMinutos: 10,
    diasLaborables: [1, 2, 3, 4, 5],
    diasTrabajo: null,
    diasDescanso: null,
    fechaInicioCiclo: null,
    activo: true,
    totalAsignaciones: 3,
    empleadosConTurnoHoy: 3,
  },
  {
    id: 2,
    codigo: 'NOCHE',
    nombre: 'Turno noche',
    tipo: 'Fijo',
    horaEntrada: '22:00:00',
    horaSalida: '06:00:00',
    cruzaMedianoche: true,
    toleranciaMinutos: 5,
    diasLaborables: [1, 2, 3, 4, 5, 6],
    diasTrabajo: null,
    diasDescanso: null,
    fechaInicioCiclo: null,
    activo: true,
    totalAsignaciones: 1,
    empleadosConTurnoHoy: 0,
  },
  {
    id: 3,
    codigo: 'ROT4X2',
    nombre: 'Rotativo 4x2',
    tipo: 'Rotativo',
    horaEntrada: '07:00:00',
    horaSalida: '19:00:00',
    cruzaMedianoche: false,
    toleranciaMinutos: 15,
    diasLaborables: [],
    diasTrabajo: 4,
    diasDescanso: 2,
    fechaInicioCiclo: '2026-01-05',
    activo: true,
    totalAsignaciones: 1,
    empleadosConTurnoHoy: 1,
  },
  {
    id: 4,
    codigo: 'SABADO',
    nombre: 'Sabado medio dia',
    tipo: 'Fijo',
    horaEntrada: '08:00:00',
    horaSalida: '13:00:00',
    cruzaMedianoche: false,
    toleranciaMinutos: 0,
    diasLaborables: [6],
    diasTrabajo: null,
    diasDescanso: null,
    fechaInicioCiclo: null,
    activo: false,
    totalAsignaciones: 0,
    empleadosConTurnoHoy: 0,
  },
  {
    id: 5,
    codigo: 'TARDE',
    nombre: 'Turno tarde',
    tipo: 'Fijo',
    horaEntrada: '14:00:00',
    horaSalida: '22:00:00',
    cruzaMedianoche: false,
    toleranciaMinutos: 10,
    diasLaborables: [1, 2, 3, 4, 5],
    diasTrabajo: null,
    diasDescanso: null,
    fechaInicioCiclo: null,
    activo: true,
    totalAsignaciones: 0,
    empleadosConTurnoHoy: 0,
  },
];

export const resumenMock: ResumenTurnos = {
  totalTurnos: 5,
  activos: 4,
  fijos: 4,
  rotativos: 1,
  empleadosConTurnoHoy: 3,
  empleadosActivosSinTurnoHoy: 2,
};

export const asignacionesMock: AsignacionTurnoListado[] = [
  { id: 1, empleadoId: 1, empleadoLegajo: 'EMP-000001', empleadoNombre: 'Ana Torres Vega', turnoId: 1, turnoCodigo: 'DIA', turnoNombre: 'Turno dia', fechaInicio: '2021-03-01', fechaFin: null, estado: 'Vigente' },
  { id: 2, empleadoId: 2, empleadoLegajo: 'EMP-000002', empleadoNombre: 'Luis Rojas Mendoza', turnoId: 2, turnoCodigo: 'NOCHE', turnoNombre: 'Turno noche', fechaInicio: '2019-08-16', fechaFin: '2025-12-31', estado: 'Finalizada' },
  { id: 3, empleadoId: 2, empleadoLegajo: 'EMP-000002', empleadoNombre: 'Luis Rojas Mendoza', turnoId: 3, turnoCodigo: 'ROT4X2', turnoNombre: 'Rotativo 4x2', fechaInicio: '2026-01-01', fechaFin: null, estado: 'Vigente' },
  { id: 4, empleadoId: 3, empleadoLegajo: 'EMP-000003', empleadoNombre: 'Maria Salazar Quispe', turnoId: 1, turnoCodigo: 'DIA', turnoNombre: 'Turno dia', fechaInicio: '2024-01-08', fechaFin: null, estado: 'Vigente' },
  { id: 5, empleadoId: 1, empleadoLegajo: 'EMP-000001', empleadoNombre: 'Ana Torres Vega', turnoId: 5, turnoCodigo: 'TARDE', turnoNombre: 'Turno tarde', fechaInicio: '2099-01-01', fechaFin: null, estado: 'Futura' },
];

const empleadosActivos: EmpleadoListado[] = [
  { id: 1, legajo: 'EMP-000001', tipoDocumento: 'DNI', numeroDocumento: '10000001', nombres: 'Ana', apellidos: 'Torres Vega', cargo: 'Gerente de Ventas', area: 'Ventas', fechaIngreso: '2021-03-01', estado: 'Activo' },
  { id: 2, legajo: 'EMP-000002', tipoDocumento: 'DNI', numeroDocumento: '10000002', nombres: 'Luis', apellidos: 'Rojas Mendoza', cargo: 'Analista de Compras', area: 'Compras', fechaIngreso: '2019-08-16', estado: 'Activo' },
  { id: 3, legajo: 'EMP-000003', tipoDocumento: 'CE', numeroDocumento: '004512367', nombres: 'Maria', apellidos: 'Salazar Quispe', cargo: 'Almacenero', area: 'Almacen', fechaIngreso: '2024-01-08', estado: 'Activo' },
];

function listado(turno: TurnoDetalle): TurnoListado {
  const { totalAsignaciones, empleadosConTurnoHoy, ...resto } = turno;
  void totalAsignaciones;
  void empleadosConTurnoHoy;
  return resto;
}

function error(status: number, mensaje: string): Response {
  return Response.json({ StatusCode: status, Message: mensaje }, { status });
}

function respuestaListaTurnos(parsed: URL, opciones: OpcionesApiTurnos): Response {
  if (opciones.errorLista) {
    return error(500, 'No se pudo cargar la lista de turnos.');
  }
  if (opciones.listaVacia) {
    return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
  }
  const busqueda = parsed.searchParams.get('busqueda')?.toLowerCase() ?? '';
  const tipo = parsed.searchParams.get('tipo');
  const activo = parsed.searchParams.get('activo');
  const pagina = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  const filtrados = turnosMock.filter((turno) => {
    const coincideBusqueda = busqueda === ''
      || [turno.codigo, turno.nombre].some((campo) => campo.toLowerCase().includes(busqueda));
    return coincideBusqueda
      && (tipo === null || turno.tipo === tipo)
      && (activo === null || String(turno.activo) === activo);
  });
  const inicio = (pagina - 1) * tamano;
  return Response.json({
    items: filtrados.slice(inicio, inicio + tamano).map(listado),
    total: opciones.totalListado ?? filtrados.length,
    pagina,
    tamanoPagina: tamano,
  });
}

function respuestaListaAsignaciones(parsed: URL, opciones: OpcionesApiTurnos): Response {
  if (opciones.errorLista) {
    return error(500, 'No se pudo cargar la lista de asignaciones.');
  }
  if (opciones.listaVacia) {
    return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
  }
  const empleadoId = parsed.searchParams.get('empleadoId');
  const turnoId = parsed.searchParams.get('turnoId');
  const estado = parsed.searchParams.get('estado');
  const pagina = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  const filtradas = asignacionesMock.filter((asignacion) => (
    (empleadoId === null || String(asignacion.empleadoId) === empleadoId)
    && (turnoId === null || String(asignacion.turnoId) === turnoId)
    && (estado === null || asignacion.estado === estado)
  ));
  const inicio = (pagina - 1) * tamano;
  return Response.json({
    items: filtradas.slice(inicio, inicio + tamano),
    total: opciones.totalListado ?? filtradas.length,
    pagina,
    tamanoPagina: tamano,
  });
}

// 14 días desde «desde»: 5 laborables, 2 de descanso, uno cesado, uno sin turno y el resto laborables.
function respuestaProgramacion(parsed: URL, opciones: OpcionesApiTurnos): Response {
  if (opciones.errorProgramacion) {
    return error(500, 'No se pudo cargar la programación del empleado.');
  }
  const desde = parsed.searchParams.get('desde') ?? '2026-01-01';
  const dias: DiaProgramado[] = [];
  for (let indice = 0; indice < 14; indice += 1) {
    const base = { fecha: sumarDias(desde, indice), cruzaMedianoche: false };
    if (indice === 5 || indice === 6) {
      dias.push({ ...base, turnoId: 1, turnoCodigo: 'DIA', laborable: false, horaEntrada: null, horaSalida: null, toleranciaMinutos: null, observacion: 'Descanso' });
    } else if (indice === 7) {
      dias.push({ ...base, turnoId: 1, turnoCodigo: 'DIA', laborable: false, horaEntrada: null, horaSalida: null, toleranciaMinutos: null, observacion: 'Cesado' });
    } else if (indice === 8) {
      dias.push({ ...base, turnoId: null, turnoCodigo: null, laborable: false, horaEntrada: null, horaSalida: null, toleranciaMinutos: null, observacion: 'SinTurno' });
    } else {
      dias.push({ ...base, turnoId: 1, turnoCodigo: 'DIA', laborable: true, horaEntrada: '08:00:00', horaSalida: '17:00:00', toleranciaMinutos: 10, observacion: null });
    }
  }
  return Response.json(dias);
}

function respuestaTurnoIndividual(id: number, metodo: string, cuerpo: unknown, opciones: OpcionesApiTurnos): Response {
  if (metodo === 'PUT') {
    if (opciones.errorEscritura?.accion === 'editar-turno') {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json({ id });
  }
  void cuerpo;
  const turno = turnosMock.find((item) => item.id === id);
  if (turno === undefined) {
    return error(404, `El turno ${id} no existe.`);
  }
  return Response.json(turno);
}

function respuestaMock(parsed: URL, metodo: string, cuerpo: unknown, opciones: OpcionesApiTurnos): Response {
  const ruta = parsed.pathname;
  if (ruta.endsWith('/empleados')) {
    return Response.json({
      items: empleadosActivos,
      total: opciones.totalEmpleados ?? empleadosActivos.length,
      pagina: 1,
      tamanoPagina: 100,
    });
  }
  if (ruta.endsWith('/turnos/resumen')) {
    return opciones.errorResumen ? error(500, 'No se pudo cargar el resumen de turnos.') : Response.json(resumenMock);
  }
  const cambioActivo = /\/turnos\/(\d+)\/(desactivacion|activacion)$/.exec(ruta);
  if (cambioActivo) {
    const accion = cambioActivo[2] === 'desactivacion' ? 'desactivar-turno' : 'activar-turno';
    if (opciones.errorEscritura?.accion === accion) {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json({ id: Number(cambioActivo[1]) });
  }
  const turnoIndividual = /\/turnos\/(\d+)$/.exec(ruta);
  if (turnoIndividual) {
    return respuestaTurnoIndividual(Number(turnoIndividual[1]), metodo, cuerpo, opciones);
  }
  if (ruta.endsWith('/turnos') && metodo === 'POST') {
    if (opciones.errorEscritura?.accion === 'crear-turno') {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json({ id: 6 });
  }
  if (ruta.endsWith('/turnos')) {
    return respuestaListaTurnos(parsed, opciones);
  }
  if (ruta.endsWith('/asignaciones-turno/programacion')) {
    return respuestaProgramacion(parsed, opciones);
  }
  const finalizacion = /\/asignaciones-turno\/(\d+)\/finalizacion$/.exec(ruta);
  if (finalizacion) {
    if (opciones.errorEscritura?.accion === 'finalizar-asignacion') {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json({ id: Number(finalizacion[1]) });
  }
  if (/\/asignaciones-turno\/\d+$/.test(ruta) && metodo === 'DELETE') {
    if (opciones.errorEscritura?.accion === 'eliminar-asignacion') {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return new Response(null, { status: 204 });
  }
  if (ruta.endsWith('/asignaciones-turno') && metodo === 'POST') {
    if (opciones.errorEscritura?.accion === 'crear-asignacion') {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json({ id: 6 });
  }
  if (ruta.endsWith('/asignaciones-turno')) {
    return respuestaListaAsignaciones(parsed, opciones);
  }
  return error(404, 'Ruta no prevista en la prueba.');
}

export function prepararApiTurnos(opciones: OpcionesApiTurnos = {}) {
  const llamadas: LlamadaTurnos[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    const cuerpo = typeof init.body === 'string' ? JSON.parse(init.body) as unknown : undefined;
    llamadas.push({ url, metodo, cuerpo });
    return respuestaMock(parsed, metodo, cuerpo, opciones);
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

// Los roles viajan en la sesión: sin RRHH ni ADMIN los botones de escritura quedan deshabilitados.
export function renderTurnos(roles: string[] = ['RRHH'], path = '/modulos/64-turnos-horarios') {
  setSesion({
    token: 'token-de-prueba',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    usuario: { id: 1, nombreUsuario: 'rrhh', nombreCompleto: 'Usuario de prueba' },
    roles,
  });
  return render(
    <SesionProvider>
      <MemoryRouter initialEntries={[path]}>
        <TurnosHorariosPage />
      </MemoryRouter>
    </SesionProvider>,
  );
}

function urlsQueTerminanEn(fetchMock: ReturnType<typeof vi.fn>, fragmento: string, final: string): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.includes(fragmento) && url.endsWith(final));
}

// El listado de la tabla usa tamanoPagina=10; los selectores de turnos usan tamanoPagina=100.
export function urlsDeListadoTurnos(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueTerminanEn(fetchMock, '/turnos?', 'tamanoPagina=10');
}

export function urlsDeListadoAsignaciones(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueTerminanEn(fetchMock, '/asignaciones-turno?', 'tamanoPagina=10');
}

export async function esperarCargaTurnos(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando turnos…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de turnos…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'DIA' })).toBeInTheDocument();
    expect(urlsDeListadoTurnos(fetchMock)).toHaveLength(1);
  });
}

export async function esperarCargaAsignaciones(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando asignaciones…')).not.toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'EMP-000001 · Ana Torres Vega' })).toHaveLength(2);
    expect(screen.getByRole('option', { name: 'EMP-000001 · Torres Vega, Ana' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'DIA · Turno dia' })).toBeInTheDocument();
    expect(urlsDeListadoAsignaciones(fetchMock)).toHaveLength(1);
  });
}

// Monta la pantalla con temporizadores falsos para que el temporizador de búsqueda (300 ms) se resuelva sin esperas reales.
async function montar(roles: string[], path: string): Promise<void> {
  vi.useFakeTimers();
  renderTurnos(roles, path);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
}

export async function montarTurnosCargados(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  await montar(roles, '/modulos/64-turnos-horarios');
  await esperarCargaTurnos(fetchMock);
}

export async function montarAsignacionesCargadas(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  await montar(roles, '/modulos/64-turnos-horarios?vista=asignaciones');
  await esperarCargaAsignaciones(fetchMock);
}

// Para listas vacías o con error no hay filas que esperar: solo se espera el fin del temporizador.
export async function montarSinFilas(roles: string[] = ['RRHH'], path = '/modulos/64-turnos-horarios'): Promise<void> {
  await montar(roles, path);
}

export function limpiarTurnos(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  limpiarSesion();
}
