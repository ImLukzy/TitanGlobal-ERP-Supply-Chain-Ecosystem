import { describe, expect, it } from 'vitest';
import { esFechaIso, interpretarAnioFiltro, textoPeriodoResumen } from './presentacion';
import {
  construirRequestAfiliacion,
  construirRequestBono,
  construirRequestPlanilla,
  interpretarMontoPositivo,
  validarAfiliacion,
  validarBono,
  validarFechaPago,
  validarPlanilla,
} from './validacionPlanilla';

describe('validación de planillas (mensajes 1 a 4 y 7)', () => {
  it('exige año entre 2000 y 2100, mes entre 1 y 12 y un tipo válido', () => {
    expect(validarPlanilla({ anio: '', mes: '', tipo: '' })).toEqual({
      anio: 'El año es obligatorio y debe estar entre 2000 y 2100.',
      mes: 'El mes es obligatorio y debe estar entre 1 y 12.',
      tipo: 'El tipo de planilla debe ser Mensual o Complementaria.',
    });
    expect(validarPlanilla({ anio: '1999', mes: '13', tipo: 'Otra' })).toEqual({
      anio: 'El año es obligatorio y debe estar entre 2000 y 2100.',
      mes: 'El mes es obligatorio y debe estar entre 1 y 12.',
      tipo: 'El tipo de planilla debe ser Mensual o Complementaria.',
    });
    expect(validarPlanilla({ anio: '2101', mes: '1', tipo: 'Mensual' }).anio).toBe('El año es obligatorio y debe estar entre 2000 y 2100.');
  });

  it('rechaza un periodo futuro y acepta uno pasado', () => {
    expect(validarPlanilla({ anio: '2100', mes: '12', tipo: 'Mensual' })).toEqual({ mes: 'No se puede crear una planilla de un periodo futuro.' });
    expect(validarPlanilla({ anio: '2020', mes: '1', tipo: 'Complementaria' })).toEqual({});
    expect(construirRequestPlanilla({ anio: '2020', mes: '1', tipo: 'Complementaria' })).toEqual({ anio: 2020, mes: 1, tipo: 'Complementaria' });
    expect(construirRequestPlanilla({ anio: '', mes: '1', tipo: 'Mensual' })).toBeNull();
  });

  it('exige una fecha de pago real', () => {
    expect(validarFechaPago('')).toBe('La fecha de pago es obligatoria.');
    expect(validarFechaPago('2026-02-31')).toBe('La fecha de pago es obligatoria.');
    expect(validarFechaPago('2026-11-30')).toBeUndefined();
  });
});

describe('validación de bonos (mensajes 16, 1, 2, 3, 18 y 19)', () => {
  const valido = { empleadoId: '1', anio: '2026', mes: '10', tipoPlanilla: 'Mensual', concepto: ' Bono por metas ', monto: '300,50' };

  it('acepta un bono válido y lo convierte tras validar el texto', () => {
    expect(validarBono(valido)).toEqual({});
    expect(construirRequestBono(valido)).toEqual({ empleadoId: 1, anio: 2026, mes: 10, tipoPlanilla: 'Mensual', concepto: 'Bono por metas', monto: 300.5 });
  });

  it('devuelve los mensajes literales por campo', () => {
    expect(validarBono({ empleadoId: '', anio: '', mes: '', tipoPlanilla: '', concepto: ' ', monto: '' })).toEqual({
      empleadoId: 'El empleado es obligatorio.',
      anio: 'El año es obligatorio y debe estar entre 2000 y 2100.',
      mes: 'El mes es obligatorio y debe estar entre 1 y 12.',
      tipoPlanilla: 'El tipo de planilla debe ser Mensual o Complementaria.',
      concepto: 'El concepto es obligatorio y no puede superar 100 caracteres.',
      monto: 'El monto debe ser mayor que cero, con 2 decimales como máximo y no superar 9999999999.99.',
    });
    expect(validarBono({ ...valido, concepto: 'x'.repeat(101) }).concepto).toBe('El concepto es obligatorio y no puede superar 100 caracteres.');
    expect(validarBono({ ...valido, concepto: 'x'.repeat(100) }).concepto).toBeUndefined();
  });

  it('valida el monto como texto: ceros, negativos, decimales y tope', () => {
    expect(interpretarMontoPositivo('0')).toBeNull();
    expect(interpretarMontoPositivo('0.00')).toBeNull();
    expect(interpretarMontoPositivo('-5')).toBeNull();
    expect(interpretarMontoPositivo('1.234')).toBeNull();
    expect(interpretarMontoPositivo('abc')).toBeNull();
    expect(interpretarMontoPositivo('10000000000')).toBeNull();
    expect(interpretarMontoPositivo('9999999999.99')).toBe(9999999999.99);
    expect(interpretarMontoPositivo('0.01')).toBe(0.01);
    expect(interpretarMontoPositivo('12.')).toBe(12);
    expect(interpretarMontoPositivo('12.5')).toBe(12.5);
  });
});

describe('validación de afiliaciones (mensajes 16, 22, 24 y 25)', () => {
  const base = { empleadoId: '3', codigoPension: 'INTEGRA', cuspp: '123456ABCDEF', fechaAfiliacion: '2026-01-15' };

  it('exige empleado, sistema y fecha', () => {
    expect(validarAfiliacion({ empleadoId: '', codigoPension: '', cuspp: '', fechaAfiliacion: '' }, false)).toEqual({
      empleadoId: 'El empleado es obligatorio.',
      codigoPension: 'El sistema de pensiones es obligatorio.',
      fechaAfiliacion: 'La fecha de afiliación es obligatoria.',
    });
  });

  it('el CUSPP solo se exige en una AFP y tiene 12 caracteres', () => {
    expect(validarAfiliacion({ ...base, cuspp: '' }, true).cuspp).toBe('El CUSPP es obligatorio en una AFP y tiene 12 caracteres.');
    expect(validarAfiliacion({ ...base, cuspp: '12345' }, true).cuspp).toBe('El CUSPP es obligatorio en una AFP y tiene 12 caracteres.');
    expect(validarAfiliacion(base, true)).toEqual({});
    expect(validarAfiliacion({ ...base, codigoPension: 'ONP', cuspp: '' }, false)).toEqual({});
  });

  it('el cuerpo solo lleva el CUSPP en una AFP', () => {
    expect(construirRequestAfiliacion(base, true)).toEqual({ codigoPension: 'INTEGRA', cuspp: '123456ABCDEF', fechaAfiliacion: '2026-01-15' });
    expect(construirRequestAfiliacion({ ...base, codigoPension: 'ONP' }, false)).toEqual({ codigoPension: 'ONP', fechaAfiliacion: '2026-01-15' });
  });
});

describe('presentación', () => {
  it('interpreta el año de un filtro: vacío todos, válido se aplica, otro no cambia', () => {
    expect(interpretarAnioFiltro('')).toBeNull();
    expect(interpretarAnioFiltro('2026')).toBe(2026);
    expect(interpretarAnioFiltro('202')).toBeUndefined();
    expect(interpretarAnioFiltro('1999')).toBeUndefined();
    expect(interpretarAnioFiltro('abcd')).toBeUndefined();
  });

  it('formatea el periodo del resumen como mes/año', () => {
    expect(textoPeriodoResumen('2026-11')).toBe('11/2026');
    expect(textoPeriodoResumen(null)).toBe('—');
  });

  it('rechaza fechas inexistentes', () => {
    expect(esFechaIso('2026-02-29')).toBe(false);
    expect(esFechaIso('2028-02-29')).toBe(true);
  });
});
