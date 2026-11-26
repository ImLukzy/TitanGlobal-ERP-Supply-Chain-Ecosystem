import { diasEntre, hoyPeru, localAUtc } from './presentacion';
import type { CalcularJornadasRequest, RechazarHorasExtraRequest, RegistrarMarcacionRequest, TipoMarcacion } from './tipos';

// Mensajes literales de la spec backend del módulo 63 §4. El backend repite las reglas y tiene la última palabra.
export const LIMITE_OBSERVACION = 200;
export const LIMITE_MOTIVO = 300;
export const LIMITE_DISPOSITIVO = 50;
export const MAXIMO_DIAS_CALCULO = 31;

export interface CamposMarcacion {
  empleadoId: string;
  tipo: string;
  fecha: string;
  hora: string;
  observacion: string;
}

export type ErroresMarcacion = Partial<Record<keyof CamposMarcacion, string>>;

export interface CamposCalculo {
  desde: string;
  hasta: string;
  empleadoId: string;
}

export type ErroresCalculo = Partial<Record<keyof CamposCalculo, string>>;

function esTipo(valor: string): valor is TipoMarcacion {
  return valor === 'Entrada' || valor === 'Salida';
}

// Reglas 1 a 5: la fecha y la hora forman el instante (regla 3); la observación es obligatoria en una marcación manual.
export function validarMarcacion(campos: CamposMarcacion): ErroresMarcacion {
  const errores: ErroresMarcacion = {};
  if (campos.empleadoId.trim() === '') {
    errores.empleadoId = 'El empleado es obligatorio.';
  }
  if (!esTipo(campos.tipo)) {
    errores.tipo = 'El tipo de marcación debe ser Entrada o Salida.';
  }
  if (campos.fecha === '') {
    errores.fecha = 'El instante de la marcación es obligatorio.';
  }
  if (campos.hora === '') {
    errores.hora = 'El instante de la marcación es obligatorio.';
  }
  const observacion = campos.observacion.trim();
  if (observacion === '') {
    errores.observacion = 'La observación es obligatoria en una marcación manual.';
  } else if (observacion.length > LIMITE_OBSERVACION) {
    errores.observacion = 'La observación no puede superar 200 caracteres.';
  }
  return errores;
}

// Los campos ya validados se convierten a la petición; la fecha y hora locales (UTC−5) viajan como instante UTC.
export function construirRequestMarcacion(campos: CamposMarcacion): RegistrarMarcacionRequest {
  return {
    empleadoId: Number(campos.empleadoId),
    tipo: campos.tipo === 'Salida' ? 'Salida' : 'Entrada',
    instante: localAUtc(campos.fecha, campos.hora),
    observacion: campos.observacion.trim(),
  };
}

// Reglas 10 y 28 de la importación.
export function validarDispositivo(dispositivo: string): string | undefined {
  const texto = dispositivo.trim();
  if (texto === '') {
    return 'El dispositivo es obligatorio.';
  }
  if (texto.length > LIMITE_DISPOSITIVO) {
    return 'El dispositivo no puede superar 50 caracteres.';
  }
  return undefined;
}

// Reglas 21 y 22.
export function validarMotivoRechazo(motivo: string): string | undefined {
  const texto = motivo.trim();
  if (texto === '') {
    return 'El motivo del rechazo es obligatorio.';
  }
  if (texto.length > LIMITE_MOTIVO) {
    return 'El motivo del rechazo no puede superar 300 caracteres.';
  }
  return undefined;
}

export function construirRequestRechazo(motivo: string): RechazarHorasExtraRequest {
  return { motivo: motivo.trim() };
}

// Reglas 16 a 18: el rango es obligatorio, de hasta 31 días y no llega al futuro (hora de Perú).
export function validarCalculo(campos: CamposCalculo): ErroresCalculo {
  const errores: ErroresCalculo = {};
  const mensajeRango = 'Las fechas desde y hasta son obligatorias y desde no puede ser posterior a hasta.';
  if (campos.desde === '') {
    errores.desde = mensajeRango;
  }
  if (campos.hasta === '') {
    errores.hasta = mensajeRango;
  }
  if (campos.desde === '' || campos.hasta === '') {
    return errores;
  }
  if (campos.desde > campos.hasta) {
    errores.desde = mensajeRango;
    return errores;
  }
  if (diasEntre(campos.desde, campos.hasta) + 1 > MAXIMO_DIAS_CALCULO) {
    errores.hasta = 'El rango de cálculo no puede superar 31 días.';
  } else if (campos.hasta > hoyPeru()) {
    errores.hasta = 'No se pueden calcular jornadas futuras.';
  }
  return errores;
}

export function construirRequestCalculo(campos: CamposCalculo): CalcularJornadasRequest {
  const request: CalcularJornadasRequest = { desde: campos.desde, hasta: campos.hasta };
  if (campos.empleadoId !== '') {
    request.empleadoId = Number(campos.empleadoId);
  }
  return request;
}
