export interface ValoresRuta {
  codigo: string;
  nombre: string;
  zona: string;
  origen: string;
  destino: string;
  distanciaKm: string;
  peajeEstimado: string;
  moneda: string;
  activo: boolean;
  observaciones: string;
}
export type ErroresRuta = Partial<Record<keyof ValoresRuta, string>>;

function errorDecimal(texto: string, distancia: boolean): string {
  const valor = texto.trim();
  const positivo = distancia
    ? 'La distancia debe ser mayor que cero.'
    : 'El peaje estimado no puede ser negativo.';
  if (!/^\d+(\.\d+)?$/.test(valor)) return positivo;
  const [entero, decimales = ''] = valor.split('.');
  const enteroExacto = BigInt(entero);
  if (distancia && enteroExacto === 0n && !/[1-9]/.test(decimales)) return positivo;
  const maximo = distancia ? 99999999n : 9999999999999999n;
  if (
    enteroExacto > maximo ||
    (enteroExacto === maximo && decimales.slice(0, 2) === '99' && /[1-9]/.test(decimales.slice(2)))
  )
    return distancia
      ? 'La distancia no puede superar 99999999.99 km.'
      : 'El peaje estimado no puede superar 9999999999999999.99.';
  // Ceros finales no añaden precisión significativa, igual que decimal.Round en la API.
  const significativos = decimales.replace(/0+$/, '');
  if (significativos.length > 2)
    return distancia
      ? 'La distancia no puede tener más de 2 decimales.'
      : 'El peaje estimado no puede tener más de 2 decimales.';
  const centavos = enteroExacto * 100n + BigInt(decimales.slice(0, 2).padEnd(2, '0'));
  if (
    centavos > BigInt(Number.MAX_SAFE_INTEGER) ||
    BigInt(Math.round(Number(valor) * 100)) !== centavos ||
    Number(valor).toFixed(2) !== `${enteroExacto}.${decimales.slice(0, 2).padEnd(2, '0')}`
  )
    return 'El peaje estimado no puede enviarse con precisión desde este navegador. Ingresa una cantidad menor.';
  return '';
}
export function validarRuta(datos: ValoresRuta): ErroresRuta {
  const errores: ErroresRuta = {};
  for (const [campo, maximo, obligatorio, longitud] of [
    [
      'codigo',
      20,
      'El código de la ruta es obligatorio.',
      'El código de la ruta no puede superar 20 caracteres.',
    ],
    [
      'nombre',
      100,
      'El nombre de la ruta es obligatorio.',
      'El nombre de la ruta no puede superar 100 caracteres.',
    ],
    ['zona', 100, 'La zona es obligatoria.', 'La zona no puede superar 100 caracteres.'],
    ['origen', 120, 'El origen es obligatorio.', 'El origen no puede superar 120 caracteres.'],
    ['destino', 120, 'El destino es obligatorio.', 'El destino no puede superar 120 caracteres.'],
  ] as const) {
    if (!datos[campo].trim()) errores[campo] = obligatorio;
    else if (datos[campo].trim().length > maximo) errores[campo] = longitud;
  }
  const distancia = errorDecimal(datos.distanciaKm, true);
  if (distancia) errores.distanciaKm = distancia;
  const peaje = errorDecimal(datos.peajeEstimado, false);
  if (peaje) errores.peajeEstimado = peaje;
  if (!/^[A-Z]{3}$/.test(datos.moneda.trim().toUpperCase()))
    errores.moneda = 'La moneda debe tener tres letras.';
  if (datos.observaciones.trim().length > 500)
    errores.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  return errores;
}
