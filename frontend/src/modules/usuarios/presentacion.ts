import { ApiError } from '../../api/client';
import type { NivelPermiso } from './tipos';

const textosNivel: Record<NivelPermiso, string> = {
  Escritura: 'Escritura',
  Lectura: 'Lectura',
  Ninguno: 'Sin acceso',
};

export function textoNivel(nivel: NivelPermiso): string {
  return textosNivel[nivel];
}

export function textoEstado(activo: boolean): string {
  return activo ? 'Activo' : 'Inactivo';
}

export function textoOtorgadoPor(codigos: string[]): string {
  return `por ${codigos.join(', ')}`;
}

export function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
}
