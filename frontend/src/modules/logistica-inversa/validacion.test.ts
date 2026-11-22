import { expect, it } from 'vitest';
import { camposTexto, validarRetorno, errorCantidad, validarMotivo } from './validacion';
import { valores } from './retornos.testUtils';
it.each(camposTexto)('valida %s y su límite', (campo, _label, max, etiqueta) => {
  expect(validarRetorno({ ...valores, [campo]: ' ' })[campo]).toBe(
    `El campo ${etiqueta} es obligatorio.`,
  );
  expect(validarRetorno({ ...valores, [campo]: 'x'.repeat(max + 1) })[campo]).toBe(
    `El campo ${etiqueta} no puede superar ${max} caracteres.`,
  );
  expect(validarRetorno({ ...valores, [campo]: 'x'.repeat(max) })[campo]).toBeUndefined();
});
it.each(['', '0', '-1', '1000000000', 'NaN', 'Infinity'])(
  'rechaza cantidad fuera de rango %s',
  (v) => {
    expect(errorCantidad(v)).toBe('La cantidad debe estar entre 0.001 y 999999999.999.');
  },
);
it.each(['0.001', '999999999.999', '1.0000'])('acepta cantidad representable %s', (v) =>
  expect(errorCantidad(v)).toBe(''),
);
it('no redondea más de tres decimales', () =>
  expect(errorCantidad('1.0001')).toBe('La cantidad no puede tener más de 3 decimales.'));
it('valida fecha tipo y observaciones', () => {
  expect(validarRetorno({ ...valores, fechaProgramada: '' }).fechaProgramada).toBe(
    'La fecha programada es obligatoria.',
  );
  expect(validarRetorno({ ...valores, tipo: 'Otro' as typeof valores.tipo }).tipo).toBe(
    'El tipo debe ser Pallet, Retornable o DevolucionRuta.',
  );
  expect(validarRetorno({ ...valores, observaciones: 'x'.repeat(501) }).observaciones).toBe(
    'Las observaciones no pueden superar 500 caracteres.',
  );
});
it('valida motivo sin perder límite', () => {
  expect(validarMotivo(' ')).toBe('El motivo de cancelación es obligatorio.');
  expect(validarMotivo('x'.repeat(501))).toBe(
    'El motivo de cancelación no puede superar 500 caracteres.',
  );
  expect(validarMotivo('x'.repeat(500))).toBe('');
});
