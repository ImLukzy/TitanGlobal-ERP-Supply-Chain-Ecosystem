import { afterEach, describe, expect, it, vi } from 'vitest';
import { apiDelete, apiGet, apiPost, apiPostSinCuerpo, apiPut, ApiError, configurarUsuarioSolicitud } from './client';
import { setSelectedRole } from './roleStore';

afterEach(() => {
  setSelectedRole(null);
  configurarUsuarioSolicitud('demo');
  vi.unstubAllGlobals();
});

describe('cliente HTTP', () => {
  it('añade el rol elegido y usa la base api/v1', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ok: true }), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);
    setSelectedRole('Administrador');

    await expect(apiGet<{ ok: boolean }>('/sistema/salud')).resolves.toEqual({ ok: true });
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/sistema/salud', expect.objectContaining({
      method: 'GET',
      headers: expect.any(Headers),
    }));
    const sentHeaders = fetchMock.mock.calls[0][1]?.headers as Headers;
    expect(sentHeaders.get('X-User-Role')).toBe('Administrador');
  });

  it('omite X-User-Role si no se eligió un rol', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    await apiPost('/ordenes-compra', { clienteId: 1 });
    const sentHeaders = fetchMock.mock.calls[0][1]?.headers as Headers;
    expect(sentHeaders.has('X-User-Role')).toBe(false);
    expect(sentHeaders.get('Content-Type')).toBe('application/json');
    expect(fetchMock.mock.calls[0][1]?.body).toBe('{"clienteId":1}');
  });

  it.each([400, 403, 500])('convierte errores HTTP %i al contrato ApiError', async (statusCode) => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ StatusCode: statusCode, Message: `Error ${statusCode}` }), { status: statusCode })));

    await expect(apiGet('/recurso')).rejects.toMatchObject({
      name: 'ApiError',
      statusCode,
      message: `Error ${statusCode}`,
    });
  });

  it('usa el mensaje de API no disponible ante un 5xx que no es JSON', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('bad gateway', { status: 502 })));

    await expect(apiGet('/recurso')).rejects.toMatchObject({
      name: 'ApiError',
      statusCode: 502,
      message: 'La API no está disponible o no respondió correctamente.',
    });
  });

  it('tolera una respuesta 200 con el cuerpo vacío', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 200 })));

    await expect(apiGet<null>('/recurso')).resolves.toBeUndefined();
  });

  it('convierte fallos de red en ApiError con statusCode 0', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    await expect(apiGet('/recurso')).rejects.toEqual(expect.objectContaining({
      name: 'ApiError',
      statusCode: 0,
      message: 'No se pudo conectar con la API.',
    } satisfies Partial<ApiError>));
  });

  it('envía X-Usuario en todas las peticiones y permite configurar el valor', async () => {
    const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(new Response('{}', { status: 200 })));
    vi.stubGlobal('fetch', fetchMock);
    configurarUsuarioSolicitud('  Ana Auditoría  ');

    await apiGet('/auditoria');
    await apiPost('/recurso', { valor: 1 });
    await apiPostSinCuerpo('/recurso/1/accion');
    await apiPut('/recurso/1', { valor: 2 });
    await apiDelete('/recurso/1');

    expect(fetchMock).toHaveBeenCalledTimes(5);
    for (const llamada of fetchMock.mock.calls) {
      const headers = llamada[1]?.headers as Headers;
      expect(headers.get('X-Usuario')).toBe('Ana Auditoría');
    }
  });

  it('usa demo como valor predeterminado de X-Usuario', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    await apiGet('/auditoria');

    const headers = fetchMock.mock.calls[0][1]?.headers as Headers;
    expect(headers.get('X-Usuario')).toBe('demo');
  });
});
