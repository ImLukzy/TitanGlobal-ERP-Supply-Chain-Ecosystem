import type { GuardarTransportista } from './tipos';

export type Errores = Record<string, string>;
export interface ValoresFlete {
  transportistaId: number;
  referencia: string;
  fecha: string;
  origen: string;
  destino: string;
  importe: string;
  moneda: string;
  observaciones: string;
}
export function validarTransportista(datos: GuardarTransportista): Errores {
  const errores: Errores = {};
  const requeridos: [keyof GuardarTransportista, number, string, string][] = [
    [
      'razonSocial',
      200,
      'La razón social es obligatoria.',
      'La razón social no puede superar 200 caracteres.',
    ],
    [
      'documentoFiscal',
      30,
      'El documento fiscal es obligatorio.',
      'El documento fiscal no puede superar 30 caracteres.',
    ],
    ['pais', 100, 'El país es obligatorio.', 'El país no puede superar 100 caracteres.'],
  ];
  for (const [campo, maximo, obligatorio, longitud] of requeridos) {
    const valor = String(datos[campo] ?? '').trim();
    if (!valor) errores[campo] = obligatorio;
    else if (valor.length > maximo) errores[campo] = longitud;
  }
  if (!['3PL', 'Flete', 'Ambos'].includes(datos.tipoServicio))
    errores.tipoServicio = 'El tipo de servicio debe ser 3PL, Flete o Ambos.';
  for (const [campo, maximo, mensaje] of [
    ['contacto', 100, 'El contacto no puede superar 100 caracteres.'],
    ['email', 150, 'El email no puede superar 150 caracteres.'],
    ['telefono', 20, 'El teléfono no puede superar 20 caracteres.'],
    ['observaciones', 500, 'Las observaciones no pueden superar 500 caracteres.'],
  ] as const) {
    if ((datos[campo]?.trim().length ?? 0) > maximo) errores[campo] = mensaje;
  }
  if (!errores.email && datos.email?.trim() && !/^[^\s@]+@[^\s@]+$/.test(datos.email.trim()))
    errores.email = 'El email no es válido.';
  return errores;
}
export function errorImporte(texto: string): string {
  const valor = texto.trim();
  if (!/^\d+(\.\d+)?$/.test(valor) || Number(valor) <= 0)
    return 'El importe debe ser mayor que cero.';
  const [entero, decimales = ''] = valor.split('.');
  if (BigInt(entero) > 9999999999999999n) return 'El importe no puede superar 9999999999999999.99.';
  if (decimales.length > 2) return 'El importe no puede tener más de 2 decimales.';
  const centavos = BigInt(entero) * 100n + BigInt(decimales.padEnd(2, '0'));
  if (
    centavos > BigInt(Number.MAX_SAFE_INTEGER) ||
    BigInt(Math.round(Number(valor) * 100)) !== centavos ||
    Number(valor).toFixed(2) !== `${BigInt(entero)}.${decimales.padEnd(2, '0')}`
  ) {
    return 'El importe no puede enviarse con precisión desde este navegador. Ingresa una cantidad menor.';
  }
  return '';
}
export function validarFlete(datos: ValoresFlete): Errores {
  const errores: Errores = {};
  if (datos.transportistaId <= 0) errores.transportistaId = 'El transportista es obligatorio.';
  for (const [campo, maximo, obligatorio, longitud] of [
    [
      'referencia',
      30,
      'La referencia del flete es obligatoria.',
      'La referencia del flete no puede superar 30 caracteres.',
    ],
    ['origen', 120, 'El origen es obligatorio.', 'El origen no puede superar 120 caracteres.'],
    ['destino', 120, 'El destino es obligatorio.', 'El destino no puede superar 120 caracteres.'],
  ] as const) {
    if (!datos[campo].trim()) errores[campo] = obligatorio;
    else if (datos[campo].trim().length > maximo) errores[campo] = longitud;
  }
  const fecha = new Date(`${datos.fecha}T00:00:00Z`);
  if (
    !/^\d{4}-\d{2}-\d{2}$/.test(datos.fecha) ||
    !Number.isFinite(fecha.getTime()) ||
    fecha.toISOString().slice(0, 10) !== datos.fecha ||
    datos.fecha.startsWith('0000')
  )
    errores.fecha = 'La fecha del flete no es válida.';
  const importe = errorImporte(datos.importe);
  if (importe) errores.importe = importe;
  if (!/^[A-Z]{3}$/.test(datos.moneda.trim().toUpperCase()))
    errores.moneda = 'La moneda debe tener tres letras.';
  if (datos.observaciones.trim().length > 500)
    errores.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  return errores;
}
export function validarMotivo(motivo: string): string {
  if (!motivo.trim()) return 'El motivo de cancelación es obligatorio.';
  return motivo.trim().length > 500
    ? 'El motivo de cancelación no puede superar 500 caracteres.'
    : '';
}
