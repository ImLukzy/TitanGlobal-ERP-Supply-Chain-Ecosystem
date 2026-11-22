import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import {
  entrega,
  enRuta,
  prepararEntregas,
  renderEntregas,
  esperarCargaInicialEntregas,
} from './entregas.testUtils';
import type { EntregaDetalle, PaginaEntregas } from './tipos';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.resetAllMocks();
});
it('consulta una vez y espera 300ms desde el último texto', async () => {
  prepararEntregas();
  vi.useFakeTimers();
  renderEntregas();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarEntregas).toHaveBeenCalledTimes(1);
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Ca' } });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(150);
  });
  fireEvent.change(screen.getByRole('searchbox'), {
    target: { value: 'Centro' },
  });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(299);
  });
  expect(api.listarEntregas).toHaveBeenCalledTimes(1);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(api.listarEntregas).toHaveBeenLastCalledWith(
    expect.objectContaining({ busqueda: 'Centro', pagina: 1 }),
  );
});
it('combina estado y paginación y vuelve a página 1', async () => {
  prepararEntregas();
  vi.mocked(api.listarEntregas).mockResolvedValue({
    items: [entrega],
    total: 11,
    pagina: 1,
    tamanoPagina: 10,
  });
  await esperarCargaInicialEntregas();
  fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
  await waitFor(() =>
    expect(api.listarEntregas).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 2 })),
  );
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'EnRuta' },
  });
  await waitFor(() =>
    expect(api.listarEntregas).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 1, estado: 'EnRuta' }),
    ),
  );
});
it('descarta detalle obsoleto y mantiene la misma selección sin atascarse', async () => {
  prepararEntregas();
  let resolver!: (r: EntregaDetalle) => void;
  vi.mocked(api.obtenerEntrega)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce(enRuta);
  renderEntregas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver entrega ENV-1' }));
  fireEvent.click(screen.getByRole('button', { name: 'Ver entrega ENV-2' }));
  await screen.findByRole('heading', { name: 'ENV-2' });
  await act(async () => resolver(entrega));
  fireEvent.click(screen.getByRole('button', { name: 'Ver entrega ENV-2' }));
  expect(screen.getByRole('heading', { name: 'ENV-2' })).toBeInTheDocument();
  expect(api.obtenerEntrega).toHaveBeenCalledTimes(2);
});
it('descarta listado anterior al cambiar estado', async () => {
  prepararEntregas();
  let resolver!: (r: PaginaEntregas) => void;
  vi.mocked(api.listarEntregas)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce({
      items: [enRuta],
      total: 1,
      pagina: 1,
      tamanoPagina: 10,
    });
  renderEntregas();
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'EnRuta' },
  });
  await screen.findByRole('button', { name: 'Ver entrega ENV-2' });
  await act(async () => resolver({ items: [entrega], total: 1, pagina: 1, tamanoPagina: 10 }));
  expect(screen.queryByRole('button', { name: 'Ver entrega ENV-1' })).not.toBeInTheDocument();
});
it.each(['listado', 'resumen', 'detalle'])(
  'permite reintentar %s de forma independiente',
  async (recurso) => {
    prepararEntregas();
    const consulta =
      recurso === 'listado'
        ? api.listarEntregas
        : recurso === 'resumen'
          ? api.resumenEntregas
          : api.obtenerEntrega;
    vi.mocked(consulta).mockRejectedValueOnce(new Error('red'));
    renderEntregas();
    if (recurso === 'detalle')
      fireEvent.click(await screen.findByRole('button', { name: 'Ver entrega ENV-1' }));
    fireEvent.click(await screen.findByRole('button', { name: `Reintentar ${recurso}` }));
    await waitFor(() =>
      expect(
        screen.queryByRole('button', { name: `Reintentar ${recurso}` }),
      ).not.toBeInTheDocument(),
    );
    expect(await screen.findByRole('button', { name: 'Ver entrega ENV-1' })).toBeInTheDocument();
  },
);
it('presenta estado vacío con resumen disponible', async () => {
  prepararEntregas();
  vi.mocked(api.listarEntregas).mockResolvedValue({
    items: [],
    total: 0,
    pagina: 1,
    tamanoPagina: 10,
  });
  renderEntregas();
  expect(await screen.findByText('Sin entregas coincidentes')).toBeInTheDocument();
  expect(screen.getByRole('region', { name: 'Resumen de entregas' })).toBeInTheDocument();
});
