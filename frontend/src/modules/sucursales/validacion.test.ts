import { expect, it } from 'vitest';
import { camposTexto, camposOpcionales, validarSucursal } from './validacion';
import { valores } from './sucursales.testUtils';
it.each(camposTexto)('valida %s y su límite', (c, _l, m, t) => {
  expect(validarSucursal({ ...valores, [c]: ' ' })[c]).toBe(`El campo ${t} es obligatorio.`);
  expect(validarSucursal({ ...valores, [c]: 'x'.repeat(m + 1) })[c]).toBe(
    `El campo ${t} no puede superar ${m} caracteres.`,
  );
  expect(validarSucursal({ ...valores, [c]: 'x'.repeat(m) })[c]).toBeUndefined();
});
it.each(camposOpcionales)('valida opcional %s', (c, _l, m, msg) => {
  expect(validarSucursal({ ...valores, [c]: 'x'.repeat(m + 1) })[c]).toBe(msg);
  const t = c === 'email' ? 'a'.repeat(138) + '@example.com' : 'x'.repeat(m);
  expect(validarSucursal({ ...valores, [c]: t })[c]).toBeUndefined();
  expect(validarSucursal({ ...valores, [c]: ' ' })[c]).toBeUndefined();
});
it.each(['sin-arroba', 'a@@b', 'Nombre <a@b>', 'a b@c', '@b'])('rechaza email %s', (email) =>
  expect(validarSucursal({ ...valores, email }).email).toBe('El email no es válido.'),
);
it.each(['a@b', 'persona@example.com', ' a@b '])('admite formato básico %s', (email) =>
  expect(validarSucursal({ ...valores, email }).email).toBeUndefined(),
);
it('tipo y fecha de apertura opcional', () => {
  expect(validarSucursal({ ...valores, tipo: 'Otro' as typeof valores.tipo }).tipo).toBe(
    'El tipo debe ser Filial, Planta u Oficina.',
  );
  expect(validarSucursal({ ...valores, fechaApertura: '' }).fechaApertura).toBeUndefined();
  expect(validarSucursal({ ...valores, fechaApertura: '0001-01-01T00:00' }).fechaApertura).toBe(
    'La fecha de apertura no es válida.',
  );
  expect(
    validarSucursal({ ...valores, fechaApertura: '0001-01-01T00:00:00.001' }).fechaApertura,
  ).toBeUndefined();
  expect(validarSucursal({ ...valores, fechaApertura: 'inválida' }).fechaApertura).toBe(
    'La fecha de apertura no es válida.',
  );
});
