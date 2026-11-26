import { describe, expect, it } from 'vitest';
import { cuotaRedondeada, evaluarTope, generarCronograma, interpretarMonto, textoMes } from './cronograma';

function montos(cuotas: { montoCentavos: number }[]): number[] {
  return cuotas.map((cuota) => cuota.montoCentavos);
}

describe('cronograma de cuotas (misma regla que el backend)', () => {
  it('1000 en 3 cuotas: 333.33, 333.33 y 333.34, con cambio de año', () => {
    const cuotas = generarCronograma(100000, 3, 2030, 11);
    expect(montos(cuotas)).toEqual([33333, 33333, 33334]);
    expect(cuotas.map((cuota) => [cuota.anio, cuota.mes])).toEqual([[2030, 11], [2030, 12], [2031, 1]]);
    expect(cuotas.map((cuota) => cuota.numeroCuota)).toEqual([1, 2, 3]);
  });

  it('100 en 6 cuotas: cinco de 16.67 y la última de 16.65', () => {
    expect(montos(generarCronograma(10000, 6, 2030, 1))).toEqual([1667, 1667, 1667, 1667, 1667, 1665]);
  });

  it('0.10 en 4 cuotas: el medio céntimo se aleja de cero (0.03) y la última queda en 0.01', () => {
    expect(montos(generarCronograma(10, 4, 2030, 1))).toEqual([3, 3, 3, 1]);
  });

  it('una cuota única es el monto completo', () => {
    expect(montos(generarCronograma(25000, 1, 2030, 5))).toEqual([25000]);
  });

  it('la suma siempre coincide con el monto y 24 cuotas cruzan dos años', () => {
    for (const monto of [1, 7, 99999, 123457, 999999999999]) {
      for (const n of [1, 2, 3, 5, 7, 12, 24]) {
        const cuotas = generarCronograma(monto, n, 2029, 12);
        expect(cuotas.reduce((suma, cuota) => suma + cuota.montoCentavos, 0)).toBe(monto);
      }
    }
    const veinticuatro = generarCronograma(100000, 24, 2029, 12);
    expect(veinticuatro[0]).toMatchObject({ anio: 2029, mes: 12 });
    expect(veinticuatro[1]).toMatchObject({ anio: 2030, mes: 1 });
    expect(veinticuatro[23]).toMatchObject({ anio: 2031, mes: 11 });
  });

  it('redondea la cuota con el medio céntimo hacia arriba', () => {
    expect(cuotaRedondeada(10, 4)).toBe(3);
    expect(cuotaRedondeada(100000, 3)).toBe(33333);
    expect(cuotaRedondeada(5, 2)).toBe(3);
  });

  it('formatea el mes con dos dígitos', () => {
    expect(textoMes(2030, 2)).toBe('02/2030');
  });
});

describe('interpretación del monto como texto', () => {
  it('convierte a céntimos sin errores de coma flotante', () => {
    expect(interpretarMonto('0.10')).toEqual({ estado: 'valido', centavos: 10 });
    expect(interpretarMonto('1.1')).toEqual({ estado: 'valido', centavos: 110 });
    expect(interpretarMonto(' 19.99 ')).toEqual({ estado: 'valido', centavos: 1999 });
    expect(interpretarMonto('1000,50')).toEqual({ estado: 'valido', centavos: 100050 });
    expect(interpretarMonto('9999999999.99')).toEqual({ estado: 'valido', centavos: 999999999999 });
  });

  it('distingue vacío, no positivo y formato', () => {
    expect(interpretarMonto('  ')).toEqual({ estado: 'vacio' });
    expect(interpretarMonto('0')).toEqual({ estado: 'noPositivo' });
    expect(interpretarMonto('0.00')).toEqual({ estado: 'noPositivo' });
    expect(interpretarMonto('-5')).toEqual({ estado: 'noPositivo' });
    expect(interpretarMonto('abc')).toEqual({ estado: 'formato' });
    expect(interpretarMonto('1.234')).toEqual({ estado: 'formato' });
    expect(interpretarMonto('10000000000')).toEqual({ estado: 'formato' });
  });
});

describe('evaluación del tope por mes', () => {
  it('suma lo ya comprometido del mes y admite la igualdad con el máximo', () => {
    const comprometidas = [{ anio: 2030, mes: 2, monto: 1000 }];
    expect(evaluarTope(generarCronograma(10000, 1, 2030, 2), comprometidas, 109620)[0]).toMatchObject({
      comprometidoCentavos: 100000,
      totalCentavos: 110000,
      superaMaximo: true,
    });
    expect(evaluarTope(generarCronograma(9620, 1, 2030, 2), comprometidas, 109620)[0]?.superaMaximo).toBe(false);
    expect(evaluarTope(generarCronograma(9621, 1, 2030, 2), comprometidas, 109620)[0]?.superaMaximo).toBe(true);
  });

  it('ignora los meses comprometidos que no coinciden', () => {
    const cuotas = generarCronograma(40000, 2, 2030, 8);
    const evaluadas = evaluarTope(cuotas, [{ anio: 2030, mes: 6, monto: 100 }, { anio: 2030, mes: 9, monto: 50 }], 30000);
    expect(evaluadas.map((cuota) => cuota.comprometidoCentavos)).toEqual([0, 5000]);
    expect(evaluadas.map((cuota) => cuota.superaMaximo)).toEqual([false, false]);
  });
});
