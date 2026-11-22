import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { vale, confirmado, prepararVales, esperarCargaInicialVales } from './vales.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
async function seleccionar() {
  prepararVales();
  await esperarCargaInicialVales();
  fireEvent.click(screen.getByRole('button', { name: 'Ver vale VAL-1' }));
  await screen.findByRole('heading', { name: 'VAL-1' });
}
it('Escape no confirma y 409 conserva la ficha para completar intervalo', async () => {
  await seleccionar();
  const b = screen.getByRole('button', { name: 'Confirmar vale' });
  b.focus();
  fireEvent.click(b);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
  expect(api.confirmarVale).not.toHaveBeenCalled();
  fireEvent.click(b);
  vi.mocked(api.confirmarVale).mockRejectedValueOnce(
    new ApiError(409, 'Registra el odómetro final antes de confirmar el vale.'),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar intervalo' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Registra el odómetro final');
  expect(screen.getByRole('heading', { name: 'VAL-1' })).toBeInTheDocument();
});
it('bloquea doble confirmación y muestra solo anulación', async () => {
  await seleccionar();
  let resolver!: (d: { id: number }) => void;
  vi.mocked(api.confirmarVale).mockReturnValue(
    new Promise((r) => {
      resolver = r;
    }),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar vale' }));
  const boton = screen.getByRole('button', { name: 'Confirmar intervalo' });
  fireEvent.click(boton);
  fireEvent.click(boton);
  expect(api.confirmarVale).toHaveBeenCalledTimes(1);
  vi.mocked(api.obtenerVale).mockResolvedValue({ ...confirmado, id: 1, codigo: 'VAL-1' });
  await act(async () => resolver({ id: 1 }));
  await waitFor(() =>
    expect(screen.queryByRole('button', { name: 'Editar vale' })).not.toBeInTheDocument(),
  );
  expect(screen.getByRole('button', { name: 'Anular vale' })).toBeInTheDocument();
});
it('exige motivo y conserva confirmación e importe al anular', async () => {
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Anular vale' }));
  const d = within(screen.getByRole('dialog'));
  expect(d.getByLabelText('Motivo de anulación')).toHaveFocus();
  fireEvent.click(d.getByRole('button', { name: 'Confirmar anulación' }));
  expect(api.anularVale).not.toHaveBeenCalled();
  expect(screen.getByText('El motivo de anulación es obligatorio.')).toBeInTheDocument();
  fireEvent.change(d.getByLabelText('Motivo de anulación'), { target: { value: ' Cambio ' } });
  vi.mocked(api.obtenerVale).mockResolvedValue({
    ...vale,
    estado: 'Anulado',
    fechaConfirmacion: '2026-11-21T12:00:00Z',
    fechaAnulacion: '2026-11-21T13:00:00Z',
    motivoAnulacion: 'Cambio',
  });
  fireEvent.click(d.getByRole('button', { name: 'Confirmar anulación' }));
  await screen.findByText('Cambio');
  expect(api.anularVale).toHaveBeenCalledWith(1, 'Cambio');
  expect(await screen.findByText('Confirmado (UTC)')).toBeInTheDocument();
  expect(
    within(screen.getByRole('region', { name: 'VAL-1' })).getByText('256.25 PEN'),
  ).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Anular vale' })).not.toBeInTheDocument();
});
it('Escape en baja no elimina y la baja confirmada limpia selección', async () => {
  await seleccionar();
  const b = screen.getByRole('button', { name: 'Eliminar vale' });
  b.focus();
  fireEvent.click(b);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
  expect(api.eliminarVale).not.toHaveBeenCalled();
  fireEvent.click(b);
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar vale' }),
  );
  await waitFor(() => expect(api.eliminarVale).toHaveBeenCalledWith(1));
  expect(await screen.findByText('Selecciona un vale para ver el detalle.')).toBeInTheDocument();
});
