import { horaParaApi } from './presentacion';
import type { GuardarAsignacionTurnoRequest, GuardarTurnoRequest, TipoTurno } from './tipos';

// Mensajes literales de la spec backend del módulo 64 §4 (reglas 1 a 19 y 25 a 28, 36). El backend repite las reglas y tiene la última palabra.
export const LIMITE_CODIGO = 20;
export const LIMITE_NOMBRE = 100;
const TOLERANCIA_MAXIMA = 30;
const CICLO_MAXIMO = 30;

export interface CamposTurno {
  codigo: string;
  nombre: string;
  tipo: string;
  horaEntrada: string;
  horaSalida: string;
  cruzaMedianoche: boolean;
  toleranciaMinutos: string;
  diasLaborables: number[];
  diasTrabajo: string;
  diasDescanso: string;
  fechaInicioCiclo: string;
}

export type ErroresTurno = Partial<Record<keyof CamposTurno, string>>;

export interface CamposAsignacion {
  empleadoId: string;
  turnoId: string;
  fechaInicio: string;
  fechaFin: string;
}

export type ErroresAsignacion = Partial<Record<keyof CamposAsignacion, string>>;

// Cada campo conserva el primer error según el orden de las reglas del backend.
class Acumulador<T extends string> {
  readonly errores: Partial<Record<T, string>> = {};

  poner(campo: T, mensaje: string): void {
    if (this.errores[campo] === undefined) {
      this.errores[campo] = mensaje;
    }
  }
}

function esEnteroDecimal(texto: string): boolean {
  return /^\d+$/.test(texto.trim());
}

function validarHorario(campos: CamposTurno, acumulador: Acumulador<keyof CamposTurno>): void {
  if (campos.horaEntrada === '') {
    acumulador.poner('horaEntrada', 'La hora de entrada es obligatoria.');
  }
  if (campos.horaSalida === '') {
    acumulador.poner('horaSalida', 'La hora de salida es obligatoria.');
  }
  if (campos.horaEntrada === '' || campos.horaSalida === '') {
    return;
  }
  if (campos.horaSalida === campos.horaEntrada) {
    acumulador.poner('horaSalida', 'La hora de salida no puede ser igual a la hora de entrada.');
    return;
  }
  const salidaAnterior = campos.horaSalida < campos.horaEntrada;
  if (salidaAnterior && !campos.cruzaMedianoche) {
    acumulador.poner('cruzaMedianoche', 'Si la hora de salida es anterior a la de entrada, el turno debe cruzar medianoche.');
  }
  if (!salidaAnterior && campos.cruzaMedianoche) {
    acumulador.poner('cruzaMedianoche', 'El turno solo cruza medianoche si la hora de salida es anterior a la de entrada.');
  }
}

function validarTolerancia(texto: string, acumulador: Acumulador<keyof CamposTurno>): void {
  if (texto.trim() === '') {
    acumulador.poner('toleranciaMinutos', 'La tolerancia es obligatoria.');
    return;
  }
  if (!esEnteroDecimal(texto) || Number(texto) > TOLERANCIA_MAXIMA) {
    acumulador.poner('toleranciaMinutos', 'La tolerancia debe estar entre 0 y 30 minutos.');
  }
}

function validarFijo(campos: CamposTurno, acumulador: Acumulador<keyof CamposTurno>): void {
  if (campos.diasLaborables.length === 0) {
    acumulador.poner('diasLaborables', 'El turno fijo debe tener al menos un día laborable.');
  } else if (
    campos.diasLaborables.some((dia) => dia < 1 || dia > 7)
    || new Set(campos.diasLaborables).size !== campos.diasLaborables.length
  ) {
    acumulador.poner('diasLaborables', 'Los días laborables deben ser números del 1 (lunes) al 7 (domingo) sin repetir.');
  }
  if (campos.diasTrabajo.trim() !== '' || campos.diasDescanso.trim() !== '' || campos.fechaInicioCiclo !== '') {
    acumulador.poner('diasTrabajo', 'El turno fijo no usa ciclo de trabajo y descanso.');
  }
}

function enRangoCiclo(texto: string): boolean {
  return esEnteroDecimal(texto) && Number(texto) >= 1 && Number(texto) <= CICLO_MAXIMO;
}

function validarRotativo(campos: CamposTurno, acumulador: Acumulador<keyof CamposTurno>): void {
  if (campos.diasLaborables.length > 0) {
    acumulador.poner('diasLaborables', 'El turno rotativo no usa días laborables de la semana.');
  }
  if (campos.diasTrabajo.trim() === '' || campos.diasDescanso.trim() === '') {
    if (campos.diasTrabajo.trim() === '') {
      acumulador.poner('diasTrabajo', 'El turno rotativo necesita días de trabajo y días de descanso.');
    }
    if (campos.diasDescanso.trim() === '') {
      acumulador.poner('diasDescanso', 'El turno rotativo necesita días de trabajo y días de descanso.');
    }
  } else {
    if (!enRangoCiclo(campos.diasTrabajo)) {
      acumulador.poner('diasTrabajo', 'Los días de trabajo y de descanso deben estar entre 1 y 30.');
    }
    if (!enRangoCiclo(campos.diasDescanso)) {
      acumulador.poner('diasDescanso', 'Los días de trabajo y de descanso deben estar entre 1 y 30.');
    }
  }
  if (campos.fechaInicioCiclo === '') {
    acumulador.poner('fechaInicioCiclo', 'La fecha de inicio del ciclo es obligatoria en el turno rotativo.');
  }
}

export function validarTurno(campos: CamposTurno): ErroresTurno {
  const acumulador = new Acumulador<keyof CamposTurno>();
  const codigo = campos.codigo.trim();
  const nombre = campos.nombre.trim();
  if (codigo === '') {
    acumulador.poner('codigo', 'El código es obligatorio.');
  } else if (codigo.length > LIMITE_CODIGO) {
    acumulador.poner('codigo', 'El código no puede superar 20 caracteres.');
  }
  if (nombre === '') {
    acumulador.poner('nombre', 'El nombre es obligatorio.');
  } else if (nombre.length > LIMITE_NOMBRE) {
    acumulador.poner('nombre', 'El nombre no puede superar 100 caracteres.');
  }
  if (campos.tipo !== 'Fijo' && campos.tipo !== 'Rotativo') {
    acumulador.poner('tipo', 'El tipo de turno debe ser Fijo o Rotativo.');
  }
  validarHorario(campos, acumulador);
  validarTolerancia(campos.toleranciaMinutos, acumulador);
  if (campos.tipo === 'Fijo') {
    validarFijo(campos, acumulador);
  } else if (campos.tipo === 'Rotativo') {
    validarRotativo(campos, acumulador);
  }
  return acumulador.errores;
}

// Solo se envían los campos del tipo elegido; el resto queda fuera del cuerpo.
export function construirRequestTurno(campos: CamposTurno, tipo: TipoTurno): GuardarTurnoRequest {
  const request: GuardarTurnoRequest = {
    codigo: campos.codigo.trim(),
    nombre: campos.nombre.trim(),
    tipo,
    horaEntrada: horaParaApi(campos.horaEntrada),
    horaSalida: horaParaApi(campos.horaSalida),
    cruzaMedianoche: campos.cruzaMedianoche,
    toleranciaMinutos: Number(campos.toleranciaMinutos),
  };
  if (tipo === 'Fijo') {
    request.diasLaborables = [...campos.diasLaborables].sort((a, b) => a - b);
  } else {
    request.diasTrabajo = Number(campos.diasTrabajo);
    request.diasDescanso = Number(campos.diasDescanso);
    request.fechaInicioCiclo = campos.fechaInicioCiclo;
  }
  return request;
}

// Reglas 25 a 28 del backend, en su orden.
export function validarAsignacion(campos: CamposAsignacion): ErroresAsignacion {
  const acumulador = new Acumulador<keyof CamposAsignacion>();
  if (campos.empleadoId.trim() === '') {
    acumulador.poner('empleadoId', 'El empleado es obligatorio.');
  }
  if (campos.turnoId.trim() === '') {
    acumulador.poner('turnoId', 'El turno es obligatorio.');
  }
  if (campos.fechaInicio === '') {
    acumulador.poner('fechaInicio', 'La fecha de inicio es obligatoria.');
  }
  if (campos.fechaInicio !== '' && campos.fechaFin !== '' && campos.fechaFin < campos.fechaInicio) {
    acumulador.poner('fechaFin', 'La fecha de fin no puede ser anterior a la fecha de inicio.');
  }
  return acumulador.errores;
}

export function construirRequestAsignacion(campos: CamposAsignacion): GuardarAsignacionTurnoRequest {
  const request: GuardarAsignacionTurnoRequest = {
    empleadoId: Number(campos.empleadoId),
    turnoId: Number(campos.turnoId),
    fechaInicio: campos.fechaInicio,
  };
  if (campos.fechaFin !== '') {
    request.fechaFin = campos.fechaFin;
  }
  return request;
}

// Reglas 36 y 28 de la finalización.
export function validarFinalizacion(fechaFin: string, fechaInicio: string): string | undefined {
  if (fechaFin === '') {
    return 'La fecha de fin es obligatoria.';
  }
  if (fechaFin < fechaInicio) {
    return 'La fecha de fin no puede ser anterior a la fecha de inicio.';
  }
  return undefined;
}
