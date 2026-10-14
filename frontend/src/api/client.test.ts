import { afterEach, describe, expect, it, vi } from 'vitest';
import { apiGet, apiPost, ApiError } from './client';
import { setSelectedRole } from './roleStore';

afterEach(() => {
  setSelectedRole(null);
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

  it('usa un mensaje genérico ante un cuerpo de error que no es JSON', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('bad gateway', { status: 502 })));

    await expect(apiGet('/recurso')).rejects.toMatchObject({
      name: 'ApiError',
      statusCode: 502,
      message: 'No se pudo completar la solicitud.',
    });
  });

  it('convierte fallos de red en ApiError con statusCode 0', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    await expect(apiGet('/recurso')).rejects.toEqual(expect.objectContaining({
      name: 'ApiError',
      statusCode: 0,
      message: 'No se pudo conectar con la API.',
    } satisfies Partial<ApiError>));
  });
});
