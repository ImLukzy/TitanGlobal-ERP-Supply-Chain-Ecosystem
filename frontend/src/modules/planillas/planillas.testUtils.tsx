import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { limpiarSesion, setSesion } from '../../api/sesionStore';
import { formateadorMoneda } from '../../lib/formato';
import { SesionProvider } from '../../layout/SesionContext';
import type { EmpleadoListado } from '../empleados/tipos';
import { PlanillasPage } from './PlanillasPage';
import type {
  AfiliacionPension,
  BoletaPago,
  BonoPlanilla,
  EstadoPlanilla,
  PlanillaEmpleado,
  PlanillaListado,
  ParametrosPlanilla,
  ResumenPlanillas,
} from './tipos';

// Importe como lo normaliza Testing Library: el espacio fino de Intl se compara como espacio normal.
export function moneda(valor: number): string {
  return formateadorMoneda.format(valor).replace(/\s/g, ' ');
}

export interface LlamadaPlanillas {
  url: string;
  metodo: string;
  cuerpo: unknown;
}

export type AccionEscrituraPlanillas =
  | 'crear' | 'calcular' | 'aprobar' | 'pagar' | 'crear-bono' | 'eliminar-bono' | 'guardar-afiliacion';

export interface OpcionesApiPlanillas {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorResumen?: boolean;
  errorDetalles?: boolean;
  errorBoleta?: boolean;
  errorBonos?: boolean;
  errorAfiliaciones?: boolean;
  totalListado?: number;
  errorEscritura?: { accion: AccionEscrituraPlanillas; status: number; mensaje: string };
}

const base = { empleados: 2, incidencias: 1, totalIngresos: 11000, totalDescuentos: 2000, totalNeto: 9000 };

export function planillasIniciales(): PlanillaListado[] {
  return [
    { id: 1, numero: 'PLA-202611-M', anio: 2026, mes: 11, tipo: 'Mensual', estado: 'Borrador', ...base, empleados: 0, incidencias: 0, totalIngresos: 0, totalDescuentos: 0, totalNeto: 0 },
    { id: 2, numero: 'PLA-202610-M', anio: 2026, mes: 10, tipo: 'Mensual', estado: 'Pagada', ...base },
    { id: 3, numero: 'PLA-202609-M', anio: 2026, mes: 9, tipo: 'Mensual', estado: 'Calculada', ...base },
    { id: 4, numero: 'PLA-202608-M', anio: 2026, mes: 8, tipo: 'Mensual', estado: 'Aprobada', ...base },
  ];
}

export const resumenMock: ResumenPlanillas = {
  ultimoPeriodo: '2026-11',
  estadoUltimo: 'Borrador',
  netoUltimo: 9000,
  empleadosUltimo: 2,
  incidenciasUltimo: 1,
  borradoresYCalculadas: 2,
};

function empleadoPlanilla(id: number, legajo: string, nombre: string, extra: Partial<PlanillaEmpleado>): PlanillaEmpleado {
  return {
    id, empleadoId: id, empleadoLegajo: legajo, empleadoNombre: nombre, cargo: 'Analista', sueldoBasico: 5000, diasContrato: 30, diasSinGoce: 0,
    diasFalta: 0, diasLaborados: 30, ingresoBasico: 5000, minutosExtra: 0, ingresoHorasExtra: 0, ingresoBonos: 0, totalIngresos: 5000,
    codigoPension: 'ONP', descuentoPension: 650, descuentoQuinta: 0, descuentoPrestamos: 100, totalDescuentos: 750, neto: 4250, aporteEssalud: 450,
    incidencia: null, numeroBoleta: 'BOL-202610-M-000001', ...extra,
  };
}

export const detallesMock: PlanillaEmpleado[] = [
  empleadoPlanilla(11, 'EMP-000001', 'Ana Torres Vega', {}),
  empleadoPlanilla(12, 'EMP-000002', 'Luis Rojas Mendoza', { totalIngresos: 6000, neto: 5000, numeroBoleta: 'BOL-202610-M-000002' }),
  empleadoPlanilla(13, 'EMP-000003', 'Maria Salazar Quispe', {
    totalIngresos: 0, neto: 0, descuentoPension: 0, descuentoPrestamos: 0, totalDescuentos: 0, codigoPension: null, numeroBoleta: null,
    incidencia: 'Sin afiliación a un sistema de pensiones.',
  }),
];

export const boletaMock: BoletaPago = {
  empresa: 'TitanGlobal', numeroBoleta: 'BOL-202610-M-000001', numeroPlanilla: 'PLA-202610-M', tipo: 'Mensual', anio: 2026, mes: 10, empleadoId: 1,
  empleadoLegajo: 'EMP-000001', empleadoNombre: 'Ana Torres Vega', cargo: 'Gerente de Ventas', codigoPension: 'ONP', sueldoBasico: 5000, diasContrato: 30,
  diasSinGoce: 0, diasFalta: 0, diasLaborados: 30, ingresoBasico: 5000, minutosExtra: 90, ingresoHorasExtra: 120.5, ingresoBonos: 300, totalIngresos: 5420.5,
  descuentoPension: 704.67, descuentoQuinta: 0, descuentoPrestamos: 100, totalDescuentos: 804.67, aporteEssalud: 487.85, neto: 4615.83, fechaPago: '2026-11-05',
};

export const bonosMock: BonoPlanilla[] = [
  { id: 1, empleadoId: 1, empleadoLegajo: 'EMP-000001', empleadoNombre: 'Ana Torres Vega', anio: 2026, mes: 10, tipoPlanilla: 'Mensual', concepto: 'Bono por metas', monto: 300, planillaId: 2, planillaNumero: 'PLA-202610-M' },
  { id: 2, empleadoId: 2, empleadoLegajo: 'EMP-000002', empleadoNombre: 'Luis Rojas Mendoza', anio: 2026, mes: 11, tipoPlanilla: 'Complementaria', concepto: 'Ajuste de campaña', monto: 150.5, planillaId: null, planillaNumero: null },
];

export const afiliacionesMock: AfiliacionPension[] = [
  { id: 1, empleadoId: 1, empleadoLegajo: 'EMP-000001', empleadoNombre: 'Ana Torres Vega', codigoPension: 'INTEGRA', sistema: 'AFP', cuspp: '123456ABCDEF', fechaAfiliacion: '2021-03-01' },
  { id: 2, empleadoId: 2, empleadoLegajo: 'EMP-000002', empleadoNombre: 'Luis Rojas Mendoza', codigoPension: 'ONP', sistema: 'ONP', cuspp: null, fechaAfiliacion: '2019-08-16' },
];

export const parametrosMock: ParametrosPlanilla = {
  parametros: [
    { clave: 'UIT', valor: 5350, descripcion: 'Unidad Impositiva Tributaria' },
    { clave: 'TASA_ESSALUD', valor: 0.09, descripcion: 'Aporte del empleador a EsSalud' },
  ],
  pensiones: [
    { codigo: 'ONP', sistema: 'ONP', tasaAporte: 0.13, tasaPrimaSeguro: 0, tasaComision: 0, remuneracionMaximaAsegurable: null },
    { codigo: 'INTEGRA', sistema: 'AFP', tasaAporte: 0.1, tasaPrimaSeguro: 0.0137, tasaComision: 0.0155, remuneracionMaximaAsegurable: 12027.1 },
  ],
  tramos: [
    { orden: 1, hastaUit: 5, tasa: 0.08 },
    { orden: 2, hastaUit: null, tasa: 0.3 },
  ],
};

const empleadosMock: EmpleadoListado[] = [
  { id: 1, legajo: 'EMP-000001', tipoDocumento: 'DNI', numeroDocumento: '10000001', nombres: 'Ana', apellidos: 'Torres Vega', cargo: 'Gerente de Ventas', area: 'Ventas', fechaIngreso: '2021-03-01', estado: 'Activo' },
  { id: 2, legajo: 'EMP-000002', tipoDocumento: 'DNI', numeroDocumento: '10000002', nombres: 'Luis', apellidos: 'Rojas Mendoza', cargo: 'Analista de Compras', area: 'Compras', fechaIngreso: '2019-08-16', estado: 'Activo' },
  { id: 3, legajo: 'EMP-000003', tipoDocumento: 'CE', numeroDocumento: '004512367', nombres: 'Maria', apellidos: 'Salazar Quispe', cargo: 'Almacenero', area: 'Almacen', fechaIngreso: '2024-01-08', estado: 'Activo' },
];

function error(status: number, mensaje: string): Response {
  return Response.json({ StatusCode: status, Message: mensaje }, { status });
}

function pagina<T>(parsed: URL, items: T[], opciones: OpcionesApiPlanillas): Response {
  const numero = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  const inicio = (numero - 1) * tamano;
  return Response.json({ items: items.slice(inicio, inicio + tamano), total: opciones.totalListado ?? items.length, pagina: numero, tamanoPagina: tamano });
}

interface Estado {
  planillas: PlanillaListado[];
  bonos: BonoPlanilla[];
}

function fallaDe(accion: AccionEscrituraPlanillas, opciones: OpcionesApiPlanillas): Response | null {
  if (opciones.errorEscritura?.accion === accion) {
    return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
  }
  return null;
}

function cambiarEstado(estado: Estado, id: number, nuevo: EstadoPlanilla): void {
  estado.planillas = estado.planillas.map((planilla) => (planilla.id === id ? { ...planilla, estado: nuevo } : planilla));
}

function respuestaPlanillas(parsed: URL, metodo: string, cuerpo: unknown, estado: Estado, opciones: OpcionesApiPlanillas): Response {
  const ruta = parsed.pathname;
  if (ruta.endsWith('/planillas/resumen')) {
    return opciones.errorResumen ? error(500, 'No se pudo cargar el resumen de planillas.') : Response.json(resumenMock);
  }
  if (ruta.endsWith('/planillas/parametros')) {
    return Response.json(parametrosMock);
  }
  if (/\/planillas\/boletas\/\d+$/.test(ruta)) {
    return opciones.errorBoleta ? error(409, 'La boleta se genera al aprobar la planilla.') : Response.json(boletaMock);
  }
  const detalles = /\/planillas\/(\d+)\/detalles$/.exec(ruta);
  if (detalles) {
    if (opciones.errorDetalles) {
      return error(500, 'No se pudo cargar el detalle por empleado.');
    }
    const busqueda = (parsed.searchParams.get('busqueda') ?? '').toLowerCase();
    const soloIncidencias = parsed.searchParams.get('soloIncidencias') === 'true';
    const filtrados = detallesMock.filter((detalle) => (
      (busqueda === '' || `${detalle.empleadoLegajo} ${detalle.empleadoNombre}`.toLowerCase().includes(busqueda))
      && (!soloIncidencias || detalle.incidencia !== null)
    ));
    return pagina(parsed, filtrados, opciones);
  }
  const accion = /\/planillas\/(\d+)\/(calculo|aprobacion|pago)$/.exec(ruta);
  if (accion) {
    const id = Number(accion[1]);
    const nombre = accion[2] === 'calculo' ? 'calcular' : accion[2] === 'aprobacion' ? 'aprobar' : 'pagar';
    const falla = fallaDe(nombre, opciones);
    if (falla !== null) {
      return falla;
    }
    cambiarEstado(estado, id, nombre === 'calcular' ? 'Calculada' : nombre === 'aprobar' ? 'Aprobada' : 'Pagada');
    return Response.json(nombre === 'calcular' ? { id, empleados: 2, incidencias: 1 } : { id });
  }
  const una = /\/planillas\/(\d+)$/.exec(ruta);
  if (una) {
    const planilla = estado.planillas.find((item) => item.id === Number(una[1]));
    if (planilla === undefined) {
      return error(404, `La planilla ${una[1]} no existe.`);
    }
    return Response.json({
      ...planilla, totalAportesEmpleador: 900, fechaCalculo: '2026-11-03T15:00:00Z', calculadoPor: 'rrhh',
      aprobadoPor: planilla.estado === 'Aprobada' || planilla.estado === 'Pagada' ? 'rrhh' : null,
      fechaAprobacion: planilla.estado === 'Aprobada' || planilla.estado === 'Pagada' ? '2026-11-04T15:00:00Z' : null,
      fechaPago: planilla.estado === 'Pagada' ? '2026-11-05' : null,
    });
  }
  if (metodo === 'POST') {
    const falla = fallaDe('crear', opciones);
    if (falla !== null) {
      return falla;
    }
    const solicitud = cuerpo as { anio: number; mes: number; tipo: PlanillaListado['tipo'] };
    estado.planillas = [
      { id: 9, numero: 'PLA-202001-M', anio: solicitud.anio, mes: solicitud.mes, tipo: solicitud.tipo, estado: 'Borrador', ...base, empleados: 0, incidencias: 0, totalIngresos: 0, totalDescuentos: 0, totalNeto: 0 },
      ...estado.planillas,
    ];
    return Response.json({ id: 9, numero: 'PLA-202001-M' });
  }
  if (opciones.errorLista) {
    return error(500, 'No se pudo cargar la lista de planillas.');
  }
  if (opciones.listaVacia) {
    return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
  }
  const anio = parsed.searchParams.get('anio');
  const tipo = parsed.searchParams.get('tipo');
  const estadoFiltro = parsed.searchParams.get('estado');
  const filtradas = estado.planillas.filter((planilla) => (
    (anio === null || String(planilla.anio) === anio) && (tipo === null || planilla.tipo === tipo) && (estadoFiltro === null || planilla.estado === estadoFiltro)
  ));
  return pagina(parsed, filtradas, opciones);
}

function respuestaMock(parsed: URL, metodo: string, cuerpo: unknown, estado: Estado, opciones: OpcionesApiPlanillas): Response {
  const ruta = parsed.pathname;
  if (ruta.endsWith('/empleados')) {
    return Response.json({ items: empleadosMock, total: empleadosMock.length, pagina: 1, tamanoPagina: 100 });
  }
  if (ruta.includes('/planillas')) {
    return respuestaPlanillas(parsed, metodo, cuerpo, estado, opciones);
  }
  if (/\/bonos-planilla\/\d+$/.test(ruta) && metodo === 'DELETE') {
    const falla = fallaDe('eliminar-bono', opciones);
    if (falla === null) {
      const id = Number(ruta.split('/').at(-1));
      estado.bonos = estado.bonos.filter((bono) => bono.id !== id);
      return new Response(null, { status: 204 });
    }
    return falla;
  }
  if (ruta.endsWith('/bonos-planilla') && metodo === 'POST') {
    return fallaDe('crear-bono', opciones) ?? Response.json({ id: 7 });
  }
  if (ruta.endsWith('/bonos-planilla')) {
    if (opciones.errorBonos) {
      return error(500, 'No se pudo cargar la lista de bonos.');
    }
    const anio = parsed.searchParams.get('anio');
    const mes = parsed.searchParams.get('mes');
    const empleadoId = parsed.searchParams.get('empleadoId');
    const filtrados = estado.bonos.filter((bono) => (
      (anio === null || String(bono.anio) === anio) && (mes === null || String(bono.mes) === mes) && (empleadoId === null || String(bono.empleadoId) === empleadoId)
    ));
    return pagina(parsed, filtrados, opciones);
  }
  if (/\/afiliaciones-pension\/\d+$/.test(ruta) && metodo === 'PUT') {
    return fallaDe('guardar-afiliacion', opciones) ?? Response.json({ id: 5 });
  }
  if (ruta.endsWith('/afiliaciones-pension')) {
    return opciones.errorAfiliaciones ? error(500, 'No se pudo cargar la lista de afiliaciones.') : pagina(parsed, afiliacionesMock, opciones);
  }
  return error(404, 'Ruta no prevista en la prueba.');
}

export function prepararApiPlanillas(opciones: OpcionesApiPlanillas = {}) {
  const llamadas: LlamadaPlanillas[] = [];
  const estado: Estado = { planillas: planillasIniciales(), bonos: [...bonosMock] };
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    const cuerpo = typeof init.body === 'string' ? JSON.parse(init.body) as unknown : undefined;
    llamadas.push({ url, metodo, cuerpo });
    return respuestaMock(parsed, metodo, cuerpo, estado, opciones);
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

// Los roles viajan en la sesión: sin RRHH ni ADMIN los botones de escritura quedan deshabilitados.
export function renderPlanillas(roles: string[] = ['RRHH'], path = '/modulos/66-planillas') {
  setSesion({
    token: 'token-de-prueba',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    usuario: { id: 1, nombreUsuario: 'rrhh', nombreCompleto: 'Usuario de prueba' },
    roles,
  });
  return render(
    <SesionProvider>
      <MemoryRouter initialEntries={[path]}>
        <PlanillasPage />
      </MemoryRouter>
    </SesionProvider>,
  );
}

export function urlsQueTerminanEn(fetchMock: ReturnType<typeof vi.fn>, fragmento: string, final: string): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.includes(fragmento) && url.endsWith(final));
}

export function urlsDeListadoPlanillas(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueTerminanEn(fetchMock, '/planillas?', 'tamanoPagina=10');
}

export function urlsDeDetalles(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueTerminanEn(fetchMock, '/detalles?', 'tamanoPagina=10');
}

export async function esperarCargaInicialPlanillas(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando planillas…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de planillas…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'PLA-202611-M' })).toBeInTheDocument();
    expect(urlsDeListadoPlanillas(fetchMock)).toHaveLength(1);
    expect(urlsQueTerminanEn(fetchMock, '/planillas/resumen', '/planillas/resumen')).toHaveLength(1);
  });
}

export async function montarPlanillasCargadas(fetchMock: ReturnType<typeof vi.fn>, roles: string[] = ['RRHH']): Promise<void> {
  renderPlanillas(roles, '/modulos/66-planillas');
  await esperarCargaInicialPlanillas(fetchMock);
}

// Selecciona una planilla y espera su cabecera y la tabla de empleados.
export async function seleccionarPlanilla(numero: string, titulo: string): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: numero }));
  await waitFor(() => {
    expect(screen.getByRole('heading', { name: titulo })).toBeInTheDocument();
    expect(screen.queryByText('Cargando empleados de la planilla…')).not.toBeInTheDocument();
    expect(screen.getByText('Ana Torres Vega')).toBeInTheDocument();
  });
}

export async function esperarCargaBonos(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando bonos…')).not.toBeInTheDocument();
    expect(screen.getByText('Bono por metas')).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'EMP-000001 · Torres Vega, Ana' })).toBeInTheDocument();
    expect(urlsQueTerminanEn(fetchMock, '/bonos-planilla?', 'tamanoPagina=10')).toHaveLength(1);
  });
}

export async function esperarCargaAfiliaciones(): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando afiliaciones…')).not.toBeInTheDocument();
    expect(screen.getByText('INTEGRA (AFP)')).toBeInTheDocument();
    expect(screen.getByText('Sin afiliación')).toBeInTheDocument();
  });
}

export function limpiarPlanillas(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  limpiarSesion();
  document.body.classList.remove('planillas-imprimiendo');
}
