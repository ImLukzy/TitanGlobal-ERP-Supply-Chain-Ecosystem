import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { limpiarSesion, setSesion } from '../../api/sesionStore';
import { SesionProvider } from '../../layout/SesionContext';
import type { EmpleadoListado } from '../empleados/tipos';
import { CapacitacionesPage } from './CapacitacionesPage';
import type { CapacitacionEmpleado, CursoListado, Inscripcion, ProgramacionDetalle } from './tipos';

export interface LlamadaCapacitaciones {
  url: string;
  metodo: string;
  cuerpo: unknown;
}

export interface OpcionesApiCapacitaciones {
  listaVacia?: boolean;
  errorLista?: boolean;
  totalListado?: number;
  errorEscritura?: { metodo: string; sufijo: string; status: number; mensaje: string };
}

// El año actual de las pruebas queda fijo: solo se falsea Date, los temporizadores siguen reales.
export const ANIO_PRUEBAS = 2026;

const empleadosMock: EmpleadoListado[] = [
  { id: 1, legajo: 'EMP-000001', tipoDocumento: 'DNI', numeroDocumento: '10000001', nombres: 'Ana', apellidos: 'Torres Vega', cargo: 'Gerente', area: 'Ventas', fechaIngreso: '2021-03-01', estado: 'Activo' },
  { id: 2, legajo: 'EMP-000002', tipoDocumento: 'DNI', numeroDocumento: '10000002', nombres: 'Luis', apellidos: 'Rojas Mendoza', cargo: 'Analista', area: 'Compras', fechaIngreso: '2019-08-16', estado: 'Activo' },
  { id: 3, legajo: 'EMP-000003', tipoDocumento: 'DNI', numeroDocumento: '10000003', nombres: 'Maria', apellidos: 'Salazar Quispe', cargo: 'Operaria', area: 'Planta', fechaIngreso: '2020-01-10', estado: 'Activo' },
];

export function cursosIniciales(): CursoListado[] {
  return [
    { id: 1, codigo: 'SEG-101', nombre: 'Seguridad industrial', horas: 8, tipo: 'Interno', proveedor: null, activo: true },
    { id: 2, codigo: 'LID-201', nombre: 'Liderazgo', horas: 16, tipo: 'Externo', proveedor: 'Instituto Andino', activo: true },
    { id: 3, codigo: 'EXC-301', nombre: 'Excel avanzado', horas: 12.5, tipo: 'Virtual', proveedor: 'Aula Digital', activo: false },
  ];
}

function inscripcion(id: number, empleadoId: number, estado: Inscripcion['estado'], nota: number | null, certificacion: Inscripcion['certificacion'] = null): Inscripcion {
  const empleado = empleadosMock.find((item) => item.id === empleadoId);
  return {
    id, empleadoId, empleadoLegajo: empleado?.legajo ?? '', empleadoNombre: `${empleado?.nombres ?? ''} ${empleado?.apellidos ?? ''}`, estado, nota, fechaInscripcion: '2026-01-15', certificacion,
  };
}

function programacion(id: number, curso: CursoListado, inicio: string, fin: string, cupo: number, estado: ProgramacionDetalle['estado'], inscripciones: Inscripcion[]): ProgramacionDetalle {
  const inscritos = inscripciones.filter((item) => item.estado !== 'Retirado').length;
  return {
    id, cursoId: curso.id, cursoCodigo: curso.codigo, cursoNombre: curso.nombre, horas: curso.horas, anio: Number(inicio.slice(0, 4)), fechaInicio: inicio, fechaFin: fin,
    cupo, inscritos, cupoLibre: cupo - inscritos, lugar: id === 12 ? null : 'Sala A', estado, inscripciones,
  };
}

export function programacionesIniciales(): ProgramacionDetalle[] {
  const [seguridad, liderazgo] = cursosIniciales();
  const vencida = { id: 1, codigo: 'CERT-001', fechaEmision: '2026-03-03', fechaVencimiento: '2026-05-01', vencida: true };
  return [
    programacion(10, seguridad as CursoListado, '2026-03-02', '2026-03-03', 3, 'Realizada', [inscripcion(100, 1, 'Aprobado', 17, vencida), inscripcion(101, 2, 'Aprobado', 15), inscripcion(102, 3, 'Inscrito', null)]),
    programacion(11, liderazgo as CursoListado, '2026-12-10', '2026-12-11', 3, 'Programada', [inscripcion(110, 1, 'Inscrito', null), inscripcion(111, 2, 'Inscrito', null)]),
    programacion(12, seguridad as CursoListado, '2026-02-01', '2026-02-02', 5, 'Cancelada', [inscripcion(120, 1, 'Retirado', null)]),
    programacion(13, liderazgo as CursoListado, '2026-11-30', '2026-12-01', 1, 'Programada', [inscripcion(130, 3, 'Inscrito', null)]),
  ];
}

interface Estado {
  cursos: CursoListado[];
  programaciones: ProgramacionDetalle[];
}

function error(status: number, mensaje: string): Response {
  return Response.json({ StatusCode: status, Message: mensaje }, { status });
}

function pagina<T>(parsed: URL, items: T[], opciones: OpcionesApiCapacitaciones, extra: object = {}): Response {
  const numero = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  const inicio = (numero - 1) * tamano;
  return Response.json({ ...extra, items: items.slice(inicio, inicio + tamano), total: opciones.totalListado ?? items.length, pagina: numero, tamanoPagina: tamano });
}

function recalcular(item: ProgramacionDetalle): void {
  item.inscritos = item.inscripciones.filter((fila) => fila.estado !== 'Retirado').length;
  item.cupoLibre = item.cupo - item.inscritos;
}

function respuestaCursos(parsed: URL, metodo: string, cuerpo: unknown, estado: Estado, opciones: OpcionesApiCapacitaciones): Response {
  const partes = /\/cursos\/(\d+)(\/[a-z]+)?$/.exec(parsed.pathname);
  const solicitud = (cuerpo ?? {}) as Partial<CursoListado>;
  if (partes === null) {
    if (metodo === 'POST') {
      estado.cursos = [...estado.cursos, { id: 9, codigo: (solicitud.codigo ?? '').toUpperCase(), nombre: solicitud.nombre ?? '', horas: solicitud.horas ?? 0, tipo: solicitud.tipo ?? 'Interno', proveedor: solicitud.proveedor ?? null, activo: true }];
      return Response.json({ id: 9 });
    }
    if (opciones.errorLista) {
      return error(500, 'No se pudo cargar la lista de cursos.');
    }
    const busqueda = (parsed.searchParams.get('busqueda') ?? '').toLowerCase();
    const tipo = parsed.searchParams.get('tipo');
    const activo = parsed.searchParams.get('activo');
    const filtrados = estado.cursos.filter((item) => (tipo === null || item.tipo === tipo) && (activo === null || String(item.activo) === activo) && `${item.codigo} ${item.nombre}`.toLowerCase().includes(busqueda));
    return opciones.listaVacia ? Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 }) : pagina(parsed, filtrados, opciones);
  }
  const id = Number(partes[1]);
  const actual = estado.cursos.find((item) => item.id === id);
  if (actual === undefined) {
    return error(404, `El curso ${id} no existe.`);
  }
  const reemplazar = (cambios: Partial<CursoListado>): void => {
    estado.cursos = estado.cursos.map((item) => (item.id === id ? { ...item, ...cambios } : item));
  };
  if (partes[2] === '/activacion' || partes[2] === '/desactivacion') {
    reemplazar({ activo: partes[2] === '/activacion' });
  } else if (metodo === 'PUT') {
    reemplazar({ ...solicitud, codigo: (solicitud.codigo ?? actual.codigo).toUpperCase() });
  } else {
    return Response.json(actual);
  }
  return Response.json({ id });
}

function accionProgramacion(item: ProgramacionDetalle, sufijo: string, cuerpo: unknown, inscripcionId: number): Response {
  const fila = item.inscripciones.find((candidata) => candidata.id === inscripcionId);
  if (sufijo === '/inscripciones' && fila === undefined && inscripcionId === 0) {
    const solicitud = cuerpo as { empleadoId: number };
    if (item.cupoLibre <= 0) {
      return error(409, `La programación ${item.id} no tiene cupo libre.`);
    }
    item.inscripciones = [...item.inscripciones, inscripcion(500 + item.inscripciones.length, solicitud.empleadoId, 'Inscrito', null)];
  } else if (sufijo === '/retiro' && fila !== undefined) {
    fila.estado = 'Retirado';
  } else if (sufijo === '/realizacion') {
    item.estado = 'Realizada';
  } else if (sufijo === '/cancelacion') {
    item.estado = 'Cancelada';
    item.inscripciones.forEach((candidata) => {
      candidata.estado = 'Retirado';
    });
  } else if (sufijo === '/resultados') {
    for (const resultado of (cuerpo as { resultados: { inscripcionId: number; aprobado: boolean; nota: number | null }[] }).resultados) {
      const destino = item.inscripciones.find((candidata) => candidata.id === resultado.inscripcionId);
      if (destino !== undefined) {
        destino.estado = resultado.aprobado ? 'Aprobado' : 'Desaprobado';
        destino.nota = resultado.nota;
      }
    }
  } else if (sufijo === '/certificacion' && fila !== undefined) {
    const solicitud = cuerpo as { codigo: string; fechaEmision: string; fechaVencimiento: string | null };
    fila.certificacion = { id: 90, codigo: solicitud.codigo.toUpperCase(), fechaEmision: solicitud.fechaEmision, fechaVencimiento: solicitud.fechaVencimiento, vencida: false };
  } else {
    return error(404, `La inscripción ${inscripcionId} no existe en esta programación.`);
  }
  recalcular(item);
  return Response.json({ id: inscripcionId === 0 ? 500 : item.id });
}

function respuestaProgramaciones(parsed: URL, metodo: string, cuerpo: unknown, estado: Estado, opciones: OpcionesApiCapacitaciones): Response {
  const partes = /\/programaciones-curso\/(\d+)(\/[a-z]+)?(?:\/(\d+)(\/[a-z]+))?$/.exec(parsed.pathname);
  if (partes === null) {
    if (metodo === 'POST') {
      const solicitud = cuerpo as { cursoId: number; fechaInicio: string; fechaFin: string; cupo: number };
      const curso = estado.cursos.find((item) => item.id === solicitud.cursoId) ?? estado.cursos[0] as CursoListado;
      estado.programaciones = [...estado.programaciones, programacion(20, curso, solicitud.fechaInicio, solicitud.fechaFin, solicitud.cupo, 'Programada', [])];
      return Response.json({ id: 20 });
    }
    if (opciones.errorLista) {
      return error(500, 'No se pudo cargar la lista de programaciones.');
    }
    const cursoId = parsed.searchParams.get('cursoId');
    const filtro = parsed.searchParams.get('estado');
    const anio = Number(parsed.searchParams.get('anio'));
    const filtradas = estado.programaciones.filter((item) => item.anio === anio && (cursoId === null || String(item.cursoId) === cursoId) && (filtro === null || item.estado === filtro));
    const listado = filtradas.map((item) => ({ ...item, inscripciones: undefined }));
    return opciones.listaVacia ? Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 }) : pagina(parsed, listado, opciones);
  }
  const id = Number(partes[1]);
  const actual = estado.programaciones.find((item) => item.id === id);
  if (actual === undefined) {
    return error(404, `La programación ${id} no existe.`);
  }
  if (partes[2] === undefined) {
    return Response.json(actual);
  }
  return accionProgramacion(actual, partes[4] ?? partes[2], cuerpo, Number(partes[3] ?? 0));
}

function respuestaCapacitaciones(parsed: URL, opciones: OpcionesApiCapacitaciones): Response {
  const anio = Number(parsed.searchParams.get('anio'));
  if (parsed.pathname.endsWith('/resumen')) {
    return Response.json({ anio, cursosActivos: 2, programadas: 2, realizadas: 1, inscritos: 6, horasTotales: 16, certificacionesVencidas: 1 });
  }
  if (parsed.pathname.endsWith('/horas')) {
    const busqueda = (parsed.searchParams.get('busqueda') ?? '').toLowerCase();
    const filas = empleadosMock.map((empleado, indice) => ({
      empleadoId: empleado.id, empleadoLegajo: empleado.legajo, empleadoNombre: `${empleado.nombres} ${empleado.apellidos}`, cursos: 2 - indice, horas: 16 - indice * 8,
    })).filter((fila) => `${fila.empleadoLegajo} ${fila.empleadoNombre}`.toLowerCase().includes(busqueda));
    if (opciones.errorLista) {
      return error(500, 'No se pudieron cargar las horas por empleado.');
    }
    return opciones.listaVacia ? Response.json({ anio, items: [], total: 0, pagina: 1, tamanoPagina: 10 }) : pagina(parsed, filas, opciones, { anio });
  }
  const empleadoId = Number(/\/empleados\/(\d+)$/.exec(parsed.pathname)?.[1] ?? 0);
  const empleado = empleadosMock.find((item) => item.id === empleadoId);
  if (empleado === undefined) {
    return error(404, `El empleado ${empleadoId} no existe.`);
  }
  const ficha: CapacitacionEmpleado = {
    empleadoId, empleadoLegajo: empleado.legajo, empleadoNombre: `${empleado.nombres} ${empleado.apellidos}`, anio, horas: 8,
    cursos: [{ inscripcionId: 100, programacionId: 10, cursoCodigo: 'SEG-101', cursoNombre: 'Seguridad industrial', horas: 8, fechaInicio: '2026-03-02', fechaFin: '2026-03-03', estado: 'Aprobado', nota: 17 }],
    certificaciones: [{ id: 1, inscripcionId: 100, cursoCodigo: 'SEG-101', cursoNombre: 'Seguridad industrial', codigo: 'CERT-001', fechaEmision: '2026-03-03', fechaVencimiento: '2026-05-01', vencida: true }],
  };
  return Response.json(ficha);
}

export function prepararApiCapacitaciones(opciones: OpcionesApiCapacitaciones = {}) {
  const llamadas: LlamadaCapacitaciones[] = [];
  const estado: Estado = { cursos: cursosIniciales(), programaciones: programacionesIniciales() };
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
    if (parsed.pathname.includes('/capacitaciones/')) {
      return respuestaCapacitaciones(parsed, opciones);
    }
    if (parsed.pathname.includes('/programaciones-curso')) {
      return respuestaProgramaciones(parsed, metodo, cuerpo, estado, opciones);
    }
    return respuestaCursos(parsed, metodo, cuerpo, estado, opciones);
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

// Los roles viajan en la sesión: sin RRHH ni ADMIN los botones de escritura quedan deshabilitados.
export function renderCapacitaciones(roles: string[] = ['RRHH'], path = '/modulos/69-capacitaciones') {
  vi.useFakeTimers({ toFake: ['Date'], now: new Date(`${ANIO_PRUEBAS}-11-22T12:00:00Z`) });
  setSesion({
    token: 'token-de-prueba',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    usuario: { id: 1, nombreUsuario: 'rrhh', nombreCompleto: 'Usuario de prueba' },
    roles,
  });
  return render(
    <SesionProvider>
      <MemoryRouter initialEntries={[path]}>
        <CapacitacionesPage />
      </MemoryRouter>
    </SesionProvider>,
  );
}

export function urlsQueContienen(fetchMock: ReturnType<typeof vi.fn>, fragmento: string): string[] {
  return fetchMock.mock.calls.map((llamada) => String(llamada[0])).filter((url) => url.includes(fragmento));
}

// Espera el resumen, la lista de programaciones, los cursos y los empleados de los selectores (cada uno se pide una sola vez).
export async function esperarCargaProgramaciones(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando programaciones…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de capacitaciones…')).not.toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Liderazgo' })).toHaveLength(2);
    expect(screen.getByRole('option', { name: 'LID-201 · Liderazgo' })).toBeInTheDocument();
    expect(urlsQueContienen(fetchMock, '/programaciones-curso?')).toHaveLength(1);
    expect(urlsQueContienen(fetchMock, '/cursos?').filter((url) => url.endsWith('tamanoPagina=100'))).toHaveLength(1);
    expect(urlsQueContienen(fetchMock, '/empleados?')).toHaveLength(1);
    expect(urlsQueContienen(fetchMock, '/capacitaciones/resumen')).toHaveLength(1);
  });
}

export async function esperarCargaCursos(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando cursos…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'LID-201' })).toBeInTheDocument();
    // El selector de Programaciones también pide cursos (con tamaño 100): aquí solo cuenta la lista de la vista.
    expect(urlsQueContienen(fetchMock, '/cursos?').filter((url) => url.endsWith('tamanoPagina=10'))).toHaveLength(1);
  });
}

export async function esperarCargaHoras(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando horas por empleado…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Ana Torres Vega' })).toBeInTheDocument();
    expect(urlsQueContienen(fetchMock, '/capacitaciones/horas?')).toHaveLength(1);
  });
}

export async function montarProgramacionesCargadas(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  renderCapacitaciones(roles);
  await esperarCargaProgramaciones(fetchMock);
}

export async function montarCursosCargados(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  renderCapacitaciones(roles, '/modulos/69-capacitaciones?vista=cursos');
  await esperarCargaCursos(fetchMock);
}

export async function montarHorasCargadas(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  renderCapacitaciones(roles, '/modulos/69-capacitaciones?vista=horas');
  await esperarCargaHoras(fetchMock);
}

export function limpiarCapacitaciones(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  limpiarSesion();
}
