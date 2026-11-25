import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { pantallas } from '../pantallas';
import type { EstadoInspeccion, InspeccionInsumoDetalle } from './tipos';

export const rutaApi = '/api/v1/inspecciones-insumos';
export const loteRepetido = 'Ya existe una inspección del lote MO-2026-001 para ese producto y proveedor.';

export interface OpcionesApi {
  vacio?: boolean;
  errorListado?: boolean;
  conflictoAlta?: boolean;
  estadoInicial?: EstadoInspeccion;
}

export function inspeccionBase(estado: EstadoInspeccion): InspeccionInsumoDetalle {
  const dictaminada = estado === 'Aprobada' || estado === 'Rechazada';
  return {
    id: 7,
    numero: 'CCI-000007',
    productoId: 3,
    productoNombre: 'Mouse Óptico',
    proveedorId: 1,
    proveedorRazonSocial: 'Andes Suministros S.A.C.',
    lote: 'MO-2026-001',
    fechaInspeccion: '2026-11-02',
    cantidadRecibida: 500,
    unidadMedida: 'unidad',
    estado,
    porcentajeDefectos: dictaminada ? 12.5 : null,
    cantidadMuestra: 50,
    toleranciaDefectosPorcentaje: 5,
    inspector: 'Rosa Díaz',
    cantidadDefectuosa: dictaminada ? 6.25 : null,
    fechaDictamen: dictaminada ? '2026-11-25T00:30:00Z' : null,
    observaciones: null,
    motivoAnulacion: estado === 'Anulada' ? 'Registro duplicado' : null,
    superaTolerancia: dictaminada ? true : null,
  };
}

export function montar({ vacio = false, errorListado = false, conflictoAlta = false, estadoInicial = 'Pendiente' }: OpcionesApi = {}) {
  let estado: EstadoInspeccion = estadoInicial;
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input), 'http://localhost');
    const metodo = init?.method ?? 'GET';
    if (url.pathname === '/api/v1/productos') {
      return Response.json({ items: [{ id: 3, nombre: 'Mouse Óptico' }], total: 1, pagina: 1, tamanoPagina: 20 });
    }
    if (url.pathname === '/api/v1/proveedores') {
      return Response.json({ items: [{ id: 1, razonSocial: 'Andes Suministros S.A.C.', activo: true }], total: 1, pagina: 1, tamanoPagina: 20 });
    }
    if (url.pathname === `${rutaApi}/resumen`) {
      return Response.json({
        totalInspecciones: 4,
        porEstado: [{ estado: 'Pendiente', total: 1 }, { estado: 'Rechazada', total: 2 }],
        tasaRechazoPorcentaje: 66.67,
        porProveedor: [{ proveedorId: 4, proveedorRazonSocial: 'Cobre Austral SpA', aprobadas: 1, rechazadas: 2, tasaRechazoPorcentaje: 66.67 }],
      });
    }
    if (metodo === 'POST' && url.pathname === rutaApi) {
      return conflictoAlta
        ? Response.json({ StatusCode: 409, Message: loteRepetido }, { status: 409 })
        : Response.json({ id: 7, numero: 'CCI-000007' }, { status: 201 });
    }
    if (url.pathname === `${rutaApi}/7/dictaminar`) {
      estado = (JSON.parse(String(init?.body)) as { resultado: EstadoInspeccion }).resultado;
      return Response.json({ id: 7 });
    }
    if (url.pathname === `${rutaApi}/7/anular`) {
      estado = 'Anulada';
      return Response.json({ id: 7 });
    }
    if (metodo === 'PUT') {
      return Response.json({ id: 7 });
    }
    if (url.pathname === `${rutaApi}/7`) {
      return Response.json(inspeccionBase(estado));
    }
    if (errorListado) {
      return Response.json({ StatusCode: 500, Message: 'Error de listado.' }, { status: 500 });
    }
    const { id, numero, productoId, productoNombre, proveedorId, proveedorRazonSocial, lote, fechaInspeccion, cantidadRecibida, unidadMedida, porcentajeDefectos } = inspeccionBase(estado);
    const fila = { id, numero, productoId, productoNombre, proveedorId, proveedorRazonSocial, lote, fechaInspeccion, cantidadRecibida, unidadMedida, estado, porcentajeDefectos };
    const items = vacio ? [] : [fila];
    return Response.json({ items, total: vacio ? 0 : 21, pagina: Number(url.searchParams.get('pagina')), tamanoPagina: 20 });
  });
  vi.stubGlobal('fetch', fetchMock);
  const Pagina = pantallas['36-control-calidad-insumos'];
  if (Pagina === undefined) throw new Error('La pantalla 36 no está registrada.');
  render(<MemoryRouter initialEntries={['/modulos/36-control-calidad-insumos']}><Pagina /></MemoryRouter>);
  return fetchMock;
}

export type FetchMock = ReturnType<typeof montar>;

export function llamadas(fetchMock: FetchMock, prefijo: string, metodo = 'GET'): string[] {
  return fetchMock.mock.calls
    .filter((call) => String(call[0]).startsWith(prefijo) && (call[1]?.method ?? 'GET') === metodo)
    .map((call) => String(call[0]));
}

// Espera la carga inicial: listado (una sola petición) y resumen.
export async function esperarCargaInicialInspecciones(fetchMock: FetchMock, conFilas = true): Promise<void> {
  await waitFor(() => {
    expect(screen.getByText('Tasa de rechazo por proveedor', { selector: 'caption' })).toBeInTheDocument();
    if (conFilas) expect(screen.getByRole('button', { name: 'Ver inspección CCI-000007' })).toBeInTheDocument();
    expect(llamadas(fetchMock, `${rutaApi}?`)).toHaveLength(1);
  });
}

export async function abrirDetalle(): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: 'Ver inspección CCI-000007' }));
  await screen.findByRole('heading', { name: 'Inspección CCI-000007' });
}

export async function elegir(etiqueta: string, opcion: string): Promise<void> {
  fireEvent.click(screen.getByLabelText(etiqueta));
  fireEvent.click(await screen.findByRole('button', { name: opcion }));
}

export async function avanzar(ms: number): Promise<void> {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms);
  });
}
