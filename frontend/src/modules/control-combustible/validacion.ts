import type { ValoresVale } from './tipos';
export type Errores = Record<string, string>;
export const camposTexto = [
  ['codigo', 'Código', 20, 'código'],
  ['placa', 'Placa', 20, 'placa'],
  ['estacion', 'Estación', 150, 'estación'],
] as const;
function decimal(valor: string, rango: string, escala: string): string {
  if (
    !/^\d+(?:\.\d+)?$/.test(valor) ||
    !Number.isFinite(Number(valor)) ||
    Number(valor) < 0.001 ||
    Number(valor) > 9999999.999
  )
    return rango;
  return (valor.split('.')[1] ?? '').replace(/0+$/, '').length > 3 ? escala : '';
}
function entero(valor: string): boolean {
  return /^\d+$/.test(valor) && Number.isSafeInteger(Number(valor)) && Number(valor) <= 999999999;
}
export function validarVale(datos: ValoresVale): Errores {
  const e: Errores = {};
  for (const [c, , m, t] of camposTexto) {
    const v = datos[c].trim();
    if (!v) e[c] = `El campo ${t} es obligatorio.`;
    else if (v.length > m) e[c] = `El campo ${t} no puede superar ${m} caracteres.`;
  }
  if (
    !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?$/.test(datos.fechaCarga) ||
    !Number.isFinite(Date.parse(datos.fechaCarga + 'Z')) ||
    datos.fechaCarga.startsWith('0000')
  )
    e.fechaCarga = 'La fecha de carga es obligatoria.';
  if (!['diesel', 'gasolina'].includes(datos.tipoCombustible.trim().toLowerCase()))
    e.tipoCombustible = 'El combustible debe ser Diesel o Gasolina.';
  const litros = decimal(
    datos.litros,
    'Los litros deben estar entre 0.001 y 9999999.999.',
    'Los litros no pueden tener más de 3 decimales.',
  );
  if (litros) e.litros = litros;
  const precio = decimal(
    datos.precioLitro,
    'El precio por litro debe estar entre 0.001 y 9999999.999.',
    'El precio por litro no puede tener más de 3 decimales.',
  );
  if (precio) e.precioLitro = precio;
  if (!/^[A-Z]{3}$/.test(datos.moneda.trim().toUpperCase()))
    e.moneda = 'La moneda debe contener 3 letras mayúsculas.';
  if (!entero(datos.odometroInicial))
    e.odometroInicial = 'El odómetro inicial debe estar entre 0 y 999999999.';
  if (datos.odometroFinal !== '') {
    if (!entero(datos.odometroFinal))
      e.odometroFinal = 'El odómetro final debe estar entre 0 y 999999999.';
    else if (!e.odometroInicial && Number(datos.odometroFinal) <= Number(datos.odometroInicial))
      e.odometroFinal = 'El odómetro final debe ser mayor que el inicial.';
  }
  if (datos.observaciones.trim().length > 500)
    e.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  return e;
}
export function validarMotivo(m: string): string {
  return !m.trim()
    ? 'El motivo de anulación es obligatorio.'
    : m.trim().length > 500
      ? 'El motivo de anulación no puede superar 500 caracteres.'
      : '';
}
