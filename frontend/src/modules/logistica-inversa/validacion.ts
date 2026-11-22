import type { ValoresRetorno } from './tipos';
export function validarMotivo(motivo: string): string {
  if (!motivo.trim()) return 'El motivo de cancelación es obligatorio.';
  if (motivo.trim().length > 500)
    return 'El motivo de cancelación no puede superar 500 caracteres.';
  return '';
}
export const camposTexto = [
  ['codigo', 'Código', 20, 'código'],
  ['referenciaOrigen', 'Referencia de origen', 40, 'referencia de origen'],
  ['contraparte', 'Contraparte', 200, 'contraparte'],
  ['puntoRecogida', 'Punto de recogida', 200, 'punto de recogida'],
  ['puntoRecepcion', 'Punto de recepción', 200, 'punto de recepción'],
  ['descripcion', 'Descripción', 200, 'descripción'],
  ['unidad', 'Unidad', 20, 'unidad'],
] as const;
export type Errores = Partial<Record<keyof ValoresRetorno, string>>;
export function errorCantidad(valor: string, etiqueta = 'cantidad'): string {
  const n = Number(valor);
  if (!/^-?\d+(?:\.\d+)?$/.test(valor) || !Number.isFinite(n) || n < 0.001 || n > 999999999.999)
    return `La ${etiqueta} debe estar entre 0.001 y 999999999.999.`;
  if ((valor.split('.')[1] || '').replace(/0+$/, '').length > 3)
    return `La ${etiqueta} no puede tener más de 3 decimales.`;
  return '';
}
export function validarRetorno(d: ValoresRetorno): Errores {
  const e: Errores = {};
  for (const [campo, , max, etiqueta] of camposTexto) {
    const t = d[campo].trim();
    if (!t) e[campo] = `El campo ${etiqueta} es obligatorio.`;
    else if (t.length > max) e[campo] = `El campo ${etiqueta} no puede superar ${max} caracteres.`;
  }
  if (!['Pallet', 'Retornable', 'DevolucionRuta'].includes(d.tipo))
    e.tipo = 'El tipo debe ser Pallet, Retornable o DevolucionRuta.';
  if (!d.fechaProgramada || Number.isNaN(Date.parse(d.fechaProgramada + 'Z')))
    e.fechaProgramada = 'La fecha programada es obligatoria.';
  const cantidad = errorCantidad(d.cantidadProgramada, 'cantidad programada');
  if (cantidad) e.cantidadProgramada = cantidad;
  if (d.observaciones.trim().length > 500)
    e.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  return e;
}
