import { ApiError } from '../../api/client';
import type { TipoCentroTrabajo } from './tipos';

export const formatoHoras = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export function nombreTipo(tipo: TipoCentroTrabajo): string {
  return tipo === 'Linea' ? 'Línea' : 'Celda';
}

export function mensajeError(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}
