import { act, cleanup, renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { permisosAdminPrueba, permisosVentasPrueba } from './permisos.testUtils';
import type { PermisoModulo } from './tipos';
import { usePermisosRol } from './usePermisosRol';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function prepararFetch(permisos: PermisoModulo[], opciones: { errorCarga?: boolean; errorGuardar?: boolean } = {}) {
  const fetchMock = vi.fn(async (_url: RequestInfo | URL, init: RequestInit = {}) => {
    const metodo = init.method ?? 'GET';
    if (metodo === 'PUT') {
      if (opciones.errorGuardar) {
        return Response.json({ StatusCode: 409, Message: 'El rol ADMIN es de sistema y no se puede modificar de esa forma.' }, { status: 409 });
      }
      return Response.json(JSON.parse(String(init.body)).permisos.map((permiso: { moduloNumero: number; puedeLeer: boolean; puedeEscribir: boolean }) => ({
        ...permisos.find((original) => original.moduloNumero === permiso.moduloNumero),
        ...permiso,
      })));
    }
    if (opciones.errorCarga) {
      return Response.json({ StatusCode: 500, Message: 'Fallo del servidor.' }, { status: 500 });
    }
    return Response.json(permisos);
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

async function cargar(rolId: number, esSistema: boolean, alGuardar: () => void = () => undefined) {
  const resultado = renderHook(() => usePermisosRol(rolId, esSistema, alGuardar));
  await waitFor(() => expect(resultado.result.current.cargando).toBe(false));
  return resultado;
}

describe('usePermisosRol (módulo 72)', () => {
  it('carga los permisos del rol con una sola petición GET', async () => {
    const fetchMock = prepararFetch(permisosVentasPrueba());
    const { result } = await cargar(2, false);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe('/api/v1/roles/2/permisos');
    expect(result.current.actual).toHaveLength(80);
    expect(result.current.cambios).toBe(0);
    expect(result.current.error).toBe('');
  });

  it('muestra el error de la API al fallar la carga', async () => {
    prepararFetch([], { errorCarga: true });
    const { result } = await cargar(2, false);
    expect(result.current.error).toBe('Fallo del servidor.');
    expect(result.current.actual).toEqual([]);
  });

  it('cuenta los cambios al editar y vuelve a cero al descartar', async () => {
    prepararFetch(permisosVentasPrueba());
    const { result } = await cargar(2, false);
    act(() => result.current.cambiarCasilla(30, 'escribir', true));
    act(() => result.current.aplicarAtajo('Compras y Abastecimiento Global', 'lectura'));
    expect(result.current.cambios).toBe(11);
    expect(result.current.actual.find((permiso) => permiso.moduloNumero === 30)).toMatchObject({ puedeLeer: true, puedeEscribir: true });
    act(() => result.current.descartar());
    expect(result.current.cambios).toBe(0);
    expect(result.current.actual).toEqual(result.current.original);
  });

  it('guarda con PUT el conjunto completo de 80 módulos y deja el nuevo estado como guardado', async () => {
    const fetchMock = prepararFetch(permisosVentasPrueba());
    const alGuardar = vi.fn();
    const { result } = await cargar(2, false, alGuardar);
    act(() => result.current.cambiarCasilla(30, 'leer', true));
    await act(async () => {
      await result.current.guardar();
    });
    const put = fetchMock.mock.calls.find((llamada) => llamada[1]?.method === 'PUT');
    expect(String(put?.[0])).toBe('/api/v1/roles/2/permisos');
    const cuerpo = JSON.parse(String(put?.[1]?.body)) as { permisos: Array<Record<string, unknown>> };
    expect(cuerpo.permisos).toHaveLength(80);
    expect(cuerpo.permisos[29]).toEqual({ moduloNumero: 30, puedeLeer: true, puedeEscribir: false });
    expect(Object.keys(cuerpo.permisos[0] ?? {})).toEqual(['moduloNumero', 'puedeLeer', 'puedeEscribir']);
    expect(alGuardar).toHaveBeenCalledTimes(1);
    expect(result.current.cambios).toBe(0);
    expect(result.current.errorGuardar).toBe('');
    expect(result.current.guardando).toBe(false);
  });

  it('si el guardado falla muestra el error y conserva los cambios', async () => {
    prepararFetch(permisosAdminPrueba(), { errorGuardar: true });
    const alGuardar = vi.fn();
    const { result } = await cargar(1, false, alGuardar);
    act(() => result.current.cambiarCasilla(5, 'escribir', false));
    await act(async () => {
      await result.current.guardar();
    });
    expect(result.current.errorGuardar).toBe('El rol ADMIN es de sistema y no se puede modificar de esa forma.');
    expect(result.current.cambios).toBe(1);
    expect(result.current.actual.find((permiso) => permiso.moduloNumero === 5)?.puedeEscribir).toBe(false);
    expect(alGuardar).not.toHaveBeenCalled();
  });

  it('en un rol de sistema los atajos no le quitan permisos', async () => {
    prepararFetch(permisosAdminPrueba());
    const { result } = await cargar(1, true);
    act(() => result.current.aplicarAtajo('Comercial, Ventas y CRM', 'ninguno'));
    expect(result.current.cambios).toBe(0);
    expect(result.current.actual.slice(0, 10).every((permiso) => permiso.puedeLeer && permiso.puedeEscribir)).toBe(true);
  });
});
