import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  abrirCiclo,
  actualizarCiclo,
  actualizarCompetencia,
  cerrarCiclo,
  cerrarEvaluacion,
  crearCiclo,
  crearCompetencia,
  crearEvaluacion,
  eliminarCompetencia,
  enviarEvaluacion,
  guardarCalificaciones,
  listarCiclos,
  listarEvaluaciones,
  obtenerCiclo,
  obtenerEvaluacion,
  obtenerResultadosCiclo,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function simularFetch(contenido: unknown = { items: [], total: 0 }, status = 200) {
  const fetchMock = vi.fn().mockImplementation(async () => (status === 204 ? new Response(null, { status }) : Response.json(contenido, { status })));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

const ciclo = {
  codigo: '2026-S2', nombre: 'Segundo semestre', periodo: 'Semestral', fechaInicioPeriodo: '2026-06-01', fechaFinPeriodo: '2026-11-30',
  fechaApertura: '2026-11-01', fechaCierre: '2026-12-31',
};

describe('API de ciclos de evaluación', () => {
  it('lista ciclos con búsqueda y estado exactos en la query y omite los vacíos', async () => {
    const fetchMock = simularFetch();
    await listarCiclos({ busqueda: ' 2026 ', estado: 'Abierto', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ciclos-evaluacion?busqueda=2026&estado=Abierto&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
    await listarCiclos({ busqueda: '', estado: '', pagina: 1, tamanoPagina: 100 });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/ciclos-evaluacion?pagina=1&tamanoPagina=100');
  });

  it('obtiene el detalle y los resultados de un ciclo', async () => {
    const fetchMock = simularFetch({});
    await obtenerCiclo(4);
    await obtenerResultadosCiclo(4);
    expect(fetchMock.mock.calls.map((llamada) => llamada[0])).toEqual(['/api/v1/ciclos-evaluacion/4', '/api/v1/ciclos-evaluacion/4/resultados']);
  });

  it('crea y actualiza un ciclo con el cuerpo JSON', async () => {
    const fetchMock = simularFetch({ id: 7 });
    await crearCiclo(ciclo);
    await actualizarCiclo(7, ciclo);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ciclos-evaluacion');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(ciclo) });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/ciclos-evaluacion/7');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(ciclo) });
  });

  it('crea, actualiza y elimina competencias del ciclo', async () => {
    const fetchMock = simularFetch({ id: 3 });
    const competencia = { nombre: 'Calidad', descripcion: null, peso: 25.5 };
    await crearCompetencia(7, competencia);
    await actualizarCompetencia(7, 3, competencia);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ciclos-evaluacion/7/competencias');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(competencia) });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/ciclos-evaluacion/7/competencias/3');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'PUT' });
    const eliminar = simularFetch(undefined, 204);
    await eliminarCompetencia(7, 3);
    expect(eliminar.mock.calls[0]?.[0]).toBe('/api/v1/ciclos-evaluacion/7/competencias/3');
    expect(eliminar.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('abre y cierra un ciclo con POST sin cuerpo', async () => {
    const fetchMock = simularFetch({ id: 7 });
    await abrirCiclo(7);
    await cerrarCiclo(7);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/ciclos-evaluacion/7/apertura');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
    expect(fetchMock.mock.calls[0]?.[1]).not.toHaveProperty('body');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/ciclos-evaluacion/7/cierre');
  });
});

describe('API de evaluaciones', () => {
  it('lista evaluaciones con ciclo, estado y búsqueda exactos y omite los vacíos', async () => {
    const fetchMock = simularFetch();
    await listarEvaluaciones({ cicloId: 2, estado: 'Enviada', busqueda: 'Ana', pagina: 3, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/evaluaciones?cicloId=2&estado=Enviada&busqueda=Ana&pagina=3&tamanoPagina=10');
    await listarEvaluaciones({ cicloId: null, estado: '', busqueda: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/evaluaciones?pagina=1&tamanoPagina=10');
  });

  it('obtiene, crea y califica una evaluación', async () => {
    const fetchMock = simularFetch({ id: 11 });
    await obtenerEvaluacion(11);
    await crearEvaluacion({ cicloId: 2, empleadoId: 1, evaluadorId: 2 });
    const calificaciones = { calificaciones: [{ competenciaId: 3, calificacion: 4 }], comentario: null };
    await guardarCalificaciones(11, calificaciones);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/evaluaciones/11');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/evaluaciones');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ cicloId: 2, empleadoId: 1, evaluadorId: 2 }) });
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/evaluaciones/11/calificaciones');
    expect(fetchMock.mock.calls[2]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(calificaciones) });
  });

  it('envía y cierra una evaluación con POST sin cuerpo', async () => {
    const fetchMock = simularFetch({ id: 11 });
    await enviarEvaluacion(11);
    await cerrarEvaluacion(11);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/evaluaciones/11/envio');
    expect(fetchMock.mock.calls[0]?.[1]).not.toHaveProperty('body');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/evaluaciones/11/cierre');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'POST' });
  });
});
