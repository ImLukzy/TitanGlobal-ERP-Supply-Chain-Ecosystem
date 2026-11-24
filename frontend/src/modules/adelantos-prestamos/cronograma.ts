// Vista previa del cronograma de cuotas. Reproduce CronogramaPrestamo.Generar del backend:
// cuota = Math.Round(monto / N, 2, AwayFromZero) y la última cuota absorbe la diferencia.
// Todo se calcula en céntimos enteros para no depender de la coma flotante.

export const MAXIMO_CUOTAS = 24;
const MAXIMO_ENTERO_DIGITOS = 10;
const formatoMonto = /^\d+(\.\d+)?$/;

export interface CuotaCronograma {
  numeroCuota: number;
  anio: number;
  mes: number;
  montoCentavos: number;
}

export interface CuotaEvaluada extends CuotaCronograma {
  comprometidoCentavos: number;
  totalCentavos: number;
  superaMaximo: boolean;
}

export type ResultadoMonto =
  | { estado: 'valido'; centavos: number }
  | { estado: 'vacio' }
  | { estado: 'noPositivo' }
  | { estado: 'formato' };

// Interpreta el monto escrito como texto (la coma se admite como separador decimal).
export function interpretarMonto(texto: string): ResultadoMonto {
  const limpio = texto.trim().replace(',', '.');
  if (limpio === '') {
    return { estado: 'vacio' };
  }
  const negativo = limpio.startsWith('-');
  const cuerpo = negativo ? limpio.slice(1) : limpio;
  if (!formatoMonto.test(cuerpo)) {
    return { estado: 'formato' };
  }
  const partes = cuerpo.split('.');
  const entero = (partes[0] ?? '').replace(/^0+(?=\d)/, '');
  const decimales = partes[1] ?? '';
  if (negativo || /^0*$/.test(`${entero}${decimales}`)) {
    return { estado: 'noPositivo' };
  }
  if (decimales.length > 2 || entero.length > MAXIMO_ENTERO_DIGITOS) {
    return { estado: 'formato' };
  }
  return { estado: 'valido', centavos: Number(entero) * 100 + Number(decimales.padEnd(2, '0')) };
}

// Cuota redondeada a céntimos con el medio céntimo alejándose de cero: floor((2c + n) / 2n) en enteros.
export function cuotaRedondeada(montoCentavos: number, numeroCuotas: number): number {
  return Math.floor((2 * montoCentavos + numeroCuotas) / (2 * numeroCuotas));
}

export function generarCronograma(montoCentavos: number, numeroCuotas: number, primerAnio: number, primerMes: number): CuotaCronograma[] {
  const cuota = cuotaRedondeada(montoCentavos, numeroCuotas);
  const cuotas: CuotaCronograma[] = [];
  let acumulado = 0;
  for (let indice = 0; indice < numeroCuotas; indice += 1) {
    const posicion = primerAnio * 12 + (primerMes - 1) + indice;
    const montoCuota = indice === numeroCuotas - 1 ? montoCentavos - acumulado : cuota;
    acumulado += montoCuota;
    cuotas.push({
      numeroCuota: indice + 1,
      anio: Math.floor(posicion / 12),
      mes: (posicion % 12) + 1,
      montoCentavos: montoCuota,
    });
  }
  return cuotas;
}

export function aCentavos(importe: number): number {
  return Math.round(importe * 100);
}

export function centavosAImporte(centavos: number): number {
  return centavos / 100;
}

// Suma de lo ya comprometido ese mes más la cuota nueva, contra el máximo (la igualdad se admite, como el backend).
export function evaluarTope(
  cuotas: CuotaCronograma[],
  comprometidas: { anio: number; mes: number; monto: number }[],
  cuotaMaximaCentavos: number,
): CuotaEvaluada[] {
  return cuotas.map((cuota) => {
    const comprometidoCentavos = comprometidas
      .filter((item) => item.anio === cuota.anio && item.mes === cuota.mes)
      .reduce((suma, item) => suma + aCentavos(item.monto), 0);
    const totalCentavos = comprometidoCentavos + cuota.montoCentavos;
    return { ...cuota, comprometidoCentavos, totalCentavos, superaMaximo: totalCentavos > cuotaMaximaCentavos };
  });
}

export function textoMes(anio: number, mes: number): string {
  return `${String(mes).padStart(2, '0')}/${anio}`;
}
