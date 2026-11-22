import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import {
  retorno,
  recogido,
  prepararRetornos,
  renderRetornos,
  esperarCargaInicialRetornos,
} from './retornos.testUtils';
import type { RetornoDetalle, PaginaRetornos } from './tipos';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.resetAllMocks();
});
it('consulta una vez y espera 300ms desde el último texto', async () => {
  prepararRetornos();
  vi.useFakeTimers();
  renderRetornos();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarRetornos).toHaveBeenCalledTimes(1);
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
  expect(api.listarRetornos).toHaveBeenCalledTimes(1);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(api.listarRetornos).toHaveBeenLastCalledWith(
    expect.objectContaining({ busqueda: 'Centro', pagina: 1 }),
  );
});
it('combina estado y paginación y vuelve a página 1', async () => {
  prepararRetornos();
  vi.mocked(api.listarRetornos).mockResolvedValue({
    items: [retorno],
    total: 11,
    pagina: 1,
    tamanoPagina: 10,
  });
  await esperarCargaInicialRetornos();
  fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
  await waitFor(() =>
    expect(api.listarRetornos).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 2 })),
  );
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'Recogido' },
  });
  await waitFor(() =>
    expect(api.listarRetornos).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 1, estado: 'Recogido' }),
    ),
  );
});
it('descarta detalle obsoleto y mantiene la misma selección sin atascarse', async () => {
  prepararRetornos();
  let resolver!: (r: RetornoDetalle) => void;
  vi.mocked(api.obtenerRetorno)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce(recogido);
  renderRetornos();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver retorno RET-1' }));
  fireEvent.click(screen.getByRole('button', { name: 'Ver retorno RET-2' }));
  await screen.findByRole('heading', { name: 'RET-2' });
  await act(async () => resolver(retorno));
  fireEvent.click(screen.getByRole('button', { name: 'Ver retorno RET-2' }));
  expect(screen.getByRole('heading', { name: 'RET-2' })).toBeInTheDocument();
  expect(api.obtenerRetorno).toHaveBeenCalledTimes(2);
});
it('descarta listado anterior al cambiar estado', async () => {
  prepararRetornos();
  let resolver!: (r: PaginaRetornos) => void;
  vi.mocked(api.listarRetornos)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce({
      items: [recogido],
      total: 1,
      pagina: 1,
      tamanoPagina: 10,
    });
  renderRetornos();
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'Recogido' },
  });
  await screen.findByRole('button', { name: 'Ver retorno RET-2' });
  await act(async () => resolver({ items: [retorno], total: 1, pagina: 1, tamanoPagina: 10 }));
  expect(screen.queryByRole('button', { name: 'Ver retorno RET-1' })).not.toBeInTheDocument();
});
it.each(['listado', 'resumen', 'detalle'])(
  'permite reintentar %s de forma independiente',
  async (recurso) => {
    prepararRetornos();
    const consulta =
      recurso === 'listado'
        ? api.listarRetornos
        : recurso === 'resumen'
          ? api.resumenRetornos
          : api.obtenerRetorno;
    vi.mocked(consulta).mockRejectedValueOnce(new Error('red'));
    renderRetornos();
    if (recurso === 'detalle')
      fireEvent.click(await screen.findByRole('button', { name: 'Ver retorno RET-1' }));
    fireEvent.click(await screen.findByRole('button', { name: `Reintentar ${recurso}` }));
    await waitFor(() =>
      expect(
        screen.queryByRole('button', { name: `Reintentar ${recurso}` }),
      ).not.toBeInTheDocument(),
    );
    expect(await screen.findByRole('button', { name: 'Ver retorno RET-1' })).toBeInTheDocument();
  },
);
it('presenta estado vacío con resumen disponible', async () => {
  prepararRetornos();
  vi.mocked(api.listarRetornos).mockResolvedValue({
    items: [],
    total: 0,
    pagina: 1,
    tamanoPagina: 10,
  });
  renderRetornos();
  expect(await screen.findByText('Sin retornos coincidentes')).toBeInTheDocument();
  expect(screen.getByRole('region', { name: 'Resumen de retornos' })).toBeInTheDocument();
});

it('combina combustible con estado y reinicia página', async () => {
  prepararRetornos();
  await esperarCargaInicialRetornos();
  fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Recogido' } });
  await waitFor(() =>
    expect(api.listarRetornos).toHaveBeenLastCalledWith(
      expect.objectContaining({ estado: 'Recogido' }),
    ),
  );
  fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Retornable' } });
  await waitFor(() =>
    expect(api.listarRetornos).toHaveBeenLastCalledWith(
      expect.objectContaining({ estado: 'Recogido', tipo: 'Retornable', pagina: 1 }),
    ),
  );
});
