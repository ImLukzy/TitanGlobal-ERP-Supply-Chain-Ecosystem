// Mensajes literales de la spec backend del módulo 62 §4 (reglas 1 a 12, 20, 21, 26 y 27). El backend repite las reglas y tiene la última palabra.
export const LIMITE_CARGO = 100;
export const LIMITE_MOTIVO_ANULACION = 300;

const SUELDO_MAXIMO = 9999999999.99;
const DECIMALES_MAXIMOS = 2;
const formatoSueldo = /^\d+(\.\d+)?$/;
const modalidades = ['Indeterminado', 'PlazoFijo', 'Formativo'];

export interface CamposContrato {
  empleadoId: string;
  modalidad: string;
  fechaInicio: string;
  fechaFin: string;
  sueldoBasico: string;
  cargo: string;
}

export type ErroresContrato = Partial<Record<keyof CamposContrato, string>>;

export interface CamposRenovacion {
  modalidad: string;
  fechaInicio: string;
  fechaFin: string;
  sueldoBasico: string;
  cargo: string;
}

export type ErroresRenovacion = Partial<Record<'modalidad' | 'fechaFin' | 'sueldoBasico' | 'cargo', string>>;

export interface CamposAdenda {
  fechaVigencia: string;
  cargo: string;
  sueldoBasico: string;
}

export type ErroresAdenda = Partial<Record<keyof CamposAdenda | 'general', string>>;

export type ErroresAnulacion = Partial<Record<'motivo', string>>;

// Convierte el texto del sueldo a número solo si tiene formato válido; si no, devuelve null.
export function sueldoComoNumero(texto: string): number | null {
  const limpio = texto.trim();
  if (!formatoSueldo.test(limpio)) {
    return null;
  }
  return Number(limpio);
}

// Reglas 4 a 7 del backend, en su orden. Se valida el texto antes de convertirlo para no redondear en silencio.
export function validarSueldo(texto: string): string | undefined {
  const limpio = texto.trim();
  if (limpio === '') {
    return 'El sueldo básico es obligatorio.';
  }
  if (!formatoSueldo.test(limpio)) {
    return 'El sueldo básico debe ser mayor que cero.';
  }
  const valor = Number(limpio);
  if (valor <= 0) {
    return 'El sueldo básico debe ser mayor que cero.';
  }
  if (valor > SUELDO_MAXIMO) {
    return 'El sueldo básico no puede superar 9999999999.99.';
  }
  const fraccion = (limpio.split('.')[1] ?? '').replace(/0+$/, '');
  if (fraccion.length > DECIMALES_MAXIMOS) {
    return 'El sueldo básico admite como máximo 2 decimales.';
  }
  return undefined;
}

function validarCargo(cargo: string): string | undefined {
  const limpio = cargo.trim();
  if (limpio === '') {
    return 'El cargo es obligatorio.';
  }
  if (limpio.length > LIMITE_CARGO) {
    return 'El cargo no puede superar 100 caracteres.';
  }
  return undefined;
}

// Reglas 10 a 12: fecha de fin según la modalidad y orden respecto al inicio.
function validarFechaFin(modalidad: string, fechaInicio: string, fechaFin: string): string | undefined {
  if (modalidad === 'Indeterminado') {
    if (fechaFin !== '') {
      return 'El contrato indeterminado no tiene fecha de fin.';
    }
    return undefined;
  }
  if (!modalidades.includes(modalidad)) {
    return undefined;
  }
  if (fechaFin === '') {
    return 'La fecha de fin es obligatoria para esta modalidad.';
  }
  if (fechaInicio !== '' && fechaFin <= fechaInicio) {
    return 'La fecha de fin debe ser posterior a la fecha de inicio.';
  }
  return undefined;
}

export function validarContrato(campos: CamposContrato): ErroresContrato {
  const errores: ErroresContrato = {};
  if (campos.empleadoId.trim() === '') {
    errores.empleadoId = 'El empleado es obligatorio.';
  }
  if (!modalidades.includes(campos.modalidad)) {
    errores.modalidad = 'La modalidad no es válida.';
  }
  if (campos.fechaInicio === '') {
    errores.fechaInicio = 'La fecha de inicio es obligatoria.';
  }
  const errorSueldo = validarSueldo(campos.sueldoBasico);
  if (errorSueldo !== undefined) {
    errores.sueldoBasico = errorSueldo;
  }
  const errorCargo = validarCargo(campos.cargo);
  if (errorCargo !== undefined) {
    errores.cargo = errorCargo;
  }
  const errorFin = validarFechaFin(campos.modalidad, campos.fechaInicio, campos.fechaFin);
  if (errorFin !== undefined) {
    errores.fechaFin = errorFin;
  }
  return errores;
}

export function validarRenovacion(campos: CamposRenovacion): ErroresRenovacion {
  const errores: ErroresRenovacion = {};
  if (!modalidades.includes(campos.modalidad)) {
    errores.modalidad = 'La modalidad no es válida.';
  }
  const errorSueldo = validarSueldo(campos.sueldoBasico);
  if (errorSueldo !== undefined) {
    errores.sueldoBasico = errorSueldo;
  }
  const errorCargo = validarCargo(campos.cargo);
  if (errorCargo !== undefined) {
    errores.cargo = errorCargo;
  }
  const errorFin = validarFechaFin(campos.modalidad, campos.fechaInicio, campos.fechaFin);
  if (errorFin !== undefined) {
    errores.fechaFin = errorFin;
  }
  return errores;
}

export function validarAdenda(campos: CamposAdenda): ErroresAdenda {
  const errores: ErroresAdenda = {};
  const cargo = campos.cargo.trim();
  const sueldo = campos.sueldoBasico.trim();
  if (campos.fechaVigencia === '') {
    errores.fechaVigencia = 'La fecha de vigencia es obligatoria.';
  }
  if (cargo === '' && sueldo === '') {
    errores.general = 'La adenda debe cambiar el cargo o el sueldo básico.';
    return errores;
  }
  if (sueldo !== '') {
    const errorSueldo = validarSueldo(sueldo);
    if (errorSueldo !== undefined) {
      errores.sueldoBasico = errorSueldo;
    }
  }
  if (cargo.length > LIMITE_CARGO) {
    errores.cargo = 'El cargo no puede superar 100 caracteres.';
  }
  return errores;
}

export function validarAnulacion(motivo: string): ErroresAnulacion {
  const limpio = motivo.trim();
  if (limpio === '') {
    return { motivo: 'El motivo de anulación es obligatorio.' };
  }
  if (limpio.length > LIMITE_MOTIVO_ANULACION) {
    return { motivo: 'El motivo de anulación no puede superar 300 caracteres.' };
  }
  return {};
}

// Día siguiente en yyyy-MM-dd, calculado en UTC para no depender de la zona horaria del navegador.
export function diaSiguiente(fecha: string): string {
  const [anio, mes, dia] = fecha.split('-').map(Number);
  return new Date(Date.UTC(anio, mes - 1, dia + 1)).toISOString().slice(0, 10);
}
