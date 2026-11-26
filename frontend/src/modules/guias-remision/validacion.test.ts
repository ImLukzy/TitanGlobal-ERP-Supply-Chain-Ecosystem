import { describe, expect, it } from 'vitest';
import { camposCabecera, validarGuia, validarMotivo } from './validacion';
import { fechaParaInput, fechaParaApi, numeroGuia } from './presentacion';
import { valores } from './guias.testUtils';
describe('validación de la guía', () => {
  it('admite datos válidos y normalización de serie', () => {
    expect(validarGuia({ ...valores, serie: ' t001 ' })).toEqual({});
  });
  it.each(camposCabecera)('valida límites de %s', (campo, _label, maximo, etiqueta) => {
    expect(validarGuia({ ...valores, [campo]: ' ' })[campo]).toBe(
      `El campo ${etiqueta} es obligatorio.`,
    );
    expect(validarGuia({ ...valores, [campo]: 'x'.repeat(maximo + 1) })[campo]).toBe(
      `El campo ${etiqueta} no puede superar ${maximo} caracteres.`,
    );
    expect(validarGuia({ ...valores, [campo]: 'x'.repeat(maximo) })[campo]).toBeUndefined();
  });
  it.each([
    ['serie', 'A-1'],
    ['serie', ''],
    ['numero', '0'],
    ['numero', '1.5'],
    ['numero', '100000000'],
    ['fechaInicioTraslado', ''],
    ['fechaInicioTraslado', '2026-11-22'],
    ['pesoBrutoKg', '0'],
    ['pesoBrutoKg', '10000000000'],
    ['pesoBrutoKg', '1.001'],
    ['pesoBrutoKg', '1e3'],
    ['observaciones', 'x'.repeat(501)],
  ])('rechaza %s inválido', (campo, valor) => {
    expect(validarGuia({ ...valores, [campo]: valor })[campo]).toBeTruthy();
  });
  it.each([
    ['sku', ''],
    ['sku', 'x'.repeat(41)],
    ['descripcion', ''],
    ['descripcion', 'x'.repeat(201)],
    ['unidadMedida', ''],
    ['unidadMedida', 'x'.repeat(11)],
    ['cantidad', '0'],
    ['cantidad', '1000000000'],
    ['cantidad', '1.0001'],
    ['cantidad', '1e3'],
  ])('rechaza %s de línea', (campo, valor) => {
    const linea = { ...valores.lineas[0], [campo]: valor };
    expect(validarGuia({ ...valores, lineas: [linea] })[`l1-${campo}`]).toBeTruthy();
  });
  it('admite extremos decimales y ceros no significativos', () => {
    expect(
      validarGuia({
        ...valores,
        pesoBrutoKg: '9999999999.9900',
        lineas: [{ ...valores.lineas[0], cantidad: '999999999.9990' }],
      }),
    ).toEqual({});
    expect(
      validarGuia({
        ...valores,
        pesoBrutoKg: '0.01',
        lineas: [{ ...valores.lineas[0], cantidad: '0.001' }],
      }),
    ).toEqual({});
  });
  it('acota de 1 a 100 líneas', () => {
    expect(validarGuia({ ...valores, lineas: [] }).lineas).toBeTruthy();
    expect(
      validarGuia({ ...valores, lineas: Array(101).fill(valores.lineas[0]) }).lineas,
    ).toBeTruthy();
    expect(
      validarGuia({ ...valores, lineas: Array(100).fill(valores.lineas[0]) }).lineas,
    ).toBeUndefined();
  });
  it('valida motivo de anulación literal', () => {
    expect(validarMotivo(' ')).toBe('El motivo de anulación es obligatorio.');
    expect(validarMotivo('x'.repeat(501))).toBe(
      'El motivo de anulación no puede superar 500 caracteres.',
    );
    expect(validarMotivo('x'.repeat(500))).toBe('');
  });
  it('edita instantes UTC sin perder segundos o milisegundos', () => {
    expect(fechaParaInput('2026-11-22T05:30:12.345-05:00')).toBe('2026-11-22T10:30:12.345');
    expect(fechaParaApi('2026-11-22T10:30:12.345')).toBe('2026-11-22T10:30:12.345Z');
    expect(numeroGuia('T001', 1)).toBe('T001-00000001');
  });
});
