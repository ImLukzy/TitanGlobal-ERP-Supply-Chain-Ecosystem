import type {
  CancelarMantenimientoRequest,
  CentroOpcion,
  CompletarMantenimientoRequest,
  EditarMantenimientoRequest,
} from './tipos';

export const mensajes = {
  centro: 'El identificador del centro de trabajo debe ser mayor que 0.',
  descripcionObligatoria: 'La descripción del mantenimiento es obligatoria.',
  descripcionLarga: 'La descripción del mantenimiento no puede superar 250 caracteres.',
  fechaObligatoria: 'La fecha programada es obligatoria.',
  fechaPasada: 'La fecha programada no puede ser anterior a hoy.',
  duracionRango: 'La duración debe ser mayor que 0 y como máximo 168 horas.',
  duracionDecimales: 'La duración admite como máximo 2 decimales.',
  frecuencia: 'La frecuencia debe estar entre 1 y 3650 días.',
  responsable: 'El responsable no puede superar 100 caracteres.',
  horasRango: 'Las horas reales deben ser mayores que 0 y como máximo 168.',
  horasDecimales: 'Las horas reales admiten como máximo 2 decimales.',
  observaciones: 'Las observaciones no pueden superar 500 caracteres.',
  motivoObligatorio: 'El motivo de cancelación es obligatorio.',
  motivoLargo: 'El motivo de cancelación no puede superar 250 caracteres.',
  rangoFechas: 'La fecha desde no puede ser posterior a la fecha hasta.',
} as const;

const desfasePeruMs = 5 * 60 * 60 * 1000;
const maximoHoras = 168;

// «Hoy» en hora de Perú (UTC-5, sin horario de verano), igual que el backend. No usa la zona del navegador
// ni toISOString() del instante actual, que da el día UTC: a partir de las 19:00 en Perú ya sería mañana.
export function hoyPeru(ahora: number = Date.now()): string {
  return new Date(ahora - desfasePeruMs).toISOString().slice(0, 10);
}

type Errores = Partial<Record<string, string>>;
type Resultado<T> = { valor: T } | { error: string };

// Lee unas horas escritas por el usuario (punto o coma decimal), con el rango (0, 168] y 2 decimales como máximo.
function leerHoras(entrada: string, errorRango: string, errorDecimales: string): Resultado<number> {
  const partes = /^(\d+)(?:[.,](\d+))?$/.exec(entrada.trim());
  if (partes === null) {
    return { error: errorRango };
  }
  const decimales = (partes[2] ?? '').replace(/0+$/, '');
  const valor = Number(`${partes[1]}${decimales === '' ? '' : `.${decimales}`}`);
  if (valor <= 0 || valor > maximoHoras) {
    return { error: errorRango };
  }
  return decimales.length > 2 ? { error: errorDecimales } : { valor };
}

function textoOpcional(entrada: string, maximo: number, error: string): Resultado<string | undefined> {
  const valor = entrada.trim();
  if (valor.length > maximo) {
    return { error };
  }
  return { valor: valor === '' ? undefined : valor };
}

function textoObligatorio(entrada: string, maximo: number, obligatorio: string, largo: string): Resultado<string> {
  const valor = entrada.trim();
  if (valor === '') {
    return { error: obligatorio };
  }
  return valor.length > maximo ? { error: largo } : { valor };
}

export interface FormularioMantenimiento {
  centro: CentroOpcion | null;
  descripcion: string;
  fechaProgramada: string;
  duracion: string;
  frecuencia: string;
  responsable: string;
}

// Valida el alta o la edición. En la edición el centro no se envía y no se valida.
export function validarMantenimiento(
  formulario: FormularioMantenimiento,
  hoy: string,
  conCentro: boolean,
): { errores: Errores; solicitud?: EditarMantenimientoRequest } {
  const errores: Errores = {};
  if (conCentro && formulario.centro === null) errores.centro = mensajes.centro;
  const descripcion = textoObligatorio(formulario.descripcion, 250, mensajes.descripcionObligatoria, mensajes.descripcionLarga);
  if ('error' in descripcion) errores.descripcion = descripcion.error;
  if (formulario.fechaProgramada === '') {
    errores.fechaProgramada = mensajes.fechaObligatoria;
  } else if (formulario.fechaProgramada < hoy) {
    errores.fechaProgramada = mensajes.fechaPasada;
  }
  const duracion = leerHoras(formulario.duracion, mensajes.duracionRango, mensajes.duracionDecimales);
  if ('error' in duracion) errores.duracion = duracion.error;
  const frecuenciaTexto = formulario.frecuencia.trim();
  const frecuencia = Number(frecuenciaTexto);
  if (frecuenciaTexto !== '' && (!/^\d+$/.test(frecuenciaTexto) || frecuencia < 1 || frecuencia > 3650)) {
    errores.frecuencia = mensajes.frecuencia;
  }
  const responsable = textoOpcional(formulario.responsable, 100, mensajes.responsable);
  if ('error' in responsable) errores.responsable = responsable.error;

  if (Object.keys(errores).length > 0 || !('valor' in descripcion) || !('valor' in duracion) || !('valor' in responsable)) {
    return { errores };
  }
  const solicitud: EditarMantenimientoRequest = {
    descripcion: descripcion.valor,
    fechaProgramada: formulario.fechaProgramada,
    duracionHorasPlanificada: duracion.valor,
  };
  if (frecuenciaTexto !== '') solicitud.frecuenciaDias = frecuencia;
  if (responsable.valor !== undefined) solicitud.responsable = responsable.valor;
  return { errores, solicitud };
}

export function validarCierre(
  horasReales: string,
  observaciones: string,
): { errores: Errores; solicitud?: CompletarMantenimientoRequest } {
  const errores: Errores = {};
  const horas = leerHoras(horasReales, mensajes.horasRango, mensajes.horasDecimales);
  if ('error' in horas) errores.horasReales = horas.error;
  const texto = textoOpcional(observaciones, 500, mensajes.observaciones);
  if ('error' in texto) errores.observaciones = texto.error;
  if (!('valor' in horas) || !('valor' in texto)) {
    return { errores };
  }
  const solicitud: CompletarMantenimientoRequest = { horasReales: horas.valor };
  if (texto.valor !== undefined) solicitud.observaciones = texto.valor;
  return { errores, solicitud };
}

export function validarCancelacion(motivo: string): { error?: string; solicitud?: CancelarMantenimientoRequest } {
  const resultado = textoObligatorio(motivo, 250, mensajes.motivoObligatorio, mensajes.motivoLargo);
  return 'error' in resultado ? { error: resultado.error } : { solicitud: { motivo: resultado.valor } };
}
