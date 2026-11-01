// Mensajes literales de la spec 43 §4 (reglas 1 a 3). El backend los repite y tiene la última palabra.
export interface CamposUsuario {
  nombreUsuario: string;
  nombreCompleto: string;
  email: string;
}

export type ErroresUsuario = Partial<Record<keyof CamposUsuario, string>>;

const formatoNombreUsuario = /^[a-z0-9._-]{3,30}$/;
const formatoEmail = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

export function validarUsuario(campos: CamposUsuario): ErroresUsuario {
  const errores: ErroresUsuario = {};
  const nombreUsuario = campos.nombreUsuario.trim().toLowerCase();
  const nombreCompleto = campos.nombreCompleto.trim();
  const email = campos.email.trim().toLowerCase();

  if (nombreUsuario === '') {
    errores.nombreUsuario = 'El nombre de usuario es obligatorio.';
  } else if (!formatoNombreUsuario.test(nombreUsuario)) {
    errores.nombreUsuario = 'El nombre de usuario solo admite letras minúsculas, números, punto, guion y guion bajo (3 a 30 caracteres).';
  }

  if (nombreCompleto === '') {
    errores.nombreCompleto = 'El nombre completo es obligatorio.';
  } else if (nombreCompleto.length > 100) {
    errores.nombreCompleto = 'El nombre completo no puede superar 100 caracteres.';
  }

  if (email === '') {
    errores.email = 'El email es obligatorio.';
  } else if (email.length > 150 || !formatoEmail.test(email)) {
    errores.email = 'El email no tiene un formato válido.';
  }

  return errores;
}
