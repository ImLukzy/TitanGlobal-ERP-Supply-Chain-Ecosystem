import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { valores, prepararRetornos, esperarCargaInicialRetornos } from './retornos.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
async function nuevo() {
  prepararRetornos();
  await esperarCargaInicialRetornos();
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo retorno' }));
  for (const [l, v] of [
    ['Código', valores.codigo],
    ['Referencia de origen', valores.referenciaOrigen],
    ['Contraparte', valores.contraparte],
    ['Punto de recogida', valores.puntoRecogida],
    ['Punto de recepción', valores.puntoRecepcion],
    ['Descripción', valores.descripcion],
    ['Unidad', valores.unidad],
    ['Fecha programada (UTC)', valores.fechaProgramada],
    ['Cantidad programada', valores.cantidadProgramada],
  ])
    fireEvent.change(screen.getByLabelText(l), { target: { value: v } });
}
it('alta exacta con UTC y cantidad numérica', async () => {
  await nuevo();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar programación' }));
  await waitFor(() =>
    expect(api.crearRetorno).toHaveBeenCalledWith({
      ...valores,
      fechaProgramada: valores.fechaProgramada + 'Z',
      cantidadProgramada: 10.125,
      observaciones: undefined,
    }),
  );
});
it('error409 preserva formulario y no pierde milisegundos', async () => {
  await nuevo();
  vi.mocked(api.crearRetorno).mockRejectedValueOnce(new ApiError(409, 'Código ocupado'));
  fireEvent.click(screen.getByRole('button', { name: 'Guardar programación' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Código ocupado');
  expect(screen.getByLabelText('Código')).toHaveValue('RET-1');
  expect(screen.getByLabelText('Fecha programada (UTC)')).toHaveValue(valores.fechaProgramada);
});
it('edita sin desplazar UTC al huso local', async () => {
  prepararRetornos();
  await esperarCargaInicialRetornos();
  fireEvent.click(screen.getByRole('button', { name: 'Ver retorno RET-1' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Editar retorno' }));
  expect(screen.getByLabelText('Fecha programada (UTC)')).toHaveValue(valores.fechaProgramada);
  fireEvent.click(screen.getByRole('button', { name: 'Guardar programación' }));
  await waitFor(() =>
    expect(api.actualizarRetorno).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ fechaProgramada: valores.fechaProgramada + 'Z' }),
    ),
  );
});
it('validación local impide enviar y enfoca el primer campo', async () => {
  prepararRetornos();
  await esperarCargaInicialRetornos();
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo retorno' }));
  fireEvent.click(screen.getByRole('button', { name: 'Guardar programación' }));
  expect(api.crearRetorno).not.toHaveBeenCalled();
  await waitFor(() => expect(screen.getByLabelText('Código')).toHaveFocus());
  expect(screen.getByLabelText('Código')).toHaveAttribute('aria-invalid', 'true');
});
