import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { limpiarSesion, setSesion } from '../../api/sesionStore';
import { SesionProvider } from '../../layout/SesionContext';
import type { EmpleadoListado } from '../empleados/tipos';
import { AsistenciaBiometricaPage } from './AsistenciaBiometricaPage';
import type { JornadaAsistencia, Marcacion, ResultadoCalculo, ResultadoImportacion, ResumenAsistencia } from './tipos';

export interface LlamadaAsistencia {
  url: string;
  metodo: string;
  cuerpo: unknown;
}

export type AccionEscrituraAsistencia =
  | 'registrar-marcacion'
  | 'importar'
  | 'eliminar-marcacion'
  | 'calcular'
  | 'aprobar'
  | 'rechazar';

export interface OpcionesApiAsistencia {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorResumen?: boolean;
  errorMarcacionesDia?: boolean;
  totalListado?: number;
  resultadoImportacion?: ResultadoImportacion;
  errorEscritura?: { accion: AccionEscrituraAsistencia; status: number; mensaje: string };
}

// Los instantes viajan en UTC; el turno DIA (08:00 a 17:00 en Perú, UTC−5) entra a las 13:00Z y sale a las 22:00Z.
export const jornadasMock: JornadaAsistencia[] = [
  {
    id: 1, empleadoId: 1, empleadoLegajo: 'EMP-000001', empleadoNombre: 'Ana Torres Vega', fecha: '2026-11-03', turnoId: 1, turnoCodigo: 'DIA',
    horaEntradaProgramada: '08:00:00', horaSalidaProgramada: '17:00:00', toleranciaMinutos: 10,
    entrada: '2026-11-03T13:15:00Z', salida: '2026-11-03T22:00:00Z', minutosTrabajados: 525, minutosTardanza: 15, minutosExtra: 0,
    estado: 'Tardanza', estadoHorasExtra: 'NoAplica', decididoPor: null, fechaDecision: null, motivoRechazo: null,
  },
  {
    id: 2, empleadoId: 2, empleadoLegajo: 'EMP-000002', empleadoNombre: 'Luis Rojas Mendoza', fecha: '2026-11-04', turnoId: 1, turnoCodigo: 'DIA',
    horaEntradaProgramada: '08:00:00', horaSalidaProgramada: '17:00:00', toleranciaMinutos: 10,
    entrada: '2026-11-04T12:55:00Z', salida: '2026-11-04T23:30:00Z', minutosTrabajados: 635, minutosTardanza: 0, minutosExtra: 90,
    estado: 'Asistencia', estadoHorasExtra: 'Pendiente', decididoPor: null, fechaDecision: null, motivoRechazo: null,
  },
  {
    id: 3, empleadoId: 3, empleadoLegajo: 'EMP-000003', empleadoNombre: 'Maria Salazar Quispe', fecha: '2026-11-05', turnoId: 1, turnoCodigo: 'DIA',
    horaEntradaProgramada: '08:00:00', horaSalidaProgramada: '17:00:00', toleranciaMinutos: 10,
    entrada: '2026-11-05T13:00:00Z', salida: null, minutosTrabajados: 0, minutosTardanza: 0, minutosExtra: 0,
    estado: 'SinMarcacion', estadoHorasExtra: 'NoAplica', decididoPor: null, fechaDecision: null, motivoRechazo: null,
  },
  {
    id: 4, empleadoId: 1, empleadoLegajo: 'EMP-000001', empleadoNombre: 'Ana Torres Vega', fecha: '2026-11-06', turnoId: 1, turnoCodigo: 'DIA',
    horaEntradaProgramada: '08:00:00', horaSalidaProgramada: '17:00:00', toleranciaMinutos: 10,
    entrada: '2026-11-06T13:00:00Z', salida: '2026-11-07T00:00:00Z', minutosTrabajados: 660, minutosTardanza: 0, minutosExtra: 60,
    estado: 'Asistencia', estadoHorasExtra: 'Aprobada', decididoPor: 'rrhh', fechaDecision: '2026-11-07T15:30:00Z', motivoRechazo: null,
  },
  {
    id: 5, empleadoId: 2, empleadoLegajo: 'EMP-000002', empleadoNombre: 'Luis Rojas Mendoza', fecha: '2026-11-02', turnoId: 2, turnoCodigo: 'NOCHE',
    horaEntradaProgramada: '22:00:00', horaSalidaProgramada: '06:00:00', toleranciaMinutos: 5,
    entrada: '2026-11-03T03:00:00Z', salida: '2026-11-03T11:00:00Z', minutosTrabajados: 480, minutosTardanza: 0, minutosExtra: 0,
    estado: 'Asistencia', estadoHorasExtra: 'NoAplica', decididoPor: null, fechaDecision: null, motivoRechazo: null,
  },
];

export const resumenMock: ResumenAsistencia = {
  jornadas: 5,
  asistencias: 3,
  tardanzas: 1,
  sinMarcacion: 1,
  faltas: 0,
  minutosTardanza: 15,
  minutosExtraPendientes: 90,
  minutosExtraAprobados: 60,
};

export const marcacionesMock: Marcacion[] = [
  { id: 1, empleadoId: 1, empleadoLegajo: 'EMP-000001', empleadoNombre: 'Ana Torres Vega', tipo: 'Entrada', instante: '2026-11-03T13:15:00Z', origen: 'Reloj', dispositivo: 'RELOJ-01', numeroRegistro: 'R-1001', observacion: null },
  { id: 2, empleadoId: 1, empleadoLegajo: 'EMP-000001', empleadoNombre: 'Ana Torres Vega', tipo: 'Salida', instante: '2026-11-03T22:00:00Z', origen: 'Reloj', dispositivo: 'RELOJ-01', numeroRegistro: 'R-1002', observacion: null },
  { id: 3, empleadoId: 2, empleadoLegajo: 'EMP-000002', empleadoNombre: 'Luis Rojas Mendoza', tipo: 'Salida', instante: '2026-11-04T23:30:00Z', origen: 'Manual', dispositivo: null, numeroRegistro: null, observacion: 'Olvidó marcar la salida' },
];

export const resultadoCalculoMock: ResultadoCalculo = { calculadas: 12, conservadas: 1, eliminadas: 0 };

export const resultadoImportacionMock: ResultadoImportacion = {
  recibidos: 3,
  importados: 1,
  yaImportados: 1,
  duplicados: 0,
  rechazados: [{ numeroRegistro: 'R-3', motivo: 'No existe un empleado con el legajo EMP-999999.' }],
};

const empleadosMock: EmpleadoListado[] = [
  { id: 1, legajo: 'EMP-000001', tipoDocumento: 'DNI', numeroDocumento: '10000001', nombres: 'Ana', apellidos: 'Torres Vega', cargo: 'Gerente de Ventas', area: 'Ventas', fechaIngreso: '2021-03-01', estado: 'Activo' },
  { id: 2, legajo: 'EMP-000002', tipoDocumento: 'DNI', numeroDocumento: '10000002', nombres: 'Luis', apellidos: 'Rojas Mendoza', cargo: 'Analista de Compras', area: 'Compras', fechaIngreso: '2019-08-16', estado: 'Activo' },
  { id: 3, legajo: 'EMP-000003', tipoDocumento: 'CE', numeroDocumento: '004512367', nombres: 'Maria', apellidos: 'Salazar Quispe', cargo: 'Almacenero', area: 'Almacen', fechaIngreso: '2024-01-08', estado: 'Activo' },
];

function error(status: number, mensaje: string): Response {
  return Response.json({ StatusCode: status, Message: mensaje }, { status });
}

function pagina<T>(parsed: URL, items: T[], opciones: OpcionesApiAsistencia): Response {
  const numero = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  const inicio = (numero - 1) * tamano;
  return Response.json({ items: items.slice(inicio, inicio + tamano), total: opciones.totalListado ?? items.length, pagina: numero, tamanoPagina: tamano });
}

function respuestaJornadas(parsed: URL, opciones: OpcionesApiAsistencia): Response {
  if (opciones.errorLista) {
    return error(500, 'No se pudo cargar la lista de jornadas.');
  }
  if (opciones.listaVacia) {
    return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
  }
  const empleadoId = parsed.searchParams.get('empleadoId');
  const estado = parsed.searchParams.get('estado');
  const estadoHorasExtra = parsed.searchParams.get('estadoHorasExtra');
  const filtradas = jornadasMock.filter((jornada) => (
    (empleadoId === null || String(jornada.empleadoId) === empleadoId)
    && (estado === null || jornada.estado === estado)
    && (estadoHorasExtra === null || jornada.estadoHorasExtra === estadoHorasExtra)
  ));
  return pagina(parsed, filtradas, opciones);
}

function respuestaMarcaciones(parsed: URL, opciones: OpcionesApiAsistencia): Response {
  const esDetalle = parsed.searchParams.get('tamanoPagina') === '100';
  if (esDetalle && opciones.errorMarcacionesDia) {
    return error(500, 'No se pudieron cargar las marcaciones del día.');
  }
  if (!esDetalle && opciones.errorLista) {
    return error(500, 'No se pudo cargar la lista de marcaciones.');
  }
  if (!esDetalle && opciones.listaVacia) {
    return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
  }
  const empleadoId = parsed.searchParams.get('empleadoId');
  const tipo = parsed.searchParams.get('tipo');
  const origen = parsed.searchParams.get('origen');
  const filtradas = marcacionesMock.filter((marcacion) => (
    (empleadoId === null || String(marcacion.empleadoId) === empleadoId)
    && (tipo === null || marcacion.tipo === tipo)
    && (origen === null || marcacion.origen === origen)
  ));
  return pagina(parsed, filtradas, opciones);
}

function fallaDe(accion: AccionEscrituraAsistencia, opciones: OpcionesApiAsistencia): Response | null {
  if (opciones.errorEscritura?.accion === accion) {
    return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
  }
  return null;
}

function respuestaMock(parsed: URL, metodo: string, opciones: OpcionesApiAsistencia): Response {
  const ruta = parsed.pathname;
  if (ruta.endsWith('/empleados')) {
    return Response.json({ items: empleadosMock, total: empleadosMock.length, pagina: 1, tamanoPagina: 100 });
  }
  if (ruta.endsWith('/jornadas-asistencia/resumen')) {
    return opciones.errorResumen ? error(500, 'No se pudo cargar el resumen de asistencia.') : Response.json(resumenMock);
  }
  if (ruta.endsWith('/jornadas-asistencia/calculo')) {
    return fallaDe('calcular', opciones) ?? Response.json(resultadoCalculoMock);
  }
  const decision = /\/jornadas-asistencia\/(\d+)\/horas-extra\/(aprobacion|rechazo)$/.exec(ruta);
  if (decision) {
    const accion = decision[2] === 'aprobacion' ? 'aprobar' : 'rechazar';
    return fallaDe(accion, opciones) ?? Response.json({ id: Number(decision[1]) });
  }
  if (ruta.endsWith('/jornadas-asistencia')) {
    return respuestaJornadas(parsed, opciones);
  }
  if (ruta.endsWith('/marcaciones/importacion')) {
    return fallaDe('importar', opciones) ?? Response.json(opciones.resultadoImportacion ?? resultadoImportacionMock);
  }
  if (/\/marcaciones\/\d+$/.test(ruta) && metodo === 'DELETE') {
    return fallaDe('eliminar-marcacion', opciones) ?? new Response(null, { status: 204 });
  }
  if (ruta.endsWith('/marcaciones') && metodo === 'POST') {
    return fallaDe('registrar-marcacion', opciones) ?? Response.json({ id: 9 });
  }
  if (ruta.endsWith('/marcaciones')) {
    return respuestaMarcaciones(parsed, opciones);
  }
  return error(404, 'Ruta no prevista en la prueba.');
}

export function prepararApiAsistencia(opciones: OpcionesApiAsistencia = {}) {
  const llamadas: LlamadaAsistencia[] = [];
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

// Los roles viajan en la sesión: sin RRHH ni ADMIN los botones de escritura quedan deshabilitados.
export function renderAsistencia(roles: string[] = ['RRHH'], path = '/modulos/63-asistencia-biometrica') {
  setSesion({
    token: 'token-de-prueba',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    usuario: { id: 1, nombreUsuario: 'rrhh', nombreCompleto: 'Usuario de prueba' },
    roles,
  });
  return render(
    <SesionProvider>
      <MemoryRouter initialEntries={[path]}>
        <AsistenciaBiometricaPage />
      </MemoryRouter>
    </SesionProvider>,
  );
}

function urlsQueTerminanEn(fetchMock: ReturnType<typeof vi.fn>, fragmento: string, final: string): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.includes(fragmento) && url.endsWith(final));
}

// El listado de la tabla usa tamanoPagina=10; las marcaciones de la ficha usan tamanoPagina=100.
export function urlsDeListadoJornadas(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueTerminanEn(fetchMock, '/jornadas-asistencia?', 'tamanoPagina=10');
}

export function urlsDeListadoMarcaciones(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueTerminanEn(fetchMock, '/marcaciones?', 'tamanoPagina=10');
}

export async function esperarCargaJornadas(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando jornadas…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de asistencia…')).not.toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'EMP-000001 · Ana Torres Vega' })).toHaveLength(2);
    expect(screen.getByRole('option', { name: 'EMP-000001 · Torres Vega, Ana' })).toBeInTheDocument();
    expect(urlsDeListadoJornadas(fetchMock)).toHaveLength(1);
  });
}

export async function esperarCargaMarcaciones(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando marcaciones…')).not.toBeInTheDocument();
    expect(screen.getAllByText('RELOJ-01 · R-1001')).toHaveLength(1);
    expect(screen.getByRole('option', { name: 'EMP-000001 · Torres Vega, Ana' })).toBeInTheDocument();
    expect(urlsDeListadoMarcaciones(fetchMock)).toHaveLength(1);
  });
}

export async function montarJornadasCargadas(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  renderAsistencia(roles, '/modulos/63-asistencia-biometrica');
  await esperarCargaJornadas(fetchMock);
}

export async function montarMarcacionesCargadas(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  renderAsistencia(roles, '/modulos/63-asistencia-biometrica?vista=marcaciones');
  await esperarCargaMarcaciones(fetchMock);
}

export function limpiarAsistencia(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  limpiarSesion();
}
