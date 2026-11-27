export interface ValoresLicitacion {
  numero: string;
  titulo: string;
  descripcion: string;
  presupuesto: string;
  fechaCierre: string;
}

export type CampoLicitacion = 'numero' | 'titulo' | 'presupuesto' | 'fechaCierre' | 'descripcion';
export type ErroresLicitacion = Partial<Record<CampoLicitacion, string>>;

/** Mismo orden y mismos literales que la spec de backend del módulo 14. */
export function validarLicitacion(valores: ValoresLicitacion, edicion: boolean): ErroresLicitacion {
  const errores: ErroresLicitacion = {};
  if (!edicion) {
    const numero = valores.numero.trim();
    if (numero === '') {
      errores.numero = 'El número es obligatorio.';
    } else if (numero.length > 20) {
      errores.numero = 'El número no puede superar 20 caracteres.';
    }
  }
  const titulo = valores.titulo.trim();
  if (titulo === '') {
    errores.titulo = 'El título es obligatorio.';
  } else if (titulo.length > 150) {
    errores.titulo = 'El título no puede superar 150 caracteres.';
  }
  const presupuesto = valores.presupuesto.trim();
  if (presupuesto === '' || Number.isNaN(Number(presupuesto)) || Number(presupuesto) <= 0) {
    errores.presupuesto = 'El presupuesto referencial debe ser mayor a 0.';
  } else if (!/^\d{1,16}(\.\d{1,2})?$/.test(presupuesto)) {
    errores.presupuesto = 'El presupuesto referencial no es válido.';
  }
  if (valores.fechaCierre === '' || !(new Date(valores.fechaCierre).getTime() > Date.now())) {
    errores.fechaCierre = 'La fecha de cierre debe ser futura.';
  }
  if (valores.descripcion.trim().length > 500) {
    errores.descripcion = 'La descripción no puede superar 500 caracteres.';
  }
  return errores;
}

export interface ValoresOferta {
  proveedorId: number | '';
  monto: string;
  plazo: string;
  observaciones: string;
}

export type CampoOferta = 'proveedor' | 'monto' | 'plazo' | 'observaciones';
export type ErroresOferta = Partial<Record<CampoOferta, string>>;

export function validarOferta(valores: ValoresOferta, presupuesto: number): ErroresOferta {
  const errores: ErroresOferta = {};
  if (valores.proveedorId === '') {
    errores.proveedor = 'El proveedor es obligatorio.';
  }
  const monto = valores.monto.trim() === '' ? Number.NaN : Number(valores.monto);
  if (Number.isNaN(monto) || monto <= 0) {
    errores.monto = 'El monto debe ser mayor a 0.';
  } else if (monto > presupuesto) {
    errores.monto = 'El monto no puede superar el presupuesto referencial.';
  }
  if (!/^\d{1,3}$/.test(valores.plazo.trim()) || Number(valores.plazo) < 1 || Number(valores.plazo) > 365) {
    errores.plazo = 'El plazo de entrega debe estar entre 1 y 365 días.';
  }
  if (valores.observaciones.trim().length > 500) {
    errores.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  }
  return errores;
}

export function validarMejora(montoTexto: string, montoActual: number): string | null {
  const monto = montoTexto.trim() === '' ? Number.NaN : Number(montoTexto);
  if (Number.isNaN(monto) || monto <= 0) {
    return 'El monto debe ser mayor a 0.';
  }
  if (monto >= montoActual) {
    return 'La nueva oferta debe ser menor a la actual.';
  }
  return null;
}
