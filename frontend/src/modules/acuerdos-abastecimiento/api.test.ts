import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarAcuerdo, cancelarAcuerdo, crearAcuerdo, crearLineaAcuerdo, eliminarAcuerdo, eliminarLineaAcuerdo, listarAcuerdos,
  listarProductosAcuerdo, listarProveedoresActivosAcuerdo, obtenerAcuerdo, obtenerResumenAcuerdos, reactivarAcuerdo, registrarConsumo, suspenderAcuerdo,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function prepararFetch() {
  const fetchMock = vi.fn(async (...args: Parameters<typeof fetch>) => {
    const metodo = args[1]?.method ?? 'GET';
    return metodo === 'DELETE' ? new Response(null, { status: 204 }) : Response.json({ id: 7, items: [] });
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

const ruta = '/api/v1/acuerdos-abastecimiento';

describe('API de acuerdos de abastecimiento', () => {
  it('A2 lista con la query exacta y omite filtros vacíos', async () => {
    const fetchMock = prepararFetch();
    await listarAcuerdos({ busqueda: 'andes', estado: 'Vigente', proveedorId: 2, pagina: 2, tamanoPagina: 10 });
    await listarAcuerdos({ busqueda: '', estado: '', proveedorId: '', pagina: 1 });
    await listarAcuerdos({});
    expect(fetchMock.mock.calls[0]?.[0]).toBe(`${ruta}?busqueda=andes&estado=Vigente&proveedorId=2&pagina=2&tamanoPagina=10`);
    expect(fetchMock.mock.calls[1]?.[0]).toBe(`${ruta}?pagina=1`);
    expect(fetchMock.mock.calls[2]?.[0]).toBe(ruta);
  });

  it('A2 consulta detalle, resumen, proveedores y productos por sus rutas', async () => {
    const fetchMock = prepararFetch();
    await obtenerAcuerdo(3);
    await obtenerResumenAcuerdos();
    await listarProveedoresActivosAcuerdo();
    await listarProductosAcuerdo();
    expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([
      `${ruta}/3`,
      `${ruta}/resumen`,
      '/api/v1/proveedores?activo=true&tamanoPagina=100',
      '/api/v1/productos?tamanoPagina=100',
    ]);
  });

  it('A2 crea y actualiza con método y cuerpo exactos', async () => {
    const fetchMock = prepararFetch();
    const datos = { numero: 'AM-1', proveedorId: 1, fechaInicio: '2026-12-01T00:00:00.000Z', fechaFin: '2027-12-01T00:00:00.000Z', montoMaximo: 100 };
    await crearAcuerdo(datos);
    await actualizarAcuerdo(4, { fechaInicio: datos.fechaInicio, fechaFin: datos.fechaFin, montoMaximo: 200 });
    expect(fetchMock.mock.calls[0]?.[1]?.method).toBe('POST');
    expect(JSON.parse(String(fetchMock.mock.calls[0]?.[1]?.body))).toEqual(datos);
    expect(fetchMock.mock.calls[1]?.[0]).toBe(`${ruta}/4`);
    expect(fetchMock.mock.calls[1]?.[1]?.method).toBe('PUT');
    expect(JSON.parse(String(fetchMock.mock.calls[1]?.[1]?.body))).toEqual({ fechaInicio: datos.fechaInicio, fechaFin: datos.fechaFin, montoMaximo: 200 });
  });

  it('A2 líneas, consumo y cambios de estado usan sus rutas', async () => {
    const fetchMock = prepararFetch();
    await crearLineaAcuerdo(2, { productoId: 1, precioAcordado: 10, cantidadComprometida: 5 });
    await eliminarLineaAcuerdo(2, 9);
    await registrarConsumo(2, 9, { cantidad: 3 });
    await suspenderAcuerdo(2);
    await reactivarAcuerdo(2);
    await cancelarAcuerdo(2);
    await eliminarAcuerdo(2);
    const llamadas = fetchMock.mock.calls.map((call) => `${call[1]?.method ?? 'GET'} ${String(call[0])}`);
    expect(llamadas).toEqual([
      `POST ${ruta}/2/lineas`,
      `DELETE ${ruta}/2/lineas/9`,
      `POST ${ruta}/2/lineas/9/consumos`,
      `POST ${ruta}/2/suspender`,
      `POST ${ruta}/2/reactivar`,
      `POST ${ruta}/2/cancelar`,
      `DELETE ${ruta}/2`,
    ]);
    expect(JSON.parse(String(fetchMock.mock.calls[2]?.[1]?.body))).toEqual({ cantidad: 3 });
    expect(fetchMock.mock.calls[3]?.[1]?.body).toBeUndefined();
  });
});
