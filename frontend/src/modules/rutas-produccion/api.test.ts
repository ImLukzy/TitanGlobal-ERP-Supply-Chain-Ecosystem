import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  buscarCentrosTrabajo,
  buscarProductos,
  cambiarEstadoRutaProduccion,
  crearRutaProduccion,
  editarRutaProduccion,
  listarRutasProduccion,
  obtenerRutaProduccion,
} from './api';
import { mensajes, validarFormularioRuta, validarMinutos, type LineaOperacion } from './validacion';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

const operaciones = [
  { secuencia: 1, nombre: 'Corte', centroTrabajoId: 3, minutosPreparacion: 0, minutosPorUnidad: 1.5 },
  { secuencia: 2, nombre: 'Ensamble "final"', centroTrabajoId: 3, minutosPreparacion: 99999999.99, minutosPorUnidad: 0.01 },
];
const cuerpoOperaciones = '[{"secuencia":1,"nombre":"Corte","centroTrabajoId":3,"minutosPreparacion":0,"minutosPorUnidad":1.5},'
  + '{"secuencia":2,"nombre":"Ensamble \\"final\\"","centroTrabajoId":3,"minutosPreparacion":99999999.99,"minutosPorUnidad":0.01}]';

describe('API de rutas de producción', () => {
  it('combina filtros y omite los filtros vacíos', async () => {
    const fetchMock = vi.fn().mockImplementation(async () => respuestaJson({ items: [], total: 0, pagina: 1, tamanoPagina: 20 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarRutasProduccion({ productoId: 2, estado: 'Activa', pagina: 2, tamanoPagina: 20 });
    await listarRutasProduccion({ productoId: undefined, estado: '', pagina: 1, tamanoPagina: 20 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/rutas-produccion?productoId=2&estado=Activa&pagina=2&tamanoPagina=20');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/rutas-produccion?pagina=1&tamanoPagina=20');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('consulta el detalle con GET', async () => {
    const detalle = { id: 7, productoId: 1, productoNombre: 'Laptop Pro', version: 2, estado: 'Borrador', operaciones: [] };
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson(detalle));
    vi.stubGlobal('fetch', fetchMock);
    await expect(obtenerRutaProduccion(7)).resolves.toEqual(detalle);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/rutas-produccion/7');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea con POST y el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 9 }, 201));
    vi.stubGlobal('fetch', fetchMock);
    await expect(crearRutaProduccion({ productoId: 1, version: 3, operaciones })).resolves.toEqual({ id: 9 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/rutas-produccion');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({
      method: 'POST',
      body: `{"productoId":1,"version":3,"operaciones":${cuerpoOperaciones}}`,
    });
  });

  it('edita con PUT enviando solo las operaciones', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 7 }));
    vi.stubGlobal('fetch', fetchMock);
    await editarRutaProduccion(7, { operaciones });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/rutas-produccion/7');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: `{"operaciones":${cuerpoOperaciones}}` });
  });

  it.each(['activar', 'retirar'] as const)('pide %s con POST sin cuerpo', async (accion) => {
    const fetchMock = vi.fn<(ruta: string, init: RequestInit) => Promise<Response>>()
      .mockResolvedValue(respuestaJson({ id: 7 }));
    vi.stubGlobal('fetch', fetchMock);
    await cambiarEstadoRutaProduccion(7, accion);
    expect(fetchMock.mock.calls[0]?.[0]).toBe(`/api/v1/rutas-produccion/7/${accion}`);
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
    expect(fetchMock.mock.calls[0]?.[1].body).toBeUndefined();
  });

  it('busca productos y centros de trabajo en sus catálogos paginados', async () => {
    const fetchMock = vi.fn().mockImplementation(async (url: string) => respuestaJson({
      items: url.startsWith('/api/v1/productos')
        ? [{ id: 1, nombre: 'Laptop Pro', categoria: 'Equipos', precio: 10 }]
        : [{ id: 3, codigo: 'CT-C', nombre: 'Corte', activo: false }],
      total: 1,
      pagina: 2,
      tamanoPagina: 10,
    }));
    vi.stubGlobal('fetch', fetchMock);
    await expect(buscarProductos('lap', 2)).resolves.toEqual({ items: [{ id: 1, nombre: 'Laptop Pro' }], total: 1, tamanoPagina: 10 });
    await expect(buscarCentrosTrabajo('cor', 2)).resolves.toEqual({ items: [{ id: 3, nombre: 'Corte', activo: false }], total: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/productos?busqueda=lap&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/centros-trabajo?busqueda=cor&pagina=2&tamanoPagina=10');
  });

  it('propaga el mensaje literal de un 409', async () => {
    const mensaje = 'El producto 1 ya tiene una ruta de producción activa.';
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(respuestaJson({ StatusCode: 409, Message: mensaje }, 409)));
    await expect(cambiarEstadoRutaProduccion(7, 'activar')).rejects.toMatchObject({ statusCode: 409, message: mensaje });
  });
});

describe('validación de los minutos', () => {
  it.each([
    ['0', true, 0],
    ['99999999.99', true, 99999999.99],
    ['1,50', false, 1.5],
    ['0.01', false, 0.01],
    ['2.500', false, 2.5],
  ])('acepta %s', (entrada, admiteCero, esperado) => {
    expect(validarMinutos(entrada, admiteCero, 'rango', 'decimales')).toEqual({ valor: esperado });
  });

  it.each([
    ['', true],
    ['-1', true],
    ['100000000', true],
    ['abc', true],
    ['0', false],
  ])('rechaza %s por rango', (entrada, admiteCero) => {
    expect(validarMinutos(entrada, admiteCero, 'rango', 'decimales')).toEqual({ error: 'rango' });
  });

  it('rechaza más de 2 decimales significativos', () => {
    expect(validarMinutos('1.234', true, 'rango', 'decimales')).toEqual({ error: 'decimales' });
  });
});

describe('validación del formulario', () => {
  const centro = { id: 3, nombre: 'Ensamblaje', activo: true };

  function linea(clave: number, cambios: Partial<LineaOperacion> = {}): LineaOperacion {
    return { clave, nombre: 'Corte', centro, minutosPreparacion: '0', minutosPorUnidad: '1.5', ...cambios };
  }

  it('asigna la secuencia por posición y recorta el nombre', () => {
    const resultado = validarFormularioRuta({
      producto: { id: 1, nombre: 'Laptop Pro' },
      version: '3',
      lineas: [linea(8, { nombre: '  Corte  ' }), linea(2, { nombre: 'Pulido' })],
    }, true);
    expect(resultado.operaciones?.map((operacion) => [operacion.secuencia, operacion.nombre])).toEqual([[1, 'Corte'], [2, 'Pulido']]);
  });

  it('devuelve cada mensaje literal asociado a su campo', () => {
    const resultado = validarFormularioRuta({
      producto: null,
      version: '0',
      lineas: [linea(1, { nombre: ' ', centro: null, minutosPreparacion: '', minutosPorUnidad: '0' }), linea(2, { nombre: 'x'.repeat(121), minutosPreparacion: '1.001', minutosPorUnidad: '1.001' })],
    }, true);
    expect(resultado.operaciones).toBeUndefined();
    expect(resultado.errores).toEqual({
      producto: mensajes.producto,
      version: mensajes.version,
      '1-nombre': mensajes.nombreObligatorio,
      '1-centro': mensajes.centro,
      '1-preparacion': mensajes.preparacionRango,
      '1-unidad': mensajes.unidadRango,
      '2-nombre': mensajes.nombreLargo,
      '2-preparacion': mensajes.preparacionDecimales,
      '2-unidad': mensajes.unidadDecimales,
    });
  });

  it('exige entre 1 y 100 operaciones', () => {
    const vacio = validarFormularioRuta({ producto: null, version: '', lineas: [] }, false);
    const exceso = validarFormularioRuta({ producto: null, version: '', lineas: Array.from({ length: 101 }, (_, indice) => linea(indice)) }, false);
    expect(vacio.errores).toEqual({ operaciones: mensajes.operaciones });
    expect(exceso.errores).toEqual({ operaciones: mensajes.operaciones });
  });

  it('no exige producto ni versión al editar', () => {
    const resultado = validarFormularioRuta({ producto: null, version: '', lineas: [linea(1)] }, false);
    expect(resultado.errores).toEqual({});
    expect(resultado.operaciones).toHaveLength(1);
  });
});
