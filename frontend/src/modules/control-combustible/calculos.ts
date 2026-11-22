import type { ValoresVale } from './tipos';
function escalado(valor: string): bigint | null {
  if (!/^\d+(?:\.\d+)?$/.test(valor)) return null;
  const [entero, fraccion = ''] = valor.split('.');
  const limpio = entero.replace(/^0+/, '') || '0';
  const decimales = fraccion.replace(/0+$/, '');
  if (limpio.length > 7 || decimales.length > 3) return null;
  const v = BigInt(limpio) * 1000n + BigInt(decimales.padEnd(3, '0'));
  return v < 1n || v > 9999999999n ? null : v;
}
function odometro(valor: string): bigint | null {
  if (!/^\d+$/.test(valor)) return null;
  const limpio = valor.replace(/^0+/, '') || '0';
  return limpio.length > 9 ? null : BigInt(limpio);
}
function redondear(n: bigint, d: bigint): bigint {
  const cociente = n / d;
  return cociente + ((n % d) * 2n >= d ? 1n : 0n);
}
function formato(v: bigint, escala: number): string {
  const texto = v.toString().padStart(escala + 1, '0');
  return texto.slice(0, -escala) + '.' + texto.slice(-escala);
}
export function previsualizar(
  datos: Pick<ValoresVale, 'litros' | 'precioLitro' | 'odometroInicial' | 'odometroFinal'>,
): {
  importe: string;
  kilometrosRecorridos: number | null;
  kmPorLitro: string | null;
  litrosPor100Km: string | null;
} | null {
  const litros = escalado(datos.litros),
    precio = escalado(datos.precioLitro),
    inicial = odometro(datos.odometroInicial);
  if (litros === null || precio === null || inicial === null) return null;
  const importe = formato(redondear(litros * precio, 10000n), 2);
  if (datos.odometroFinal === '')
    return { importe, kilometrosRecorridos: null, kmPorLitro: null, litrosPor100Km: null };
  const final = odometro(datos.odometroFinal);
  if (final === null || final <= inicial) return null;
  const km = final - inicial;
  return {
    importe,
    kilometrosRecorridos: Number(km),
    kmPorLitro: formato(redondear(km * 1000000n, litros), 3),
    litrosPor100Km: formato(redondear(litros * 100n, km), 3),
  };
}
