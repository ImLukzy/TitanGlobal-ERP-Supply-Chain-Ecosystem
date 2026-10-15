import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { ContratosComercialesPage } from './ContratosComercialesPage';
import type { ContratoDetalle, ContratoListado } from './tipos';

export const contratosMock: ContratoDetalle[] = [
  { id: 1, numero: 'CC-DEV-001', clienteId: 1, clienteNombre: 'Acme Industrial', titulo: 'Acuerdo marco Acme 2026', fechaInicio: '2026-01-01T00:00:00Z', fechaFin: '2026-12-31T00:00:00Z', estado: 'Vigente', vencido: false, montoComprometido: 20000, montoConsumido: 9000, porcentajeConsumido: 45, condicionPago: 'Credito30', observaciones: 'Contrato anual', motivoCancelacion: null, fechaActivacion: '2026-01-01T00:00:00Z', fechaCierre: null, saldo: 11000, ordenesPeriodo: [{ ordenId: 2, numeroOrden: 'OC-002', fecha: '2026-02-05T00:00:00Z', total: 3000 }, { ordenId: 4, numeroOrden: 'OC-004', fecha: '2026-03-10T00:00:00Z', total: 4000 }, { ordenId: 1, numeroOrden: 'OC-001', fecha: '2026-01-02T00:00:00Z', total: 2000 }] },
  { id: 2, numero: 'CC-DEV-002', clienteId: 2, clienteNombre: 'Beta Comercio', titulo: 'Acuerdo 2027', fechaInicio: '2027-01-01T00:00:00Z', fechaFin: '2027-12-31T00:00:00Z', estado: 'Borrador', vencido: false, montoComprometido: 8000, montoConsumido: 0, porcentajeConsumido: 0, condicionPago: 'Contado', observaciones: null, motivoCancelacion: null, fechaActivacion: null, fechaCierre: null, saldo: 8000, ordenesPeriodo: [] },
  { id: 3, numero: 'CC-DEV-003', clienteId: 3, clienteNombre: 'Gamma Manufactura', titulo: 'Convenio cancelado', fechaInicio: '2026-01-01T00:00:00Z', fechaFin: '2026-12-31T00:00:00Z', estado: 'Cancelado', vencido: false, montoComprometido: 5000, montoConsumido: 0, porcentajeConsumido: 0, condicionPago: 'Credito60', observaciones: null, motivoCancelacion: 'El cliente desistió del acuerdo.', fechaActivacion: null, fechaCierre: '2026-03-01T00:00:00Z', saldo: 5000, ordenesPeriodo: [] },
  { id: 4, numero: 'CC-DEV-004', clienteId: 4, clienteNombre: 'Delta Logística', titulo: 'Acuerdo con consumo excedido', fechaInicio: '2026-01-01T00:00:00Z', fechaFin: '2026-12-31T00:00:00Z', estado: 'Vigente', vencido: false, montoComprometido: 1000, montoConsumido: 1250, porcentajeConsumido: 125, condicionPago: 'Credito90', observaciones: null, motivoCancelacion: null, fechaActivacion: '2025-12-22T00:00:00Z', fechaCierre: null, saldo: -250, ordenesPeriodo: [{ ordenId: 2, numeroOrden: 'OC-000002', fecha: '2026-04-05T00:00:00Z', total: 1250 }] },
];

export interface OpcionesApi { listaVacia?: boolean; errorLista?: boolean; errorActivar?: boolean; errorCrear?: boolean; }
export interface LlamadaContrato { url: string; init: RequestInit; }

function listado({ id, numero, clienteId, clienteNombre, titulo, fechaInicio, fechaFin, estado, vencido, montoComprometido, montoConsumido, porcentajeConsumido }: ContratoDetalle): ContratoListado {
  return { id, numero, clienteId, clienteNombre, titulo, fechaInicio, fechaFin, estado, vencido, montoComprometido, montoConsumido, porcentajeConsumido };
}

export function prepararApiContratos(opciones: OpcionesApi = {}) {
  const llamadas: LlamadaContrato[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });
    if (parsed.pathname.endsWith('/contratos-comerciales/resumen')) return Response.json({ total: 4, borradores: 1, vigentes: 2, vencidos: 0, finalizados: 0, cancelados: 1, montoComprometidoVigente: 21000, montoConsumidoVigente: 10250, porcentajeConsumoVigente: 48.81, porCondicion: [{ condicionPago: 'Credito30', contratos: 1 }, { condicionPago: 'Credito90', contratos: 1 }] });
    if (parsed.pathname.endsWith('/clientes')) return Response.json({ items: [{ id: 1, nombre: 'Acme Industrial' }, { id: 2, nombre: 'Beta Comercio' }, { id: 3, nombre: 'Gamma Manufactura' }, { id: 4, nombre: 'Delta Logística' }] });
    if (parsed.pathname.endsWith('/contratos-comerciales') && metodo === 'GET') {
      if (opciones.errorLista) return Response.json({ StatusCode: 500, Message: 'Falló el listado.' }, { status: 500 });
      const estados = parsed.searchParams.get('estado');
      const vencidos = parsed.searchParams.get('vencidos');
      const items = opciones.listaVacia ? [] : contratosMock.filter((item) => (!estados || item.estado === estados) && (vencidos !== 'true' || item.vencido));
      return Response.json({ items: items.map(listado), total: items.length, pagina: Number(parsed.searchParams.get('pagina') ?? 1), tamanoPagina: 10 });
    }
    if (parsed.pathname.endsWith('/contratos-comerciales') && metodo === 'POST') {
      if (opciones.errorCrear) return Response.json({ StatusCode: 400, Message: 'El cliente 9999 no existe.' }, { status: 400 });
      return Response.json({ id: 9, numero: 'CC-DEV-009' });
    }
    if (/\/contratos-comerciales\/\d+\/activar$/.test(parsed.pathname) && metodo === 'POST' && opciones.errorActivar) return Response.json({ StatusCode: 409, Message: 'El cliente ya tiene el contrato vigente CC-DEV-001 en ese periodo.' }, { status: 409 });
    if (/\/contratos-comerciales\/\d+\/(activar|finalizar|cancelar)$/.test(parsed.pathname)) return Response.json(contratosMock[0]);
    if (metodo === 'PUT') return Response.json({ id: 2, numero: 'CC-DEV-002' });
    if (metodo === 'DELETE') return new Response(null, { status: 204 });
    const detalle = contratosMock.find((item) => parsed.pathname.endsWith(`/contratos-comerciales/${item.id}`));
    if (detalle) return Response.json(detalle);
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

export function renderContratos(initialEntries = ['/modulos/05-contratos-comerciales']) {
  return render(<MemoryRouter initialEntries={initialEntries}><ContratosComercialesPage /></MemoryRouter>);
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await screen.findByRole('button', { name: 'CC-DEV-001' });
  await screen.findByText('Contratos por condición de pago');
  await screen.findByRole('combobox', { name: 'Cliente' });
  await waitFor(() => expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/contratos-comerciales?'))).toHaveLength(1));
}

export function limpiarContratos(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
