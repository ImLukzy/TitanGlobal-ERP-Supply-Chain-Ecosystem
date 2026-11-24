import { diaUtc } from './presentacion';

export interface ValoresConductor {
  nombre: string;
  documentoLicencia: string;
  vencimientoLicencia: string;
  habilitado: boolean;
  activo: boolean;
  observaciones: string;
}

export type ErroresConductor = Partial<Record<keyof ValoresConductor, string>>;

export function validarConductor(valores: ValoresConductor, fechaOriginal: string | null): ErroresConductor {
  const errores: ErroresConductor = {};
  const nombre = valores.nombre.trim();
  const licencia = valores.documentoLicencia.trim();
  if (nombre === '') { errores.nombre = 'El nombre es obligatorio.'; }
  else if (nombre.length > 100) { errores.nombre = 'El nombre no puede superar 100 caracteres.'; }
  if (licencia === '') { errores.documentoLicencia = 'El documento de licencia es obligatorio.'; }
  else if (licencia.length > 20) { errores.documentoLicencia = 'El documento de licencia no puede superar 20 caracteres.'; }
  const fecha = valores.vencimientoLicencia;
  const instante = new Date(`${fecha}T00:00:00Z`);
  const formatoValido = /^\d{4}-\d{2}-\d{2}$/.test(fecha) && Number(fecha.slice(0, 4)) >= 1
    && !Number.isNaN(instante.getTime()) && instante.toISOString().slice(0, 10) === fecha;
  const conservaFecha = fechaOriginal !== null && fecha === diaUtc(fechaOriginal);
  if (!formatoValido || (!conservaFecha && fecha < new Date().toISOString().slice(0, 10))) {
    errores.vencimientoLicencia = 'La fecha de vencimiento de la licencia no es válida.';
  }
  if (valores.observaciones.trim().length > 500) {
    errores.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  }
  return errores;
}

export function fechaParaGuardar(fecha: string, fechaOriginal: string | null): string {
  return fechaOriginal !== null && diaUtc(fechaOriginal) === fecha ? fechaOriginal : `${fecha}T00:00:00Z`;
}
