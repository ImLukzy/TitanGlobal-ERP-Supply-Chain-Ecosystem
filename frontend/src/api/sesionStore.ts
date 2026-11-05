export interface UsuarioSesion {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
}

export interface Sesion {
  token: string;
  expiraEn: string;
  usuario: UsuarioSesion;
  roles: string[];
}

const STORAGE_KEY = 'titanglobal-sesion';
const listeners = new Set<() => void>();
let sesionMemoria: Sesion | null = null;
let cargada = false;

function esObjeto(valor: unknown): valor is Record<string, unknown> {
  return typeof valor === 'object' && valor !== null;
}

export function esSesion(valor: unknown): valor is Sesion {
  if (!esObjeto(valor) || !esObjeto(valor.usuario)) {
    return false;
  }
  const usuario: Record<string, unknown> = valor.usuario;
  return typeof valor.token === 'string'
    && valor.token !== ''
    && typeof valor.expiraEn === 'string'
    && !Number.isNaN(Date.parse(valor.expiraEn))
    && typeof usuario.id === 'number'
    && typeof usuario.nombreUsuario === 'string'
    && typeof usuario.nombreCompleto === 'string'
    && Array.isArray(valor.roles)
    && valor.roles.every((rol: unknown) => typeof rol === 'string');
}

function estaVigente(sesion: Sesion): boolean {
  return Date.parse(sesion.expiraEn) > Date.now();
}

function leerAlmacenamiento(): Sesion | null {
  try {
    const crudo: string | null = window.localStorage.getItem(STORAGE_KEY);
    if (crudo === null) {
      return null;
    }
    const valor: unknown = JSON.parse(crudo);
    if (esSesion(valor) && estaVigente(valor)) {
      return valor;
    }
    window.localStorage.removeItem(STORAGE_KEY);
    return null;
  } catch {
    return null;
  }
}

export function getSesion(): Sesion | null {
  if (!cargada) {
    sesionMemoria = leerAlmacenamiento();
    cargada = true;
  }
  if (sesionMemoria !== null && !estaVigente(sesionMemoria)) {
    // getSesion se usa como snapshot de useSyncExternalStore y debe ser pura: se avisa a los
    // suscriptores fuera del render.
    sesionMemoria = null;
    try {
      window.localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Nada que borrar si el almacenamiento está bloqueado.
    }
    queueMicrotask(() => listeners.forEach((listener) => listener()));
  }
  return sesionMemoria;
}

export function subscribeToSesion(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function setSesion(sesion: Sesion): void {
  sesionMemoria = sesion;
  cargada = true;
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(sesion));
  } catch {
    // La sesión sigue activa mientras dure la pestaña si el almacenamiento está bloqueado.
  }
  listeners.forEach((listener) => listener());
}

export function limpiarSesion(): void {
  sesionMemoria = null;
  cargada = true;
  try {
    window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Nada que borrar si el almacenamiento está bloqueado.
  }
  listeners.forEach((listener) => listener());
}

/** Solo para pruebas: olvida el estado en memoria para volver a leer el almacenamiento. */
export function reiniciarSesionStore(): void {
  sesionMemoria = null;
  cargada = false;
}
