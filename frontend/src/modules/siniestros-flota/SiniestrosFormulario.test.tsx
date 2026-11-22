import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { valores, prepararSiniestros, esperarCargaInicialSiniestros } from './siniestros.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
async function nuevo() {
  prepararSiniestros();
  await esperarCargaInicialSiniestros();
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo siniestro' }));
  for (const [l, v] of [
    ['Código', valores.codigo],
    ['Placa', ' abc-500 '],
    ['Lugar', valores.lugar],
    ['Descripción', valores.descripcion],
    ['Fecha del incidente (UTC)', valores.fechaIncidente],
  ])
    fireEvent.change(screen.getByLabelText(l), { target: { value: v } });
}
it('alta normaliza placa y conserva UTC con milisegundos', async () => {
  await nuevo();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar incidente' }));
  await waitFor(() =>
    expect(api.crearSiniestro).toHaveBeenCalledWith({
      ...valores,
      fechaIncidente: valores.fechaIncidente + 'Z',
      observaciones: undefined,
    }),
  );
});
it('409 preserva campos', async () => {
  await nuevo();
  vi.mocked(api.crearSiniestro).mockRejectedValueOnce(new ApiError(409, 'Código ocupado'));
  fireEvent.click(screen.getByRole('button', { name: 'Guardar incidente' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Código ocupado');
  expect(screen.getByLabelText('Código')).toHaveValue('SIN-1');
  expect(screen.getByLabelText('Fecha del incidente (UTC)')).toHaveValue(valores.fechaIncidente);
});
it('edita sin desplazar UTC', async () => {
  prepararSiniestros();
  await esperarCargaInicialSiniestros();
  fireEvent.click(screen.getByRole('button', { name: 'Ver siniestro SIN-1' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Editar siniestro' }));
  expect(screen.getByLabelText('Fecha del incidente (UTC)')).toHaveValue(valores.fechaIncidente);
  fireEvent.click(screen.getByRole('button', { name: 'Guardar incidente' }));
  await waitFor(() =>
    expect(api.actualizarSiniestro).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ fechaIncidente: valores.fechaIncidente + 'Z' }),
    ),
  );
});
it('validación enfoca primer error sin enviar', async () => {
  prepararSiniestros();
  await esperarCargaInicialSiniestros();
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo siniestro' }));
  fireEvent.click(screen.getByRole('button', { name: 'Guardar incidente' }));
  expect(api.crearSiniestro).not.toHaveBeenCalled();
  await waitFor(() => expect(screen.getByLabelText('Código')).toHaveFocus());
  expect(screen.getByLabelText('Código')).toHaveAttribute('aria-invalid', 'true');
});
