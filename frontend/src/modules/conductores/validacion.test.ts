import { afterEach, describe, expect, it, vi } from 'vitest';
import { diaUtc, licenciaVencida } from './presentacion';
import { fechaParaGuardar, validarConductor, type ValoresConductor } from './validacion';

afterEach(() => vi.useRealTimers());
const base: ValoresConductor = {
  nombre: 'Rosa Quispe', documentoLicencia: 'PLT-QA-01', vencimientoLicencia: '2026-11-21',
  habilitado: true, activo: true, observaciones: '',
};

function fijarHoyUtc(): void {
  vi.useFakeTimers();
  vi.setSystemTime(new Date('2026-11-21T01:00:00Z'));
}

describe('Fechas UTC de conductores', () => {
  it('acepta hoy UTC aunque en Lima todavía sea el día anterior', () => {
    fijarHoyUtc();
    expect(validarConductor(base, null)).toEqual({});
    expect(licenciaVencida('2026-11-21T00:00:00Z')).toBe(false);
    expect(licenciaVencida('2026-11-20T23:59:59Z')).toBe(true);
  });

  it.each(['2026-11-20', '2026-02-30', '2025-02-29', '0000-01-01', 'invalid', ''])('rechaza la fecha nueva %s', (fecha) => {
    fijarHoyUtc();
    expect(validarConductor({ ...base, vencimientoLicencia: fecha }, null).vencimientoLicencia)
      .toBe('La fecha de vencimiento de la licencia no es válida.');
  });

  it('conserva una fecha vencida al editar o desactivar sin perder su instante original', () => {
    fijarHoyUtc();
    const original = '2026-06-30T13:42:15Z';
    expect(validarConductor({ ...base, vencimientoLicencia: '2026-06-30', activo: false }, original)).toEqual({});
    expect(fechaParaGuardar('2026-06-30', original)).toBe(original);
    expect(validarConductor({ ...base, vencimientoLicencia: '2026-06-29', activo: false }, original).vencimientoLicencia)
      .toBe('La fecha de vencimiento de la licencia no es válida.');
  });

  it('envía una nueva fecha como medianoche UTC sin cambiar el día', () => {
    expect(fechaParaGuardar('2027-12-31', null)).toBe('2027-12-31T00:00:00Z');
    expect(fechaParaGuardar('2027-12-31', '2026-06-30T13:42:15Z')).toBe('2027-12-31T00:00:00Z');
    expect(diaUtc('2026-11-20T23:30:00-05:00')).toBe('2026-11-21');
  });
});
