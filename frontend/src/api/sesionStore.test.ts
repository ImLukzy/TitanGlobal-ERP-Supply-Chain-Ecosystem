import { afterEach, describe, expect, it, vi } from 'vitest';
import { getSesion, limpiarSesion, reiniciarSesionStore, setSesion, subscribeToSesion, type Sesion } from './sesionStore';

const CLAVE = 'titanglobal-sesion';

function crearSesion(expiraEn: string): Sesion {
  return {
    token: 'token-falso',
    expiraEn,
    usuario: { id: 1, nombreUsuario: 'demo', nombreCompleto: 'Usuario de demostración' },
    roles: ['ADMIN'],
  };
}

function enUnaHora(): string {
  return new Date(Date.now() + 3_600_000).toISOString();
}

afterEach(() => {
  vi.restoreAllMocks();
  window.localStorage.clear();
  reiniciarSesionStore();
});

describe('sesionStore', () => {
  it('guarda la sesión y la lee tras reiniciar la memoria', () => {
    const sesion: Sesion = crearSesion(enUnaHora());
    setSesion(sesion);
    expect(window.localStorage.getItem(CLAVE)).not.toBeNull();
    reiniciarSesionStore();
    expect(getSesion()).toEqual(sesion);
  });

  it('descarta una sesión vencida al leerla', () => {
    window.localStorage.setItem(CLAVE, JSON.stringify(crearSesion('2000-01-01T00:00:00Z')));
    expect(getSesion()).toBeNull();
    expect(window.localStorage.getItem(CLAVE)).toBeNull();
  });

  it('ignora JSON inválido o con forma incorrecta', () => {
    window.localStorage.setItem(CLAVE, '{no es json');
    expect(getSesion()).toBeNull();
    reiniciarSesionStore();
    window.localStorage.setItem(CLAVE, JSON.stringify({ token: 'x', expiraEn: enUnaHora(), usuario: { id: 'uno' }, roles: [] }));
    expect(getSesion()).toBeNull();
  });

  it('funciona en memoria si el almacenamiento está bloqueado', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('bloqueado');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('bloqueado');
    });
    vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(() => {
      throw new Error('bloqueado');
    });
    expect(getSesion()).toBeNull();
    const sesion: Sesion = crearSesion(enUnaHora());
    setSesion(sesion);
    expect(getSesion()).toEqual(sesion);
    limpiarSesion();
    expect(getSesion()).toBeNull();
  });

  it('notifica a los suscriptores y borra con limpiarSesion', () => {
    const oyente = vi.fn();
    const baja: () => void = subscribeToSesion(oyente);
    setSesion(crearSesion(enUnaHora()));
    limpiarSesion();
    baja();
    limpiarSesion();
    expect(oyente).toHaveBeenCalledTimes(2);
    expect(window.localStorage.getItem(CLAVE)).toBeNull();
  });
});
