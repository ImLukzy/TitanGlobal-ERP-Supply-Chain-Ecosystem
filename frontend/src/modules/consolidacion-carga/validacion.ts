import type { LineaValores, ValoresCarga } from './tipos';
export type Errores = Record<string, string>;
export const camposNumericos = [
  'cantidad',
  'cantidadRecogida',
  'unidadesPorBulto',
  'largoCm',
  'anchoCm',
  'altoCm',
  'pesoBultoKg',
] as const;
export function validarLinea(linea: LineaValores, indice: number): Errores {
  const e: Errores = {};
  const prefijo = `Línea ${indice + 1}: `;
  for (const [campo, maximo, requerido, largo] of [
    ['sku', 40, 'el SKU es obligatorio.', 'el SKU no puede superar 40 caracteres.'],
    [
      'descripcion',
      200,
      'la descripción es obligatoria.',
      'la descripción no puede superar 200 caracteres.',
    ],
  ] as const) {
    if (!linea[campo].trim()) e[campo] = prefijo + requerido;
    else if (linea[campo].trim().length > maximo) e[campo] = prefijo + largo;
  }
  if (
    !/^\d+$/.test(linea.cantidad.trim()) ||
    Number(linea.cantidad) < 1 ||
    Number(linea.cantidad) > 1000000
  )
    e.cantidad = prefijo + 'la cantidad debe estar entre 1 y 1000000.';
  if (
    !/^\d+$/.test(linea.cantidadRecogida.trim()) ||
    Number(linea.cantidadRecogida) < 0 ||
    Number(linea.cantidadRecogida) > 1000000
  )
    e.cantidadRecogida = prefijo + 'la cantidad recogida debe estar entre 0 y 1000000.';
  else if (Number(linea.cantidadRecogida) > Number(linea.cantidad))
    e.cantidadRecogida = prefijo + 'la cantidad recogida no puede superar la cantidad prevista.';
  if (
    !/^\d+$/.test(linea.unidadesPorBulto.trim()) ||
    Number(linea.unidadesPorBulto) < 0 ||
    Number(linea.unidadesPorBulto) > 1000000
  )
    e.unidadesPorBulto = prefijo + 'las unidades por bulto debe estar entre 0 y 1000000.';
  if (
    !/^\d+(\.\d+)?$/.test(linea.largoCm.trim()) ||
    Number(linea.largoCm) < 0 ||
    Number(linea.largoCm) > 9999.99
  )
    e.largoCm = prefijo + 'el largo debe estar entre 0 y 9999.99.';
  else if ((linea.largoCm.trim().split('.')[1] ?? '').replace(/0+$/, '').length > 2)
    e.largoCm = prefijo + 'el largo no puede tener más de 2 decimales.';
  if (
    !/^\d+(\.\d+)?$/.test(linea.anchoCm.trim()) ||
    Number(linea.anchoCm) < 0 ||
    Number(linea.anchoCm) > 9999.99
  )
    e.anchoCm = prefijo + 'el ancho debe estar entre 0 y 9999.99.';
  else if ((linea.anchoCm.trim().split('.')[1] ?? '').replace(/0+$/, '').length > 2)
    e.anchoCm = prefijo + 'el ancho no puede tener más de 2 decimales.';
  if (
    !/^\d+(\.\d+)?$/.test(linea.altoCm.trim()) ||
    Number(linea.altoCm) < 0 ||
    Number(linea.altoCm) > 9999.99
  )
    e.altoCm = prefijo + 'el alto debe estar entre 0 y 9999.99.';
  else if ((linea.altoCm.trim().split('.')[1] ?? '').replace(/0+$/, '').length > 2)
    e.altoCm = prefijo + 'el alto no puede tener más de 2 decimales.';
  if (
    !/^\d+(\.\d+)?$/.test(linea.pesoBultoKg.trim()) ||
    Number(linea.pesoBultoKg) < 0 ||
    Number(linea.pesoBultoKg) > 999999.99
  )
    e.pesoBultoKg = prefijo + 'el peso por bulto debe estar entre 0 y 999999.99.';
  else if ((linea.pesoBultoKg.trim().split('.')[1] ?? '').replace(/0+$/, '').length > 2)
    e.pesoBultoKg = prefijo + 'el peso por bulto no puede tener más de 2 decimales.';
  return e;
}
export function validarCarga(datos: ValoresCarga): Errores {
  const e: Errores = {};
  for (const [campo, maximo, requerido, largo] of [
    [
      'codigo',
      20,
      'El código de la consolidación es obligatorio.',
      'El código de la consolidación no puede superar 20 caracteres.',
    ],
    [
      'nombre',
      100,
      'El nombre de la consolidación es obligatorio.',
      'El nombre de la consolidación no puede superar 100 caracteres.',
    ],
  ] as const) {
    if (!datos[campo].trim()) e[campo] = requerido;
    else if (datos[campo].trim().length > maximo) e[campo] = largo;
  }
  if (datos.observaciones.trim().length > 500)
    e.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  if (datos.lineas.length < 1 || datos.lineas.length > 100)
    e.lineas = 'La consolidación debe tener entre 1 y 100 líneas.';
  datos.lineas.forEach((linea, i) => {
    for (const [campo, mensaje] of Object.entries(validarLinea(linea, i)))
      e[`${linea.clave}-${campo}`] = mensaje;
  });
  return e;
}
export function validarMotivo(motivo: string): string {
  if (!motivo.trim()) return 'El motivo de cancelación es obligatorio.';
  return motivo.trim().length > 500
    ? 'El motivo de cancelación no puede superar 500 caracteres.'
    : '';
}
