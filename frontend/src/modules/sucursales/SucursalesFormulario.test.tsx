import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import {
  valores,
  inactiva,
  prepararSucursales,
  esperarCargaInicialSucursales,
} from './sucursales.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
async function nuevo() {
  prepararSucursales();
  await esperarCargaInicialSucursales();
  fireEvent.click(screen.getByRole('button', { name: 'Nueva sucursal' }));
  for (const [l, v] of [
    ['Código', valores.codigo],
    ['Nombre', valores.nombre],
    ['País', valores.pais],
    ['Ciudad', valores.ciudad],
    ['Dirección', valores.direccion],
  ])
    fireEvent.change(screen.getByLabelText(l), { target: { value: v } });
}
it('alta envía opcionales null y no expone estado editable', async () => {
  await nuevo();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar sucursal' }));
  await waitFor(() =>
    expect(api.crearSucursal).toHaveBeenCalledWith({
      ...valores,
      contacto: null,
      email: null,
      telefono: null,
      observaciones: null,
      fechaApertura: null,
    }),
  );
  expect(api.crearSucursal).toHaveBeenCalledWith(
    expect.not.objectContaining({ activa: expect.anything() }),
  );
});
it('409 conserva formulario y apertura UTC', async () => {
  await nuevo();
  fireEvent.change(screen.getByLabelText('Fecha de apertura (UTC, opcional)'), {
    target: { value: valores.fechaApertura },
  });
  vi.mocked(api.crearSucursal).mockRejectedValueOnce(new ApiError(409, 'Código ocupado'));
  fireEvent.click(screen.getByRole('button', { name: 'Guardar sucursal' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Código ocupado');
  expect(screen.getByLabelText('Código')).toHaveValue('SUC-1');
  expect(screen.getByLabelText('Fecha de apertura (UTC, opcional)')).toHaveValue(
    valores.fechaApertura,
  );
});
it('edita preservando UTC con milisegundos', async () => {
  prepararSucursales();
  await esperarCargaInicialSucursales();
  fireEvent.click(screen.getByRole('button', { name: 'Ver sucursal SUC-1' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Editar sucursal' }));
  expect(screen.getByLabelText('Fecha de apertura (UTC, opcional)')).toHaveValue(
    valores.fechaApertura,
  );
  fireEvent.click(screen.getByRole('button', { name: 'Guardar sucursal' }));
  await waitFor(() =>
    expect(api.actualizarSucursal).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ fechaApertura: valores.fechaApertura + 'Z' }),
    ),
  );
});
it('permite editar una inactiva y limpiar apertura/contacto', async () => {
  prepararSucursales();
  vi.mocked(api.obtenerSucursal).mockResolvedValue({ ...inactiva, contacto: 'Ana' });
  await esperarCargaInicialSucursales();
  fireEvent.click(screen.getByRole('button', { name: 'Ver sucursal SUC-2' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Editar sucursal' }));
  fireEvent.change(screen.getByLabelText('Fecha de apertura (UTC, opcional)'), {
    target: { value: '' },
  });
  fireEvent.change(screen.getByLabelText('Contacto'), { target: { value: ' ' } });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar sucursal' }));
  await waitFor(() =>
    expect(api.actualizarSucursal).toHaveBeenCalledWith(
      2,
      expect.objectContaining({ fechaApertura: null, contacto: null }),
    ),
  );
});
it('datos inválidos no envían y enfocan primer error', async () => {
  prepararSucursales();
  await esperarCargaInicialSucursales();
  fireEvent.click(screen.getByRole('button', { name: 'Nueva sucursal' }));
  fireEvent.click(screen.getByRole('button', { name: 'Guardar sucursal' }));
  expect(api.crearSucursal).not.toHaveBeenCalled();
  await waitFor(() => expect(screen.getByLabelText('Código')).toHaveFocus());
  expect(screen.getByLabelText('Código')).toHaveAttribute('aria-invalid', 'true');
});
