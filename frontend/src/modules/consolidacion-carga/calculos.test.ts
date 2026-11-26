import { expect, it } from 'vitest';
import { nuevaLinea, previsualizar } from './calculos';
import { validarCarga, validarLinea, validarMotivo } from './validacion';
const linea = {
  ...nuevaLinea(),
  sku: 'SKU',
  descripcion: 'Caja',
  cantidad: '10',
  cantidadRecogida: '10',
  unidadesPorBulto: '4',
  largoCm: '50',
  anchoCm: '40',
  altoCm: '30',
  pesoBultoKg: '2',
};
it('calcula bultos parciales, volumen y peso sin Number', () => {
  expect(previsualizar([linea])).toEqual({ bultos: '3', volumenM3: '0.180000', pesoKg: '6.00' });
});
it('redondea por línea antes de sumar con mitades hacia fuera de cero', () => {
  const l = {
    ...linea,
    cantidad: '1',
    cantidadRecogida: '1',
    unidadesPorBulto: '1',
    largoCm: '1',
    anchoCm: '1',
    altoCm: '0.50',
  };
  expect(previsualizar([l, l])?.volumenM3).toBe('0.000002');
});
it('conserva máximos de cien líneas y cantidades superiores a la precisión segura de Number', () => {
  const l = {
    ...linea,
    cantidad: '1000000',
    cantidadRecogida: '1000000',
    unidadesPorBulto: '1',
    largoCm: '9999.99',
    anchoCm: '9999.99',
    altoCm: '9999.99',
    pesoBultoKg: '999999.99',
  };
  expect(previsualizar(Array.from({ length: 100 }, () => l))).toEqual({
    bultos: '100000000',
    volumenM3: '99999700000299.999900',
    pesoKg: '99999999000000.00',
  });
});
it('admite guardar packing incompleto con estimación cero y ceros finales', () => {
  expect(validarLinea({ ...linea, unidadesPorBulto: '0', largoCm: '0.0000' }, 0)).toEqual({});
  expect(previsualizar([{ ...linea, unidadesPorBulto: '0' }])).toEqual({
    bultos: '0',
    volumenM3: '0.000000',
    pesoKg: '0.00',
  });
});
it.each([
  ['cantidad', '0', 'la cantidad debe estar entre 1 y 1000000.'],
  ['cantidad', '1000001', 'la cantidad debe estar entre 1 y 1000000.'],
  ['cantidadRecogida', '-1', 'la cantidad recogida debe estar entre 0 y 1000000.'],
  ['cantidadRecogida', '11', 'la cantidad recogida no puede superar la cantidad prevista.'],
  ['unidadesPorBulto', '1000001', 'las unidades por bulto debe estar entre 0 y 1000000.'],
  ['largoCm', '10000', 'el largo debe estar entre 0 y 9999.99.'],
  ['largoCm', '1.001', 'el largo no puede tener más de 2 decimales.'],
  ['anchoCm', '-1', 'el ancho debe estar entre 0 y 9999.99.'],
  ['anchoCm', '1.001', 'el ancho no puede tener más de 2 decimales.'],
  ['altoCm', '10000', 'el alto debe estar entre 0 y 9999.99.'],
  ['altoCm', '1.001', 'el alto no puede tener más de 2 decimales.'],
  ['pesoBultoKg', '1000000', 'el peso por bulto debe estar entre 0 y 999999.99.'],
  ['pesoBultoKg', '1.001', 'el peso por bulto no puede tener más de 2 decimales.'],
])('valida %s = %s con el literal del servidor', (campo, valor, mensaje) => {
  expect(validarLinea({ ...linea, [campo]: valor }, 0)[campo]).toBe(`Línea 1: ${mensaje}`);
});
it.each(['', 'NaN', 'Infinity', '1e3', '1.5'])(
  'rechaza cantidad no entera %s y no calcula sobre ella',
  (cantidad) => {
    expect(validarLinea({ ...linea, cantidad }, 0).cantidad).toBeDefined();
    expect(previsualizar([{ ...linea, cantidad }])).toBeNull();
  },
);
it.each([
  ['sku', 40, 'el SKU'],
  ['descripcion', 200, 'la descripción'],
] as const)('valida texto de línea %s', (campo, maximo, etiqueta) => {
  expect(validarLinea({ ...linea, [campo]: '' }, 1)[campo]).toContain(`Línea 2: ${etiqueta}`);
  expect(validarLinea({ ...linea, [campo]: 'x'.repeat(maximo) }, 0)[campo]).toBeUndefined();
  expect(validarLinea({ ...linea, [campo]: 'x'.repeat(maximo + 1) }, 0)[campo]).toBeDefined();
});
it('valida cabecera, longitudes y número de líneas', () => {
  expect(
    validarCarga({ codigo: '', nombre: '', observaciones: 'x'.repeat(501), lineas: [] }),
  ).toMatchObject({
    codigo: 'El código de la consolidación es obligatorio.',
    nombre: 'El nombre de la consolidación es obligatorio.',
    observaciones: 'Las observaciones no pueden superar 500 caracteres.',
    lineas: 'La consolidación debe tener entre 1 y 100 líneas.',
  });
  expect(
    validarCarga({
      codigo: 'c'.repeat(20),
      nombre: 'n'.repeat(100),
      observaciones: 'o'.repeat(500),
      lineas: [linea],
    }),
  ).toEqual({});
  expect(
    validarCarga({
      codigo: 'c'.repeat(21),
      nombre: 'n'.repeat(101),
      observaciones: '',
      lineas: Array.from({ length: 101 }, () => linea),
    }),
  ).toHaveProperty('lineas');
});
it('valida motivo obligatorio y límite exacto de cancelación', () => {
  expect(validarMotivo(' ')).toBe('El motivo de cancelación es obligatorio.');
  expect(validarMotivo('x'.repeat(500))).toBe('');
  expect(validarMotivo('x'.repeat(501))).toBe(
    'El motivo de cancelación no puede superar 500 caracteres.',
  );
});
