import type { ValoresEntrega, RegistrarPod } from './tipos';
export type Errores = Record<string, string>;
export const camposEntrega = [
  ['codigo', 'Código', 20, 'código'],
  ['referenciaGuia', 'Referencia de guía', 40, 'referencia de guía'],
  ['destinatario', 'Destinatario', 200, 'destinatario'],
  ['direccionDestino', 'Dirección de destino', 300, 'dirección de destino'],
] as const;
export const camposPod = [
  ['receptor', 'Receptor', 100, 'receptor'],
  ['documentoReceptor', 'Documento del receptor', 20, 'documento del receptor'],
  ['referenciaConstancia', 'Referencia de constancia', 100, 'referencia de constancia'],
] as const;
function texto(entrada: string, etiqueta: string, maximo: number): string {
  const v = entrada.trim();
  return !v
    ? `El campo ${etiqueta} es obligatorio.`
    : v.length > maximo
      ? `El campo ${etiqueta} no puede superar ${maximo} caracteres.`
      : '';
}
export function validarEntrega(datos: ValoresEntrega): Errores {
  const e: Errores = {};
  for (const [campo, , maximo, etiqueta] of camposEntrega) {
    const m = texto(datos[campo], etiqueta, maximo);
    if (m) e[campo] = m;
  }
  if (
    !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?$/.test(datos.fechaProgramada) ||
    !Number.isFinite(Date.parse(`${datos.fechaProgramada}Z`)) ||
    datos.fechaProgramada.startsWith('0000')
  )
    e.fechaProgramada = 'La fecha programada es obligatoria.';
  if (datos.observaciones.trim().length > 500)
    e.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  return e;
}
export function validarPod(datos: RegistrarPod): Errores {
  const e: Errores = {};
  for (const [campo, , maximo, etiqueta] of camposPod) {
    const m = texto(datos[campo], etiqueta, maximo);
    if (m) e[campo] = m;
  }
  if ((datos.observaciones ?? '').trim().length > 500)
    e.observaciones = 'Las observaciones de entrega no pueden superar 500 caracteres.';
  return e;
}
export function validarMotivo(motivo: string): string {
  return !motivo.trim()
    ? 'El motivo de cancelación es obligatorio.'
    : motivo.trim().length > 500
      ? 'El motivo de cancelación no puede superar 500 caracteres.'
      : '';
}
