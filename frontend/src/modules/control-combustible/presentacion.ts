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

export function fechaParaInput(iso: string): string {
  return new Date(iso).toISOString().slice(0, 23);
}
export function fechaParaApi(valor: string): string {
  return `${valor}Z`;
}
