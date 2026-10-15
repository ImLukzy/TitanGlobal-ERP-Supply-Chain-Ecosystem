import { describe, expect, it } from 'vitest';
import { formatearCantidad } from './formato';

describe('formatearCantidad', () => {
  it.each([
    [0, '0 unidades'],
    [1, '1 unidad'],
    [2, '2 unidades'],
  ] as const)('formatea cantidad %s', (cantidad, esperado) => {
    expect(formatearCantidad(cantidad, 'unidad', 'unidades')).toBe(esperado);
  });
});
