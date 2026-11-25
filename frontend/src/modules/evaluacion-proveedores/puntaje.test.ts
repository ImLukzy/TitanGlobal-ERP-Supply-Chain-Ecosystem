import { describe, expect, it } from 'vitest';
import { calcularPuntajeFinal, calificarPuntaje } from './puntaje';
import { puntajeValido, validarEvaluacion } from './validacion';

describe('cálculo del puntaje', () => {
  it('A4 aplica 40/30/30 como la API', () => {
    expect(calcularPuntajeFinal(90, 85, 80)).toBe(85.5);
    expect(calcularPuntajeFinal(95, 92, 90)).toBe(92.6);
    expect(calcularPuntajeFinal(0, 0, 0)).toBe(0);
    expect(calcularPuntajeFinal(100, 100, 100)).toBe(100);
  });

  it('A4 respeta los umbrales 90, 75 y 60', () => {
    expect(calificarPuntaje(90)).toBe('Excelente');
    expect(calificarPuntaje(89.99)).toBe('Bueno');
    expect(calificarPuntaje(75)).toBe('Bueno');
    expect(calificarPuntaje(74.99)).toBe('Regular');
    expect(calificarPuntaje(60)).toBe('Regular');
    expect(calificarPuntaje(59.99)).toBe('Deficiente');
  });

  it('A4 solo acepta enteros de 0 a 100', () => {
    expect(['0', '55', '100'].every(puntajeValido)).toBe(true);
    expect(['', '-1', '101', '5.5', 'abc', '1000'].some(puntajeValido)).toBe(false);
  });

  it('A4 en edición no valida proveedor ni periodo', () => {
    const errores = validarEvaluacion({ proveedorId: '', periodo: '', cumplimiento: '80', calidad: '80', plazos: '80', observaciones: '' }, true);
    expect(errores).toEqual({});
  });
});
