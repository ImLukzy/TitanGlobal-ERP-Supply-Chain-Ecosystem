import { getSelectedRole } from './roleStore';

const API_BASE = '/api/v1';
const GENERIC_ERROR = 'No se pudo completar la solicitud.';
const SERVER_GENERIC_ERROR = 'La API no está disponible o no respondió correctamente.';
const NETWORK_ERROR = 'No se pudo conectar con la API.';

export class ApiError extends Error {
  readonly statusCode: number;

  constructor(statusCode: number, message: string) {
    super(message);
    this.name = 'ApiError';
    this.statusCode = statusCode;
  }
}

function crearHeaders(): Headers {
  const headers = new Headers();
  const role = getSelectedRole();
  if (role !== null) {
    headers.set('X-User-Role', role);
  }
  return headers;
}

async function solicitar<T>(ruta: string, init: RequestInit): Promise<T> {
  const headers = crearHeaders();
  new Headers(init.headers).forEach((value, key) => headers.set(key, value));
  try {
    const response = await fetch(`${API_BASE}${ruta}`, { ...init, headers });
    if (!response.ok) {
      let message = GENERIC_ERROR;
      try {
        const body: unknown = await response.json();
        if (typeof body === 'object' && body !== null && 'Message' in body && typeof body.Message === 'string') {
          message = body.Message;
        }
      } catch {
        message = response.status >= 500 ? SERVER_GENERIC_ERROR : GENERIC_ERROR;
      }
      throw new ApiError(response.status, message);
    }
    if (response.status === 204) {
      return undefined as T;
    }
    const body = await response.text();
    if (body.trim() === '') {
      return undefined as T;
    }
    return JSON.parse(body) as T;
  } catch (error: unknown) {
    if (error instanceof ApiError) {
      throw error;
    }
    if (error instanceof TypeError) {
      throw new ApiError(0, NETWORK_ERROR);
    }
    throw error;
  }
}

export function apiGet<T>(ruta: string): Promise<T> {
  return solicitar<T>(ruta, { method: 'GET' });
}

export function apiPost<T>(ruta: string, cuerpo: unknown): Promise<T> {
  const headers = new Headers();
  headers.set('Content-Type', 'application/json');
  return solicitar<T>(ruta, { method: 'POST', headers, body: JSON.stringify(cuerpo) });
}

export function apiPostSinCuerpo<T>(ruta: string): Promise<T> {
  return solicitar<T>(ruta, { method: 'POST' });
}

export function apiPut<T>(ruta: string, cuerpo: unknown): Promise<T> {
  const headers = new Headers();
  headers.set('Content-Type', 'application/json');
  return solicitar<T>(ruta, { method: 'PUT', headers, body: JSON.stringify(cuerpo) });
}

export function apiDelete(ruta: string): Promise<void> {
  return solicitar<void>(ruta, { method: 'DELETE' });
}
