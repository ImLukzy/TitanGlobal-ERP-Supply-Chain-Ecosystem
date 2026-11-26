import type { ValoresGuia, LineaValores } from './tipos';
export type Errores = Record<string, string>;
export const camposCabecera = [
  ['remitenteNombre', 'Nombre del remitente', 200, 'nombre del remitente'],
  ['remitenteDocumento', 'Documento del remitente', 20, 'documento del remitente'],
  ['destinatarioNombre', 'Nombre del destinatario', 200, 'nombre del destinatario'],
  ['destinatarioDocumento', 'Documento del destinatario', 20, 'documento del destinatario'],
  ['direccionOrigen', 'Dirección de origen', 300, 'dirección de origen'],
  ['direccionDestino', 'Dirección de destino', 300, 'dirección de destino'],
  ['motivoTraslado', 'Motivo del traslado', 200, 'motivo del traslado'],
  ['transportista', 'Transportista', 200, 'transportista'],
  ['conductor', 'Conductor', 100, 'conductor'],
  ['placa', 'Placa', 20, 'placa'],
] as const;
export function nuevaLinea(): LineaValores {
  return {
    clave: crypto.randomUUID(),
    sku: '',
    descripcion: '',
    unidadMedida: '',
    cantidad: '1',
  };
}
function texto(valor: string, etiqueta: string, maximo: number, prefijo = 'El campo '): string {
  const v = valor.trim();
  return !v
    ? `${prefijo}${etiqueta} es obligatorio.`
    : v.length > maximo
      ? `${prefijo}${etiqueta} no puede superar ${maximo} caracteres.`
      : '';
}
function decimal(
  valor: string,
  min: number,
  max: number,
  escala: number,
  rango: string,
  precision: string,
): string {
  if (
    !/^\d+(?:\.\d+)?$/.test(valor) ||
    !Number.isFinite(Number(valor)) ||
    Number(valor) < min ||
    Number(valor) > max
  )
    return rango;
  const fraccion = (valor.split('.')[1] ?? '').replace(/0+$/, '');
  return fraccion.length > escala ? precision : '';
}
export function validarGuia(datos: ValoresGuia): Errores {
  const e: Errores = {};
  const asignar = (campo: string, mensaje: string) => {
    if (mensaje) e[campo] = mensaje;
  };
  if (!/^[A-Z0-9]{1,8}$/.test(datos.serie.trim().toUpperCase()))
    e.serie = 'La serie debe contener entre 1 y 8 letras mayúsculas o dígitos.';
  if (!/^\d+$/.test(datos.numero) || Number(datos.numero) < 1 || Number(datos.numero) > 99999999)
    e.numero = 'El número debe estar entre 1 y 99999999.';
  if (
    !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?$/.test(datos.fechaInicioTraslado) ||
    !Number.isFinite(Date.parse(`${datos.fechaInicioTraslado}Z`)) ||
    datos.fechaInicioTraslado.startsWith('0000')
  )
    e.fechaInicioTraslado = 'La fecha de inicio del traslado es obligatoria.';
  for (const [campo, , maximo, etiqueta] of camposCabecera)
    asignar(campo, texto(datos[campo], etiqueta, maximo));
  asignar(
    'pesoBrutoKg',
    decimal(
      datos.pesoBrutoKg,
      0.01,
      9999999999.99,
      2,
      'El peso bruto debe estar entre 0.01 y 9999999999.99 kg.',
      'El peso bruto no puede tener más de 2 decimales.',
    ),
  );
  if (datos.observaciones.trim().length > 500)
    e.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  if (datos.lineas.length < 1 || datos.lineas.length > 100)
    e.lineas = 'La guía debe tener entre 1 y 100 líneas.';
  datos.lineas.forEach((l, i) => {
    for (const [campo, etiqueta, maximo] of [
      ['sku', 'SKU', 40],
      ['descripcion', 'descripción', 200],
      ['unidadMedida', 'unidad de medida', 10],
    ] as const)
      asignar(
        `${l.clave}-${campo}`,
        texto(l[campo], etiqueta, maximo, `Línea ${i + 1}: el campo `),
      );
    asignar(
      `${l.clave}-cantidad`,
      decimal(
        l.cantidad,
        0.001,
        999999999.999,
        3,
        `Línea ${i + 1}: la cantidad debe estar entre 0.001 y 999999999.999.`,
        `Línea ${i + 1}: la cantidad no puede tener más de 3 decimales.`,
      ),
    );
  });
  return e;
}
export function validarMotivo(motivo: string): string {
  return !motivo.trim()
    ? 'El motivo de anulación es obligatorio.'
    : motivo.trim().length > 500
      ? 'El motivo de anulación no puede superar 500 caracteres.'
      : '';
}
