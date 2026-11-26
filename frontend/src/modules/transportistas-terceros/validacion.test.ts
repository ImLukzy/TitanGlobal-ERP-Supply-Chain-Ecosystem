import { expect, it } from 'vitest';
import { errorImporte, validarFlete, validarMotivo, validarTransportista } from './validacion';
const maestro = {
  razonSocial: 'Nombre',
  documentoFiscal: 'Doc',
  pais: 'País',
  tipoServicio: 'Ambos',
  activo: true,
};
const flete = {
  transportistaId: 1,
  referencia: 'F1',
  fecha: '2026-11-21',
  origen: 'Lima',
  destino: 'Callao',
  importe: '12.30',
  moneda: 'PEN',
  observaciones: '',
};
it('valida límites completos del maestro y opcionales vacíos', () => {
  expect(
    validarTransportista({
      ...maestro,
      razonSocial: 'x'.repeat(200),
      documentoFiscal: 'd'.repeat(30),
      pais: 'p'.repeat(100),
      contacto: 'c'.repeat(100),
      telefono: 't'.repeat(20),
      observaciones: 'o'.repeat(500),
    }),
  ).toEqual({});
  expect(
    validarTransportista({
      ...maestro,
      razonSocial: ' ',
      documentoFiscal: '',
      pais: '',
      email: 'incorrecto',
      tipoServicio: 'Otro',
    }),
  ).toMatchObject({
    razonSocial: 'La razón social es obligatoria.',
    documentoFiscal: 'El documento fiscal es obligatorio.',
    pais: 'El país es obligatorio.',
    email: 'El email no es válido.',
    tipoServicio: 'El tipo de servicio debe ser 3PL, Flete o Ambos.',
  });
  expect(
    Object.keys(
      validarTransportista({
        ...maestro,
        razonSocial: 'x'.repeat(201),
        documentoFiscal: 'x'.repeat(31),
        pais: 'x'.repeat(101),
        contacto: 'x'.repeat(101),
        email: 'x'.repeat(151),
        telefono: 'x'.repeat(21),
        observaciones: 'x'.repeat(501),
      }),
    ),
  ).toHaveLength(7);
});
it.each(['0', '-1', '', '1,20', 'NaN', 'Infinity', '1e3'])(
  'rechaza importe inválido %s',
  (texto) => {
    expect(errorImporte(texto)).toBe('El importe debe ser mayor que cero.');
  },
);
it.each(['0.01', '12.30', '0012.30', '1000', '999999.99'])(
  'conserva centavos seguros %s',
  (texto) => {
    expect(errorImporte(texto)).toBe('');
  },
);
it('rechaza redondeo, desbordamiento y números no representables', () => {
  expect(errorImporte('1.001')).toBe('El importe no puede tener más de 2 decimales.');
  expect(errorImporte('10000000000000000')).toBe(
    'El importe no puede superar 9999999999999999.99.',
  );
  expect(errorImporte('9999999999999999.99')).toContain('precisión');
});
it('valida fecha real, moneda ASCII y campos de flete', () => {
  expect(validarFlete(flete)).toEqual({});
  expect(
    validarFlete({ ...flete, fecha: '2026-02-30', moneda: 'P3N', transportistaId: 0 }),
  ).toMatchObject({
    fecha: 'La fecha del flete no es válida.',
    moneda: 'La moneda debe tener tres letras.',
    transportistaId: 'El transportista es obligatorio.',
  });
  expect(
    Object.keys(
      validarFlete({
        ...flete,
        referencia: '',
        origen: '',
        destino: '',
        observaciones: 'x'.repeat(501),
      }),
    ),
  ).toHaveLength(4);
  expect(
    validarFlete({
      ...flete,
      referencia: 'x'.repeat(31),
      origen: 'x'.repeat(121),
      destino: 'x'.repeat(121),
    }),
  ).toHaveProperty('referencia', 'La referencia del flete no puede superar 30 caracteres.');
});
it('exige motivo de cancelación y admite exactamente 500 caracteres', () => {
  expect(validarMotivo(' ')).toBe('El motivo de cancelación es obligatorio.');
  expect(validarMotivo('x'.repeat(501))).toBe(
    'El motivo de cancelación no puede superar 500 caracteres.',
  );
  expect(validarMotivo('x'.repeat(500))).toBe('');
});
