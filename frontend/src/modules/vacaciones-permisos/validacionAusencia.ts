import { aTipo, calcularDias, TIPOS_CON_MOTIVO_OBLIGATORIO } from './presentacion';
import type { RegistrarAusenciaRequest } from './tipos';

// Mensajes literales de la spec backend del módulo 65 §4 (reglas 1 a 9, 17 y 18). El backend repite las reglas y tiene la última palabra.
export const LIMITE_MOTIVO = 300;
const MAXIMO_DIAS_MATERNIDAD = 98;
const MAXIMO_DIAS_PATERNIDAD = 10;

export interface CamposAusencia {
  empleadoId: string;
  tipo: string;
  fechaInicio: string;
  fechaFin: string;
  motivo: string;
}

export type ErroresAusencia = Partial<Record<keyof CamposAusencia, string>>;

// Reglas 1 a 9: el primer fallo de cada campo gana. Las reglas de días máximos cuelgan del campo de fin.
export function validarAusencia(campos: CamposAusencia): ErroresAusencia {
  const errores: ErroresAusencia = {};
  if (campos.empleadoId.trim() === '') {
    errores.empleadoId = 'El empleado es obligatorio.';
  }
  const tipo = aTipo(campos.tipo);
  if (tipo === null) {
    errores.tipo = 'El tipo debe ser Vacaciones, PermisoConGoce, PermisoSinGoce, LicenciaMaternidad, LicenciaPaternidad o DescansoMedico.';
  }
  if (campos.fechaInicio === '') {
    errores.fechaInicio = 'La fecha de inicio es obligatoria.';
  }
  if (campos.fechaFin === '') {
    errores.fechaFin = 'La fecha de fin es obligatoria.';
  } else if (campos.fechaInicio !== '' && campos.fechaFin < campos.fechaInicio) {
    errores.fechaFin = 'La fecha de fin no puede ser anterior a la fecha de inicio.';
  }
  const motivo = campos.motivo.trim();
  if (motivo === '' && tipo !== null && TIPOS_CON_MOTIVO_OBLIGATORIO.includes(tipo)) {
    errores.motivo = 'El motivo es obligatorio para este tipo de ausencia.';
  } else if (motivo.length > LIMITE_MOTIVO) {
    errores.motivo = 'El motivo no puede superar 300 caracteres.';
  }
  if (errores.fechaFin === undefined) {
    const dias = calcularDias(campos.fechaInicio, campos.fechaFin);
    if (dias !== null && tipo === 'LicenciaMaternidad' && dias > MAXIMO_DIAS_MATERNIDAD) {
      errores.fechaFin = 'La licencia de maternidad no puede superar 98 días.';
    } else if (dias !== null && tipo === 'LicenciaPaternidad' && dias > MAXIMO_DIAS_PATERNIDAD) {
      errores.fechaFin = 'La licencia de paternidad no puede superar 10 días.';
    }
  }
  return errores;
}

// Los campos ya validados se convierten a la petición; un motivo vacío viaja como nulo.
export function construirRequest(campos: CamposAusencia): RegistrarAusenciaRequest | null {
  const tipo = aTipo(campos.tipo);
  if (tipo === null) {
    return null;
  }
  const motivo = campos.motivo.trim();
  return {
    empleadoId: Number(campos.empleadoId),
    tipo,
    fechaInicio: campos.fechaInicio,
    fechaFin: campos.fechaFin,
    motivo: motivo === '' ? null : motivo,
  };
}

// Reglas 17 y 18 del rechazo.
export function validarMotivoRechazo(motivo: string): string | undefined {
  const limpio = motivo.trim();
  if (limpio === '') {
    return 'El motivo del rechazo es obligatorio.';
  }
  if (limpio.length > LIMITE_MOTIVO) {
    return 'El motivo del rechazo no puede superar 300 caracteres.';
  }
  return undefined;
}
