import type { ValoresSucursal } from './tipos';
export const camposTexto = [
  ['codigo', 'Código', 20, 'código'],
  ['nombre', 'Nombre', 150, 'nombre'],
  ['pais', 'País', 100, 'país'],
  ['ciudad', 'Ciudad', 100, 'ciudad'],
  ['direccion', 'Dirección', 300, 'dirección'],
] as const;
export const camposOpcionales = [
  ['contacto', 'Contacto', 100, 'El contacto no puede superar 100 caracteres.'],
  ['email', 'Email', 150, 'El email no puede superar 150 caracteres.'],
  ['telefono', 'Teléfono', 30, 'El teléfono no puede superar 30 caracteres.'],
  ['observaciones', 'Observaciones', 500, 'Las observaciones no pueden superar 500 caracteres.'],
] as const;
export type Errores = Partial<Record<keyof ValoresSucursal, string>>;
export function validarSucursal(d: ValoresSucursal): Errores {
  const e: Errores = {};
  for (const [c, , m, l] of camposTexto) {
    const t = d[c].trim();
    if (!t) e[c] = `El campo ${l} es obligatorio.`;
    else if (t.length > m) e[c] = `El campo ${l} no puede superar ${m} caracteres.`;
  }
  if (!['Filial', 'Planta', 'Oficina'].includes(d.tipo))
    e.tipo = 'El tipo debe ser Filial, Planta u Oficina.';
  for (const [c, , m, msg] of camposOpcionales) {
    if (d[c].trim().length > m) e[c] = msg;
  }
  if (!e.email && d.email.trim() && !/^[^\s@<>]+@[^\s@<>]+$/.test(d.email.trim()))
    e.email = 'El email no es válido.';
  if (
    d.fechaApertura &&
    (Date.parse(d.fechaApertura + 'Z') === Date.parse('0001-01-01T00:00:00.000Z') ||
      Number.isNaN(Date.parse(d.fechaApertura + 'Z')))
  )
    e.fechaApertura = 'La fecha de apertura no es válida.';
  return e;
}
