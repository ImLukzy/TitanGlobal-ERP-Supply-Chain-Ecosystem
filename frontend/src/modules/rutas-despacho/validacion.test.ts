import { expect, it } from 'vitest';
import { validarRuta, type ValoresRuta } from './validacion';
const datos: ValoresRuta = {
  codigo: 'R1',
  nombre: 'Ruta',
  zona: 'Centro',
  origen: 'Lima',
  destino: 'Callao',
  distanciaKm: '25.50',
  peajeEstimado: '0',
  moneda: 'PEN',
  activo: true,
  observaciones: '',
};
it.each([
  ['codigo', 'El código de la ruta es obligatorio.'],
  ['nombre', 'El nombre de la ruta es obligatorio.'],
  ['zona', 'La zona es obligatoria.'],
  ['origen', 'El origen es obligatorio.'],
  ['destino', 'El destino es obligatorio.'],
] as const)('exige %s con el mensaje del backend', (campo, mensaje) => {
  expect(validarRuta({ ...datos, [campo]: ' ' })[campo]).toBe(mensaje);
});
it.each([
  ['codigo', 20, 'El código de la ruta no puede superar 20 caracteres.'],
  ['nombre', 100, 'El nombre de la ruta no puede superar 100 caracteres.'],
  ['zona', 100, 'La zona no puede superar 100 caracteres.'],
  ['origen', 120, 'El origen no puede superar 120 caracteres.'],
  ['destino', 120, 'El destino no puede superar 120 caracteres.'],
  ['observaciones', 500, 'Las observaciones no pueden superar 500 caracteres.'],
] as const)('admite exactamente el límite de %s', (campo, limite, mensaje) => {
  expect(validarRuta({ ...datos, [campo]: 'x'.repeat(limite) })[campo]).toBeUndefined();
  expect(validarRuta({ ...datos, [campo]: 'x'.repeat(limite + 1) })[campo]).toBe(mensaje);
});
it.each([
  ['distanciaKm', '0', 'La distancia debe ser mayor que cero.'],
  ['distanciaKm', '-1', 'La distancia debe ser mayor que cero.'],
  ['distanciaKm', '100000000', 'La distancia no puede superar 99999999.99 km.'],
  ['distanciaKm', '99999999.999', 'La distancia no puede superar 99999999.99 km.'],
  ['distanciaKm', '0.001', 'La distancia no puede tener más de 2 decimales.'],
  ['peajeEstimado', '-0.01', 'El peaje estimado no puede ser negativo.'],
  ['peajeEstimado', '10000000000000000', 'El peaje estimado no puede superar 9999999999999999.99.'],
  [
    'peajeEstimado',
    '9999999999999999.999',
    'El peaje estimado no puede superar 9999999999999999.99.',
  ],
  ['peajeEstimado', '1.001', 'El peaje estimado no puede tener más de 2 decimales.'],
] as const)('valida %s = %s sin redondear', (campo, valor, mensaje) => {
  expect(validarRuta({ ...datos, [campo]: valor })[campo]).toBe(mensaje);
});
it.each(['', 'NaN', 'Infinity', '1e3', '1,25'])(
  'rechaza distancia no decimal %s',
  (distanciaKm) => {
    expect(validarRuta({ ...datos, distanciaKm }).distanciaKm).toBeDefined();
  },
);
it('admite peaje cero y ceros finales sin decimales significativos adicionales', () => {
  expect(validarRuta(datos)).toEqual({});
  expect(
    validarRuta({
      ...datos,
      distanciaKm: '99999999.9900',
      peajeEstimado: '000.0000',
      moneda: ' usd ',
    }),
  ).toEqual({});
});
it('detecta peajes que perderían precisión en el navegador', () => {
  expect(validarRuta({ ...datos, peajeEstimado: '9999999999999999.99' }).peajeEstimado).toBe(
    'El peaje estimado no puede enviarse con precisión desde este navegador. Ingresa una cantidad menor.',
  );
});
it.each(['P3N', 'PÉN', 'PENN', ''])('valida moneda ASCII %s', (moneda) => {
  expect(validarRuta({ ...datos, moneda }).moneda).toBe('La moneda debe tener tres letras.');
});
