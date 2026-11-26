import { describe, expect, it } from 'vitest';
import { construirRequest, interpretarMes, validarMotivoAnulacion, validarPrestamo, type CamposPrestamo } from './validacionPrestamo';

const validos: CamposPrestamo = {
  empleadoId: '1',
  tipo: 'Prestamo',
  monto: '1000.50',
  numeroCuotas: '3',
  fechaOtorgamiento: '2026-11-22',
  primerMes: '2026-12',
  motivo: ' Gastos médicos ',
};

describe('validación local del préstamo (mensajes 1 a 12)', () => {
  it('acepta unos campos correctos', () => {
    expect(validarPrestamo(validos)).toEqual({});
  });

  it('devuelve los mensajes literales de la spec', () => {
    const errores = validarPrestamo({ empleadoId: '', tipo: 'Otro', monto: '', numeroCuotas: '', fechaOtorgamiento: '', primerMes: '', motivo: '' });
    expect(errores).toEqual({
      empleadoId: 'El empleado es obligatorio.',
      tipo: 'El tipo debe ser Adelanto o Prestamo.',
      monto: 'El monto es obligatorio.',
      fechaOtorgamiento: 'La fecha de otorgamiento es obligatoria.',
      primerMes: 'El primer mes de descuento es obligatorio y debe ser un mes válido.',
      motivo: 'El motivo es obligatorio.',
    });
  });

  it('distingue las reglas del monto', () => {
    expect(validarPrestamo({ ...validos, monto: '0' }).monto).toBe('El monto debe ser mayor que cero.');
    expect(validarPrestamo({ ...validos, monto: '-3' }).monto).toBe('El monto debe ser mayor que cero.');
    const mensaje5 = 'El monto admite como máximo 2 decimales y no puede superar 9999999999.99.';
    expect(validarPrestamo({ ...validos, monto: '10.123' }).monto).toBe(mensaje5);
    expect(validarPrestamo({ ...validos, monto: '10000000000' }).monto).toBe(mensaje5);
    expect(validarPrestamo({ ...validos, monto: 'abc' }).monto).toBe(mensaje5);
  });

  it('valida el número de cuotas de un préstamo entre 1 y 24', () => {
    const mensaje = 'El número de cuotas debe estar entre 1 y 24.';
    expect(validarPrestamo({ ...validos, numeroCuotas: '0' }).numeroCuotas).toBe(mensaje);
    expect(validarPrestamo({ ...validos, numeroCuotas: '25' }).numeroCuotas).toBe(mensaje);
    expect(validarPrestamo({ ...validos, numeroCuotas: '2.5' }).numeroCuotas).toBe(mensaje);
    expect(validarPrestamo({ ...validos, numeroCuotas: '24' }).numeroCuotas).toBeUndefined();
  });

  it('un adelanto se descuenta en una sola cuota', () => {
    expect(validarPrestamo({ ...validos, tipo: 'Adelanto', numeroCuotas: '2' }).numeroCuotas).toBe('Un adelanto se descuenta en una sola cuota.');
    expect(validarPrestamo({ ...validos, tipo: 'Adelanto', numeroCuotas: '1' }).numeroCuotas).toBeUndefined();
  });

  it('el primer mes no puede ser anterior al mes de otorgamiento', () => {
    expect(validarPrestamo({ ...validos, primerMes: '2026-10' }).primerMes).toBe('El primer mes de descuento no puede ser anterior al mes de otorgamiento.');
    expect(validarPrestamo({ ...validos, primerMes: '2026-11' }).primerMes).toBeUndefined();
  });

  it('limita el motivo a 300 caracteres tras recortar', () => {
    expect(validarPrestamo({ ...validos, motivo: 'a'.repeat(301) }).motivo).toBe('El motivo no puede superar 300 caracteres.');
    expect(validarPrestamo({ ...validos, motivo: ` ${'a'.repeat(300)} ` }).motivo).toBeUndefined();
  });

  it('interpreta el mes y rechaza los años fuera de 2000 a 2100', () => {
    expect(interpretarMes('2030-11')).toEqual({ anio: 2030, mes: 11 });
    expect(interpretarMes('1999-12')).toBeNull();
    expect(interpretarMes('2101-01')).toBeNull();
    expect(interpretarMes('2030-13')).toBeNull();
    expect(interpretarMes('')).toBeNull();
  });
});

describe('petición de registro', () => {
  it('convierte el monto desde el texto y recorta el motivo', () => {
    expect(construirRequest(validos)).toEqual({
      empleadoId: 1,
      tipo: 'Prestamo',
      monto: 1000.5,
      numeroCuotas: 3,
      fechaOtorgamiento: '2026-11-22',
      primerAnio: 2026,
      primerMes: 12,
      motivo: 'Gastos médicos',
    });
  });

  it('un adelanto viaja con una cuota aunque el campo diga otra cosa', () => {
    expect(construirRequest({ ...validos, tipo: 'Adelanto', numeroCuotas: '9' })?.numeroCuotas).toBe(1);
  });

  it('devuelve null si algo no se puede convertir', () => {
    expect(construirRequest({ ...validos, monto: 'x' })).toBeNull();
  });
});

describe('motivo de la anulación (mensajes 23 y 24)', () => {
  it('exige motivo y limita su longitud', () => {
    expect(validarMotivoAnulacion('  ')).toBe('El motivo de la anulación es obligatorio.');
    expect(validarMotivoAnulacion('a'.repeat(301))).toBe('El motivo de la anulación no puede superar 300 caracteres.');
    expect(validarMotivoAnulacion('Error de carga')).toBeUndefined();
  });
});
