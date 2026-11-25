import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  aprobarPlanilla,
  calcularPlanilla,
  crearBono,
  crearPlanilla,
  eliminarBono,
  guardarAfiliacion,
  listarAfiliaciones,
  listarBonos,
  listarDetallesPlanilla,
  listarPlanillas,
  obtenerBoleta,
  obtenerParametrosPlanilla,
  obtenerPlanilla,
  obtenerResumenPlanillas,
  pagarPlanilla,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function simularFetch(contenido: unknown = { items: [], total: 0 }, status = 200) {
  const fetchMock = vi.fn().mockImplementation(async () => (status === 204 ? new Response(null, { status }) : Response.json(contenido, { status })));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('API de planillas', () => {
  it('lista planillas con año, tipo y estado exactos en la query', async () => {
    const fetchMock = simularFetch();
    await listarPlanillas({ anio: 2026, tipo: 'Mensual', estado: 'Calculada', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/planillas?anio=2026&tipo=Mensual&estado=Calculada&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite los filtros vacíos de planillas', async () => {
    const fetchMock = simularFetch();
    await listarPlanillas({ anio: null, tipo: '', estado: '', pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/planillas?pagina=1&tamanoPagina=10');
  });

  it('pide el resumen, los parámetros y una planilla por id', async () => {
    const fetchMock = simularFetch({});
    await obtenerResumenPlanillas();
    await obtenerParametrosPlanilla();
    await obtenerPlanilla(4);
    expect(fetchMock.mock.calls.map((llamada) => llamada[0])).toEqual([
      '/api/v1/planillas/resumen',
      '/api/v1/planillas/parametros',
      '/api/v1/planillas/4',
    ]);
  });

  it('lista los detalles con búsqueda recortada y solo incidencias', async () => {
    const fetchMock = simularFetch();
    await listarDetallesPlanilla(3, { busqueda: '  Ana  ', soloIncidencias: true, pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/planillas/3/detalles?busqueda=Ana&soloIncidencias=true&pagina=2&tamanoPagina=10');
  });

  it('omite la búsqueda vacía y soloIncidencias en falso', async () => {
    const fetchMock = simularFetch();
    await listarDetallesPlanilla(3, { busqueda: ' ', soloIncidencias: false, pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/planillas/3/detalles?pagina=1&tamanoPagina=10');
  });

  it('pide la boleta de un detalle', async () => {
    const fetchMock = simularFetch({});
    await obtenerBoleta(12);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/planillas/boletas/12');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea una planilla con POST y el cuerpo exacto', async () => {
    const fetchMock = simularFetch({ id: 9, numero: 'PLA-202611-M' });
    const cuerpo = { anio: 2026, mes: 11, tipo: 'Mensual' as const };
    await expect(crearPlanilla(cuerpo)).resolves.toEqual({ id: 9, numero: 'PLA-202611-M' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/planillas');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpo) });
  });

  it('calcula y aprueba con POST sin cuerpo', async () => {
    const fetchMock = simularFetch({ id: 1 });
    await calcularPlanilla(1);
    await aprobarPlanilla(1);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/planillas/1/calculo');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/planillas/1/aprobacion');
    for (const llamada of fetchMock.mock.calls) {
      expect(llamada[1]).toMatchObject({ method: 'POST' });
      expect(llamada[1]).not.toHaveProperty('body');
    }
  });

  it('registra el pago con POST y la fecha', async () => {
    const fetchMock = simularFetch({ id: 1 });
    await pagarPlanilla(1, { fechaPago: '2026-11-30' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/planillas/1/pago');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ fechaPago: '2026-11-30' }) });
  });

  it('lista bonos con todos los filtros y omite los vacíos', async () => {
    const fetchMock = simularFetch();
    await listarBonos({ anio: 2026, mes: 10, empleadoId: 1, pagina: 1, tamanoPagina: 10 });
    await listarBonos({ anio: null, mes: null, empleadoId: null, pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/bonos-planilla?anio=2026&mes=10&empleadoId=1&pagina=1&tamanoPagina=10');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/bonos-planilla?pagina=1&tamanoPagina=10');
  });

  it('crea un bono con POST y lo elimina con DELETE', async () => {
    const fetchMock = simularFetch({ id: 3 });
    const cuerpo = { empleadoId: 1, anio: 2026, mes: 10, tipoPlanilla: 'Mensual' as const, concepto: 'Bono por metas', monto: 300 };
    await expect(crearBono(cuerpo)).resolves.toEqual({ id: 3 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/bonos-planilla');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpo) });
    const eliminar = simularFetch(null, 204);
    await eliminarBono(3);
    expect(eliminar.mock.calls[0]?.[0]).toBe('/api/v1/bonos-planilla/3');
    expect(eliminar.mock.calls[0]?.[1]).toMatchObject({ method: 'DELETE' });
  });

  it('lista afiliaciones con el empleado opcional', async () => {
    const fetchMock = simularFetch();
    await listarAfiliaciones({ empleadoId: 2, pagina: 1, tamanoPagina: 100 });
    await listarAfiliaciones({ empleadoId: null, pagina: 1, tamanoPagina: 100 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/afiliaciones-pension?empleadoId=2&pagina=1&tamanoPagina=100');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/afiliaciones-pension?pagina=1&tamanoPagina=100');
  });

  it('guarda una afiliación con PUT por empleado', async () => {
    const fetchMock = simularFetch({ id: 5 });
    const cuerpo = { codigoPension: 'INTEGRA', cuspp: '123456ABCDEF', fechaAfiliacion: '2026-01-15' };
    await expect(guardarAfiliacion(3, cuerpo)).resolves.toEqual({ id: 5 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/afiliaciones-pension/3');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(cuerpo) });
  });
});
