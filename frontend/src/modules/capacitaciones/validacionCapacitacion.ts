import { esFechaIso, TIPOS_CURSO } from './presentacion';
import type { EmitirCertificacionRequest, GuardarCursoRequest, GuardarProgramacionRequest } from './tipos';

export const LIMITE_CODIGO = 20;
export const LIMITE_NOMBRE = 150;
export const LIMITE_PROVEEDOR = 150;
export const LIMITE_LUGAR = 150;
export const LIMITE_CODIGO_CERTIFICACION = 40;

export interface CamposCurso {
  codigo: string;
  nombre: string;
  horas: string;
  tipo: string;
  proveedor: string;
}

export type ErroresCurso = Partial<Record<keyof CamposCurso, string>>;

export interface CamposProgramacion {
  cursoId: string;
  fechaInicio: string;
  fechaFin: string;
  cupo: string;
  lugar: string;
}

export type ErroresProgramacion = Partial<Record<keyof CamposProgramacion, string>>;

export interface CamposCertificacion {
  codigo: string;
  fechaEmision: string;
  fechaVencimiento: string;
}

export type ErroresCertificacion = Partial<Record<keyof CamposCertificacion, string>>;

// Acepta coma o punto; devuelve null si no es un número con hasta un decimal.
export function interpretarDecimal(texto: string): number | null {
  const limpio = texto.trim().replace(',', '.');
  if (!/^\d+(\.\d)?$/.test(limpio)) {
    return null;
  }
  return Number(limpio);
}

export function interpretarHoras(texto: string): number | null {
  const horas = interpretarDecimal(texto);
  return horas !== null && horas > 0 && horas <= 500 ? horas : null;
}

export function interpretarNota(texto: string): number | null {
  const nota = interpretarDecimal(texto);
  return nota !== null && nota <= 20 ? nota : null;
}

export function validarCurso(campos: CamposCurso): ErroresCurso {
  const errores: ErroresCurso = {};
  const codigo = campos.codigo.trim();
  if (codigo === '' || codigo.length > LIMITE_CODIGO) {
    errores.codigo = 'El código es obligatorio y no puede superar 20 caracteres.';
  }
  const nombre = campos.nombre.trim();
  if (nombre === '' || nombre.length > LIMITE_NOMBRE) {
    errores.nombre = 'El nombre es obligatorio y no puede superar 150 caracteres.';
  }
  if (interpretarHoras(campos.horas) === null) {
    errores.horas = 'Las horas deben ser mayores que 0 y como máximo 500, con un decimal.';
  }
  if (!TIPOS_CURSO.some((tipo) => tipo === campos.tipo)) {
    errores.tipo = 'El tipo debe ser Interno, Externo o Virtual.';
  }
  if (campos.proveedor.trim().length > LIMITE_PROVEEDOR) {
    errores.proveedor = 'El proveedor no puede superar 150 caracteres.';
  }
  return errores;
}

export function construirRequestCurso(campos: CamposCurso): GuardarCursoRequest {
  const proveedor = campos.proveedor.trim();
  return {
    codigo: campos.codigo.trim(),
    nombre: campos.nombre.trim(),
    horas: interpretarHoras(campos.horas) ?? 0,
    tipo: campos.tipo,
    proveedor: proveedor === '' ? null : proveedor,
  };
}

export function validarProgramacion(campos: CamposProgramacion): ErroresProgramacion {
  const errores: ErroresProgramacion = {};
  if (campos.cursoId === '') {
    errores.cursoId = 'Selecciona un curso.';
  }
  const inicioValido = esFechaIso(campos.fechaInicio);
  const finValido = esFechaIso(campos.fechaFin);
  if (!inicioValido) {
    errores.fechaInicio = 'Las fechas de inicio y fin son obligatorias.';
  }
  if (!finValido) {
    errores.fechaFin = 'Las fechas de inicio y fin son obligatorias.';
  }
  if (inicioValido && finValido && campos.fechaFin < campos.fechaInicio) {
    errores.fechaFin = 'La fecha de fin no puede ser anterior a la de inicio.';
  }
  const cupo = campos.cupo.trim();
  if (!/^\d+$/.test(cupo) || Number(cupo) < 1 || Number(cupo) > 500) {
    errores.cupo = 'El cupo debe estar entre 1 y 500.';
  }
  if (campos.lugar.trim().length > LIMITE_LUGAR) {
    errores.lugar = 'El lugar no puede superar 150 caracteres.';
  }
  return errores;
}

export function construirRequestProgramacion(campos: CamposProgramacion): GuardarProgramacionRequest {
  const lugar = campos.lugar.trim();
  return {
    cursoId: Number(campos.cursoId),
    fechaInicio: campos.fechaInicio,
    fechaFin: campos.fechaFin,
    cupo: Number(campos.cupo.trim()),
    lugar: lugar === '' ? null : lugar,
  };
}

// La emisión no puede ser anterior al fin del curso (mensaje 28) ni el vencimiento a la emisión (mensaje 29).
export function validarCertificacion(campos: CamposCertificacion, fechaFinCurso: string): ErroresCertificacion {
  const errores: ErroresCertificacion = {};
  const codigo = campos.codigo.trim();
  if (codigo === '' || codigo.length > LIMITE_CODIGO_CERTIFICACION) {
    errores.codigo = 'El código de certificación es obligatorio y no puede superar 40 caracteres.';
  }
  const emisionValida = esFechaIso(campos.fechaEmision);
  if (!emisionValida || campos.fechaEmision < fechaFinCurso) {
    errores.fechaEmision = 'La fecha de emisión es obligatoria y no puede ser anterior al fin del curso.';
  }
  if (campos.fechaVencimiento !== '' && (!esFechaIso(campos.fechaVencimiento) || (emisionValida && campos.fechaVencimiento < campos.fechaEmision))) {
    errores.fechaVencimiento = 'La fecha de vencimiento no puede ser anterior a la de emisión.';
  }
  return errores;
}

export function construirRequestCertificacion(campos: CamposCertificacion): EmitirCertificacionRequest {
  return {
    codigo: campos.codigo.trim(),
    fechaEmision: campos.fechaEmision,
    fechaVencimiento: campos.fechaVencimiento === '' ? null : campos.fechaVencimiento,
  };
}
