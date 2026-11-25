const maximoVersion = 2147483647;

// Un number de JavaScript conserva sin redondeo hasta 15 dígitos significativos.
const maximoDigitosSignificativos = 15;

export function versionValida(version: string): boolean {
  return /^\d+$/.test(version) && Number(version) >= 1 && Number(version) <= maximoVersion;
}

export function validarCantidad(entrada: string): { cantidad: number } | { error: string } {
  const formato = /^\+?(?:(\d+)(?:[.,](\d*))?|[.,](\d+))$/;
  const valor = formato.exec(entrada.trim());
  const errorRango = 'La cantidad debe ser mayor que 0 y no superar 999999999999.999999.';
  if (valor === null) {
    return { error: errorRango };
  }
  const entero = (valor[1] ?? '0').replace(/^0+(?=\d)/, '');
  const decimales = (valor[2] ?? valor[3] ?? '').replace(/0+$/, '');
  if (entero.length > 12 || (entero === '0' && decimales === '')) {
    return { error: errorRango };
  }
  if (decimales.length > 6) {
    return { error: 'La cantidad admite como máximo 6 decimales.' };
  }
  const significativos = entero === '0' ? decimales.replace(/^0+/, '') : `${entero}${decimales}`;
  if (significativos.length > maximoDigitosSignificativos) {
    return { error: 'La cantidad admite como máximo 15 dígitos significativos.' };
  }
  return { cantidad: Number(decimales === '' ? entero : `${entero}.${decimales}`) };
}
