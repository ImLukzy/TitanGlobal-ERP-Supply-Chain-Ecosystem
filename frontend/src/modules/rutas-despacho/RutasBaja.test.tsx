import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { prepararRutas, renderRutas, ruta, rutaDos } from './rutasDespacho.testUtils';
import { ApiError } from '../../api/client';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});

it('conserva el diálogo con el 409 de baja y Escape restaura el foco', async () => {
  prepararRutas();
  vi.mocked(api.eliminarRuta).mockRejectedValue(
    new ApiError(409, 'No se puede eliminar la ruta de despacho porque está en uso.'),
  );
  renderRutas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver ruta R01' }));
  const boton = await screen.findByRole('button', { name: 'Eliminar ruta' });
  boton.focus();
  fireEvent.click(boton);
  const dialogo = screen.getByRole('dialog');
  fireEvent.click(within(dialogo).getByRole('button', { name: 'Eliminar ruta' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('No se puede eliminar');
  fireEvent.keyDown(document, { key: 'Tab', shiftKey: true });
  expect(within(dialogo).getByRole('button', { name: 'Eliminar ruta' })).toHaveFocus();
  fireEvent.keyDown(document, { key: 'Escape' });
  await waitFor(() => expect(boton).toHaveFocus());
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});
it('elimina, limpia el detalle y corrige una última página vacía', async () => {
  prepararRutas();
  vi.mocked(api.listarRutas).mockResolvedValue({
    items: [ruta],
    total: 11,
    pagina: 1,
    tamanoPagina: 10,
  });
  renderRutas();
  fireEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));
  await waitFor(() =>
    expect(api.listarRutas).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 2 })),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Ver ruta R01' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Eliminar ruta' }));
  vi.mocked(api.listarRutas).mockImplementation(async (filtros) => ({
    items: filtros.pagina === 2 ? [] : [rutaDos],
    total: 10,
    pagina: filtros.pagina!,
    tamanoPagina: 10,
  }));
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar ruta' }),
  );
  await waitFor(() =>
    expect(api.listarRutas).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 1 })),
  );
  expect(screen.getByText('Selecciona una ruta para ver el detalle.')).toBeInTheDocument();
  expect(screen.queryByText('Cargando detalle…')).not.toBeInTheDocument();
});
