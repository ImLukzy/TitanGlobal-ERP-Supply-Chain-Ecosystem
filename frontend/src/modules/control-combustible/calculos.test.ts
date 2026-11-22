import { expect, it } from 'vitest';
import { previsualizar } from './calculos';
it.each([
  ['50', '5.125', '500', '256.25', '10.000', '10.000'],
  ['1', '1.005', '1', '1.01', '1.000', '100.000'],
  ['16', '1', '1', '16.00', '0.063', '1600.000'],
  ['0.001', '1', '200', '0.00', '200000.000', '0.001'],
  ['9999999.999', '9999999.999', '999999999', '99999999980000.00', '100.000', '1.000'],
  ['0.001', '0.001', '999999999', '0.00', '999999999000.000', '0.000'],
  ['9999999.999', '1', '1', '10000000.00', '0.000', '999999999.900'],
])(
  'coincide con decimal del servidor: %s L',
  (litros, precioLitro, odometroFinal, importe, kmPorLitro, litrosPor100Km) => {
    expect(previsualizar({ litros, precioLitro, odometroInicial: '0', odometroFinal })).toEqual({
      importe,
      kilometrosRecorridos: Number(odometroFinal),
      kmPorLitro,
      litrosPor100Km,
    });
  },
);
it('mantiene pendiente el intervalo y admite ceros no significativos', () => {
  expect(
    previsualizar({
      litros: '050.0000',
      precioLitro: '5.1250',
      odometroInicial: '12000',
      odometroFinal: '',
    }),
  ).toEqual({
    importe: '256.25',
    kilometrosRecorridos: null,
    kmPorLitro: null,
    litrosPor100Km: null,
  });
});
it.each([
  ['0', '1', '0', '1'],
  ['1.0001', '1', '0', '1'],
  ['1', '1e3', '0', '1'],
  ['1', '1', '2', '1'],
  ['1', '1', '1', '1'],
  ['1', '1', '-1', '1'],
  ['1', '1', '0', '1000000000'],
  ['10000000', '1', '0', '1'],
])('no calcula entradas inválidas %s', (litros, precioLitro, odometroInicial, odometroFinal) => {
  expect(previsualizar({ litros, precioLitro, odometroInicial, odometroFinal })).toBeNull();
});
