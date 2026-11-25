import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { limpiarSesion, setSesion } from '../../api/sesionStore';
import { SesionProvider } from '../../layout/SesionContext';
import type { EmpleadoListado } from '../empleados/tipos';
import { EvaluacionDesempenoPage } from './EvaluacionDesempenoPage';
import type { CicloDetalle, Competencia, EvaluacionDetalle, EvaluacionListado, ResultadosCiclo } from './tipos';

export interface LlamadaEvaluacion {
  url: string;
  metodo: string;
  cuerpo: unknown;
}

export interface OpcionesApiEvaluacion {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorResultados?: boolean;
  totalListado?: number;
  errorEscritura?: { metodo: string; sufijo: string; status: number; mensaje: string };
}

function competencia(id: number, nombre: string, peso: number): Competencia {
  return { id, nombre, descripcion: null, peso };
}

function ciclo(id: number, codigo: string, nombre: string, estado: CicloDetalle['estado'], competencias: Competencia[]): CicloDetalle {
  const suma = competencias.reduce((total, item) => total + Math.round(item.peso * 100), 0) / 100;
  return {
    id, codigo, nombre, estado, periodo: 'Semestral', fechaInicioPeriodo: '2026-06-01', fechaFinPeriodo: '2026-11-30', fechaApertura: '2026-11-01',
    fechaCierre: '2026-12-31', sumaPesos: suma, competencias, evaluaciones: { borrador: 1, enviada: 1, cerrada: 0 },
  };
}

export function ciclosIniciales(): CicloDetalle[] {
  return [
    ciclo(1, '2026-S1', 'Primer semestre 2026', 'Cerrado', [competencia(1, 'Liderazgo', 50), competencia(2, 'Trabajo en equipo', 50)]),
    ciclo(2, '2026-S2', 'Segundo semestre 2026', 'Abierto', [competencia(3, 'Liderazgo', 45.5), competencia(4, 'Trabajo en equipo', 54.5)]),
    ciclo(3, '2027-S1', 'Primer semestre 2027', 'Configuracion', [competencia(5, 'Calidad', 60), competencia(6, 'Puntualidad', 25)]),
    ciclo(4, '2027-S2', 'Segundo semestre 2027', 'Configuracion', [competencia(7, 'Calidad', 100)]),
  ];
}

export const resultadosMock: ResultadosCiclo = {
  evaluadas: 3,
  puntajePromedio: 3.55,
  distribucion: [
    { etiqueta: 'Insuficiente', cantidad: 0 }, { etiqueta: 'En desarrollo', cantidad: 1 }, { etiqueta: 'Competente', cantidad: 2 },
    { etiqueta: 'Destacado', cantidad: 0 }, { etiqueta: 'Excepcional', cantidad: 0 },
  ],
  porCompetencia: [{ competencia: 'Liderazgo', promedio: 3.5 }, { competencia: 'Trabajo en equipo', promedio: 4 }],
};

function evaluacion(id: number, cicloId: number, empleadoId: number, nombre: string, estado: EvaluacionListado['estado'], puntaje: number | null, etiqueta: string | null): EvaluacionListado {
  return {
    id, cicloId, cicloCodigo: cicloId === 1 ? '2026-S1' : '2026-S2', empleadoId, empleadoLegajo: `EMP-00000${empleadoId}`, empleadoNombre: nombre,
    evaluadorId: 2, evaluadorNombre: 'Luis Rojas Mendoza', estado, puntajeFinal: puntaje, etiqueta,
  };
}

export function evaluacionesIniciales(): EvaluacionListado[] {
  return [
    evaluacion(11, 2, 1, 'Ana Torres Vega', 'Borrador', null, null),
    evaluacion(12, 2, 3, 'Maria Salazar Quispe', 'Enviada', 3.55, 'Competente'),
    evaluacion(13, 1, 4, 'Pedro Díaz Lara', 'Cerrada', 4.2, 'Destacado'),
  ];
}

const empleadosMock: EmpleadoListado[] = [
  { id: 1, legajo: 'EMP-000001', tipoDocumento: 'DNI', numeroDocumento: '10000001', nombres: 'Ana', apellidos: 'Torres Vega', cargo: 'Gerente', area: 'Ventas', fechaIngreso: '2021-03-01', estado: 'Activo' },
  { id: 2, legajo: 'EMP-000002', tipoDocumento: 'DNI', numeroDocumento: '10000002', nombres: 'Luis', apellidos: 'Rojas Mendoza', cargo: 'Analista', area: 'Compras', fechaIngreso: '2019-08-16', estado: 'Activo' },
];

interface Estado {
  ciclos: CicloDetalle[];
  evaluaciones: EvaluacionListado[];
  calificaciones: Map<number, Map<number, number>>;
}

function error(status: number, mensaje: string): Response {
  return Response.json({ StatusCode: status, Message: mensaje }, { status });
}

function pagina<T>(parsed: URL, items: T[], opciones: OpcionesApiEvaluacion): Response {
  const numero = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  const inicio = (numero - 1) * tamano;
  return Response.json({ items: items.slice(inicio, inicio + tamano), total: opciones.totalListado ?? items.length, pagina: numero, tamanoPagina: tamano });
}

function detalleEvaluacion(estado: Estado, id: number): EvaluacionDetalle | null {
  const base = estado.evaluaciones.find((item) => item.id === id);
  const origen = estado.ciclos.find((item) => item.id === base?.cicloId);
  if (base === undefined || origen === undefined) {
    return null;
  }
  const guardadas = estado.calificaciones.get(id) ?? new Map<number, number>();
  return {
    ...base, comentario: null, fechaEnvio: base.estado === 'Borrador' ? null : '2026-11-20T10:00:00Z', fechaCierre: base.estado === 'Cerrada' ? '2026-11-21T10:00:00Z' : null,
    calificaciones: origen.competencias.map((item) => ({ competenciaId: item.id, competencia: item.nombre, peso: item.peso, calificacion: guardadas.get(item.id) ?? null })),
  };
}

function respuestaCiclos(parsed: URL, metodo: string, cuerpo: unknown, estado: Estado, opciones: OpcionesApiEvaluacion): Response {
  const ruta = parsed.pathname;
  const partes = /\/ciclos-evaluacion\/(\d+)(\/[a-z]+)?(?:\/(\d+))?$/.exec(ruta);
  const solicitud = cuerpo as { nombre?: string; peso?: number; codigo?: string } | undefined;
  if (partes === null) {
    if (metodo === 'POST') {
      estado.ciclos = [ciclo(9, solicitud?.codigo ?? 'NUEVO', solicitud?.nombre ?? '', 'Configuracion', []), ...estado.ciclos];
      return Response.json({ id: 9 });
    }
    if (opciones.errorLista) {
      return error(500, 'No se pudo cargar la lista de ciclos.');
    }
    const busqueda = (parsed.searchParams.get('busqueda') ?? '').toLowerCase();
    const filtro = parsed.searchParams.get('estado');
    const filtrados = estado.ciclos.filter((item) => (filtro === null || item.estado === filtro) && `${item.codigo} ${item.nombre}`.toLowerCase().includes(busqueda));
    return opciones.listaVacia ? Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 }) : pagina(parsed, filtrados, opciones);
  }
  const id = Number(partes[1]);
  const actual = estado.ciclos.find((item) => item.id === id);
  if (actual === undefined) {
    return error(404, `El ciclo de evaluación ${id} no existe.`);
  }
  const reemplazar = (cambios: Partial<CicloDetalle>): void => {
    estado.ciclos = estado.ciclos.map((item) => (item.id === id ? { ...item, ...cambios } : item));
  };
  const reemplazarCompetencias = (competencias: Competencia[]): void => {
    reemplazar({ competencias, sumaPesos: competencias.reduce((total, item) => total + Math.round(item.peso * 100), 0) / 100 });
  };
  if (partes[2] === '/resultados') {
    return opciones.errorResultados ? error(500, 'No se pudieron cargar los resultados del ciclo.') : Response.json(resultadosMock);
  }
  if (partes[2] === '/apertura' || partes[2] === '/cierre') {
    reemplazar({ estado: partes[2] === '/apertura' ? 'Abierto' : 'Cerrado' });
  } else if (partes[2] === '/competencias') {
    if (metodo === 'DELETE') {
      reemplazarCompetencias(actual.competencias.filter((item) => item.id !== Number(partes[3])));
      return new Response(null, { status: 204 });
    }
    const nueva = competencia(Number(partes[3] ?? 50), solicitud?.nombre ?? '', solicitud?.peso ?? 0);
    reemplazarCompetencias(metodo === 'PUT' ? actual.competencias.map((item) => (item.id === nueva.id ? nueva : item)) : [...actual.competencias, nueva]);
  } else if (metodo === 'PUT') {
    reemplazar({ nombre: solicitud?.nombre ?? actual.nombre });
  } else {
    return Response.json(estado.ciclos.find((item) => item.id === id));
  }
  return Response.json({ id });
}

function respuestaEvaluaciones(parsed: URL, metodo: string, cuerpo: unknown, estado: Estado, opciones: OpcionesApiEvaluacion): Response {
  const partes = /\/evaluaciones\/(\d+)(\/[a-z]+)?$/.exec(parsed.pathname);
  if (partes === null) {
    if (metodo === 'POST') {
      estado.evaluaciones = [evaluacion(21, 2, 2, 'Luis Rojas Mendoza', 'Borrador', null, null), ...estado.evaluaciones];
      return Response.json({ id: 21 });
    }
    if (opciones.errorLista) {
      return error(500, 'No se pudo cargar la lista de evaluaciones.');
    }
    const cicloId = parsed.searchParams.get('cicloId');
    const filtro = parsed.searchParams.get('estado');
    const busqueda = (parsed.searchParams.get('busqueda') ?? '').toLowerCase();
    const filtradas = estado.evaluaciones.filter((item) => (
      (cicloId === null || String(item.cicloId) === cicloId) && (filtro === null || item.estado === filtro) && item.empleadoNombre.toLowerCase().includes(busqueda)
    ));
    return opciones.listaVacia ? Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 }) : pagina(parsed, filtradas, opciones);
  }
  const id = Number(partes[1]);
  const cambiar = (nuevo: EvaluacionListado['estado'], puntaje: number | null, etiqueta: string | null): void => {
    estado.evaluaciones = estado.evaluaciones.map((item) => (item.id === id ? { ...item, estado: nuevo, puntajeFinal: puntaje, etiqueta } : item));
  };
  if (partes[2] === '/calificaciones') {
    const solicitud = cuerpo as { calificaciones: { competenciaId: number; calificacion: number }[] };
    estado.calificaciones.set(id, new Map(solicitud.calificaciones.map((item) => [item.competenciaId, item.calificacion])));
  } else if (partes[2] === '/envio') {
    cambiar('Enviada', 3.55, 'Competente');
  } else if (partes[2] === '/cierre') {
    cambiar('Cerrada', 3.55, 'Competente');
  } else {
    const detalle = detalleEvaluacion(estado, id);
    return detalle === null ? error(404, `La evaluación ${id} no existe.`) : Response.json(detalle);
  }
  return Response.json({ id });
}

export function prepararApiEvaluacion(opciones: OpcionesApiEvaluacion = {}) {
  const llamadas: LlamadaEvaluacion[] = [];
  const estado: Estado = { ciclos: ciclosIniciales(), evaluaciones: evaluacionesIniciales(), calificaciones: new Map() };
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
    if (parsed.pathname.includes('/ciclos-evaluacion')) {
      return respuestaCiclos(parsed, metodo, cuerpo, estado, opciones);
    }
    return respuestaEvaluaciones(parsed, metodo, cuerpo, estado, opciones);
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

// Los roles viajan en la sesión: sin RRHH ni ADMIN los botones de escritura quedan deshabilitados.
export function renderEvaluacion(roles: string[] = ['RRHH'], path = '/modulos/68-evaluacion-desempeno') {
  setSesion({
    token: 'token-de-prueba',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    usuario: { id: 1, nombreUsuario: 'rrhh', nombreCompleto: 'Usuario de prueba' },
    roles,
  });
  return render(
    <SesionProvider>
      <MemoryRouter initialEntries={[path]}>
        <EvaluacionDesempenoPage />
      </MemoryRouter>
    </SesionProvider>,
  );
}

export function urlsQueContienen(fetchMock: ReturnType<typeof vi.fn>, fragmento: string): string[] {
  return fetchMock.mock.calls.map((llamada) => String(llamada[0])).filter((url) => url.includes(fragmento));
}

export async function esperarCargaCiclos(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando ciclos…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: '2026-S2' })).toBeInTheDocument();
    expect(urlsQueContienen(fetchMock, '/ciclos-evaluacion?')).toHaveLength(1);
  });
}

// Espera la lista, los ciclos del filtro y los empleados del selector (cada uno se pide una sola vez).
export async function esperarCargaEvaluaciones(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando evaluaciones…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Ana Torres Vega' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: '2026-S2' })).toBeInTheDocument();
    expect(urlsQueContienen(fetchMock, '/evaluaciones?')).toHaveLength(1);
    expect(urlsQueContienen(fetchMock, '/empleados?')).toHaveLength(1);
  });
}

export async function montarCiclosCargados(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  renderEvaluacion(roles);
  await esperarCargaCiclos(fetchMock);
}

export async function montarEvaluacionesCargadas(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  renderEvaluacion(roles, '/modulos/68-evaluacion-desempeno?vista=evaluaciones');
  await esperarCargaEvaluaciones(fetchMock);
}

export function limpiarEvaluacion(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  limpiarSesion();
}
