import { ApiError } from '../../api/client';
export function mensajeError(error: unknown): string {
  return error instanceof ApiError
    ? error.message
    : 'No se pudo completar la operación. Intenta de nuevo.';
}
export function fechaUtc(fecha?: string | null): string {
  return fecha
    ? new Intl.DateTimeFormat('es-PE', {
        timeZone: 'UTC',
        dateStyle: 'medium',
        timeStyle: 'short',
      }).format(new Date(fecha))
    : '—';
}
