import { apiPost } from './client';
import { esSesion, type Sesion } from './sesionStore';

export async function iniciarSesion(nombreUsuario: string, contrasena: string): Promise<Sesion> {
  const respuesta: unknown = await apiPost<unknown>('/autenticacion/login', { nombreUsuario, contrasena });
  if (!esSesion(respuesta)) {
    throw new Error('La respuesta de inicio de sesión no es válida.');
  }
  return respuesta;
}
