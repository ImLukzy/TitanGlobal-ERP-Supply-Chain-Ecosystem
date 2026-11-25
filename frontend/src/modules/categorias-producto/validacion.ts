import type { GuardarCategoriaProductoRequest } from './tipos';

export interface ErroresCategoriaProducto {
  nombre?: string;
  descripcion?: string;
}

export function validarCategoriaProducto(datos: GuardarCategoriaProductoRequest): ErroresCategoriaProducto {
  const errores: ErroresCategoriaProducto = {};
  const nombre = datos.nombre.trim();

  if (nombre === '') {
    errores.nombre = 'El nombre de la categoría es obligatorio.';
  } else if (nombre.length > 100) {
    errores.nombre = 'El nombre no puede superar los 100 caracteres.';
  }

  if (datos.descripcion !== undefined && datos.descripcion.trim().length > 250) {
    errores.descripcion = 'La descripción no puede superar los 250 caracteres.';
  }

  return errores;
}
