import type { ValoresSiniestro, ValoresReclamo, ValoresCierre } from './tipos';
export const camposTexto = [
  ['codigo', 'Código', 20, 'código'],
  ['placa', 'Placa', 20, 'placa'],
  ['lugar', 'Lugar', 200, 'lugar'],
  ['descripcion', 'Descripción', 500, 'descripción'],
] as const;
export const camposReclamo = [
  ['numeroPoliza', 'Número de póliza', 40, 'número de póliza'],
  ['aseguradora', 'Aseguradora', 150, 'aseguradora'],
  ['numeroReclamo', 'Número de reclamo', 40, 'número de reclamo'],
] as const;
export type Errores = Partial<Record<keyof ValoresSiniestro, string>>;
export function errorTexto(valor: string, etiqueta: string, maximo: number) {
  const t = valor.trim();
  return !t
    ? `El campo ${etiqueta} es obligatorio.`
    : t.length > maximo
      ? `El campo ${etiqueta} no puede superar ${maximo} caracteres.`
      : '';
}
export function errorMonto(valor: string, etiqueta: string, minimo: number) {
  const n = Number(valor);
  if (!/^-?\d+(?:\.\d+)?$/.test(valor) || !Number.isFinite(n) || n < minimo || n > 9999999999.99)
    return `El ${etiqueta} debe estar entre ${minimo === 0 ? '0' : '0.01'} y 9999999999.99.`;
  if ((valor.split('.')[1] || '').replace(/0+$/, '').length > 2)
    return `El ${etiqueta} no puede tener más de 2 decimales.`;
  return '';
}
export function validarSiniestro(d: ValoresSiniestro): Errores {
  const e: Errores = {};
  for (const [c, , m, l] of camposTexto) {
    const f = errorTexto(d[c], l, m);
    if (f) e[c] = f;
  }
  if (!['Accidente', 'Robo', 'DanoMaterial'].includes(d.tipo))
    e.tipo = 'El tipo debe ser Accidente, Robo o DanoMaterial.';
  if (!d.fechaIncidente || Number.isNaN(Date.parse(d.fechaIncidente + 'Z')))
    e.fechaIncidente = 'La fecha del incidente es obligatoria.';
  if (d.observaciones.trim().length > 500)
    e.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  return e;
}
export function validarReclamo(d: ValoresReclamo): Partial<Record<keyof ValoresReclamo, string>> {
  const e: Partial<Record<keyof ValoresReclamo, string>> = {};
  for (const [c, , m, l] of camposReclamo) {
    const f = errorTexto(d[c], l, m);
    if (f) e[c] = f;
  }
  if (!/^[A-Z]{3}$/.test(d.moneda.trim().toUpperCase()))
    e.moneda = 'La moneda debe contener 3 letras mayúsculas.';
  const f = errorMonto(d.montoReclamado, 'monto reclamado', 0.01);
  if (f) e.montoReclamado = f;
  return e;
}
export function validarCierre(
  d: ValoresCierre,
  maximo: string,
): Partial<Record<keyof ValoresCierre, string>> {
  const e: Partial<Record<keyof ValoresCierre, string>> = {};
  if (!['Aceptado', 'Rechazado'].includes(d.resultado))
    e.resultado = 'El resultado debe ser Aceptado o Rechazado.';
  const f = errorMonto(d.montoReconocido, 'monto reconocido', 0);
  if (f) e.montoReconocido = f;
  else if (Number(d.montoReconocido) > Number(maximo))
    e.montoReconocido = 'El monto reconocido no puede superar el reclamado.';
  else if (d.resultado === 'Aceptado' && Number(d.montoReconocido) === 0)
    e.montoReconocido = 'Un reclamo aceptado debe reconocer un importe mayor que cero.';
  else if (d.resultado === 'Rechazado' && Number(d.montoReconocido) !== 0)
    e.montoReconocido = 'Un reclamo rechazado debe reconocer un importe de cero.';
  const r = errorTexto(d.resolucion, 'resolución', 500);
  if (r) e.resolucion = r;
  return e;
}
export function validarMotivo(m: string) {
  return !m.trim()
    ? 'El motivo de anulación es obligatorio.'
    : m.trim().length > 500
      ? 'El motivo de anulación no puede superar 500 caracteres.'
      : '';
}
