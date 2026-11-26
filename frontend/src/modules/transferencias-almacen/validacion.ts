import type { GuardarTransferenciaRequest } from './tipos';

export interface ErroresTransferencia {
  productoId?: string;
  almacenOrigenId?: string;
  almacenDestinoId?: string;
  cantidad?: string;
  observaciones?: string;
}

export function validarTransferencia(
  valores: GuardarTransferenciaRequest,
): ErroresTransferencia {
  const errores: ErroresTransferencia = {};

  if (!Number.isInteger(valores.productoId) || valores.productoId < 1) {
    errores.productoId = 'El producto es obligatorio.';
  }
  if (!Number.isInteger(valores.almacenOrigenId) || valores.almacenOrigenId < 1) {
    errores.almacenOrigenId = 'El almacén de origen es obligatorio.';
  }
  if (!Number.isInteger(valores.almacenDestinoId) || valores.almacenDestinoId < 1) {
    errores.almacenDestinoId = 'El almacén de destino es obligatorio.';
  } else if (valores.almacenOrigenId === valores.almacenDestinoId) {
    errores.almacenDestinoId = 'El almacén de origen y el de destino deben ser distintos.';
  }
  if (!Number.isInteger(valores.cantidad) || valores.cantidad < 1 || valores.cantidad > 1000000) {
    errores.cantidad = 'La cantidad de la transferencia debe estar entre 1 y 1000000.';
  }
  if (valores.observaciones !== undefined && valores.observaciones.trim().length > 500) {
    errores.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  }

  return errores;
}
