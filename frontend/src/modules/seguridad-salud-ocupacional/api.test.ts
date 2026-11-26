import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  activarEquipo,
  actualizarEquipo,
  agregarAccion,
  cerrarIncidente,
  completarAccion,
  crearEquipo,
  desactivarEquipo,
  listarEntregas,
  listarEquipos,
  listarExamenes,
  listarIncidentes,
  obtenerIncidente,
  obtenerResumen,
  registrarEntrega,
  registrarExamen,
  registrarIncidente,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function simularFetch(contenido: unknown = { items: [], total: 0 }) {
  const fetchMock = vi.fn().mockImplementation(async () => Response.json(contenido));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('API de incidentes', () => {
  it('lista incidentes con filtros exactos en la query y omite los vacíos', async () => {
    const fetchMock = simularFetch();
    await listarIncidentes({ busqueda: ' ana ', tipo: 'AccidenteConBaja', severidad: 'Grave', estado: 'Abierto', desde: '2026-01-01', hasta: '2026-12-31', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/incidentes-sst?busqueda=ana&tipo=AccidenteConBaja&severidad=Grave&estado=Abierto&desde=2026-01-01&hasta=2026-12-31&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
    await listarIncidentes({ busqueda: '', tipo: '', severidad: '', estado: '', desde: '', hasta: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/incidentes-sst?pagina=1&tamanoPagina=10');
  });

  it('obtiene, registra y consulta el resumen del año', async () => {
    const fetchMock = simularFetch({ id: 7 });
    const solicitud = { empleadoId: 1, tipo: 'IncidenteSinLesion', severidad: 'Leve', fecha: '2026-05-01', lugar: null, descripcion: 'Resbalón', diasBaja: 0 };
    await obtenerIncidente(7);
    await registrarIncidente(solicitud);
    await obtenerResumen(2026);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/incidentes-sst/7');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/incidentes-sst');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(solicitud) });
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/incidentes-sst/resumen?anio=2026');
  });

  it('agrega y completa acciones con cuerpo y cierra sin cuerpo', async () => {
    const fetchMock = simularFetch({ id: 7 });
    const accion = { descripcion: 'Instalar guarda', responsable: 'Seguridad', fechaCompromiso: '2026-12-01' };
    await agregarAccion(7, accion);
    await completarAccion(7, 11, { fechaCompletada: '2026-11-22' });
    await cerrarIncidente(7);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/incidentes-sst/7/acciones');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(accion) });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/incidentes-sst/7/acciones/11/completar');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ fechaCompletada: '2026-11-22' }) });
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/incidentes-sst/7/cierre');
    expect(fetchMock.mock.calls[2]?.[1]?.body).toBeUndefined();
  });
});

describe('API de EPP', () => {
  it('lista equipos y convierte el estado en activo=true o false', async () => {
    const fetchMock = simularFetch();
    await listarEquipos({ busqueda: ' casco ', activo: 'Activos', pagina: 1, tamanoPagina: 10 });
    await listarEquipos({ busqueda: '', activo: 'Inactivos', pagina: 2, tamanoPagina: 100 });
    await listarEquipos({ busqueda: '', activo: 'Todos', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/equipos-proteccion?busqueda=casco&activo=true&pagina=1&tamanoPagina=10');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/equipos-proteccion?activo=false&pagina=2&tamanoPagina=100');
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/equipos-proteccion?pagina=1&tamanoPagina=10');
  });

  it('crea y actualiza con JSON y activa o desactiva con POST sin cuerpo', async () => {
    const fetchMock = simularFetch({ id: 3 });
    const equipo = { codigo: 'CASCO', nombre: 'Casco', vidaUtilDias: 365 };
    await crearEquipo(equipo);
    await actualizarEquipo(3, equipo);
    await desactivarEquipo(3);
    await activarEquipo(3);
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(equipo) });
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/equipos-proteccion/3');
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(equipo) });
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/v1/equipos-proteccion/3/desactivacion');
    expect(fetchMock.mock.calls[3]?.[0]).toBe('/api/v1/equipos-proteccion/3/activacion');
    expect(fetchMock.mock.calls[3]?.[1]?.body).toBeUndefined();
  });

  it('lista entregas con empleado, equipo y vencidas, y registra una entrega', async () => {
    const fetchMock = simularFetch({ id: 5 });
    await listarEntregas({ empleadoId: 2, equipoId: 1, vencidas: true, pagina: 1, tamanoPagina: 10 });
    await listarEntregas({ empleadoId: null, equipoId: null, vencidas: false, pagina: 1, tamanoPagina: 10 });
    await registrarEntrega({ equipoId: 1, empleadoId: 2, fechaEntrega: '2026-11-22', cantidad: 2 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/entregas-epp?empleadoId=2&equipoId=1&vencidas=true&pagina=1&tamanoPagina=10');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/entregas-epp?pagina=1&tamanoPagina=10');
    expect(fetchMock.mock.calls[2]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ equipoId: 1, empleadoId: 2, fechaEntrega: '2026-11-22', cantidad: 2 }) });
  });
});

describe('API de exámenes médicos', () => {
  it('lista con empleado, tipo, resultado y vencidos, y omite los vacíos', async () => {
    const fetchMock = simularFetch();
    await listarExamenes({ empleadoId: 1, tipo: 'Periodico', resultado: 'NoApto', vencidos: true, pagina: 2, tamanoPagina: 10 });
    await listarExamenes({ empleadoId: null, tipo: '', resultado: '', vencidos: false, pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/examenes-medicos?empleadoId=1&tipo=Periodico&resultado=NoApto&vencidos=true&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/examenes-medicos?pagina=1&tamanoPagina=10');
  });

  it('registra un examen con el cuerpo JSON', async () => {
    const fetchMock = simularFetch({ id: 4 });
    const examen = { empleadoId: 1, tipo: 'Retiro', fecha: '2026-11-01', resultado: 'Apto', restricciones: null, fechaVencimiento: null, clinica: null };
    await registrarExamen(examen);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/examenes-medicos');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(examen) });
  });
});
