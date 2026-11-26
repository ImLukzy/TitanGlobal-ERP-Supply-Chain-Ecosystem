import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { pantallas } from '../pantallas';
import type { EstadoMantenimiento, MantenimientoDetalle } from './tipos';

export const rutaApi = '/api/v1/mantenimientos-preventivos';
export const fechaOcupada = 'El centro de trabajo CT-LIM-L01 ya tiene un mantenimiento programado el 2999-03-01.';

export interface OpcionesApi {
  vacio?: boolean;
  errorListado?: boolean;
  conflictoAlta?: boolean;
  estadoInicial?: EstadoMantenimiento;
  vencido?: boolean;
}

export function mantenimientoBase(estado: EstadoMantenimiento, vencido = false): MantenimientoDetalle {
  const iniciado = estado === 'EnEjecucion' || estado === 'Completado';
  const completado = estado === 'Completado';
  return {
    id: 7,
    numero: 'MP-000007',
    centroTrabajoId: 4,
    centroTrabajoCodigo: 'CT-LIM-L01',
    centroTrabajoNombre: 'Línea de ensamble',
    descripcion: 'Cambio de rodamientos',
    fechaProgramada: '2026-11-20',
    duracionHorasPlanificada: 2.5,
    estado,
    vencido,
    frecuenciaDias: completado ? 30 : null,
    responsable: completado ? 'Luis Rojas' : null,
    fechaInicioReal: iniciado ? '2026-11-20T13:00:00Z' : null,
    fechaFinReal: completado ? '2026-11-21T00:30:00Z' : null,
    horasReales: completado ? 3.25 : null,
    observaciones: null,
    motivoCancelacion: estado === 'Cancelado' ? 'Línea detenida' : null,
    fechaSiguienteSugerida: completado ? '2026-12-20' : null,
  };
}

export function montar(opciones: OpcionesApi = {}) {
  const { vacio = false, errorListado = false, conflictoAlta = false, estadoInicial = 'Programado', vencido = false } = opciones;
  let estado: EstadoMantenimiento = estadoInicial;
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input), 'http://localhost');
    const metodo = init?.method ?? 'GET';
    if (url.pathname === '/api/v1/centros-trabajo') {
      return Response.json({ items: [{ id: 4, codigo: 'CT-LIM-L01', nombre: 'Línea de ensamble', activo: true }], total: 1, pagina: 1, tamanoPagina: 20 });
    }
    if (url.pathname === `${rutaApi}/resumen`) {
      return Response.json({
        totalMantenimientos: 4,
        porEstado: [{ estado: 'Programado', total: 2 }, { estado: 'EnEjecucion', total: 1 }, { estado: 'Completado', total: 1 }],
        vencidos: 1,
        proximos7Dias: 2,
        horasPlanificadasPendientes: 12.5,
      });
    }
    if (metodo === 'POST' && url.pathname === rutaApi) {
      return conflictoAlta
        ? Response.json({ StatusCode: 409, Message: fechaOcupada }, { status: 409 })
        : Response.json({ id: 7, numero: 'MP-000007' }, { status: 201 });
    }
    if (url.pathname === `${rutaApi}/7/iniciar`) {
      estado = 'EnEjecucion';
      return Response.json({ id: 7 });
    }
    if (url.pathname === `${rutaApi}/7/completar`) {
      estado = 'Completado';
      return Response.json({ id: 7 });
    }
    if (url.pathname === `${rutaApi}/7/cancelar`) {
      estado = 'Cancelado';
      return Response.json({ id: 7 });
    }
    if (metodo === 'PUT') {
      return Response.json({ id: 7 });
    }
    if (url.pathname === `${rutaApi}/7`) {
      return Response.json(mantenimientoBase(estado, vencido));
    }
    if (errorListado) {
      return Response.json({ StatusCode: 500, Message: 'Error de listado.' }, { status: 500 });
    }
    const base = mantenimientoBase(estado, vencido);
    const fila = {
      id: base.id,
      numero: base.numero,
      centroTrabajoId: base.centroTrabajoId,
      centroTrabajoCodigo: base.centroTrabajoCodigo,
      centroTrabajoNombre: base.centroTrabajoNombre,
      descripcion: base.descripcion,
      fechaProgramada: base.fechaProgramada,
      duracionHorasPlanificada: base.duracionHorasPlanificada,
      estado: base.estado,
      vencido: base.vencido,
    };
    return Response.json({ items: vacio ? [] : [fila], total: vacio ? 0 : 21, pagina: Number(url.searchParams.get('pagina')), tamanoPagina: 20 });
  });
  vi.stubGlobal('fetch', fetchMock);
  const Pagina = pantallas['39-mantenimiento-preventivo'];
  if (Pagina === undefined) throw new Error('La pantalla 39 no está registrada.');
  render(<MemoryRouter initialEntries={['/modulos/39-mantenimiento-preventivo']}><Pagina /></MemoryRouter>);
  return fetchMock;
}

export type FetchMock = ReturnType<typeof montar>;

export function llamadas(fetchMock: FetchMock, prefijo: string, metodo = 'GET'): string[] {
  return fetchMock.mock.calls
    .filter((call) => String(call[0]).startsWith(prefijo) && (call[1]?.method ?? 'GET') === metodo)
    .map((call) => String(call[0]));
}

export function cuerpo(fetchMock: FetchMock, ruta: string, metodo: string): unknown {
  const llamada = fetchMock.mock.calls.find((call) => String(call[0]) === ruta && call[1]?.method === metodo);
  return llamada?.[1]?.body === undefined ? undefined : JSON.parse(String(llamada[1].body));
}

// Espera la carga inicial: listado (una sola petición) y resumen.
export async function esperarCargaInicialMantenimientos(fetchMock: FetchMock, conFilas = true): Promise<void> {
  await waitFor(() => {
    expect(screen.getByText('Horas pendientes')).toBeInTheDocument();
    if (conFilas) expect(screen.getByRole('button', { name: 'Ver mantenimiento MP-000007' })).toBeInTheDocument();
    expect(llamadas(fetchMock, `${rutaApi}?`)).toHaveLength(1);
  });
}

export async function abrirDetalle(): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: 'Ver mantenimiento MP-000007' }));
  await screen.findByRole('heading', { name: 'Mantenimiento MP-000007' });
}

export async function elegirCentro(etiqueta: string): Promise<void> {
  fireEvent.click(screen.getByLabelText(etiqueta));
  fireEvent.click(await screen.findByRole('button', { name: 'CT-LIM-L01 · Línea de ensamble' }));
}

export async function avanzar(ms: number): Promise<void> {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms);
  });
}
