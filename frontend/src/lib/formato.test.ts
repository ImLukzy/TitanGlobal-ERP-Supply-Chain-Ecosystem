import { describe, expect, it } from 'vitest';
import { formatearFecha } from './formato';

describe('formatearFecha', () => {
  it('formatea las fechas de la API en UTC', () => {
    expect(formatearFecha('2026-01-02T00:00:00Z')).toBe('2/1/2026');
  });
});
