import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  construirRequestCalculo,
  construirRequestMarcacion,
  construirRequestRechazo,
  validarCalculo,
  validarDispositivo,
  validarMarcacion,
  validarMotivoRechazo,
  type CamposMarcacion,
} from './validacionMarcacion';

afterEach(() => {
  vi.useRealTimers();
});

const MARCACION_VALIDA: CamposMarcacion = { empleadoId: '1', tipo: 'Entrada', fecha: '2026-11-03', hora: '08:00', observacion: 'Olvidó marcar' };

describe('validarMarcacion', () => {
  it('no devuelve errores con datos válidos', () => {
    expect(validarMarcacion(MARCACION_VALIDA)).toEqual({});
  });

  it('usa los mensajes literales 1 a 4 cuando todo falta', () => {
    expect(validarMarcacion({ empleadoId: '', tipo: '', fecha: '', hora: '', observacion: '  ' })).toEqual({
      empleadoId: 'El empleado es obligatorio.',
      tipo: 'El tipo de marcación debe ser Entrada o Salida.',
      fecha: 'El instante de la marcación es obligatorio.',
      hora: 'El instante de la marcación es obligatorio.',
      observacion: 'La observación es obligatoria en una marcación manual.',
    });
  });

  it('acepta 200 caracteres de observación y rechaza 201 con el mensaje 5', () => {
    expect(validarMarcacion({ ...MARCACION_VALIDA, observacion: 'a'.repeat(200) })).toEqual({});
    expect(validarMarcacion({ ...MARCACION_VALIDA, observacion: 'a'.repeat(201) }).observacion).toBe('La observación no puede superar 200 caracteres.');
  });

  it('pide el instante si falta solo la hora', () => {
    expect(validarMarcacion({ ...MARCACION_VALIDA, hora: '' })).toEqual({ hora: 'El instante de la marcación es obligatorio.' });
  });
});

describe('construirRequestMarcacion', () => {
  it('convierte la hora local a UTC y recorta la observación', () => {
    expect(construirRequestMarcacion({ ...MARCACION_VALIDA, observacion: '  Olvidó marcar  ' })).toEqual({
      empleadoId: 1,
      tipo: 'Entrada',
      instante: '2026-11-03T13:00:00.000Z',
      observacion: 'Olvidó marcar',
    });
    expect(construirRequestMarcacion({ ...MARCACION_VALIDA, tipo: 'Salida', hora: '22:30' }).instante).toBe('2026-11-04T03:30:00.000Z');
  });
});

describe('validarDispositivo y validarMotivoRechazo', () => {
  it('valida el dispositivo con los mensajes 10 y 28', () => {
    expect(validarDispositivo(' ')).toBe('El dispositivo es obligatorio.');
    expect(validarDispositivo('a'.repeat(51))).toBe('El dispositivo no puede superar 50 caracteres.');
    expect(validarDispositivo('a'.repeat(50))).toBeUndefined();
  });

  it('valida el motivo con los mensajes 21 y 22', () => {
    expect(validarMotivoRechazo('  ')).toBe('El motivo del rechazo es obligatorio.');
    expect(validarMotivoRechazo('a'.repeat(301))).toBe('El motivo del rechazo no puede superar 300 caracteres.');
    expect(validarMotivoRechazo('a'.repeat(300))).toBeUndefined();
    expect(construirRequestRechazo('  No autorizadas ')).toEqual({ motivo: 'No autorizadas' });
  });
});

describe('validarCalculo', () => {
  it('pide ambas fechas y que desde no sea posterior a hasta (mensaje 16)', () => {
    const mensaje = 'Las fechas desde y hasta son obligatorias y desde no puede ser posterior a hasta.';
    expect(validarCalculo({ desde: '', hasta: '', empleadoId: '' })).toEqual({ desde: mensaje, hasta: mensaje });
    expect(validarCalculo({ desde: '2026-11-06', hasta: '2026-11-02', empleadoId: '' })).toEqual({ desde: mensaje });
  });

  it('limita el rango a 31 días contando ambos extremos (mensaje 17)', () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-12-31T15:00:00Z'));
    expect(validarCalculo({ desde: '2026-12-01', hasta: '2026-12-31', empleadoId: '' })).toEqual({});
    expect(validarCalculo({ desde: '2026-11-30', hasta: '2026-12-31', empleadoId: '' })).toEqual({ hasta: 'El rango de cálculo no puede superar 31 días.' });
  });

  it('no deja calcular jornadas futuras en hora de Perú (mensaje 18)', () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-11-05T03:00:00Z'));
    expect(validarCalculo({ desde: '2026-11-02', hasta: '2026-11-04', empleadoId: '' })).toEqual({});
    expect(validarCalculo({ desde: '2026-11-02', hasta: '2026-11-05', empleadoId: '' })).toEqual({ hasta: 'No se pueden calcular jornadas futuras.' });
  });

  it('envía el empleado solo si se eligió', () => {
    expect(construirRequestCalculo({ desde: '2026-11-02', hasta: '2026-11-06', empleadoId: '' })).toEqual({ desde: '2026-11-02', hasta: '2026-11-06' });
    expect(construirRequestCalculo({ desde: '2026-11-02', hasta: '2026-11-06', empleadoId: '2' })).toEqual({ desde: '2026-11-02', hasta: '2026-11-06', empleadoId: 2 });
  });
});
