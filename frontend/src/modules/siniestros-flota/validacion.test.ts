import { expect, it } from 'vitest';
import {
  camposTexto,
  camposReclamo,
  validarSiniestro,
  validarReclamo,
  validarCierre,
  errorMonto,
  validarMotivo,
} from './validacion';
import { valores, valoresReclamo } from './siniestros.testUtils';
it.each(camposTexto)('valida %s y su límite', (c, _l, m, t) => {
  expect(validarSiniestro({ ...valores, [c]: ' ' })[c]).toBe(`El campo ${t} es obligatorio.`);
  expect(validarSiniestro({ ...valores, [c]: 'x'.repeat(m + 1) })[c]).toBe(
    `El campo ${t} no puede superar ${m} caracteres.`,
  );
  expect(validarSiniestro({ ...valores, [c]: 'x'.repeat(m) })[c]).toBeUndefined();
});
it.each(camposReclamo)('valida referencia %s', (c, _l, m, t) => {
  expect(validarReclamo({ ...valoresReclamo, [c]: ' ' })[c]).toBe(`El campo ${t} es obligatorio.`);
  expect(validarReclamo({ ...valoresReclamo, [c]: 'x'.repeat(m + 1) })[c]).toBe(
    `El campo ${t} no puede superar ${m} caracteres.`,
  );
  expect(validarReclamo({ ...valoresReclamo, [c]: 'x'.repeat(m) })[c]).toBeUndefined();
});
it.each(['', '0', '-1', '10000000000', 'NaN', 'Infinity'])('rechaza monto reclamado %s', (v) =>
  expect(errorMonto(v, 'monto reclamado', 0.01)).toBe(
    'El monto reclamado debe estar entre 0.01 y 9999999999.99.',
  ),
);
it.each(['0.01', '9999999999.99', '1.0000'])('admite monto representable %s', (v) =>
  expect(errorMonto(v, 'monto reclamado', 0.01)).toBe(''),
);
it('no redondea entradas', () =>
  expect(errorMonto('1.001', 'monto reclamado', 0.01)).toBe(
    'El monto reclamado no puede tener más de 2 decimales.',
  ));
it.each(['', 'P3N', 'PENN', 'ÅBC'])('moneda inválida %s', (moneda) =>
  expect(validarReclamo({ ...valoresReclamo, moneda }).moneda).toBe(
    'La moneda debe contener 3 letras mayúsculas.',
  ),
);
it('fecha tipo y observaciones', () => {
  expect(validarSiniestro({ ...valores, fechaIncidente: '' }).fechaIncidente).toBe(
    'La fecha del incidente es obligatoria.',
  );
  expect(validarSiniestro({ ...valores, tipo: 'Otro' as typeof valores.tipo }).tipo).toBe(
    'El tipo debe ser Accidente, Robo o DanoMaterial.',
  );
  expect(validarSiniestro({ ...valores, observaciones: 'x'.repeat(501) }).observaciones).toBe(
    'Las observaciones no pueden superar 500 caracteres.',
  );
});
it.each([
  ['Aceptado', '-1', 'El monto reconocido debe estar entre 0 y 9999999999.99.'],
  ['Aceptado', '10000000000', 'El monto reconocido debe estar entre 0 y 9999999999.99.'],
  ['Aceptado', '1.001', 'El monto reconocido no puede tener más de 2 decimales.'],
  ['Aceptado', '1251', 'El monto reconocido no puede superar el reclamado.'],
  ['Aceptado', '0', 'Un reclamo aceptado debe reconocer un importe mayor que cero.'],
  ['Rechazado', '1', 'Un reclamo rechazado debe reconocer un importe de cero.'],
])('valida resultado %s importe %s', (resultado, montoReconocido, mensaje) =>
  expect(
    validarCierre(
      {
        resultado: resultado as 'Aceptado' | 'Rechazado',
        montoReconocido,
        resolucion: 'Referencia manual',
      },
      '1250.50',
    ).montoReconocido,
  ).toBe(mensaje),
);
it('cierre exige resultado y resolución y permite rechazo cero', () => {
  expect(
    validarCierre(
      { resultado: 'Otro' as 'Aceptado', montoReconocido: '1', resolucion: ' ' },
      '1250.50',
    ),
  ).toMatchObject({
    resultado: 'El resultado debe ser Aceptado o Rechazado.',
    resolucion: 'El campo resolución es obligatorio.',
  });
  expect(
    validarCierre(
      { resultado: 'Rechazado', montoReconocido: '0', resolucion: 'x'.repeat(501) },
      '1250.50',
    ).resolucion,
  ).toBe('El campo resolución no puede superar 500 caracteres.');
  expect(
    validarCierre(
      { resultado: 'Rechazado', montoReconocido: '0', resolucion: 'No aceptado' },
      '1250.50',
    ),
  ).toEqual({});
});
it('motivo de anulación', () => {
  expect(validarMotivo(' ')).toBe('El motivo de anulación es obligatorio.');
  expect(validarMotivo('x'.repeat(501))).toBe(
    'El motivo de anulación no puede superar 500 caracteres.',
  );
  expect(validarMotivo('x'.repeat(500))).toBe('');
});
