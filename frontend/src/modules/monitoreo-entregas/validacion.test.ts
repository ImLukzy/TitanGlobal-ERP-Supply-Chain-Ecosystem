import { expect, it } from 'vitest';
import { camposEntrega, camposPod, validarEntrega, validarPod, validarMotivo } from './validacion';
import { fechaParaInput, fechaParaApi } from './presentacion';
import { valores } from './entregas.testUtils';
it('admite programación válida', () => {
  expect(validarEntrega(valores)).toEqual({});
});
it.each(camposEntrega)('valida cada límite de %s', (campo, _label, maximo, etiqueta) => {
  expect(validarEntrega({ ...valores, [campo]: ' ' })[campo]).toBe(
    `El campo ${etiqueta} es obligatorio.`,
  );
  expect(validarEntrega({ ...valores, [campo]: 'x'.repeat(maximo + 1) })[campo]).toBe(
    `El campo ${etiqueta} no puede superar ${maximo} caracteres.`,
  );
  expect(validarEntrega({ ...valores, [campo]: 'x'.repeat(maximo) })[campo]).toBeUndefined();
});
it.each(['', '2026-11-22', '0000-01-01T10:00', 'texto'])(
  'exige fecha UTC editable %s',
  (fechaProgramada) => {
    expect(validarEntrega({ ...valores, fechaProgramada }).fechaProgramada).toBe(
      'La fecha programada es obligatoria.',
    );
  },
);
it('acota observaciones de programación', () => {
  expect(validarEntrega({ ...valores, observaciones: 'x'.repeat(501) }).observaciones).toBe(
    'Las observaciones no pueden superar 500 caracteres.',
  );
  expect(validarEntrega({ ...valores, observaciones: 'x'.repeat(500) })).toEqual({});
});
const pod = {
  receptor: 'Persona',
  documentoReceptor: 'DOC',
  referenciaConstancia: 'POD',
  observaciones: '',
};
it.each(camposPod)('valida cada campo POD %s', (campo, _label, maximo, etiqueta) => {
  expect(validarPod({ ...pod, [campo]: ' ' })[campo]).toBe(`El campo ${etiqueta} es obligatorio.`);
  expect(validarPod({ ...pod, [campo]: 'x'.repeat(maximo + 1) })[campo]).toBe(
    `El campo ${etiqueta} no puede superar ${maximo} caracteres.`,
  );
  expect(validarPod({ ...pod, [campo]: 'x'.repeat(maximo) })[campo]).toBeUndefined();
});
it('acota observaciones POD y motivo', () => {
  expect(validarPod({ ...pod, observaciones: 'x'.repeat(501) }).observaciones).toBe(
    'Las observaciones de entrega no pueden superar 500 caracteres.',
  );
  expect(validarPod({ ...pod, observaciones: 'x'.repeat(500) })).toEqual({});
  expect(validarMotivo(' ')).toBe('El motivo de cancelación es obligatorio.');
  expect(validarMotivo('x'.repeat(501))).toBe(
    'El motivo de cancelación no puede superar 500 caracteres.',
  );
  expect(validarMotivo('x'.repeat(500))).toBe('');
});
it('preserva instante y milisegundos al editar desde otro huso', () => {
  expect(fechaParaInput('2026-11-22T05:30:12.345-05:00')).toBe('2026-11-22T10:30:12.345');
  expect(fechaParaApi(valores.fechaProgramada)).toBe('2026-11-22T10:30:12.345Z');
});
