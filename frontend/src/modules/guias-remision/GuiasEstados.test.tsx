import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { ApiError } from '../../api/client';
import { guia, prepararGuias, esperarCargaInicialGuias } from './guias.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
async function seleccionar() {
  prepararGuias();
  await esperarCargaInicialGuias();
  fireEvent.click(screen.getByRole('button', { name: 'Ver guía T001-00000001' }));
  await screen.findByRole('heading', { name: 'T001-00000001' });
}
it('exige confirmar emisión, permite Escape y conserva ficha con 409', async () => {
  await seleccionar();
  const boton = screen.getByRole('button', { name: 'Emitir internamente' });
  boton.focus();
  fireEvent.click(boton);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(boton).toHaveFocus());
  expect(api.emitirGuia).not.toHaveBeenCalled();
  fireEvent.click(boton);
  vi.mocked(api.emitirGuia).mockRejectedValueOnce(
    new ApiError(409, 'Solo se puede emitir una guía en borrador.'),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar emisión interna' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Solo se puede emitir');
  expect(screen.getByRole('heading', { name: 'T001-00000001' })).toBeInTheDocument();
});
it('emite una vez aunque se repita el clic y muestra solo anulación', async () => {
  await seleccionar();
  let resolver!: (d: { id: number }) => void;
  vi.mocked(api.emitirGuia).mockReturnValue(
    new Promise((r) => {
      resolver = r;
    }),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Emitir internamente' }));
  const confirmar = screen.getByRole('button', {
    name: 'Confirmar emisión interna',
  });
  fireEvent.click(confirmar);
  fireEvent.click(confirmar);
  expect(api.emitirGuia).toHaveBeenCalledTimes(1);
  vi.mocked(api.obtenerGuia).mockResolvedValue({
    ...guia,
    estado: 'Emitida',
    fechaEmision: '2026-11-21T12:00:00Z',
  });
  await act(async () => resolver({ id: 1 }));
  await waitFor(() =>
    expect(screen.queryByRole('button', { name: 'Editar guía' })).not.toBeInTheDocument(),
  );
  expect(screen.getByRole('button', { name: 'Anular guía' })).toBeInTheDocument();
});
it('valida motivo y anula manteniendo el historial', async () => {
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Anular guía' }));
  const dialogo = within(screen.getByRole('dialog'));
  expect(dialogo.getByLabelText('Motivo de anulación')).toHaveFocus();
  fireEvent.click(dialogo.getByRole('button', { name: 'Anular guía' }));
  expect(api.anularGuia).not.toHaveBeenCalled();
  expect(screen.getByText('El motivo de anulación es obligatorio.')).toBeInTheDocument();
  fireEvent.change(dialogo.getByLabelText('Motivo de anulación'), {
    target: { value: ' Cambio ' },
  });
  vi.mocked(api.obtenerGuia).mockResolvedValue({
    ...guia,
    estado: 'Anulada',
    fechaEmision: '2026-11-21T11:00:00Z',
    fechaAnulacion: '2026-11-21T12:00:00Z',
    motivoAnulacion: 'Cambio',
  });
  fireEvent.click(dialogo.getByRole('button', { name: 'Anular guía' }));
  await waitFor(() => expect(api.anularGuia).toHaveBeenCalledWith(1, 'Cambio'));
  await screen.findByText('Cambio');
  expect(screen.queryByRole('button', { name: 'Editar guía' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Anular guía' })).not.toBeInTheDocument();
  expect(screen.getByText('Emisión interna (UTC)')).toBeInTheDocument();
});
it('cierra baja con Escape sin borrar, devuelve foco y permite baja real', async () => {
  await seleccionar();
  const boton = screen.getByRole('button', { name: 'Eliminar guía' });
  boton.focus();
  fireEvent.click(boton);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(boton).toHaveFocus());
  expect(api.eliminarGuia).not.toHaveBeenCalled();
  fireEvent.click(boton);
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', {
      name: 'Eliminar guía',
    }),
  );
  await waitFor(() => expect(api.eliminarGuia).toHaveBeenCalledWith(1));
  expect(
    await screen.findByText('Selecciona una guía de remisión para ver el detalle.'),
  ).toBeInTheDocument();
});
