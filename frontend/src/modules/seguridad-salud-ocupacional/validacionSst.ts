import { esFechaIso, RESULTADOS_EXAMEN, SEVERIDADES, TIPOS_EXAMEN, TIPOS_INCIDENTE } from './presentacion';
import type {
  GuardarAccionRequest,
  GuardarEquipoRequest,
  RegistrarEntregaRequest,
  RegistrarExamenRequest,
  RegistrarIncidenteRequest,
} from './tipos';

export const LIMITE_CODIGO = 20;
export const LIMITE_NOMBRE = 100;
export const LIMITE_DESCRIPCION_INCIDENTE = 1000;
export const LIMITE_LUGAR = 150;
export const LIMITE_DESCRIPCION_ACCION = 500;
export const LIMITE_RESPONSABLE = 150;
export const LIMITE_RESTRICCIONES = 300;
export const LIMITE_CLINICA = 150;

export const MENSAJE_EMPLEADO = 'El empleado es obligatorio.';
export const MENSAJE_FECHA = 'La fecha es obligatoria y no puede ser posterior a hoy.';

export interface CamposEquipo {
  codigo: string;
  nombre: string;
  vidaUtilDias: string;
}

export interface CamposEntrega {
  equipoId: string;
  empleadoId: string;
  fechaEntrega: string;
  cantidad: string;
}

export interface CamposExamen {
  empleadoId: string;
  tipo: string;
  fecha: string;
  resultado: string;
  restricciones: string;
  fechaVencimiento: string;
  clinica: string;
}

export interface CamposIncidente {
  empleadoId: string;
  tipo: string;
  severidad: string;
  fecha: string;
  diasBaja: string;
  lugar: string;
  descripcion: string;
}

export interface CamposAccion {
  descripcion: string;
  responsable: string;
  fechaCompromiso: string;
}

export type Errores<T> = Partial<Record<keyof T, string>>;

function enteroEnRango(texto: string, minimo: number, maximo: number): boolean {
  const limpio = texto.trim();
  return /^\d+$/.test(limpio) && Number(limpio) >= minimo && Number(limpio) <= maximo;
}

// Fecha obligatoria, válida y no posterior a hoy (mensaje 11).
export function fechaValida(fecha: string, hoy: string): boolean {
  return esFechaIso(fecha) && fecha <= hoy;
}

export function validarEquipo(campos: CamposEquipo): Errores<CamposEquipo> {
  const errores: Errores<CamposEquipo> = {};
  const codigo = campos.codigo.trim();
  if (codigo === '' || codigo.length > LIMITE_CODIGO) {
    errores.codigo = 'El código es obligatorio y no puede superar 20 caracteres.';
  }
  const nombre = campos.nombre.trim();
  if (nombre === '' || nombre.length > LIMITE_NOMBRE) {
    errores.nombre = 'El nombre es obligatorio y no puede superar 100 caracteres.';
  }
  if (!enteroEnRango(campos.vidaUtilDias, 1, 3650)) {
    errores.vidaUtilDias = 'La vida útil debe estar entre 1 y 3650 días.';
  }
  return errores;
}

export function construirRequestEquipo(campos: CamposEquipo): GuardarEquipoRequest {
  return { codigo: campos.codigo.trim(), nombre: campos.nombre.trim(), vidaUtilDias: Number(campos.vidaUtilDias.trim()) };
}

export function validarEntrega(campos: CamposEntrega, hoy: string): Errores<CamposEntrega> {
  const errores: Errores<CamposEntrega> = {};
  if (campos.empleadoId === '') {
    errores.empleadoId = MENSAJE_EMPLEADO;
  }
  if (campos.equipoId === '') {
    errores.equipoId = 'Selecciona un equipo.';
  }
  if (!fechaValida(campos.fechaEntrega, hoy)) {
    errores.fechaEntrega = MENSAJE_FECHA;
  }
  if (!enteroEnRango(campos.cantidad, 1, 100)) {
    errores.cantidad = 'La cantidad debe estar entre 1 y 100.';
  }
  return errores;
}

export function construirRequestEntrega(campos: CamposEntrega): RegistrarEntregaRequest {
  return {
    equipoId: Number(campos.equipoId),
    empleadoId: Number(campos.empleadoId),
    fechaEntrega: campos.fechaEntrega,
    cantidad: Number(campos.cantidad.trim()),
  };
}

// Mensajes 8, 11 y 13 a 19; las restricciones solo cuentan con AptoConRestricciones y el vencimiento no existe en Retiro.
export function validarExamen(campos: CamposExamen, hoy: string): Errores<CamposExamen> {
  const errores: Errores<CamposExamen> = {};
  if (campos.empleadoId === '') {
    errores.empleadoId = MENSAJE_EMPLEADO;
  }
  if (!TIPOS_EXAMEN.some((tipo) => tipo === campos.tipo)) {
    errores.tipo = 'El tipo de examen debe ser Ingreso, Periodico o Retiro.';
  }
  if (!fechaValida(campos.fecha, hoy)) {
    errores.fecha = MENSAJE_FECHA;
  }
  if (!RESULTADOS_EXAMEN.some((resultado) => resultado === campos.resultado)) {
    errores.resultado = 'El resultado debe ser Apto, AptoConRestricciones o NoApto.';
  }
  const restricciones = campos.restricciones.trim();
  if ((campos.resultado === 'AptoConRestricciones' && restricciones === '') || restricciones.length > LIMITE_RESTRICCIONES) {
    errores.restricciones = 'Las restricciones son obligatorias si el resultado es AptoConRestricciones y no pueden superar 300 caracteres.';
  }
  if (campos.tipo !== 'Retiro' && (!esFechaIso(campos.fechaVencimiento) || campos.fechaVencimiento <= campos.fecha)) {
    errores.fechaVencimiento = 'La fecha de vencimiento es obligatoria y posterior al examen en los exámenes de ingreso y periódicos.';
  }
  if (campos.clinica.trim().length > LIMITE_CLINICA) {
    errores.clinica = 'La clínica no puede superar 150 caracteres.';
  }
  return errores;
}

export function construirRequestExamen(campos: CamposExamen): RegistrarExamenRequest {
  const restricciones = campos.restricciones.trim();
  const clinica = campos.clinica.trim();
  return {
    empleadoId: Number(campos.empleadoId),
    tipo: campos.tipo,
    fecha: campos.fecha,
    resultado: campos.resultado,
    restricciones: campos.resultado === 'AptoConRestricciones' && restricciones !== '' ? restricciones : null,
    fechaVencimiento: campos.tipo === 'Retiro' ? null : campos.fechaVencimiento,
    clinica: clinica === '' ? null : clinica,
  };
}

// Mensajes 8, 11 y 20 a 25: solo un accidente con baja lleva días de baja.
export function validarIncidente(campos: CamposIncidente, hoy: string): Errores<CamposIncidente> {
  const errores: Errores<CamposIncidente> = {};
  if (campos.empleadoId === '') {
    errores.empleadoId = MENSAJE_EMPLEADO;
  }
  if (!TIPOS_INCIDENTE.some((tipo) => tipo === campos.tipo)) {
    errores.tipo = 'El tipo de incidente debe ser AccidenteSinBaja, AccidenteConBaja, IncidenteSinLesion o EnfermedadOcupacional.';
  }
  if (!SEVERIDADES.some((severidad) => severidad === campos.severidad)) {
    errores.severidad = 'La severidad debe ser Leve, Moderada, Grave o Mortal.';
  }
  if (!fechaValida(campos.fecha, hoy)) {
    errores.fecha = MENSAJE_FECHA;
  }
  if (campos.tipo === 'AccidenteConBaja' && !enteroEnRango(campos.diasBaja, 1, 3650)) {
    errores.diasBaja = 'Un accidente con baja exige al menos 1 día de baja.';
  }
  const descripcion = campos.descripcion.trim();
  if (descripcion === '' || descripcion.length > LIMITE_DESCRIPCION_INCIDENTE) {
    errores.descripcion = 'La descripción es obligatoria y no puede superar 1000 caracteres.';
  }
  if (campos.lugar.trim().length > LIMITE_LUGAR) {
    errores.lugar = 'El lugar no puede superar 150 caracteres.';
  }
  return errores;
}

export function construirRequestIncidente(campos: CamposIncidente): RegistrarIncidenteRequest {
  const lugar = campos.lugar.trim();
  return {
    empleadoId: Number(campos.empleadoId),
    tipo: campos.tipo,
    severidad: campos.severidad,
    fecha: campos.fecha,
    lugar: lugar === '' ? null : lugar,
    descripcion: campos.descripcion.trim(),
    diasBaja: campos.tipo === 'AccidenteConBaja' ? Number(campos.diasBaja.trim()) : 0,
  };
}

// Mensaje 28: descripción, responsable y fecha de compromiso.
export function validarAccion(campos: CamposAccion): Errores<CamposAccion> {
  const errores: Errores<CamposAccion> = {};
  const mensaje = 'La acción correctiva necesita descripción (máximo 500), responsable (máximo 150) y fecha de compromiso.';
  const descripcion = campos.descripcion.trim();
  if (descripcion === '' || descripcion.length > LIMITE_DESCRIPCION_ACCION) {
    errores.descripcion = mensaje;
  }
  const responsable = campos.responsable.trim();
  if (responsable === '' || responsable.length > LIMITE_RESPONSABLE) {
    errores.responsable = mensaje;
  }
  if (!esFechaIso(campos.fechaCompromiso)) {
    errores.fechaCompromiso = mensaje;
  }
  return errores;
}

export function construirRequestAccion(campos: CamposAccion): GuardarAccionRequest {
  return { descripcion: campos.descripcion.trim(), responsable: campos.responsable.trim(), fechaCompromiso: campos.fechaCompromiso };
}
