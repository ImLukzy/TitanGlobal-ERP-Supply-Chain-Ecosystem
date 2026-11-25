import { aTipoPlanilla, esFechaIso, hoyUtc } from './presentacion';
import type { CrearPlanillaRequest, GuardarAfiliacionRequest, GuardarBonoRequest, TipoPlanilla } from './tipos';

// Mensajes literales de la spec backend del módulo 66 §4. El servidor repite las reglas y tiene la última palabra.
export const LIMITE_CONCEPTO = 100;
const ANIO_MINIMO = 2000;
const ANIO_MAXIMO = 2100;
const LONGITUD_CUSPP = 12;
const MAXIMO_DIGITOS_ENTEROS = 10;
const formatoMonto = /^\d+(?:[.,]\d*)?$/;

const MENSAJE_ANIO = 'El año es obligatorio y debe estar entre 2000 y 2100.';
const MENSAJE_MES = 'El mes es obligatorio y debe estar entre 1 y 12.';
const MENSAJE_TIPO = 'El tipo de planilla debe ser Mensual o Complementaria.';

export interface CamposPlanilla {
  anio: string;
  mes: string;
  tipo: string;
}

export type ErroresPlanilla = Partial<Record<keyof CamposPlanilla, string>>;

export interface CamposBono {
  empleadoId: string;
  anio: string;
  mes: string;
  tipoPlanilla: string;
  concepto: string;
  monto: string;
}

export type ErroresBono = Partial<Record<keyof CamposBono, string>>;

export interface CamposAfiliacion {
  empleadoId: string;
  codigoPension: string;
  cuspp: string;
  fechaAfiliacion: string;
}

export type ErroresAfiliacion = Partial<Record<keyof CamposAfiliacion, string>>;

// Entero escrito como texto (nunca Number('') ni decimales): null si no lo es.
export function interpretarEntero(texto: string): number | null {
  const limpio = texto.trim();
  return /^\d{1,6}$/.test(limpio) ? Number(limpio) : null;
}

function anioValido(texto: string): boolean {
  const anio = interpretarEntero(texto);
  return anio !== null && anio >= ANIO_MINIMO && anio <= ANIO_MAXIMO;
}

function mesValido(texto: string): boolean {
  const mes = interpretarEntero(texto);
  return mes !== null && mes >= 1 && mes <= 12;
}

// Importe positivo de hasta 2 decimales y 10 enteros, validado como texto antes de convertirlo; null si no cumple (mensaje 19).
export function interpretarMontoPositivo(texto: string): number | null {
  const limpio = texto.trim();
  if (!formatoMonto.test(limpio)) {
    return null;
  }
  const partes = limpio.replace(',', '.').split('.');
  const entero = (partes[0] ?? '').replace(/^0+(?=\d)/, '');
  const decimales = partes[1] ?? '';
  if (decimales.length > 2 || entero.length > MAXIMO_DIGITOS_ENTEROS || /^0*$/.test(`${entero}${decimales}`)) {
    return null;
  }
  return Number(`${entero}.${decimales === '' ? '0' : decimales}`);
}

function esPeriodoFuturo(anio: number, mes: number): boolean {
  const hoy = hoyUtc();
  return anio * 12 + mes > Number(hoy.slice(0, 4)) * 12 + Number(hoy.slice(5, 7));
}

// Mensajes 1 a 4.
export function validarPlanilla(campos: CamposPlanilla): ErroresPlanilla {
  const errores: ErroresPlanilla = {};
  if (!anioValido(campos.anio)) {
    errores.anio = MENSAJE_ANIO;
  }
  if (!mesValido(campos.mes)) {
    errores.mes = MENSAJE_MES;
  }
  if (aTipoPlanilla(campos.tipo) === null) {
    errores.tipo = MENSAJE_TIPO;
  }
  const anio = interpretarEntero(campos.anio);
  const mes = interpretarEntero(campos.mes);
  if (errores.anio === undefined && errores.mes === undefined && anio !== null && mes !== null && esPeriodoFuturo(anio, mes)) {
    errores.mes = 'No se puede crear una planilla de un periodo futuro.';
  }
  return errores;
}

export function construirRequestPlanilla(campos: CamposPlanilla): CrearPlanillaRequest | null {
  const anio = interpretarEntero(campos.anio);
  const mes = interpretarEntero(campos.mes);
  const tipo = aTipoPlanilla(campos.tipo);
  if (anio === null || mes === null || tipo === null) {
    return null;
  }
  return { anio, mes, tipo };
}

// Mensaje 7.
export function validarFechaPago(fecha: string): string | undefined {
  return esFechaIso(fecha) ? undefined : 'La fecha de pago es obligatoria.';
}

// Mensajes 16, 1, 2, 3, 18 y 19.
export function validarBono(campos: CamposBono): ErroresBono {
  const errores: ErroresBono = {};
  if (campos.empleadoId.trim() === '') {
    errores.empleadoId = 'El empleado es obligatorio.';
  }
  if (!anioValido(campos.anio)) {
    errores.anio = MENSAJE_ANIO;
  }
  if (!mesValido(campos.mes)) {
    errores.mes = MENSAJE_MES;
  }
  if (aTipoPlanilla(campos.tipoPlanilla) === null) {
    errores.tipoPlanilla = MENSAJE_TIPO;
  }
  const concepto = campos.concepto.trim();
  if (concepto === '' || concepto.length > LIMITE_CONCEPTO) {
    errores.concepto = 'El concepto es obligatorio y no puede superar 100 caracteres.';
  }
  if (interpretarMontoPositivo(campos.monto) === null) {
    errores.monto = 'El monto debe ser mayor que cero, con 2 decimales como máximo y no superar 9999999999.99.';
  }
  return errores;
}

export function construirRequestBono(campos: CamposBono): GuardarBonoRequest | null {
  const empleadoId = interpretarEntero(campos.empleadoId);
  const anio = interpretarEntero(campos.anio);
  const mes = interpretarEntero(campos.mes);
  const tipoPlanilla: TipoPlanilla | null = aTipoPlanilla(campos.tipoPlanilla);
  const monto = interpretarMontoPositivo(campos.monto);
  if (empleadoId === null || anio === null || mes === null || tipoPlanilla === null || monto === null) {
    return null;
  }
  return { empleadoId, anio, mes, tipoPlanilla, concepto: campos.concepto.trim(), monto };
}

// Mensajes 16, 22, 24 y 25. El CUSPP solo se exige (y se envía) con una AFP.
export function validarAfiliacion(campos: CamposAfiliacion, esAfp: boolean): ErroresAfiliacion {
  const errores: ErroresAfiliacion = {};
  if (campos.empleadoId.trim() === '') {
    errores.empleadoId = 'El empleado es obligatorio.';
  }
  if (campos.codigoPension.trim() === '') {
    errores.codigoPension = 'El sistema de pensiones es obligatorio.';
  }
  if (esAfp && campos.cuspp.trim().length !== LONGITUD_CUSPP) {
    errores.cuspp = 'El CUSPP es obligatorio en una AFP y tiene 12 caracteres.';
  }
  if (!esFechaIso(campos.fechaAfiliacion)) {
    errores.fechaAfiliacion = 'La fecha de afiliación es obligatoria.';
  }
  return errores;
}

export function construirRequestAfiliacion(campos: CamposAfiliacion, esAfp: boolean): GuardarAfiliacionRequest {
  const request: GuardarAfiliacionRequest = {
    codigoPension: campos.codigoPension.trim(),
    fechaAfiliacion: campos.fechaAfiliacion,
  };
  if (esAfp) {
    request.cuspp = campos.cuspp.trim();
  }
  return request;
}
