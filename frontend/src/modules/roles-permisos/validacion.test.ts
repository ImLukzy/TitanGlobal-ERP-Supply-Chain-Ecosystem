import { describe, expect, it } from 'vitest';
import { validarRol } from './validacion';

describe('validación del formulario de roles', () => {
  it('acepta un rol válido sin descripción', () => {
    expect(validarRol({ codigo: 'AUDITOR', nombre: 'Auditor' })).toEqual({});
  });

  it('exige código y nombre', () => {
    const errores = validarRol({ codigo: '  ', nombre: '  ' });
    expect(errores.codigo).toBe('El código es obligatorio.');
    expect(errores.nombre).toBe('El nombre del rol es obligatorio.');
  });

  it('normaliza el código a mayúsculas antes de validarlo', () => {
    expect(validarRol({ codigo: 'ventas_2', nombre: 'Ventas' }).codigo).toBeUndefined();
  });

  it('rechaza códigos con caracteres no permitidos o de largo inválido', () => {
    const mensaje = 'El código solo admite letras, números, guiones y guion bajo (2 a 30 caracteres).';
    expect(validarRol({ codigo: 'AB CD', nombre: 'Rol' }).codigo).toBe(mensaje);
    expect(validarRol({ codigo: 'A', nombre: 'Rol' }).codigo).toBe(mensaje);
    expect(validarRol({ codigo: 'A'.repeat(31), nombre: 'Rol' }).codigo).toBe(mensaje);
    expect(validarRol({ codigo: 'A'.repeat(30), nombre: 'Rol' }).codigo).toBeUndefined();
  });

  it('respeta los límites de nombre (100) y descripción (300)', () => {
    expect(validarRol({ codigo: 'ROL', nombre: 'N'.repeat(100), descripcion: 'D'.repeat(300) })).toEqual({});
    const errores = validarRol({ codigo: 'ROL', nombre: 'N'.repeat(101), descripcion: 'D'.repeat(301) });
    expect(errores.nombre).toBe('El nombre del rol no puede superar 100 caracteres.');
    expect(errores.descripcion).toBe('La descripción no puede superar 300 caracteres.');
  });
});
