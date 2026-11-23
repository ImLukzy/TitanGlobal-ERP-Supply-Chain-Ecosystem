import { ApiError } from '../../api/client';

export function diaUtc(fecha: string): string {
  return new Date(fecha).toISOString().slice(0, 10);
}

export function licenciaVencida(fecha: string): boolean {
  return diaUtc(fecha) < new Date().toISOString().slice(0, 10);
}

export function mensajeError(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}
