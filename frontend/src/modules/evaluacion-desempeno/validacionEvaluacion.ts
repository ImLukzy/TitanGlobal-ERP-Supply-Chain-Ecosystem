import { esFechaIso, PERIODOS } from './presentacion';
import type { GuardarCalificacionesRequest, GuardarCicloRequest, GuardarCompetenciaRequest } from './tipos';

export const LIMITE_CODIGO = 20;
export const LIMITE_NOMBRE = 100;
export const LIMITE_DESCRIPCION = 300;
export const LIMITE_COMENTARIO = 500;

const MS_POR_DIA = 86_400_000;

export interface CamposCiclo {
  codigo: string;
  nombre: string;
  periodo: string;
  fechaInicioPeriodo: string;
  fechaFinPeriodo: string;
  fechaApertura: string;
  fechaCierre: string;
}

export type ErroresCiclo = Partial<Record<keyof CamposCiclo, string>>;

export interface CamposCompetencia {
  nombre: string;
  descripcion: string;
  peso: string;
}

export type ErroresCompetencia = Partial<Record<keyof CamposCompetencia, string>>;

export interface CamposNuevaEvaluacion {
  cicloId: string;
  empleadoId: string;
  evaluadorId: string;
}

export type ErroresNuevaEvaluacion = Partial<Record<keyof CamposNuevaEvaluacion, string>>;

function dias(fecha: string): number {
  const partes = fecha.split('-').map(Number);
  return Date.UTC(partes[0] ?? 0, (partes[1] ?? 1) - 1, partes[2] ?? 1) / MS_POR_DIA;
}

function duracionValida(periodo: string, inicio: string, fin: string): boolean {
  const duracion = dias(fin) - dias(inicio) + 1;
  if (periodo === 'Semestral') {
    return duracion >= 170 && duracion <= 190;
  }
  return duracion >= 360 && duracion <= 370;
}

export function validarCiclo(campos: CamposCiclo): ErroresCiclo {
  const errores: ErroresCiclo = {};
  const codigo = campos.codigo.trim();
  if (codigo === '' || codigo.length > LIMITE_CODIGO) {
    errores.codigo = 'El código es obligatorio y no puede superar 20 caracteres.';
  }
  const nombre = campos.nombre.trim();
  if (nombre === '' || nombre.length > LIMITE_NOMBRE) {
    errores.nombre = 'El nombre es obligatorio y no puede superar 100 caracteres.';
  }
  if (!PERIODOS.some((periodo) => periodo === campos.periodo)) {
    errores.periodo = 'El periodo debe ser Semestral o Anual.';
  }
  const fechas: (keyof CamposCiclo)[] = ['fechaInicioPeriodo', 'fechaFinPeriodo', 'fechaApertura', 'fechaCierre'];
  const faltantes = fechas.filter((clave) => !esFechaIso(campos[clave]));
  for (const clave of faltantes) {
    errores[clave] = 'Las fechas del periodo, de apertura y de cierre son obligatorias.';
  }
  if (faltantes.length > 0) {
    return errores;
  }
  if (errores.periodo === undefined && !duracionValida(campos.periodo, campos.fechaInicioPeriodo, campos.fechaFinPeriodo)) {
    errores.fechaFinPeriodo = `La duración del periodo no corresponde a un ciclo ${campos.periodo}.`;
  }
  if (dias(campos.fechaApertura) < dias(campos.fechaFinPeriodo) - 30) {
    errores.fechaApertura = 'La apertura no puede ser anterior a 30 días antes del fin del periodo.';
  }
  if (dias(campos.fechaCierre) <= dias(campos.fechaApertura)) {
    errores.fechaCierre = 'La fecha de cierre debe ser posterior a la de apertura.';
  }
  return errores;
}

export function construirRequestCiclo(campos: CamposCiclo): GuardarCicloRequest {
  return {
    codigo: campos.codigo.trim(),
    nombre: campos.nombre.trim(),
    periodo: campos.periodo,
    fechaInicioPeriodo: campos.fechaInicioPeriodo,
    fechaFinPeriodo: campos.fechaFinPeriodo,
    fechaApertura: campos.fechaApertura,
    fechaCierre: campos.fechaCierre,
  };
}

// Peso con coma o punto, mayor que 0, como máximo 100 y hasta 2 decimales (mensaje 11).
export function interpretarPeso(texto: string): number | null {
  const limpio = texto.trim().replace(',', '.');
  if (!/^\d+(\.\d{1,2})?$/.test(limpio)) {
    return null;
  }
  const peso = Number(limpio);
  return peso > 0 && peso <= 100 ? peso : null;
}

export function validarCompetencia(campos: CamposCompetencia): ErroresCompetencia {
  const errores: ErroresCompetencia = {};
  const nombre = campos.nombre.trim();
  if (nombre === '' || nombre.length > LIMITE_NOMBRE) {
    errores.nombre = 'El nombre es obligatorio y no puede superar 100 caracteres.';
  }
  if (campos.descripcion.trim().length > LIMITE_DESCRIPCION) {
    errores.descripcion = 'La descripción no puede superar 300 caracteres.';
  }
  if (interpretarPeso(campos.peso) === null) {
    errores.peso = 'El peso debe ser mayor que 0 y como máximo 100, con 2 decimales.';
  }
  return errores;
}

export function construirRequestCompetencia(campos: CamposCompetencia): GuardarCompetenciaRequest | null {
  const peso = interpretarPeso(campos.peso);
  if (peso === null) {
    return null;
  }
  const descripcion = campos.descripcion.trim();
  return { nombre: campos.nombre.trim(), descripcion: descripcion === '' ? null : descripcion, peso };
}

export function validarNuevaEvaluacion(campos: CamposNuevaEvaluacion): ErroresNuevaEvaluacion {
  const errores: ErroresNuevaEvaluacion = {};
  const obligatorio = 'El ciclo, el empleado y el evaluador son obligatorios.';
  if (campos.cicloId === '') {
    errores.cicloId = obligatorio;
  }
  if (campos.empleadoId === '') {
    errores.empleadoId = obligatorio;
  }
  if (campos.evaluadorId === '') {
    errores.evaluadorId = obligatorio;
  } else if (campos.evaluadorId === campos.empleadoId) {
    errores.evaluadorId = 'El evaluador no puede ser el propio evaluado.';
  }
  return errores;
}

export function validarComentario(comentario: string): string {
  return comentario.trim().length > LIMITE_COMENTARIO ? 'El comentario no puede superar 500 caracteres.' : '';
}

export function construirRequestCalificaciones(calificaciones: Map<number, number>, comentario: string): GuardarCalificacionesRequest {
  const texto = comentario.trim();
  return {
    calificaciones: Array.from(calificaciones, ([competenciaId, calificacion]) => ({ competenciaId, calificacion })),
    comentario: texto === '' ? null : texto,
  };
}

// Puntaje ponderado en centésimas, con enteros: Σ(peso/100 × calificación) redondeado a 2 decimales (mitad hacia arriba),
// igual que el backend. peso en centésimas × calificación / 100 = puntaje en centésimas.
export function calcularPuntajeCentesimas(items: { peso: number; calificacion: number }[]): number {
  const suma = items.reduce((total, item) => total + Math.round(item.peso * 100) * item.calificacion, 0);
  return Math.floor((suma + 50) / 100);
}

export function formatearCentesimas(centesimas: number): string {
  const decimales = String(centesimas % 100).padStart(2, '0');
  return `${Math.floor(centesimas / 100)},${decimales}`;
}

export function formatearDecimal(valor: number): string {
  return formatearCentesimas(Math.round(valor * 100));
}

// Etiquetas del servidor: < 2 Insuficiente, < 3 En desarrollo, < 4 Competente, < 4,5 Destacado y si no Excepcional.
export function etiquetaPuntaje(centesimas: number): string {
  if (centesimas < 200) {
    return 'Insuficiente';
  }
  if (centesimas < 300) {
    return 'En desarrollo';
  }
  if (centesimas < 400) {
    return 'Competente';
  }
  return centesimas < 450 ? 'Destacado' : 'Excepcional';
}
