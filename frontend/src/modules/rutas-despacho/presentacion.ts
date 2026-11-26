import { ApiError } from '../../api/client';
export function mensajeError(error: unknown): string {
  return error instanceof ApiError
    ? error.message
    : 'No se pudo completar la operación. Intenta de nuevo.';
}
