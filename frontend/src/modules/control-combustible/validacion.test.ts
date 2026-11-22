import { expect, it } from 'vitest';
import { validarVale, validarMotivo, camposTexto } from './validacion';
import { fechaParaApi, fechaParaInput } from './presentacion';
import { valores } from './vales.testUtils';
it('admite vale válido y final pendiente', () => {
  expect(validarVale(valores)).toEqual({});
  expect(validarVale({ ...valores, odometroFinal: '' })).toEqual({});
});
it.each(camposTexto)('valida %s', (campo, _label, maximo, etiqueta) => {
  expect(validarVale({ ...valores, [campo]: ' ' })[campo]).toBe(
    `El campo ${etiqueta} es obligatorio.`,
  );
  expect(validarVale({ ...valores, [campo]: 'x'.repeat(maximo + 1) })[campo]).toBe(
    `El campo ${etiqueta} no puede superar ${maximo} caracteres.`,
  );
  expect(validarVale({ ...valores, [campo]: 'x'.repeat(maximo) })[campo]).toBeUndefined();
});
it.each([
  ['litros', '0'],
  ['litros', '10000000'],
  ['litros', '1.0001'],
  ['precioLitro', '0'],
  ['precioLitro', '10000000'],
  ['precioLitro', '1.0001'],
  ['moneda', 'P3N'],
  ['moneda', ''],
  ['tipoCombustible', 'GNV'],
  ['odometroInicial', '-1'],
  ['odometroInicial', '1000000000'],
  ['odometroInicial', '1.5'],
  ['odometroFinal', '12000'],
  ['odometroFinal', '-1'],
  ['odometroFinal', '1000000000'],
  ['fechaCarga', ''],
  ['fechaCarga', '2026-11-22'],
  ['observaciones', 'x'.repeat(501)],
])('rechaza %s inválido', (campo, v) => {
  expect(validarVale({ ...valores, [campo]: v })[campo]).toBeTruthy();
});
it('admite extremos, ceros no significativos y normalizaciones', () => {
  expect(
    validarVale({
      ...valores,
      litros: '0.0010',
      precioLitro: '9999999.9990',
      moneda: ' pen ',
      tipoCombustible: ' diesel ',
      odometroInicial: '0',
      odometroFinal: '999999999',
      observaciones: 'x'.repeat(500),
    }),
  ).toEqual({});
});
it('motivo literal y UTC preservada', () => {
  expect(validarMotivo(' ')).toBe('El motivo de anulación es obligatorio.');
  expect(validarMotivo('x'.repeat(501))).toBe(
    'El motivo de anulación no puede superar 500 caracteres.',
  );
  expect(validarMotivo('x'.repeat(500))).toBe('');
  expect(fechaParaInput('2026-11-22T05:30:12.345-05:00')).toBe(valores.fechaCarga);
  expect(fechaParaApi(valores.fechaCarga)).toBe(valores.fechaCarga + 'Z');
});
