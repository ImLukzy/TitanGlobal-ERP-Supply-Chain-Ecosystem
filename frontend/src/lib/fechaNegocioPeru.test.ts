import { describe, expect, it } from 'vitest';
import { diaDePeru } from './fechaNegocioPeru';

describe('día de negocio de Perú', () => {
  it.each([
    ['2026-09-30T23:59:59Z', '2026-09-30'],
    ['2026-10-01T00:00:00Z', '2026-09-30'],
    ['2026-10-01T04:59:59Z', '2026-09-30'],
    ['2026-10-01T05:00:00Z', '2026-10-01'],
  ])('convierte %s al día %s', (instante, dia) => {
    expect(diaDePeru(new Date(instante))).toBe(dia);
  });
});
