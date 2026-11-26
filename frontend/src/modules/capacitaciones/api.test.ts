import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  activarCurso,
  actualizarCurso,
  cancelarProgramacion,
  crearCurso,
  crearProgramacion,
  desactivarCurso,
  emitirCertificacion,
  inscribirEmpleado,
  listarCursos,
  listarHoras,
  listarProgramaciones,
  marcarRealizada,
  obtenerCapacitacionEmpleado,
  obtenerCurso,
  obtenerProgramacion,
  obtenerResumen,
  registrarResultados,
  retirarInscripcion,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function simularFetch(contenido: unknown = { items: [], total: 0 }) {
  const fetchMock = vi.fn().mockImplementation(async () => Response.json(contenido));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

const curso = { codigo: 'SEG-101', nombre: 'Seguridad industrial', horas: 8, tipo: 'Interno', proveedor: null };
const programacion = { cursoId: 1, fechaInicio: '2026-12-10', fechaFin: '2026-12-11', cupo: 3, lugar: 'Sala A' };

describe('API de cursos', () => {
  it('lista cursos con filtros exactos en la query y omite los vacíos', async () => {
    const fetchMock = simularFetch();
    await listarCursos({ busqueda: ' seg ', tipo: 'Externo', activo: 'Activos', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/cursos?busqueda=seg&tipo=Externo&activo=true&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
    await listarCursos({ busqueda: '', tipo: '', activo: 'Inactivos', pagina: 1, tamanoPagina: 100 });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/cursos?activo=false&pagina=1&tamanoPagina=100');
    await listarCursos({ busqueda: '', tipo: '', activo: 'Todos', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/cursos?pagina=1&tamanoPagina=10');
  });

  it('obtiene, crea y actualiza un curso con el cuerpo JSON', async () => {
    const fetchMock = simularFetch({ id: 7 });
    await obtenerCurso(7);
    await crearCurso(curso);
    await actualizarCurso(7, curso);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/cursos/7');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/cursos');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(curso) });
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/cursos/7');
    expect(fetchMock.mock.calls[2]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(curso) });
  });

  it('activa y desactiva con POST sin cuerpo', async () => {
    const fetchMock = simularFetch({ id: 7 });
    await desactivarCurso(7);
    await activarCurso(7);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/cursos/7/desactivacion');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
    expect(fetchMock.mock.calls[0]?.[1]?.body).toBeUndefined();
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/cursos/7/activacion');
  });
});

describe('API de programaciones', () => {
  it('lista programaciones con año, curso y estado; omite los vacíos', async () => {
    const fetchMock = simularFetch();
    await listarProgramaciones({ anio: 2026, cursoId: 2, estado: 'Programada', pagina: 3, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/programaciones-curso?anio=2026&cursoId=2&estado=Programada&pagina=3&tamanoPagina=10');
    await listarProgramaciones({ anio: 2027, cursoId: null, estado: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/programaciones-curso?anio=2027&pagina=1&tamanoPagina=10');
  });

  it('obtiene y crea una programación', async () => {
    const fetchMock = simularFetch({ id: 4 });
    await obtenerProgramacion(4);
    await crearProgramacion(programacion);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/programaciones-curso/4');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/programaciones-curso');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(programacion) });
  });

  it('inscribe con el empleado en el cuerpo y retira sin cuerpo', async () => {
    const fetchMock = simularFetch({ id: 1 });
    await inscribirEmpleado(4, 9);
    await retirarInscripcion(4, 12);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/programaciones-curso/4/inscripciones');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ empleadoId: 9 }) });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/programaciones-curso/4/inscripciones/12/retiro');
    expect(fetchMock.mock.calls[1]?.[1]?.body).toBeUndefined();
  });

  it('marca como realizada y cancela sin cuerpo', async () => {
    const fetchMock = simularFetch({ id: 4 });
    await marcarRealizada(4);
    await cancelarProgramacion(4);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/programaciones-curso/4/realizacion');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/programaciones-curso/4/cancelacion');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'POST' });
  });

  it('registra resultados y emite una certificación con el cuerpo exacto', async () => {
    const fetchMock = simularFetch({ id: 4 });
    const resultados = { resultados: [{ inscripcionId: 12, aprobado: true, nota: 17.5 }, { inscripcionId: 13, aprobado: false, nota: null }] };
    const certificacion = { codigo: 'CERT-9', fechaEmision: '2026-12-12', fechaVencimiento: null };
    await registrarResultados(4, resultados);
    await emitirCertificacion(4, 12, certificacion);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/programaciones-curso/4/resultados');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(resultados) });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/programaciones-curso/4/inscripciones/12/certificacion');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(certificacion) });
  });
});

describe('API de capacitaciones', () => {
  it('pide el resumen, las horas y la ficha con el año en la query', async () => {
    const fetchMock = simularFetch();
    await obtenerResumen(2026);
    await listarHoras({ anio: 2026, busqueda: ' ana ', pagina: 2, tamanoPagina: 10 });
    await listarHoras({ anio: 2025, busqueda: '', pagina: 1, tamanoPagina: 10 });
    await obtenerCapacitacionEmpleado(5, 2026);
    expect(fetchMock.mock.calls.map((llamada) => llamada[0])).toEqual([
      '/api/v1/capacitaciones/resumen?anio=2026',
      '/api/v1/capacitaciones/horas?anio=2026&busqueda=ana&pagina=2&tamanoPagina=10',
      '/api/v1/capacitaciones/horas?anio=2025&pagina=1&tamanoPagina=10',
      '/api/v1/capacitaciones/empleados/5?anio=2026',
    ]);
  });
});
