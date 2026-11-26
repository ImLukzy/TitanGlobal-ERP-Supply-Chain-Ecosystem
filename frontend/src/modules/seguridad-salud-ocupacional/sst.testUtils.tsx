import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { limpiarSesion, setSesion } from '../../api/sesionStore';
import { SesionProvider } from '../../layout/SesionContext';
import type { EmpleadoListado } from '../empleados/tipos';
import { SeguridadSaludOcupacionalPage } from './SeguridadSaludOcupacionalPage';
import type { EntregaEpp, EquipoProteccion, ExamenMedico, IncidenteDetalle } from './tipos';

export interface LlamadaSst {
  url: string;
  metodo: string;
  cuerpo: unknown;
}

export interface OpcionesApiSst {
  errorEscritura?: { metodo: string; sufijo: string; status: number; mensaje: string };
}

// El día de las pruebas queda fijo: solo se falsea Date, los temporizadores siguen reales.
export const ANIO_PRUEBAS = 2026;
export const HOY_PRUEBAS = '2026-11-22';

function empleado(id: number, nombres: string, apellidos: string, estado: 'Activo' | 'Cesado'): EmpleadoListado {
  return { id, legajo: `EMP-00000${id}`, tipoDocumento: 'DNI', numeroDocumento: `1000000${id}`, nombres, apellidos, cargo: 'Operario', area: 'Planta', fechaIngreso: '2020-01-10', estado };
}

const empleadosMock: EmpleadoListado[] = [empleado(1, 'Ana', 'Torres Vega', 'Activo'), empleado(2, 'Luis', 'Rojas Mendoza', 'Activo'), empleado(3, 'Maria', 'Salazar Quispe', 'Cesado')];

function nombre(id: number): string {
  const item = empleadosMock.find((candidato) => candidato.id === id);
  return `${item?.apellidos ?? ''}, ${item?.nombres ?? ''}`;
}

function incidente(id: number, empleadoId: number, tipo: IncidenteDetalle['tipo'], severidad: IncidenteDetalle['severidad'], fecha: string, estado: IncidenteDetalle['estado'], acciones: IncidenteDetalle['acciones']): IncidenteDetalle {
  return {
    id, numero: `INC-00000${id}`, empleadoId, empleado: nombre(empleadoId), tipo, severidad, fecha, lugar: id === 1 ? 'Planta 2' : null, descripcion: `Descripción del incidente ${id}`,
    diasBaja: tipo === 'AccidenteConBaja' ? 5 : 0, estado, fechaCierre: estado === 'Cerrado' ? '2026-03-10' : null, acciones,
  };
}

export function incidentesIniciales(): IncidenteDetalle[] {
  return [
    incidente(1, 1, 'AccidenteConBaja', 'Grave', '2026-05-10', 'Abierto', [{ id: 11, descripcion: 'Instalar guarda', responsable: 'Jefe de planta', fechaCompromiso: '2026-06-01', completada: false, fechaCompletada: null }]),
    incidente(2, 2, 'IncidenteSinLesion', 'Leve', '2026-02-01', 'Cerrado', [{ id: 21, descripcion: 'Señalizar pasillo', responsable: 'Seguridad', fechaCompromiso: '2026-02-10', completada: true, fechaCompletada: '2026-02-09' }]),
    incidente(3, 1, 'EnfermedadOcupacional', 'Moderada', '2026-07-01', 'Abierto', []),
  ];
}

export function equiposIniciales(): EquipoProteccion[] {
  return [
    { id: 1, codigo: 'CASCO', nombre: 'Casco de seguridad', vidaUtilDias: 365, activo: true },
    { id: 2, codigo: 'GUANTE', nombre: 'Guantes dieléctricos', vidaUtilDias: 180, activo: true },
    { id: 3, codigo: 'ARNES', nombre: 'Arnés', vidaUtilDias: 730, activo: false },
  ];
}

function entrega(id: number, equipoId: number, empleadoId: number, fecha: string, vencimiento: string, cantidad: number): EntregaEpp {
  const equipo = equiposIniciales().find((item) => item.id === equipoId) as EquipoProteccion;
  return { id, equipoId, equipoCodigo: equipo.codigo, equipoNombre: equipo.nombre, empleadoId, empleado: nombre(empleadoId), fechaEntrega: fecha, cantidad, fechaVencimiento: vencimiento, vencida: vencimiento < HOY_PRUEBAS };
}

export function entregasIniciales(): EntregaEpp[] {
  return [entrega(1, 1, 1, '2025-01-10', '2026-01-10', 1), entrega(2, 2, 2, '2026-10-01', '2027-03-30', 2)];
}

export function examenesIniciales(): ExamenMedico[] {
  return [
    { id: 1, empleadoId: 1, empleado: nombre(1), tipo: 'Periodico', fecha: '2025-01-05', resultado: 'AptoConRestricciones', restricciones: 'Sin trabajo en altura', fechaVencimiento: '2026-01-05', clinica: 'Clínica Norte', vencido: true },
    { id: 2, empleadoId: 2, empleado: nombre(2), tipo: 'Ingreso', fecha: '2026-06-01', resultado: 'Apto', restricciones: null, fechaVencimiento: '2027-06-01', clinica: null, vencido: false },
    { id: 3, empleadoId: 3, empleado: nombre(3), tipo: 'Retiro', fecha: '2026-09-01', resultado: 'NoApto', restricciones: null, fechaVencimiento: null, clinica: null, vencido: false },
  ];
}

interface Estado {
  incidentes: IncidenteDetalle[];
  equipos: EquipoProteccion[];
  entregas: EntregaEpp[];
  examenes: ExamenMedico[];
}

function error(status: number, mensaje: string): Response {
  return Response.json({ StatusCode: status, Message: mensaje }, { status });
}

function pagina<T>(parsed: URL, items: T[]): Response {
  const numero = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  return Response.json({ items: items.slice((numero - 1) * tamano, numero * tamano), total: items.length, pagina: numero, tamanoPagina: tamano });
}

function listadoIncidentes(parsed: URL, estado: Estado): Response {
  const p = parsed.searchParams;
  const filtrados = estado.incidentes.filter((item) => (p.get('tipo') === null || item.tipo === p.get('tipo')) && (p.get('estado') === null || item.estado === p.get('estado')) && (p.get('desde') === null || item.fecha >= (p.get('desde') ?? '')) && (p.get('hasta') === null || item.fecha <= (p.get('hasta') ?? '')));
  return pagina(parsed, filtrados.map((item) => ({ ...item, acciones: item.acciones.length, accionesPendientes: item.acciones.filter((accion) => !accion.completada).length })));
}

function accionIncidente(item: IncidenteDetalle, sufijo: string, cuerpo: unknown, accionId: number): Response {
  if (sufijo === '/acciones') {
    const solicitud = cuerpo as { descripcion: string; responsable: string; fechaCompromiso: string };
    item.acciones = [...item.acciones, { id: 90 + item.acciones.length, ...solicitud, completada: false, fechaCompletada: null }];
  } else if (sufijo === '/completar') {
    const accion = item.acciones.find((candidata) => candidata.id === accionId);
    if (accion === undefined) {
      return error(404, `La acción correctiva ${accionId} no existe en el incidente.`);
    }
    accion.completada = true;
    accion.fechaCompletada = (cuerpo as { fechaCompletada: string }).fechaCompletada;
  } else {
    const pendientes = item.acciones.filter((accion) => !accion.completada).length;
    if (item.acciones.length === 0) {
      return error(409, `El incidente ${item.numero} necesita al menos una acción correctiva para cerrarse.`);
    }
    if (pendientes > 0) {
      return error(409, `El incidente ${item.numero} tiene ${pendientes} acciones correctivas sin completar.`);
    }
    item.estado = 'Cerrado';
  }
  return Response.json({ id: item.id });
}

function respuestaIncidentes(parsed: URL, metodo: string, cuerpo: unknown, estado: Estado): Response {
  if (parsed.pathname.endsWith('/resumen')) {
    return Response.json({ anio: Number(parsed.searchParams.get('anio')), incidentesAbiertos: 2, incidentesAnio: 3, accidentesConBaja: 1, diasBajaAnio: 5, entregasEppVencidas: 1, examenesVencidos: 1, empleadosNoAptos: 0 });
  }
  const partes = /\/incidentes-sst\/(\d+)(\/[a-z]+)?(?:\/(\d+)(\/[a-z]+))?$/.exec(parsed.pathname);
  if (partes === null) {
    if (metodo === 'POST') {
      const solicitud = cuerpo as Omit<IncidenteDetalle, 'id'>;
      estado.incidentes = [...estado.incidentes, incidente(9, solicitud.empleadoId, solicitud.tipo, solicitud.severidad, solicitud.fecha, 'Abierto', [])];
      return Response.json({ id: 9, numero: 'INC-000009' });
    }
    return listadoIncidentes(parsed, estado);
  }
  const item = estado.incidentes.find((candidato) => candidato.id === Number(partes[1]));
  if (item === undefined) {
    return error(404, `El incidente ${partes[1]} no existe.`);
  }
  return partes[2] === undefined ? Response.json(item) : accionIncidente(item, partes[4] ?? partes[2], cuerpo, Number(partes[3] ?? 0));
}

function respuestaEquipos(parsed: URL, metodo: string, cuerpo: unknown, estado: Estado): Response {
  const partes = /\/equipos-proteccion\/(\d+)(\/[a-z]+)?$/.exec(parsed.pathname);
  const solicitud = (cuerpo ?? {}) as Partial<EquipoProteccion>;
  if (partes === null) {
    if (metodo === 'POST') {
      estado.equipos = [...estado.equipos, { id: 9, codigo: (solicitud.codigo ?? '').toUpperCase(), nombre: solicitud.nombre ?? '', vidaUtilDias: solicitud.vidaUtilDias ?? 1, activo: true }];
      return Response.json({ id: 9 });
    }
    const activo = parsed.searchParams.get('activo');
    return pagina(parsed, estado.equipos.filter((item) => activo === null || String(item.activo) === activo));
  }
  const id = Number(partes[1]);
  estado.equipos = estado.equipos.map((item) => {
    if (item.id !== id) {
      return item;
    }
    return partes[2] === undefined ? { ...item, ...solicitud, codigo: (solicitud.codigo ?? item.codigo).toUpperCase() } : { ...item, activo: partes[2] === '/activacion' };
  });
  return Response.json({ id });
}

export function prepararApiSst(opciones: OpcionesApiSst = {}) {
  const llamadas: LlamadaSst[] = [];
  const estado: Estado = { incidentes: incidentesIniciales(), equipos: equiposIniciales(), entregas: entregasIniciales(), examenes: examenesIniciales() };
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    const cuerpo = typeof init.body === 'string' ? JSON.parse(init.body) as unknown : undefined;
    llamadas.push({ url, metodo, cuerpo });
    if (opciones.errorEscritura !== undefined && opciones.errorEscritura.metodo === metodo && parsed.pathname.endsWith(opciones.errorEscritura.sufijo)) {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    if (parsed.pathname.endsWith('/empleados')) {
      return Response.json({ items: empleadosMock, total: empleadosMock.length, pagina: 1, tamanoPagina: 100 });
    }
    if (parsed.pathname.includes('/incidentes-sst')) {
      return respuestaIncidentes(parsed, metodo, cuerpo, estado);
    }
    if (parsed.pathname.includes('/equipos-proteccion')) {
      return respuestaEquipos(parsed, metodo, cuerpo, estado);
    }
    const lista = parsed.pathname.includes('/entregas-epp') ? estado.entregas : estado.examenes;
    if (metodo === 'POST') {
      return Response.json({ id: 9 });
    }
    const vencidas = parsed.searchParams.get('vencidas') ?? parsed.searchParams.get('vencidos');
    const resultado = parsed.searchParams.get('resultado');
    return pagina(parsed, (lista as { vencida?: boolean; vencido?: boolean; resultado?: string }[]).filter((item) => (vencidas !== 'true' || item.vencida === true || item.vencido === true) && (resultado === null || item.resultado === resultado)));
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

// Los roles viajan en la sesión: sin RRHH ni ADMIN los botones de escritura quedan deshabilitados.
export function renderSst(roles: string[] = ['RRHH'], path = '/modulos/70-seguridad-salud-ocupacional') {
  vi.useFakeTimers({ toFake: ['Date'], now: new Date(`${HOY_PRUEBAS}T12:00:00Z`) });
  setSesion({
    token: 'token-de-prueba',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    usuario: { id: 1, nombreUsuario: 'rrhh', nombreCompleto: 'Usuario de prueba' },
    roles,
  });
  return render(
    <SesionProvider>
      <MemoryRouter initialEntries={[path]}>
        <SeguridadSaludOcupacionalPage />
      </MemoryRouter>
    </SesionProvider>,
  );
}

export function urlsQueContienen(fetchMock: ReturnType<typeof vi.fn>, fragmento: string): string[] {
  return fetchMock.mock.calls.map((llamada) => String(llamada[0])).filter((url) => url.includes(fragmento));
}

export function llamadasSst(llamadas: LlamadaSst[], metodo: string, sufijo: string): LlamadaSst[] {
  return llamadas.filter((llamada) => llamada.metodo === metodo && llamada.url.endsWith(sufijo));
}

// Espera el resumen, el listado y los empleados del selector; cada uno se pide una sola vez.
export async function esperarCargaIncidentes(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando incidentes…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de seguridad y salud…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'INC-000001' })).toBeInTheDocument();
    expect(urlsQueContienen(fetchMock, '/incidentes-sst?')).toHaveLength(1);
    expect(urlsQueContienen(fetchMock, '/incidentes-sst/resumen')).toHaveLength(1);
    expect(urlsQueContienen(fetchMock, '/empleados?')).toHaveLength(1);
  });
}

export async function esperarCargaEpp(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando entregas…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando equipos…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de seguridad y salud…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Editar equipo CASCO' })).toBeInTheDocument();
    expect(screen.getByRole('table', { name: 'Listado de entregas de EPP' })).toBeInTheDocument();
    expect(urlsQueContienen(fetchMock, '/entregas-epp?')).toHaveLength(1);
    expect(urlsQueContienen(fetchMock, '/equipos-proteccion?').filter((url) => url.endsWith('tamanoPagina=10'))).toHaveLength(1);
    expect(urlsQueContienen(fetchMock, '/equipos-proteccion?').filter((url) => url.endsWith('tamanoPagina=100'))).toHaveLength(1);
    expect(urlsQueContienen(fetchMock, '/empleados?')).toHaveLength(1);
  });
}

export async function esperarCargaExamenes(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando exámenes médicos…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de seguridad y salud…')).not.toBeInTheDocument();
    expect(screen.getByRole('table', { name: 'Listado de exámenes médicos' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'EMP-000001 · Torres Vega, Ana' })).toBeInTheDocument();
    expect(urlsQueContienen(fetchMock, '/examenes-medicos?')).toHaveLength(1);
    expect(urlsQueContienen(fetchMock, '/empleados?')).toHaveLength(1);
  });
}

export async function montarIncidentesCargados(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  renderSst(roles);
  await esperarCargaIncidentes(fetchMock);
}

export async function montarEppCargado(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH'], consulta = ''): Promise<void> {
  renderSst(roles, `/modulos/70-seguridad-salud-ocupacional?vista=epp${consulta}`);
  await esperarCargaEpp(fetchMock);
}

export async function montarExamenesCargados(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH'], consulta = ''): Promise<void> {
  renderSst(roles, `/modulos/70-seguridad-salud-ocupacional?vista=examenes${consulta}`);
  await esperarCargaExamenes(fetchMock);
}

export function limpiarSst(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  limpiarSesion();
}
