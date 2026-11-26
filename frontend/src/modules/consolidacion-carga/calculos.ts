import type { LineaValores } from './tipos';
import { validarLinea } from './validacion';
export function nuevaLinea(): LineaValores {
  return {
    clave: crypto.randomUUID(),
    sku: '',
    descripcion: '',
    cantidad: '1',
    cantidadRecogida: '0',
    unidadesPorBulto: '0',
    largoCm: '0',
    anchoCm: '0',
    altoCm: '0',
    pesoBultoKg: '0',
  };
}
function centesimas(texto: string): bigint {
  const [entero, frac = ''] = texto.trim().split('.');
  return BigInt(entero) * 100n + BigInt(frac.slice(0, 2).padEnd(2, '0'));
}
export function decimalTexto(valor: bigint, escala: number): string {
  const divisor = 10n ** BigInt(escala);
  return `${valor / divisor}.${(valor % divisor).toString().padStart(escala, '0')}`;
}
export function previsualizar(
  lineas: LineaValores[],
): { bultos: string; volumenM3: string; pesoKg: string } | null {
  let bultos = 0n;
  let volumen = 0n;
  let peso = 0n;
  for (const [i, linea] of lineas.entries()) {
    if (Object.keys(validarLinea(linea, i)).length) return null;
    const cantidad = BigInt(linea.cantidad.trim());
    const unidades = BigInt(linea.unidadesPorBulto.trim());
    const numero = unidades === 0n ? 0n : (cantidad + unidades - 1n) / unidades;
    bultos += numero;
    volumen +=
      (numero * centesimas(linea.largoCm) * centesimas(linea.anchoCm) * centesimas(linea.altoCm) +
        500000n) /
      1000000n;
    peso += numero * centesimas(linea.pesoBultoKg);
  }
  return {
    bultos: bultos.toString(),
    volumenM3: decimalTexto(volumen, 6),
    pesoKg: decimalTexto(peso, 2),
  };
}
