import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { AuditoriaPage } from './AuditoriaPage';
import type { CambioCampo, RegistroDetalle, RegistroListado, ResumenAuditoria } from './tipos';

export const registrosMock: RegistroListado[] = [
  { id: 11, fecha: '2026-10-15T13:00:00Z', entidad: 'Cliente', entidadId: '3', accion: 'Modificacion', usuario: 'demo', ruta: 'PUT /api/v1/clientes/3', camposCambiados: 2 },
  { id: 10, fecha: '2026-10-15T12:00:00Z', entidad: 'Producto', entidadId: '8', accion: 'Alta', usuario: 'demo', ruta: 'POST /api/v1/productos', camposCambiados: 4 },
];

export const cambiosMock: CambioCampo[] = [
  { campo: 'Nombre', antes: 'Acme anterior', despues: 'Acme Industrial' },
  { campo: 'Descripcion', antes: null, despues: 'Cliente actualizado' },
];

export const detalleMock: RegistroDetalle = { ...registrosMock[0], cambios: cambiosMock };

export const resumenMock: ResumenAuditoria = {
  total: 145,
  altas: 46,
  modificaciones: 72,
  bajas: 27,
  porEntidad: [{ entidad: 'Cliente', registros: 52 }, { entidad: 'Producto', registros: 36 }],
  ultimos7Dias: [
    { fecha: '2026-10-09T00:00:00Z', registros: 2 },
    { fecha: '2026-10-10T00:00:00Z', registros: 0 },
    { fecha: '2026-10-11T00:00:00Z', registros: 5 },
    { fecha: '2026-10-12T00:00:00Z', registros: 1 },
    { fecha: '2026-10-13T00:00:00Z', registros: 0 },
    { fecha: '2026-10-14T00:00:00Z', registros: 4 },
    { fecha: '2026-10-15T00:00:00Z', registros: 3 },
  ],
};

interface OpcionesApi {
  listaVacia?: boolean;
  errorLista?: boolean;
}

export function prepararApiAuditoria(opciones: OpcionesApi = {}) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input);
    const parseada = new URL(url, 'http://localhost');
    if (parseada.pathname.endsWith('/auditoria/resumen')) {
      return Response.json(resumenMock);
    }
    if (parseada.pathname.endsWith('/auditoria/entidades')) {
      return Response.json(['Cliente', 'Producto', 'OrdenCompra']);
    }
    if (/\/auditoria\/\d+$/.test(parseada.pathname)) {
      return Response.json(detalleMock);
    }
    if (parseada.pathname.endsWith('/auditoria')) {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'Falló la consulta de auditoría.' }, { status: 500 });
      }
      const pagina = Number(parseada.searchParams.get('pagina') ?? 1);
      const items = opciones.listaVacia ? [] : pagina === 1 ? registrosMock : [registrosMock[0]];
      return Response.json({ items, total: opciones.listaVacia ? 0 : 21, pagina, tamanoPagina: 20 });
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

export function renderAuditoria(entrada = '/modulos/77-auditoria-transaccional') {
  return render(<MemoryRouter initialEntries={[entrada]}><AuditoriaPage /></MemoryRouter>);
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof prepararApiAuditoria>): Promise<void> {
  await screen.findByRole('button', { name: 'Ver registro 11' });
  await screen.findByRole('heading', { name: 'Actividad de los últimos 7 días' });
  await screen.findByRole('combobox', { name: 'Entidad' });
  await waitFor(() => {
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/auditoria?'))).toHaveLength(1);
    expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/auditoria/resumen'))).toBe(true);
    expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/auditoria/entidades'))).toBe(true);
  });
}

export async function montarAuditoriaCargada(fetchMock: ReturnType<typeof prepararApiAuditoria>, entrada?: string): Promise<void> {
  vi.useFakeTimers();
  renderAuditoria(entrada);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock);
}

export function limpiarAuditoria(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
