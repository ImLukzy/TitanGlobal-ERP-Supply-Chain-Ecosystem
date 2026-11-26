import type { GuardarMovimientoRequest } from './tipos';

export interface ErroresMovimiento {
  productoId?: string;
  almacenId?: string;
  cantidad?: string;
  costoUnitario?: string;
  referencia?: string;
}

export function validarMovimiento(valores: GuardarMovimientoRequest): ErroresMovimiento {
  const errores: ErroresMovimiento = {};

  if (!Number.isInteger(valores.productoId) || valores.productoId < 1) {
    errores.productoId = 'El producto es obligatorio.';
  }
  if (!Number.isInteger(valores.almacenId) || valores.almacenId < 1) {
    errores.almacenId = 'El almacén es obligatorio.';
  }
  if (!Number.isInteger(valores.cantidad) || valores.cantidad < 1 || valores.cantidad > 1000000) {
    errores.cantidad = 'La cantidad del movimiento debe estar entre 1 y 1000000.';
  }
  if (valores.tipo === 'Entrada') {
    const costo = valores.costoUnitario;
    if (costo === undefined || Number.isNaN(costo) || costo <= 0 || costo > 1000000) {
      errores.costoUnitario =
        'El costo unitario es obligatorio en las entradas y debe ser mayor que 0.';
    }
  }
  if (valores.referencia !== undefined && valores.referencia.trim().length > 100) {
    errores.referencia = 'La referencia no puede superar 100 caracteres.';
  }

  return errores;
}
