import { afterEach, describe, expect, it, vi } from 'vitest';
import { cambiarEstadoListaMaterial, crearListaMaterial, editarListaMaterial, listarListasMateriales, obtenerListaMaterial } from './api';
import { validarCantidad } from './validacion';

afterEach(() => vi.unstubAllGlobals());

describe('API de listas de materiales', () => {
  it('combina filtros y omite los filtros vacíos', async () => {
    const fetchMock = vi.fn(async () => Response.json({ items: [] }));
    vi.stubGlobal('fetch', fetchMock);
    await listarListasMateriales({ productoId: 2, estado: 'Activa', pagina: 2, tamanoPagina: 20 });
    await listarListasMateriales({ estado: '', pagina: 1, tamanoPagina: 20 });
    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/v1/listas-materiales?productoId=2&estado=Activa&pagina=2&tamanoPagina=20', expect.objectContaining({ method: 'GET' }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/v1/listas-materiales?pagina=1&tamanoPagina=20', expect.objectContaining({ method: 'GET' }));
  });

  it('consulta el detalle con GET y conserva nombres y cantidades', async () => {
    const detalle = { id: 7, productoNombre: 'Kit "cantidad":8', componentes: [{ cantidad: 999999999.999999 }, { cantidad: 0.000001 }] };
    const fetchMock = vi.fn(async () => Response.json(detalle));
    vi.stubGlobal('fetch', fetchMock);
    await expect(obtenerListaMaterial(7)).resolves.toMatchObject(detalle);
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/listas-materiales/7', expect.objectContaining({ method: 'GET' }));
  });

  it('envía alta y edición con el cuerpo exacto y la unidad recortada', async () => {
    const fetchMock = vi.fn(async () => Response.json({ id: 7 }));
    const componentes = [
      { componenteProductoId: 2, cantidad: 999999999.999999, unidadMedida: ' kg ' },
      { componenteProductoId: 3, cantidad: 0.000001, unidadMedida: 'un "A"' },
    ];
    const cuerpo = '[{"componenteProductoId":2,"cantidad":999999999.999999,"unidadMedida":"kg"},{"componenteProductoId":3,"cantidad":0.000001,"unidadMedida":"un \\"A\\""}]';
    vi.stubGlobal('fetch', fetchMock);
    await crearListaMaterial({ productoId: 1, version: 3, componentes });
    await editarListaMaterial(7, { componentes });
    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/v1/listas-materiales', expect.objectContaining({ method: 'POST', body: `{"productoId":1,"version":3,"componentes":${cuerpo}}` }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/v1/listas-materiales/7', expect.objectContaining({ method: 'PUT', body: `{"componentes":${cuerpo}}` }));
  });

  it.each(['activar', 'retirar'] as const)('pide %s con POST sin cuerpo', async (accion) => {
    const fetchMock = vi.fn<(ruta: string, init: RequestInit) => Promise<Response>>().mockImplementation(async () => Response.json({ id: 7 }));
    vi.stubGlobal('fetch', fetchMock);
    await cambiarEstadoListaMaterial(7, accion);
    expect(fetchMock).toHaveBeenCalledWith(`/api/v1/listas-materiales/7/${accion}`, expect.objectContaining({ method: 'POST' }));
    expect(fetchMock.mock.calls[0]?.[1].body).toBeUndefined();
  });
});

describe('validación de la cantidad', () => {
  it.each([
    ['999999999999.999', 999999999999.999],
    ['999999999,999999', 999999999.999999],
    ['0.000001', 0.000001],
    ['2,50', 2.5],
  ])('acepta %s y la convierte a un número exacto', (entrada, esperado) => {
    const resultado = validarCantidad(entrada);
    expect(resultado).toEqual({ cantidad: esperado });
    if ('cantidad' in resultado) {
      expect(JSON.stringify(resultado.cantidad)).toBe(String(esperado));
    }
  });

  it.each(['0', '-1', '1000000000000', 'abc'])('rechaza %s por rango', (cantidad) => {
    expect(validarCantidad(cantidad)).toEqual({ error: 'La cantidad debe ser mayor que 0 y no superar 999999999999.999999.' });
  });

  it('rechaza más de 6 decimales', () => {
    expect(validarCantidad('0.0000001')).toEqual({ error: 'La cantidad admite como máximo 6 decimales.' });
  });

  it.each(['999999999999.9999', '999999999999.999999', '1234567890.123456'])('rechaza %s por superar 15 dígitos significativos', (cantidad) => {
    expect(validarCantidad(cantidad)).toEqual({ error: 'La cantidad admite como máximo 15 dígitos significativos.' });
  });
});
