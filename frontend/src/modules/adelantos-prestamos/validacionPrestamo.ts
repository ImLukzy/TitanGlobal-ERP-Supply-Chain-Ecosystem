import { interpretarMonto, MAXIMO_CUOTAS } from './cronograma';
import { aTipo, esFechaIso } from './presentacion';
import type { RegistrarPrestamoRequest } from './tipos';

// Mensajes literales de la spec backend del módulo 67 §4 (reglas 1 a 12, 23 y 24). El backend repite las reglas y tiene la última palabra.
export const LIMITE_MOTIVO = 300;
const ANIO_MINIMO = 2000;
const ANIO_MAXIMO = 2100;
const formatoMes = /^(\d{4})-(\d{2})$/;

export interface CamposPrestamo {
  empleadoId: string;
  tipo: string;
  monto: string;
  numeroCuotas: string;
  fechaOtorgamiento: string;
  primerMes: string;
  motivo: string;
}

export type ErroresPrestamo = Partial<Record<keyof CamposPrestamo, string>>;

export interface MesInicial {
  anio: number;
  mes: number;
}

// «2030-11» → { anio: 2030, mes: 11 }; null si no es un mes válido del rango admitido.
export function interpretarMes(valor: string): MesInicial | null {
  const partes = formatoMes.exec(valor);
  if (partes === null) {
    return null;
  }
  const anio = Number(partes[1]);
  const mes = Number(partes[2]);
  if (anio < ANIO_MINIMO || anio > ANIO_MAXIMO || mes < 1 || mes > 12) {
    return null;
  }
  return { anio, mes };
}

// Número de cuotas efectivo: un adelanto siempre es una; en un préstamo, el entero escrito (null si no es un entero).
export function cuotasEfectivas(campos: CamposPrestamo): number | null {
  if (campos.tipo === 'Adelanto') {
    return 1;
  }
  const texto = campos.numeroCuotas.trim();
  return /^\d+$/.test(texto) ? Number(texto) : null;
}

// Reglas 1 a 12: el primer fallo de cada campo gana.
export function validarPrestamo(campos: CamposPrestamo): ErroresPrestamo {
  const errores: ErroresPrestamo = {};
  if (campos.empleadoId.trim() === '') {
    errores.empleadoId = 'El empleado es obligatorio.';
  }
  const tipo = aTipo(campos.tipo);
  if (tipo === null) {
    errores.tipo = 'El tipo debe ser Adelanto o Prestamo.';
  }
  const monto = interpretarMonto(campos.monto);
  if (monto.estado === 'vacio') {
    errores.monto = 'El monto es obligatorio.';
  } else if (monto.estado === 'noPositivo') {
    errores.monto = 'El monto debe ser mayor que cero.';
  } else if (monto.estado === 'formato') {
    errores.monto = 'El monto admite como máximo 2 decimales y no puede superar 9999999999.99.';
  }
  if (tipo === 'Adelanto') {
    if (campos.numeroCuotas.trim() !== '' && campos.numeroCuotas.trim() !== '1') {
      errores.numeroCuotas = 'Un adelanto se descuenta en una sola cuota.';
    }
  } else if (tipo === 'Prestamo') {
    const cuotas = cuotasEfectivas(campos);
    if (cuotas === null || cuotas < 1 || cuotas > MAXIMO_CUOTAS) {
      errores.numeroCuotas = 'El número de cuotas debe estar entre 1 y 24.';
    }
  }
  if (!esFechaIso(campos.fechaOtorgamiento)) {
    errores.fechaOtorgamiento = 'La fecha de otorgamiento es obligatoria.';
  }
  const mes = interpretarMes(campos.primerMes);
  if (mes === null) {
    errores.primerMes = 'El primer mes de descuento es obligatorio y debe ser un mes válido.';
  } else if (esFechaIso(campos.fechaOtorgamiento)) {
    const otorgamiento = Number(campos.fechaOtorgamiento.slice(0, 4)) * 12 + Number(campos.fechaOtorgamiento.slice(5, 7));
    if (mes.anio * 12 + mes.mes < otorgamiento) {
      errores.primerMes = 'El primer mes de descuento no puede ser anterior al mes de otorgamiento.';
    }
  }
  const motivo = campos.motivo.trim();
  if (motivo === '') {
    errores.motivo = 'El motivo es obligatorio.';
  } else if (motivo.length > LIMITE_MOTIVO) {
    errores.motivo = 'El motivo no puede superar 300 caracteres.';
  }
  return errores;
}

// Los campos ya validados se convierten a la petición. El monto se envía como número solo tras validarse como texto.
export function construirRequest(campos: CamposPrestamo): RegistrarPrestamoRequest | null {
  const tipo = aTipo(campos.tipo);
  const monto = interpretarMonto(campos.monto);
  const cuotas = cuotasEfectivas(campos);
  const mes = interpretarMes(campos.primerMes);
  if (tipo === null || monto.estado !== 'valido' || cuotas === null || mes === null) {
    return null;
  }
  return {
    empleadoId: Number(campos.empleadoId),
    tipo,
    monto: monto.centavos / 100,
    numeroCuotas: cuotas,
    fechaOtorgamiento: campos.fechaOtorgamiento,
    primerAnio: mes.anio,
    primerMes: mes.mes,
    motivo: campos.motivo.trim(),
  };
}

// Reglas 23 y 24 de la anulación.
export function validarMotivoAnulacion(motivo: string): string | undefined {
  const limpio = motivo.trim();
  if (limpio === '') {
    return 'El motivo de la anulación es obligatorio.';
  }
  if (limpio.length > LIMITE_MOTIVO) {
    return 'El motivo de la anulación no puede superar 300 caracteres.';
  }
  return undefined;
}
