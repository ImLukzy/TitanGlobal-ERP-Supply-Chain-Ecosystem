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

  it('lee el decimal exacto del detalle y conserva los nombres', async () => {
    const nombre = 'Kit "cantidad":8';
    const body = `{"productoNombre":${JSON.stringify(nombre)},"componentes":[{"cantidad":999999999999.999999},{"cantidad":0.000001}]}`;
    const fetchMock = vi.fn(async () => new Response(body));
    vi.stubGlobal('fetch', fetchMock);
    await expect(obtenerListaMaterial(7)).resolves.toMatchObject({ productoNombre: nombre, componentes: [{ cantidad: '999999999999.999999' }, { cantidad: '0.000001' }] });
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/listas-materiales/7', expect.objectContaining({ method: 'GET' }));
  });

  it('envía alta y edición con cantidades exactas como números JSON', async () => {
    const fetchMock = vi.fn(async () => Response.json({ id: 7 }));
    const componentes = [
      { componenteProductoId: 2, cantidad: '999999999999,999999', unidadMedida: ' kg ' },
      { componenteProductoId: 3, cantidad: '0.000001', unidadMedida: 'un "A"' },
    ];
    const cuerpo = '[{"componenteProductoId":2,"cantidad":999999999999.999999,"unidadMedida":"kg"},{"componenteProductoId":3,"cantidad":0.000001,"unidadMedida":"un \\"A\\""}]';
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

  it.each(['0', '-1', '1000000000000', '0.0000001'])('rechaza %s sin aproximar el decimal', (cantidad) => {
    expect(validarCantidad(cantidad)).toHaveProperty('error');
  });
});
