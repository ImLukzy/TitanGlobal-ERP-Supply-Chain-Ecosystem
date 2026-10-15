import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { ComisionesVendedoresPage } from './ComisionesVendedoresPage';
import type { ComisionVendedor, ComisionesMes, Liquidacion, OrdenVendedor, VendedorDetalle, VendedorListado } from './tipos';

export const vendedoresIniciales: VendedorDetalle[] = [
  { id: 1, codigo: 'V-ANA', nombre: 'Ana Torres', email: 'ana@titanglobal.example', porcentajeComision: 5, activo: true, ordenesAsignadas: 2, ventasTotales: 12000 },
  { id: 2, codigo: 'V-LUIS', nombre: 'Luis Ramos', email: 'luis@titanglobal.example', porcentajeComision: 3, activo: true, ordenesAsignadas: 1, ventasTotales: 8000 },
  { id: 3, codigo: 'V-OLD', nombre: 'Vendedor inactivo', email: null, porcentajeComision: 4, activo: false, ordenesAsignadas: 0, ventasTotales: 0 },
];

const ordenesIniciales: OrdenVendedor[] = [
  { ordenCompraId: 101, numeroOrden: 'OC-2026-000101', fecha: '2026-01-12T10:00:00Z', clienteNombre: 'Acme Industrial', total: 4000, vendedorId: 1, vendedorCodigo: 'V-ANA', vendedorNombre: 'Ana Torres' },
  { ordenCompraId: 102, numeroOrden: 'OC-2026-000102', fecha: '2026-01-20T14:00:00Z', clienteNombre: 'Beta Comercio', total: 2500, vendedorId: null, vendedorCodigo: null, vendedorNombre: null },
];

const comisionCalculada: ComisionVendedor = { vendedorId: 1, codigo: 'V-ANA', nombre: 'Ana Torres', ordenes: 2, ventas: 10000, porcentaje: 5, comision: 500, estado: 'Calculada', liquidacionId: 21 };
const comisionPendiente: ComisionVendedor = { vendedorId: 2, codigo: 'V-LUIS', nombre: 'Luis Ramos', ordenes: 1, ventas: 7500, porcentaje: 3, comision: 225, estado: 'Pendiente', liquidacionId: null };

function comisionesMes(mes: number, liquidado: boolean): ComisionesMes {
  const pagadas = mes === 1;
  const filas: ComisionVendedor[] = mes === 1
    ? [{ ...comisionCalculada, estado: 'Pagada', comision: 500 }, { ...comisionCalculada, vendedorId: 2, codigo: 'V-LUIS', nombre: 'Luis Ramos', estado: 'Pagada', porcentaje: 3, comision: 300, liquidacionId: 22 }]
    : mes === 2
      ? [{ ...comisionCalculada }]
      : [{ ...comisionPendiente }];
  if (pagadas) {
    filas.forEach((fila) => { fila.estado = 'Pagada'; });
  }
  return { anio: 2026, mes, liquidado, filas, totalVentas: filas.reduce((suma, fila) => suma + fila.ventas, 0), totalComision: filas.reduce((suma, fila) => suma + fila.comision, 0) };
}

interface OpcionesMock {
  codigoDuplicado?: boolean;
  bajaConVentas?: boolean;
  asignacionMesLiquidado?: boolean;
  liquidacionDuplicada?: boolean;
  pagoDuplicado?: boolean;
}

export function prepararApiComisiones(opciones: OpcionesMock = {}) {
  const vendedores: VendedorDetalle[] = structuredClone(vendedoresIniciales);
  const ordenes: OrdenVendedor[] = structuredClone(ordenesIniciales);
  const llamadas: { url: string; init: RequestInit }[] = [];
  let estadoMarzoLiquidado = false;
  let estadoFebreroPagado = false;
  const fetchMock = vi.fn(async (entrada: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(entrada);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });

    if (parsed.pathname.endsWith('/vendedores') && metodo === 'GET') {
      let items: VendedorListado[] = vendedores;
      const busqueda = (parsed.searchParams.get('busqueda') ?? '').toLocaleLowerCase();
      const activo = parsed.searchParams.get('activo');
      items = items.filter((vendedor) => `${vendedor.codigo} ${vendedor.nombre}`.toLocaleLowerCase().includes(busqueda));
      if (activo !== null) {
        items = items.filter((vendedor) => String(vendedor.activo) === activo);
      }
      const total = items.length;
      return Response.json({ items, total, pagina: Number(parsed.searchParams.get('pagina') ?? 1), tamanoPagina: Number(parsed.searchParams.get('tamanoPagina') ?? 10) });
    }
    if (parsed.pathname.match(/\/vendedores\/\d+$/) && metodo === 'GET') {
      const id = Number(parsed.pathname.split('/').at(-1));
      const vendedor = vendedores.find((item) => item.id === id);
      return vendedor === undefined ? Response.json({ StatusCode: 404, Message: `El vendedor ${id} no existe.` }, { status: 404 }) : Response.json(vendedor);
    }
    if ((parsed.pathname.endsWith('/vendedores') && metodo === 'POST') || (/\/vendedores\/\d+$/.test(parsed.pathname) && metodo === 'PUT')) {
      const request = JSON.parse(String(init.body)) as Omit<VendedorDetalle, 'id' | 'ordenesAsignadas' | 'ventasTotales'>;
      if (opciones.codigoDuplicado) {
        return Response.json({ StatusCode: 409, Message: `Ya existe un vendedor con el código ${request.codigo}.` }, { status: 409 });
      }
      const id = metodo === 'POST' ? 4 : Number(parsed.pathname.split('/').at(-1));
      const vendedor: VendedorDetalle = { id, ...request, ordenesAsignadas: 0, ventasTotales: 0 };
      if (metodo === 'POST') {
        vendedores.push(vendedor);
      } else {
        const indice = vendedores.findIndex((item) => item.id === id);
        vendedores[indice] = { ...vendedores[indice], ...vendedor };
      }
      return Response.json({ id });
    }
    if (parsed.pathname.match(/\/vendedores\/\d+$/) && metodo === 'DELETE') {
      const id = Number(parsed.pathname.split('/').at(-1));
      const vendedor = vendedores.find((item) => item.id === id);
      if (opciones.bajaConVentas && vendedor !== undefined) {
        return Response.json({ StatusCode: 409, Message: `No se puede eliminar el vendedor ${vendedor.codigo} porque tiene ventas asignadas.` }, { status: 409 });
      }
      return new Response(null, { status: 204 });
    }
    if (parsed.pathname.endsWith('/ordenes') && metodo === 'GET') {
      let items = ordenes;
      if (parsed.searchParams.get('sinAsignar') === 'true') {
        items = items.filter((orden) => orden.vendedorId === null);
      }
      if (parsed.searchParams.has('vendedorId')) {
        items = items.filter((orden) => orden.vendedorId === Number(parsed.searchParams.get('vendedorId')));
      }
      return Response.json({ items, total: items.length, pagina: Number(parsed.searchParams.get('pagina') ?? 1), tamanoPagina: Number(parsed.searchParams.get('tamanoPagina') ?? 10) });
    }
    if (/\/ordenes\/\d+\/vendedor$/.test(parsed.pathname) && metodo === 'PUT') {
      const id = Number(parsed.pathname.split('/').at(-2));
      const orden = ordenes.find((item) => item.ordenCompraId === id);
      if (opciones.asignacionMesLiquidado || parsed.searchParams.get('mes') === '1') {
        return Response.json({ StatusCode: 409, Message: 'El mes 01/2026 ya está liquidado; no se pueden cambiar sus asignaciones.' }, { status: 409 });
      }
      const solicitud = JSON.parse(String(init.body)) as { vendedorId: number };
      const vendedor = vendedores.find((item) => item.id === solicitud.vendedorId);
      if (orden !== undefined && vendedor !== undefined) {
        orden.vendedorId = vendedor.id;
        orden.vendedorNombre = vendedor.nombre;
        orden.vendedorCodigo = vendedor.codigo;
        return Response.json(orden);
      }
      return Response.json({});
    }
    if (/\/ordenes\/\d+\/vendedor$/.test(parsed.pathname) && metodo === 'DELETE') {
      const id = Number(parsed.pathname.split('/').at(-2));
      const orden = ordenes.find((item) => item.ordenCompraId === id);
      if (orden !== undefined) {
        orden.vendedorId = null;
        orden.vendedorCodigo = null;
        orden.vendedorNombre = null;
      }
      return new Response(null, { status: 204 });
    }
    if (parsed.pathname.endsWith('/comisiones') && metodo === 'GET') {
      const mes = Number(parsed.searchParams.get('mes'));
      const resultado = comisionesMes(mes, mes < 3 || (mes === 3 && estadoMarzoLiquidado));
      if (mes === 2 && estadoFebreroPagado) {
        resultado.filas[0]!.estado = 'Pagada';
      }
      return Response.json(resultado);
    }
    if (parsed.pathname.endsWith('/liquidaciones') && metodo === 'POST') {
      const request = JSON.parse(String(init.body)) as { anio: number; mes: number };
      if (opciones.liquidacionDuplicada || estadoMarzoLiquidado) {
        return Response.json({ StatusCode: 409, Message: 'El mes 03/2026 ya está liquidado.' }, { status: 409 });
      }
      estadoMarzoLiquidado = true;
      return Response.json(comisionesMes(request.mes, true));
    }
    if (/\/liquidaciones\/\d+\/pagar$/.test(parsed.pathname) && metodo === 'POST') {
      const id = Number(parsed.pathname.split('/').at(-2));
      if (opciones.pagoDuplicado || estadoFebreroPagado) {
        return Response.json({ StatusCode: 409, Message: `La liquidación ${id} ya está pagada.` }, { status: 409 });
      }
      estadoFebreroPagado = true;
      const resultado: Liquidacion = { id, vendedorId: 1, codigo: 'V-ANA', nombre: 'Ana Torres', anio: 2026, mes: 2, ordenes: 2, ventas: 10000, porcentaje: 5, comision: 500, estado: 'Pagada', liquidacionId: id, fechaCalculo: '2026-03-01T00:00:00Z', fechaPago: '2026-03-02T00:00:00Z' };
      return Response.json(resultado);
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

export function renderComisiones(entrada = '/modulos/08-comisiones-vendedores', elemento: ReactElement = <ComisionesVendedoresPage />) {
  return render(<MemoryRouter initialEntries={[entrada]}>{elemento}</MemoryRouter>);
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof prepararApiComisiones>['fetchMock'], vista: 'vendedores' | 'asignacion' | 'comisiones' = 'vendedores'): Promise<void> {
  if (vista === 'vendedores') {
    await screen.findByRole('button', { name: 'Nuevo vendedor' });
    await screen.findByRole('button', { name: 'V-ANA' });
  } else if (vista === 'asignacion') {
    await screen.findByText('OC-2026-000101');
    await screen.findByLabelText('Vendedor para OC-2026-000101');
  } else {
    await screen.findByText('Ventas del periodo');
    await screen.findByText('V-ANA · Ana Torres');
  }
  await waitFor(() => expect(fetchMock.mock.calls.length).toBeGreaterThan(0));
}

export async function montarComisionesCargadas(fetchMock: ReturnType<typeof prepararApiComisiones>['fetchMock'], entrada?: string, vista: 'vendedores' | 'asignacion' | 'comisiones' = 'vendedores'): Promise<void> {
  vi.useFakeTimers();
  renderComisiones(entrada);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
    await Promise.resolve();
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock, vista);
}

export function limpiarComisiones(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
