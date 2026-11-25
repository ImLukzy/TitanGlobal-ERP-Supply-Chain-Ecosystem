import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { carga, confirmada, prepararCargas, renderCargas } from './cargas.testUtils';
import type { CargaDetalle, PaginaCargas } from './tipos';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.resetAllMocks();
});
it('consulta una vez y espera 300ms desde el último texto', async () => {
  prepararCargas();
  vi.useFakeTimers();
  renderCargas();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarCargas).toHaveBeenCalledTimes(1);
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Ca' } });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(150);
  });
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Carga' } });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(299);
  });
  expect(api.listarCargas).toHaveBeenCalledTimes(1);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(api.listarCargas).toHaveBeenLastCalledWith(
    expect.objectContaining({ busqueda: 'Carga', pagina: 1 }),
  );
});
it('combina estado y paginación y vuelve a página 1', async () => {
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
  fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Confirmada' } });
  await waitFor(() =>
    expect(api.listarCargas).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 1, estado: 'Confirmada' }),
    ),
  );
});
it('descarta detalle obsoleto y mantiene la misma selección sin atascarse', async () => {
  prepararCargas();
  let resolver!: (r: CargaDetalle) => void;
  vi.mocked(api.obtenerCarga)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce(confirmada);
  renderCargas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver carga C01' }));
  fireEvent.click(screen.getByRole('button', { name: 'Ver carga C02' }));
  await screen.findByRole('heading', { name: 'Carga Centro' });
  await act(async () => resolver(carga));
  fireEvent.click(screen.getByRole('button', { name: 'Ver carga C02' }));
  expect(screen.getByRole('heading', { name: 'Carga Centro' })).toBeInTheDocument();
  expect(api.obtenerCarga).toHaveBeenCalledTimes(2);
});
it('descarta listado anterior al cambiar estado', async () => {
  prepararCargas();
  let resolver!: (r: PaginaCargas) => void;
  vi.mocked(api.listarCargas)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce({ items: [confirmada], total: 1, pagina: 1, tamanoPagina: 10 });
  renderCargas();
  fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Confirmada' } });
  await screen.findByRole('button', { name: 'Ver carga C02' });
  await act(async () => resolver({ items: [carga], total: 1, pagina: 1, tamanoPagina: 10 }));
  expect(screen.queryByRole('button', { name: 'Ver carga C01' })).not.toBeInTheDocument();
});
it.each(['listado', 'resumen', 'detalle'])(
  'permite reintentar %s de forma independiente',
  async (recurso) => {
    prepararCargas();
    const consulta =
      recurso === 'listado'
        ? api.listarCargas
        : recurso === 'resumen'
          ? api.resumenCargas
          : api.obtenerCarga;
    vi.mocked(consulta).mockRejectedValueOnce(new Error('red'));
    renderCargas();
    if (recurso === 'detalle')
      fireEvent.click(await screen.findByRole('button', { name: 'Ver carga C01' }));
    fireEvent.click(await screen.findByRole('button', { name: `Reintentar ${recurso}` }));
    await waitFor(() =>
      expect(
        screen.queryByRole('button', { name: `Reintentar ${recurso}` }),
      ).not.toBeInTheDocument(),
    );
    expect(await screen.findByRole('button', { name: 'Ver carga C01' })).toBeInTheDocument();
  },
);
it('presenta estado vacío con resumen disponible', async () => {
  prepararCargas();
  vi.mocked(api.listarCargas).mockResolvedValue({
    items: [],
    total: 0,
    pagina: 1,
    tamanoPagina: 10,
  });
  renderCargas();
  expect(await screen.findByText('Sin consolidaciones coincidentes')).toBeInTheDocument();
  expect(screen.getByRole('region', { name: 'Resumen de consolidaciones' })).toBeInTheDocument();
});
