export interface ValoresAcuerdo {
  numero: string;
  proveedorId: number | '';
  descripcion: string;
  fechaInicio: string;
  fechaFin: string;
  montoMaximo: string;
}

export type CampoAcuerdo = 'numero' | 'proveedor' | 'descripcion' | 'montoMaximo' | 'fechas';
export type ErroresAcuerdo = Partial<Record<CampoAcuerdo, string>>;

const decimalValido = /^\d{1,16}(\.\d{1,2})?$/;

/** Mismo orden y mismos literales que la spec de backend del módulo 20. */
export function validarAcuerdo(valores: ValoresAcuerdo, edicion: boolean): ErroresAcuerdo {
  const errores: ErroresAcuerdo = {};
  if (!edicion) {
    const numero = valores.numero.trim();
    if (numero === '') {
      errores.numero = 'El número es obligatorio.';
    } else if (numero.length > 20) {
      errores.numero = 'El número no puede superar 20 caracteres.';
    }
    if (valores.proveedorId === '') {
      errores.proveedor = 'El proveedor es obligatorio.';
    }
  }
  if (valores.descripcion.trim().length > 500) {
    errores.descripcion = 'La descripción no puede superar 500 caracteres.';
  }
  const monto = valores.montoMaximo.trim();
  if (monto === '' || Number.isNaN(Number(monto)) || Number(monto) <= 0) {
    errores.montoMaximo = 'El monto máximo debe ser mayor a 0.';
  } else if (!decimalValido.test(monto)) {
    errores.montoMaximo = 'El monto máximo no es válido.';
  }
  const finDelDia = new Date(`${valores.fechaFin}T23:59:59`).getTime();
  if (valores.fechaInicio === '' || valores.fechaFin === '' || Number.isNaN(finDelDia) || valores.fechaFin <= valores.fechaInicio) {
    errores.fechas = 'La fecha de fin debe ser posterior a la de inicio.';
  } else if (finDelDia <= Date.now()) {
    errores.fechas = 'La fecha de fin debe ser futura.';
  }
  return errores;
}

export interface ValoresLinea {
  productoId: number | '';
  precio: string;
  cantidad: string;
}

export type CampoLinea = 'producto' | 'precio' | 'cantidad';
export type ErroresLinea = Partial<Record<CampoLinea, string>>;

export function validarLinea(valores: ValoresLinea): ErroresLinea {
  const errores: ErroresLinea = {};
  if (valores.productoId === '') {
    errores.producto = 'El producto es obligatorio.';
  }
  const precio = valores.precio.trim();
  if (precio === '' || Number.isNaN(Number(precio)) || Number(precio) <= 0) {
    errores.precio = 'El precio acordado debe ser mayor a 0.';
  } else if (!decimalValido.test(precio)) {
    errores.precio = 'El precio acordado no es válido.';
  }
  const cantidad = valores.cantidad.trim();
  if (!/^\d+$/.test(cantidad) || Number(cantidad) <= 0) {
    errores.cantidad = 'La cantidad comprometida debe ser mayor a 0.';
  } else if (Number(cantidad) > 1000000) {
    errores.cantidad = 'La cantidad comprometida no puede superar 1000000.';
  }
  return errores;
}

export function validarConsumo(cantidadTexto: string, saldo: number): string | null {
  const texto = cantidadTexto.trim();
  if (!/^\d+$/.test(texto) || Number(texto) <= 0) {
    return 'La cantidad debe ser mayor a 0.';
  }
  if (Number(texto) > saldo) {
    return `La cantidad supera el saldo de la línea (${saldo}).`;
  }
  return null;
}
