import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import {
  sucursal,
  inactiva,
  prepararSucursales,
  esperarCargaInicialSucursales,
} from './sucursales.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
async function seleccionar(id = 1) {
  prepararSucursales();
  await esperarCargaInicialSucursales();
  fireEvent.click(screen.getByRole('button', { name: `Ver sucursal SUC-${id}` }));
  await screen.findByRole('heading', { name: `SUC-${id}` });
}
it('activa no muestra baja y Escape no desactiva', async () => {
  await seleccionar();
  expect(screen.queryByRole('button', { name: 'Eliminar sucursal' })).not.toBeInTheDocument();
  const b = screen.getByRole('button', { name: 'Desactivar sucursal' });
  b.focus();
  fireEvent.click(b);
  expect(screen.getByRole('button', { name: 'Volver' })).toHaveFocus();
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
  expect(api.desactivarSucursal).not.toHaveBeenCalled();
});
it('Tab y ShiftTab quedan dentro de la confirmación', async () => {
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Desactivar sucursal' }));
  const primero = screen.getByRole('button', { name: 'Volver' }),
    ultimo = screen.getByRole('button', { name: 'Confirmar desactivación' });
  fireEvent.keyDown(primero, { key: 'Tab', shiftKey: true });
  expect(ultimo).toHaveFocus();
  fireEvent.keyDown(ultimo, { key: 'Tab' });
  expect(primero).toHaveFocus();
});
it('409 conserva ficha y foco para cerrar', async () => {
  await seleccionar();
  const b = screen.getByRole('button', { name: 'Desactivar sucursal' });
  b.focus();
  fireEvent.click(b);
  vi.mocked(api.desactivarSucursal).mockRejectedValueOnce(new ApiError(409, 'Estado cambiado'));
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar desactivación' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Estado cambiado');
  expect(screen.getByRole('dialog')).toHaveFocus();
  fireEvent.keyDown(document.activeElement!, { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
  expect(screen.getByText('Sucursal Norte', { selector: 'dd' })).toBeInTheDocument();
});
it('bloquea doble envío, Tab y cierre mientras guarda', async () => {
  await seleccionar();
  let resolver!: (d: { id: number }) => void;
  vi.mocked(api.desactivarSucursal).mockReturnValue(
    new Promise((r) => {
      resolver = r;
    }),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Desactivar sucursal' }));
  const b = screen.getByRole('button', { name: 'Confirmar desactivación' });
  fireEvent.click(b);
  fireEvent.click(b);
  const d = screen.getByRole('dialog');
  expect(d).toHaveFocus();
  expect(fireEvent.keyDown(d, { key: 'Tab' })).toBe(false);
  expect(fireEvent.keyDown(d, { key: 'Tab', shiftKey: true })).toBe(false);
  fireEvent.keyDown(d, { key: 'Escape' });
  expect(api.desactivarSucursal).toHaveBeenCalledTimes(1);
  expect(d).toBeInTheDocument();
  vi.mocked(api.obtenerSucursal).mockResolvedValue({
    ...sucursal,
    activa: false,
    fechaCambioEstado: '2026-11-22T12:00:00Z',
  });
  await act(async () => resolver({ id: 1 }));
  expect(await screen.findByRole('button', { name: 'Activar sucursal' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Eliminar sucursal' })).toBeInTheDocument();
});
it('reactiva sin perder datos y oculta baja', async () => {
  await seleccionar(2);
  fireEvent.click(screen.getByRole('button', { name: 'Activar sucursal' }));
  vi.mocked(api.obtenerSucursal).mockResolvedValue({
    ...inactiva,
    activa: true,
    fechaCambioEstado: '2026-11-22T13:00:00Z',
  });
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar activación' }));
  expect(await screen.findByRole('button', { name: 'Desactivar sucursal' })).toBeInTheDocument();
  expect(api.activarSucursal).toHaveBeenCalledWith(2);
  expect(screen.queryByRole('button', { name: 'Eliminar sucursal' })).not.toBeInTheDocument();
  expect(screen.getByText('Avenida Norte 123')).toBeInTheDocument();
  expect(screen.getByText('Último cambio de estado (UTC)')).toBeInTheDocument();
});
it('baja inactiva no escribe con Escape y limpia selección al confirmar', async () => {
  await seleccionar(2);
  const b = screen.getByRole('button', { name: 'Eliminar sucursal' });
  b.focus();
  fireEvent.click(b);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
  expect(api.eliminarSucursal).not.toHaveBeenCalled();
  fireEvent.click(b);
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar sucursal' }),
  );
  expect(
    await screen.findByText('Selecciona una sucursal para ver el detalle.'),
  ).toBeInTheDocument();
});
