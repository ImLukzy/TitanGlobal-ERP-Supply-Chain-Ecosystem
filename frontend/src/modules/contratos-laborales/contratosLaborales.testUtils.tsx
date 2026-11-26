import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { limpiarSesion, setSesion } from '../../api/sesionStore';
import { SesionProvider } from '../../layout/SesionContext';
import type { EmpleadoListado } from '../empleados/tipos';
import { ContratosLaboralesPage } from './ContratosLaboralesPage';
import type { ContratoLaboralDetalle, ContratoLaboralListado } from './tipos';

export interface LlamadaContratos {
  url: string;
  metodo: string;
  cuerpo: unknown;
}

export type AccionEscritura = 'crear' | 'renovacion' | 'adendas' | 'anulacion';

export interface OpcionesApiContratos {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorResumen?: boolean;
  errorDetalle?: boolean;
  totalEmpleados?: number;
  totalListado?: number;
  errorEscritura?: { accion: AccionEscritura; status: number; mensaje: string };
}

const base = {
  contratoAnteriorId: null,
  contratoAnteriorNumero: null,
  fechaCierre: null,
  motivoCierre: null,
  adendas: [],
};

// Los contratos replican el seed de la spec backend del 62 (CTR-000005 vencido, CTR-000001 con adenda) y añaden CTR-000008 para las acciones.
export const contratosMock: ContratoLaboralDetalle[] = [
  {
    ...base,
    id: 1,
    numero: 'CTR-000001',
    empleadoId: 1,
    empleadoLegajo: 'EMP-000001',
    empleadoNombre: 'Ana Torres Vega',
    modalidad: 'Indeterminado',
    fechaInicio: '2021-03-01',
    fechaFin: null,
    sueldoBasico: 8500,
    cargo: 'Gerente de Ventas',
    estado: 'Vigente',
    cargoVigente: 'Gerente Comercial',
    sueldoVigente: 9500,
    adendas: [{ id: 1, fechaVigencia: '2024-01-01', cargo: 'Gerente Comercial', sueldoBasico: 9500 }],
  },
  {
    ...base,
    id: 2,
    numero: 'CTR-000002',
    empleadoId: 2,
    empleadoLegajo: 'EMP-000002',
    empleadoNombre: 'Luis Rojas Mendoza',
    modalidad: 'PlazoFijo',
    fechaInicio: '2019-08-16',
    fechaFin: '2020-08-15',
    sueldoBasico: 3200,
    cargo: 'Analista de Compras',
    estado: 'Renovado',
    cargoVigente: 'Analista de Compras',
    sueldoVigente: 3200,
  },
  {
    ...base,
    id: 3,
    numero: 'CTR-000003',
    empleadoId: 2,
    empleadoLegajo: 'EMP-000002',
    empleadoNombre: 'Luis Rojas Mendoza',
    modalidad: 'PlazoFijo',
    fechaInicio: '2020-08-16',
    fechaFin: '2021-08-15',
    sueldoBasico: 3400,
    cargo: 'Analista de Compras',
    estado: 'Renovado',
    cargoVigente: 'Analista de Compras',
    sueldoVigente: 3400,
    contratoAnteriorId: 2,
    contratoAnteriorNumero: 'CTR-000002',
  },
  {
    ...base,
    id: 4,
    numero: 'CTR-000004',
    empleadoId: 2,
    empleadoLegajo: 'EMP-000002',
    empleadoNombre: 'Luis Rojas Mendoza',
    modalidad: 'Indeterminado',
    fechaInicio: '2021-08-16',
    fechaFin: null,
    sueldoBasico: 4200,
    cargo: 'Analista de Compras',
    estado: 'Vigente',
    cargoVigente: 'Analista de Compras',
    sueldoVigente: 4200,
    contratoAnteriorId: 3,
    contratoAnteriorNumero: 'CTR-000003',
  },
  {
    ...base,
    id: 5,
    numero: 'CTR-000005',
    empleadoId: 3,
    empleadoLegajo: 'EMP-000003',
    empleadoNombre: 'Maria Salazar Quispe',
    modalidad: 'Formativo',
    fechaInicio: '2024-01-08',
    fechaFin: '2024-12-31',
    sueldoBasico: 1500,
    cargo: 'Almacenero',
    estado: 'Vencido',
    cargoVigente: 'Almacenero',
    sueldoVigente: 1500,
  },
  {
    ...base,
    id: 6,
    numero: 'CTR-000006',
    empleadoId: 4,
    empleadoLegajo: 'EMP-000004',
    empleadoNombre: 'Carlos Diaz Soto',
    modalidad: 'PlazoFijo',
    fechaInicio: '2024-05-02',
    fechaFin: '2027-05-01',
    sueldoBasico: 2600,
    cargo: 'Auxiliar de Logistica',
    estado: 'Cerrado',
    cargoVigente: 'Auxiliar de Logistica',
    sueldoVigente: 2600,
    fechaCierre: '2026-09-30',
    motivoCierre: 'Cese del empleado',
  },
  {
    ...base,
    id: 7,
    numero: 'CTR-000007',
    empleadoId: 1,
    empleadoLegajo: 'EMP-000001',
    empleadoNombre: 'Ana Torres Vega',
    modalidad: 'PlazoFijo',
    fechaInicio: '2026-01-01',
    fechaFin: '2026-12-31',
    sueldoBasico: 8500,
    cargo: 'Gerente de Ventas',
    estado: 'Anulado',
    cargoVigente: 'Gerente de Ventas',
    sueldoVigente: 8500,
    fechaCierre: '2026-01-05',
    motivoCierre: 'Registro duplicado por error',
  },
  {
    ...base,
    id: 8,
    numero: 'CTR-000008',
    empleadoId: 5,
    empleadoLegajo: 'EMP-000005',
    empleadoNombre: 'Rosa Vega Paz',
    modalidad: 'PlazoFijo',
    fechaInicio: '2026-05-02',
    fechaFin: '2027-05-01',
    sueldoBasico: 2600,
    cargo: 'Auxiliar de Logistica',
    estado: 'Vigente',
    cargoVigente: 'Auxiliar de Logistica',
    sueldoVigente: 2600,
  },
];

const empleadosActivos: EmpleadoListado[] = [
  { id: 1, legajo: 'EMP-000001', tipoDocumento: 'DNI', numeroDocumento: '10000001', nombres: 'Ana', apellidos: 'Torres Vega', cargo: 'Gerente de Ventas', area: 'Ventas', fechaIngreso: '2021-03-01', estado: 'Activo' },
  { id: 2, legajo: 'EMP-000002', tipoDocumento: 'DNI', numeroDocumento: '10000002', nombres: 'Luis', apellidos: 'Rojas Mendoza', cargo: 'Analista de Compras', area: 'Compras', fechaIngreso: '2019-08-16', estado: 'Activo' },
  { id: 3, legajo: 'EMP-000003', tipoDocumento: 'CE', numeroDocumento: '004512367', nombres: 'Maria', apellidos: 'Salazar Quispe', cargo: 'Almacenero', area: 'Almacen', fechaIngreso: '2024-01-08', estado: 'Activo' },
];

function listado(contrato: ContratoLaboralDetalle): ContratoLaboralListado {
  return {
    id: contrato.id,
    numero: contrato.numero,
    empleadoId: contrato.empleadoId,
    empleadoLegajo: contrato.empleadoLegajo,
    empleadoNombre: contrato.empleadoNombre,
    modalidad: contrato.modalidad,
    fechaInicio: contrato.fechaInicio,
    fechaFin: contrato.fechaFin,
    sueldoBasico: contrato.sueldoBasico,
    cargo: contrato.cargo,
    estado: contrato.estado,
  };
}

function error(status: number, mensaje: string): Response {
  return Response.json({ StatusCode: status, Message: mensaje }, { status });
}

function respuestaLista(parsed: URL, opciones: OpcionesApiContratos): Response {
  if (opciones.errorLista) {
    return error(500, 'No se pudo cargar la lista de contratos.');
  }
  if (opciones.listaVacia) {
    return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
  }
  const busqueda = parsed.searchParams.get('busqueda')?.toLowerCase() ?? '';
  const estado = parsed.searchParams.get('estado');
  const modalidad = parsed.searchParams.get('modalidad');
  const pagina = Number(parsed.searchParams.get('pagina') ?? '1');
  const tamano = Number(parsed.searchParams.get('tamanoPagina') ?? '10');
  const filtrados = contratosMock.filter((contrato) => {
    const coincideBusqueda = busqueda === ''
      || [contrato.numero, contrato.cargo, contrato.empleadoNombre, contrato.empleadoLegajo]
        .some((campo) => campo.toLowerCase().includes(busqueda));
    return coincideBusqueda
      && (estado === null || contrato.estado === estado)
      && (modalidad === null || contrato.modalidad === modalidad);
  });
  const inicio = (pagina - 1) * tamano;
  return Response.json({
    items: filtrados.slice(inicio, inicio + tamano).map(listado),
    total: opciones.totalListado ?? filtrados.length,
    pagina,
    tamanoPagina: tamano,
  });
}

function respuestaConteo(parsed: URL, opciones: OpcionesApiContratos): Response {
  if (opciones.errorResumen) {
    return error(500, 'No se pudo cargar el resumen de contratos.');
  }
  return respuestaLista(parsed, {});
}

function respuestaDetalle(id: number, opciones: OpcionesApiContratos): Response {
  if (opciones.errorDetalle) {
    return error(500, 'No se pudo cargar el detalle del contrato.');
  }
  const contrato = contratosMock.find((item) => item.id === id);
  if (contrato === undefined) {
    return error(404, `El contrato laboral ${id} no existe.`);
  }
  return Response.json(contrato);
}

function respuestaEmpleados(opciones: OpcionesApiContratos): Response {
  return Response.json({
    items: empleadosActivos,
    total: opciones.totalEmpleados ?? empleadosActivos.length,
    pagina: 1,
    tamanoPagina: 100,
  });
}

function respuestaMock(parsed: URL, metodo: string, opciones: OpcionesApiContratos): Response {
  const ruta = parsed.pathname;
  if (ruta.endsWith('/empleados')) {
    return respuestaEmpleados(opciones);
  }
  const accion = /\/contratos-laborales\/(\d+)\/(renovacion|adendas|anulacion)$/.exec(ruta);
  if (accion) {
    if (opciones.errorEscritura?.accion === accion[2]) {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json(accion[2] === 'renovacion' ? { id: 9, numero: 'CTR-000009' } : { id: Number(accion[1]) });
  }
  const individual = /\/contratos-laborales\/(\d+)$/.exec(ruta);
  if (individual) {
    return respuestaDetalle(Number(individual[1]), opciones);
  }
  if (ruta.endsWith('/contratos-laborales') && metodo === 'POST') {
    if (opciones.errorEscritura?.accion === 'crear') {
      return error(opciones.errorEscritura.status, opciones.errorEscritura.mensaje);
    }
    return Response.json({ id: 9, numero: 'CTR-000009' });
  }
  if (ruta.endsWith('/contratos-laborales') && parsed.searchParams.get('tamanoPagina') === '1') {
    return respuestaConteo(parsed, opciones);
  }
  if (ruta.endsWith('/contratos-laborales')) {
    return respuestaLista(parsed, opciones);
  }
  return error(404, 'Ruta no prevista en la prueba.');
}

export function prepararApiContratos(opciones: OpcionesApiContratos = {}) {
  const llamadas: LlamadaContratos[] = [];
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
export function renderContratos(roles: string[] = ['RRHH'], path = '/modulos/62-contratos-laborales') {
  setSesion({
    token: 'token-de-prueba',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    usuario: { id: 1, nombreUsuario: 'rrhh', nombreCompleto: 'Usuario de prueba' },
    roles,
  });
  return render(
    <SesionProvider>
      <MemoryRouter initialEntries={[path]}>
        <ContratosLaboralesPage />
      </MemoryRouter>
    </SesionProvider>,
  );
}

// Los conteos del resumen son listados con tamanoPagina=1; el listado de la tabla usa tamanoPagina=10.
export function urlsDeListado(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.includes('/contratos-laborales?') && url.endsWith('tamanoPagina=10'));
}

export function urlsDeConteo(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.includes('/contratos-laborales?') && url.endsWith('tamanoPagina=1'));
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando contratos…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de contratos…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'CTR-000001' })).toBeInTheDocument();
    expect(urlsDeListado(fetchMock)).toHaveLength(1);
    expect(urlsDeConteo(fetchMock)).toHaveLength(3);
  });
}

// Monta la pantalla con temporizadores falsos para que el temporizador de búsqueda (300 ms) se resuelva sin esperas reales.
export async function montarContratosCargados(
  fetchMock: ReturnType<typeof vi.fn>,
  roles: string[] = ['RRHH'],
): Promise<void> {
  vi.useFakeTimers();
  renderContratos(roles);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock);
}

// Para listas vacías o con error no hay filas que esperar: solo se espera el fin del temporizador.
export async function montarSinFilas(roles: string[] = ['RRHH']): Promise<void> {
  vi.useFakeTimers();
  renderContratos(roles);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
}

export function limpiarContratos(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  limpiarSesion();
}
