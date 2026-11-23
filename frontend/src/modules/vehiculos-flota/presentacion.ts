import { ApiError } from '../../api/client';

export const formatoCapacidad = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export function nombreTipo(tipo: string): string {
  const nombres: Record<string, string> = { Camion: 'Camión', Furgon: 'Furgón', Moto: 'Moto', Otro: 'Otro' };
  return nombres[tipo] ?? tipo;
}

export function mensajeError(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}
