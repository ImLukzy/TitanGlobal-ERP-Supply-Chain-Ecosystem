import { afterEach, describe, expect, it, vi } from 'vitest';
import { actualizarCliente, crearCliente, eliminarCliente, listarClientes, obtenerCliente, obtenerResumenClientes } from './api';

afterEach(() => vi.unstubAllGlobals());

async function comprobarSolicitud(solicitud: () => Promise<unknown>, esperado: { url: string; method: string; body?: string }, respuesta = '{}') {
  const fetchMock = vi.fn().mockResolvedValue(new Response(respuesta, { status: 200 }));
  vi.stubGlobal('fetch', fetchMock);
  await solicitud();
  const llamada = fetchMock.mock.calls[0];
  expect(llamada?.[0]).toBe(esperado.url);
  const init = llamada?.[1] as RequestInit | undefined;
  expect(init?.method).toBe(esperado.method);
  if (esperado.body !== undefined) {
    expect(init?.body).toBe(esperado.body);
    expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json');
  }
}

describe('API de clientes', () => {
  it('lista con los parámetros exactos de búsqueda y paginación', async () => {
    await comprobarSolicitud(() => listarClientes('Acme Industrial', 2, 10), {
      url: '/api/v1/clientes?busqueda=Acme+Industrial&pagina=2&tamanoPagina=10',
      method: 'GET',
    });
  });

  it('obtiene el detalle por id', async () => {
    await comprobarSolicitud(() => obtenerCliente(5), { url: '/api/v1/clientes/5', method: 'GET' });
  });

  it('crea clientes con POST y su DTO', async () => {
    await comprobarSolicitud(() => crearCliente({ nombre: 'Gamma', ruc: '20333333333', descripcion: 'Prueba' }), {
      url: '/api/v1/clientes',
      method: 'POST',
      body: JSON.stringify({ nombre: 'Gamma', ruc: '20333333333', descripcion: 'Prueba' }),
    });
  });

  it('actualiza por id con PUT y su DTO', async () => {
    await comprobarSolicitud(() => actualizarCliente(3, { nombre: 'Gamma', ruc: '20333333333' }), {
      url: '/api/v1/clientes/3',
      method: 'PUT',
      body: JSON.stringify({ nombre: 'Gamma', ruc: '20333333333' }),
    });
  });

  it('elimina por id con DELETE y acepta 204 sin cuerpo', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await expect(eliminarCliente(5)).resolves.toBeUndefined();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/clientes/5');
    expect((fetchMock.mock.calls[0]?.[1] as RequestInit | undefined)?.method).toBe('DELETE');
  });

  it('obtiene el resumen', async () => {
    await comprobarSolicitud(() => obtenerResumenClientes(), { url: '/api/v1/clientes/resumen', method: 'GET' });
  });
});
