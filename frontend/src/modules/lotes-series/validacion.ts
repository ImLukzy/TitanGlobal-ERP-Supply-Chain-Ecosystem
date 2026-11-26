import type { ActualizarLoteRequest, GuardarLoteRequest, GuardarSerieRequest } from './tipos';

export interface ErroresLote {
  productoId?: string;
  codigo?: string;
  cantidad?: string;
  fechas?: string;
}

export interface ErroresSerie {
  productoId?: string;
  loteId?: string;
  numeroSerie?: string;
}

const REGEX_CODIGO_LOTE = /^[A-Z0-9-]{1,30}$/;
const REGEX_NUMERO_SERIE = /^[A-Z0-9-]{1,50}$/;

export function validarNuevoLote(valores: GuardarLoteRequest): ErroresLote {
  const errores: ErroresLote = {};
  if (!Number.isInteger(valores.productoId) || valores.productoId < 1) {
    errores.productoId = 'El producto es obligatorio.';
  }
  const codigo = valores.codigo.trim().toUpperCase();
  if (codigo === '') {
    errores.codigo = 'El código del lote es obligatorio.';
  } else if (!REGEX_CODIGO_LOTE.test(codigo)) {
    errores.codigo = 'El código del lote solo admite letras, números y guiones (1 a 30 caracteres).';
  }
  if (!Number.isInteger(valores.cantidad) || valores.cantidad < 1 || valores.cantidad > 1000000) {
    errores.cantidad = 'La cantidad del lote debe estar entre 1 y 1000000.';
  }
  if (
    valores.fechaFabricacion !== undefined &&
    valores.fechaVencimiento !== undefined &&
    valores.fechaVencimiento < valores.fechaFabricacion
  ) {
    errores.fechas = 'La fecha de vencimiento no puede ser anterior a la de fabricación.';
  }
  return errores;
}

export function validarEdicionLote(valores: ActualizarLoteRequest): ErroresLote {
  const errores: ErroresLote = {};
  if (!Number.isInteger(valores.cantidad) || valores.cantidad < 1 || valores.cantidad > 1000000) {
    errores.cantidad = 'La cantidad del lote debe estar entre 1 y 1000000.';
  }
  if (
    valores.fechaFabricacion !== undefined &&
    valores.fechaVencimiento !== undefined &&
    valores.fechaVencimiento < valores.fechaFabricacion
  ) {
    errores.fechas = 'La fecha de vencimiento no puede ser anterior a la de fabricación.';
  }
  return errores;
}

export function validarNuevaSerie(valores: GuardarSerieRequest): ErroresSerie {
  const errores: ErroresSerie = {};
  if (!Number.isInteger(valores.productoId) || valores.productoId < 1) {
    errores.productoId = 'El producto es obligatorio.';
  }
  if (valores.loteId !== undefined && (!Number.isInteger(valores.loteId) || valores.loteId < 1)) {
    errores.loteId = 'El identificador de lote debe ser válido.';
  }
  const numeroSerie = valores.numeroSerie.trim().toUpperCase();
  if (numeroSerie === '') {
    errores.numeroSerie = 'El número de serie es obligatorio.';
  } else if (!REGEX_NUMERO_SERIE.test(numeroSerie)) {
    errores.numeroSerie = 'El número de serie solo admite letras, números y guiones (1 a 50 caracteres).';
  }
  return errores;
}
