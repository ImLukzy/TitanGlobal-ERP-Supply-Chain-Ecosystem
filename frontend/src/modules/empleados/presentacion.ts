import { ApiError } from '../../api/client';
import type { EstadoEmpleado } from './tipos';

const textosEstado: Record<EstadoEmpleado, string> = {
  Activo: 'Activo',
  Cesado: 'Cesado',
};

export function textoEstado(estado: EstadoEmpleado): string {
  return textosEstado[estado];
}

export function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
}
