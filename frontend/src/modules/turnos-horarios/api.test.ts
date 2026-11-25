import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  activarTurno,
  actualizarTurno,
  crearAsignacionTurno,
  crearTurno,
  desactivarTurno,
  eliminarAsignacionTurno,
  finalizarAsignacionTurno,
  listarAsignacionesTurno,
  listarTurnos,
  obtenerProgramacion,
  obtenerResumenTurnos,
  obtenerTurno,
} from './api';
import type { GuardarTurnoRequest } from './tipos';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

function simularFetch(contenido: unknown = { items: [], total: 0 }) {
  const fetchMock = vi.fn().mockImplementation(async () => respuestaJson(contenido));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

const turnoFijo: GuardarTurnoRequest = {
  codigo: 'TARDE',
  nombre: 'Turno tarde',
  tipo: 'Fijo',
  horaEntrada: '14:00:00',
  horaSalida: '22:00:00',
  cruzaMedianoche: false,
  toleranciaMinutos: 10,
  diasLaborables: [1, 2, 3, 4, 5],
};

describe('API de turnos', () => {
  it('lista turnos con todos los filtros exactos en la query', async () => {
    const fetchMock = simularFetch();
    await listarTurnos({ busqueda: ' dia ', tipo: 'Fijo', activo: 'Activos', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/turnos?busqueda=dia&tipo=Fijo&activo=true&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('envía activo=false para Inactivos y omite busqueda, tipo y activo vacíos', async () => {
    const fetchMock = simularFetch();
    await listarTurnos({ busqueda: '', tipo: '', activo: 'Inactivos', pagina: 1, tamanoPagina: 10 });
    await listarTurnos({ busqueda: '  ', tipo: '', activo: 'Todos', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/turnos?activo=false&pagina=1&tamanoPagina=10');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/turnos?pagina=1&tamanoPagina=10');
  });

  it('obtiene el resumen y el detalle por id', async () => {
    const fetchMock = simularFetch({ id: 3 });
    await obtenerResumenTurnos();
    await obtenerTurno(3);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/turnos/resumen');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/turnos/3');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea un turno con POST y el cuerpo exacto', async () => {
    const fetchMock = simularFetch({ id: 6 });
    const resultado = await crearTurno(turnoFijo);
    expect(resultado).toEqual({ id: 6 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/turnos');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(turnoFijo) });
  });

  it('edita un turno con PUT y el cuerpo exacto', async () => {
    const fetchMock = simularFetch({ id: 5 });
    await actualizarTurno(5, turnoFijo);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/turnos/5');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(turnoFijo) });
  });

  it('desactiva y activa con POST sin cuerpo', async () => {
    const fetchMock = simularFetch({ id: 5 });
    await desactivarTurno(5);
    await activarTurno(4);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/turnos/5/desactivacion');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
    expect(fetchMock.mock.calls[0]?.[1]?.body).toBeUndefined();
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/turnos/4/activacion');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'POST' });
  });
});

describe('API de asignaciones de turno', () => {
  it('lista asignaciones con todos los filtros exactos en la query', async () => {
    const fetchMock = simularFetch();
    await listarAsignacionesTurno({ empleadoId: 2, turnoId: 3, estado: 'Vigente', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/asignaciones-turno?empleadoId=2&turnoId=3&estado=Vigente&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite empleado, turno y estado vacíos', async () => {
    const fetchMock = simularFetch();
    await listarAsignacionesTurno({ empleadoId: null, turnoId: null, estado: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/asignaciones-turno?pagina=1&tamanoPagina=10');
  });

  it('pide la programación con empleado y rango', async () => {
    const fetchMock = simularFetch([]);
    await obtenerProgramacion(2, '2026-01-05', '2026-01-18');
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/asignaciones-turno/programacion?empleadoId=2&desde=2026-01-05&hasta=2026-01-18');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea una asignación con POST y el cuerpo exacto', async () => {
    const fetchMock = simularFetch({ id: 6 });
    const cuerpo = { empleadoId: 3, turnoId: 5, fechaInicio: '2026-12-01', fechaFin: '2027-01-31' };
    await crearAsignacionTurno(cuerpo);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/asignaciones-turno');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpo) });
  });

  it('fija el fin con POST a /finalizacion', async () => {
    const fetchMock = simularFetch({ id: 3 });
    await finalizarAsignacionTurno(3, { fechaFin: '2026-12-31' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/asignaciones-turno/3/finalizacion');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ fechaFin: '2026-12-31' }) });
  });

  it('elimina con DELETE', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await eliminarAsignacionTurno(5);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/asignaciones-turno/5');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });
});
