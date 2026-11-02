import type { GuardarRolRequest } from './tipos';

export interface ErroresRol {
  codigo?: string;
  nombre?: string;
  descripcion?: string;
}

export function validarRol(valores: GuardarRolRequest): ErroresRol {
  const errores: ErroresRol = {};
  const codigo = valores.codigo.trim().toUpperCase();
  const nombre = valores.nombre.trim();
  const descripcion = valores.descripcion?.trim() ?? '';

  if (codigo === '') {
    errores.codigo = 'El código es obligatorio.';
  } else if (!/^[A-Z0-9_-]{2,30}$/.test(codigo)) {
    errores.codigo = 'El código solo admite letras, números, guiones y guion bajo (2 a 30 caracteres).';
  }
  if (nombre === '') {
    errores.nombre = 'El nombre del rol es obligatorio.';
  } else if (nombre.length > 100) {
    errores.nombre = 'El nombre del rol no puede superar 100 caracteres.';
  }
  if (descripcion.length > 300) {
    errores.descripcion = 'La descripción no puede superar 300 caracteres.';
  }
  return errores;
}
