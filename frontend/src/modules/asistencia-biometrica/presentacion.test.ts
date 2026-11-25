import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  aFechaHoraLocal,
  aFechaLocal,
  aHoraLocal,
  diasEntre,
  formatearMinutos,
  hoyPeru,
  horaDeJornada,
  localAUtc,
  semanaActual,
  sumarDias,
} from './presentacion';

afterEach(() => {
  vi.useRealTimers();
});

describe('horas en UTC−5', () => {
  it('convierte un instante UTC a la hora local de Perú', () => {
    expect(aHoraLocal('2026-11-03T13:15:00Z')).toBe('08:15');
    expect(aHoraLocal('2026-11-03T22:00:00Z')).toBe('17:00');
    expect(aHoraLocal('2026-11-03T03:00:00Z')).toBe('22:00');
  });

  it('cambia el día local cuando el instante cae antes de las 05:00 UTC', () => {
    expect(aFechaLocal('2026-11-03T03:00:00Z')).toBe('2026-11-02');
    expect(aFechaLocal('2026-11-03T05:00:00Z')).toBe('2026-11-03');
    expect(aFechaHoraLocal('2026-11-03T03:30:00Z')).toBe('02/11/2026 22:30');
  });

  it('interpreta como UTC un instante sin zona', () => {
    expect(aHoraLocal('2026-11-03T13:15:00')).toBe('08:15');
  });

  it('respeta el desfase explícito de un instante con zona', () => {
    expect(aHoraLocal('2026-11-03T08:15:00-05:00')).toBe('08:15');
  });

  it('convierte fecha y hora locales a UTC', () => {
    expect(localAUtc('2026-11-03', '08:00')).toBe('2026-11-03T13:00:00.000Z');
    expect(localAUtc('2026-11-03', '22:30')).toBe('2026-11-04T03:30:00.000Z');
  });

  it('marca con (+1) la hora que cae en otro día local que la jornada', () => {
    expect(horaDeJornada('2026-11-03T11:00:00Z', '2026-11-02')).toBe('06:00 (+1)');
    expect(horaDeJornada('2026-11-03T13:00:00Z', '2026-11-03')).toBe('08:00');
    expect(horaDeJornada(null, '2026-11-03')).toBe('—');
  });
});

describe('minutos y fechas', () => {
  it('formatea los minutos como horas y minutos', () => {
    expect(formatearMinutos(0)).toBe('0 min');
    expect(formatearMinutos(45)).toBe('45 min');
    expect(formatearMinutos(60)).toBe('1 h');
    expect(formatearMinutos(85)).toBe('1 h 25 min');
    expect(formatearMinutos(635)).toBe('10 h 35 min');
  });

  it('suma días y cuenta la distancia entre fechas', () => {
    expect(sumarDias('2026-11-30', 1)).toBe('2026-12-01');
    expect(sumarDias('2026-11-03', -3)).toBe('2026-10-31');
    expect(diasEntre('2026-11-02', '2026-12-02')).toBe(30);
  });

  it('calcula hoy en hora de Perú, no en UTC', () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-11-05T03:00:00Z'));
    expect(hoyPeru()).toBe('2026-11-04');
  });

  it('la semana actual va del lunes a hoy', () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-11-05T15:00:00Z'));
    expect(semanaActual()).toEqual({ desde: '2026-11-02', hasta: '2026-11-05' });
    vi.setSystemTime(new Date('2026-11-02T15:00:00Z'));
    expect(semanaActual()).toEqual({ desde: '2026-11-02', hasta: '2026-11-02' });
    vi.setSystemTime(new Date('2026-11-08T15:00:00Z'));
    expect(semanaActual()).toEqual({ desde: '2026-11-02', hasta: '2026-11-08' });
  });
});
