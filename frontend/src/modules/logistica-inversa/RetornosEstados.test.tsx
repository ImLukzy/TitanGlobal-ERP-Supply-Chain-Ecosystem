import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { recogido, prepararRetornos, esperarCargaInicialRetornos } from './retornos.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
async function seleccionar(id = 1) {
  prepararRetornos();
  await esperarCargaInicialRetornos();
  fireEvent.click(screen.getByRole('button', { name: `Ver retorno RET-${id}` }));
  await screen.findByRole('heading', { name: `RET-${id}` });
}
function rellenar(cantidad = '9.125') {
  fireEvent.change(screen.getByLabelText('Cantidad', { exact: true }), {
    target: { value: cantidad },
  });
  fireEvent.change(screen.getByLabelText('Responsable'), { target: { value: ' Ana ' } });
}
it('Escape no recoge y regresa el foco', async () => {
  await seleccionar();
  const b = screen.getByRole('button', { name: 'Registrar recogida' });
  b.focus();
  fireEvent.click(b);
  expect(screen.getByLabelText('Cantidad', { exact: true })).toHaveFocus();
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Shift', shiftKey: true });
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
  expect(api.recogerRetorno).not.toHaveBeenCalled();
});
it('valida cantidad máxima y responsable antes de pedir', async () => {
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Registrar recogida' }));
  rellenar('11');
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar recogida' }));
  expect(
    screen.getByText('La cantidad recogida no puede superar la programada.'),
  ).toBeInTheDocument();
  expect(api.recogerRetorno).not.toHaveBeenCalled();
});
it('409 conserva datos y foco para poder cerrar con Escape', async () => {
  await seleccionar();
  const b = screen.getByRole('button', { name: 'Registrar recogida' });
  b.focus();
  fireEvent.click(b);
  rellenar();
  vi.mocked(api.recogerRetorno).mockRejectedValueOnce(new ApiError(409, 'Estado cambiado'));
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar recogida' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Estado cambiado');
  expect(screen.getByLabelText('Cantidad', { exact: true })).toHaveValue('9.125');
  expect(screen.getByRole('dialog')).toHaveFocus();
  fireEvent.keyDown(document.activeElement!, { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
});
it('bloquea doble recogida y cierre mientras guarda', async () => {
  await seleccionar();
  let resolver!: (d: { id: number }) => void;
  vi.mocked(api.recogerRetorno).mockReturnValue(
    new Promise((r) => {
      resolver = r;
    }),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Registrar recogida' }));
  rellenar();
  const boton = screen.getByRole('button', { name: 'Confirmar recogida' });
  fireEvent.click(boton);
  fireEvent.click(boton);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  expect(api.recogerRetorno).toHaveBeenCalledTimes(1);
  expect(screen.getByRole('dialog')).toHaveFocus();
  vi.mocked(api.obtenerRetorno).mockResolvedValue({ ...recogido, id: 1, codigo: 'RET-1' });
  await act(async () => resolver({ id: 1 }));
  expect(await screen.findByRole('button', { name: 'Registrar recepción' })).toBeInTheDocument();
});
it('recibe cantidades y muestra diferencias sin acciones de terminal', async () => {
  await seleccionar(2);
  fireEvent.click(screen.getByRole('button', { name: 'Registrar recepción' }));
  rellenar('8.001');
  vi.mocked(api.obtenerRetorno).mockResolvedValue({
    ...recogido,
    estado: 'Recibido',
    cantidadRecibida: '8.001',
    noRecibida: '1.124',
    fechaRecepcion: '2026-11-21T13:00:00Z',
    responsableRecepcion: 'Ana',
  });
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar recepción' }));
  expect(await screen.findByText('1.124 unidad')).toBeInTheDocument();
  expect(api.recibirRetorno).toHaveBeenCalledWith(2, { cantidad: 8.001, responsable: 'Ana' });
  expect(screen.queryByRole('button', { name: 'Cancelar retorno' })).not.toBeInTheDocument();
  expect(screen.getByText('Recogida (UTC)')).toBeInTheDocument();
});
it('cancela con motivo conservando recogida y cantidades', async () => {
  await seleccionar(2);
  fireEvent.click(screen.getByRole('button', { name: 'Cancelar retorno' }));
  const motivo = screen.getByLabelText('Motivo de cancelación');
  expect(motivo).toHaveFocus();
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar cancelación' }));
  expect(api.cancelarRetorno).not.toHaveBeenCalled();
  fireEvent.change(motivo, { target: { value: ' Cambio ' } });
  vi.mocked(api.obtenerRetorno).mockResolvedValue({
    ...recogido,
    estado: 'Cancelado',
    motivoCancelacion: 'Cambio',
    fechaCancelacion: '2026-11-21T13:00:00Z',
  });
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar cancelación' }));
  await waitFor(() =>
    expect(
      within(screen.getByRole('region', { name: 'RET-2' })).getByText('Cambio'),
    ).toBeInTheDocument(),
  );
  expect(screen.getByText('9.125 unidad')).toBeInTheDocument();
  expect(screen.getByText('Recogida (UTC)')).toBeInTheDocument();
});
it('baja accesible no escribe con Escape y elimina tras confirmar', async () => {
  await seleccionar();
  const b = screen.getByRole('button', { name: 'Eliminar retorno' });
  b.focus();
  fireEvent.click(b);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
  expect(api.eliminarRetorno).not.toHaveBeenCalled();
  fireEvent.click(b);
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar retorno' }),
  );
  expect(await screen.findByText('Selecciona un retorno para ver el detalle.')).toBeInTheDocument();
});
