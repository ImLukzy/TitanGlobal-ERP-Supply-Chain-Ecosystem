import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { carga, confirmada, prepararCargas, renderCargas } from './cargas.testUtils';
import { DialogoCancelarCarga } from './DialogoCancelarCarga';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
it('muestra errores de picking/packing al confirmar sin perder el detalle', async () => {
  prepararCargas();
  vi.mocked(api.confirmarCarga).mockRejectedValue(
    new ApiError(409, 'Completa el picking de todas las líneas antes de confirmar.'),
  );
  renderCargas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver carga C01' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Confirmar carga' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Completa el picking');
  expect(screen.getByRole('heading', { name: 'Carga Norte' })).toBeInTheDocument();
});
it('confirma una vez y deja de ofrecer edición y baja', async () => {
  prepararCargas();
  let resolver!: (r: { id: number }) => void;
  vi.mocked(api.confirmarCarga).mockReturnValue(
    new Promise((resolve) => {
      resolver = resolve;
    }),
  );
  renderCargas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver carga C01' }));
  const boton = await screen.findByRole('button', { name: 'Confirmar carga' });
  fireEvent.click(boton);
  fireEvent.click(boton);
  expect(api.confirmarCarga).toHaveBeenCalledTimes(1);
  vi.mocked(api.obtenerCarga).mockResolvedValue({ ...confirmada, id: 1, nombre: 'Carga Norte' });
  await act(async () => resolver({ id: 1 }));
  await waitFor(() =>
    expect(screen.queryByRole('button', { name: 'Editar carga' })).not.toBeInTheDocument(),
  );
  expect(screen.queryByRole('button', { name: 'Eliminar carga' })).not.toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Cancelar carga' })).toBeInTheDocument();
});
it('cancela con motivo y muestra historial sin nuevas acciones de escritura', async () => {
  prepararCargas();
  renderCargas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver carga C02' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Cancelar carga' }));
  fireEvent.change(screen.getByLabelText('Motivo de cancelación'), {
    target: { value: ' Cambio de plan ' },
  });
  vi.mocked(api.obtenerCarga).mockResolvedValue({
    ...confirmada,
    estado: 'Cancelada',
    motivoCancelacion: 'Cambio de plan',
    fechaCancelacion: '2026-11-21T13:00:00Z',
  });
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Cancelar carga' }),
  );
  await waitFor(() =>
    expect(screen.queryByRole('button', { name: 'Cancelar carga' })).not.toBeInTheDocument(),
  );
  expect(screen.getByText('Cambio de plan')).toBeInTheDocument();
  expect(api.cancelarCarga).toHaveBeenCalledWith(2, 'Cambio de plan');
});
it('cancelación conserva motivo ante un error y tiene foco, Tab y Escape', () => {
  const cerrar = vi.fn();
  const confirmar = vi.fn();
  const origen = document.createElement('button');
  document.body.append(origen);
  origen.focus();
  const vista = render(
    <DialogoCancelarCarga
      referencia="C1"
      ocupado={false}
      error="Conflicto"
      cerrar={cerrar}
      confirmar={confirmar}
    />,
  );
  const motivo = screen.getByLabelText('Motivo de cancelación');
  expect(motivo).toHaveFocus();
  fireEvent.click(screen.getByRole('button', { name: 'Cancelar carga' }));
  expect(screen.getByText('El motivo de cancelación es obligatorio.')).toBeInTheDocument();
  expect(confirmar).not.toHaveBeenCalled();
  fireEvent.change(motivo, { target: { value: 'Se conserva' } });
  fireEvent.keyDown(motivo, { key: 'Tab', shiftKey: true });
  expect(screen.getByRole('button', { name: 'Cancelar carga' })).toHaveFocus();
  fireEvent.keyDown(document.activeElement!, { key: 'Tab' });
  expect(motivo).toHaveFocus();
  fireEvent.keyDown(motivo, { key: 'Escape' });
  expect(cerrar).toHaveBeenCalled();
  expect(motivo).toHaveValue('Se conserva');
  vista.unmount();
  expect(origen).toHaveFocus();
  origen.remove();
});
it('baja fallida conserva diálogo y Escape devuelve el foco', async () => {
  prepararCargas();
  vi.mocked(api.eliminarCarga).mockRejectedValue(
    new ApiError(409, 'No se puede eliminar la consolidación porque está en uso.'),
  );
  renderCargas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver carga C01' }));
  const boton = await screen.findByRole('button', { name: 'Eliminar carga' });
  boton.focus();
  fireEvent.click(boton);
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar carga' }),
  );
  expect(await screen.findByRole('alert')).toHaveTextContent('porque está en uso');
  fireEvent.keyDown(document, { key: 'Escape' });
  await waitFor(() => expect(boton).toHaveFocus());
});
it('elimina borrador y corrige última página vacía sin detalle atascado', async () => {
  prepararCargas();
  vi.mocked(api.listarCargas).mockResolvedValue({
    items: [carga],
    total: 11,
    pagina: 1,
    tamanoPagina: 10,
  });
  renderCargas();
  fireEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));
  await waitFor(() =>
    expect(api.listarCargas).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 2 })),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Ver carga C01' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Eliminar carga' }));
  vi.mocked(api.listarCargas).mockImplementation(async (f) => ({
    items: f.pagina === 2 ? [] : [confirmada],
    total: 10,
    pagina: f.pagina!,
    tamanoPagina: 10,
  }));
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar carga' }),
  );
  await waitFor(() =>
    expect(api.listarCargas).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 1 })),
  );
  expect(screen.getByText('Selecciona una consolidación para ver el detalle.')).toBeInTheDocument();
});
