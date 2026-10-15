import { describe, expect, it } from 'vitest';
import { formatearCantidad, formatearFecha } from './formato';

describe('formatearFecha', () => {
  it('formatea las fechas de la API en UTC', () => {
    expect(formatearFecha('2026-01-02T00:00:00Z')).toBe('2/1/2026');
  });
});

describe('formatearCantidad', () => {
  it.each([
    [0, '0 unidades'],
    [1, '1 unidad'],
    [2, '2 unidades'],
  ] as const)('formatea cantidad %s', (cantidad, esperado) => {
    expect(formatearCantidad(cantidad, 'unidad', 'unidades')).toBe(esperado);
  });
});
