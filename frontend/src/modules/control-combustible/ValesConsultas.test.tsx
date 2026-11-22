import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import {
  vale,
  confirmado,
  prepararVales,
  renderVales,
  esperarCargaInicialVales,
} from './vales.testUtils';
import type { ValeDetalle, PaginaVales } from './tipos';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.resetAllMocks();
});
it('consulta una vez y espera 300ms desde el último texto', async () => {
  prepararVales();
  vi.useFakeTimers();
  renderVales();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarVales).toHaveBeenCalledTimes(1);
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
  expect(api.listarVales).toHaveBeenCalledTimes(1);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(api.listarVales).toHaveBeenLastCalledWith(
    expect.objectContaining({ busqueda: 'Centro', pagina: 1 }),
  );
});
it('combina estado y paginación y vuelve a página 1', async () => {
  prepararVales();
  vi.mocked(api.listarVales).mockResolvedValue({
    items: [vale],
    total: 11,
    pagina: 1,
    tamanoPagina: 10,
  });
  await esperarCargaInicialVales();
  fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
  await waitFor(() =>
    expect(api.listarVales).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 2 })),
  );
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'Confirmado' },
  });
  await waitFor(() =>
    expect(api.listarVales).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 1, estado: 'Confirmado' }),
    ),
  );
});
it('descarta detalle obsoleto y mantiene la misma selección sin atascarse', async () => {
  prepararVales();
  let resolver!: (r: ValeDetalle) => void;
  vi.mocked(api.obtenerVale)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce(confirmado);
  renderVales();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver vale VAL-1' }));
  fireEvent.click(screen.getByRole('button', { name: 'Ver vale VAL-2' }));
  await screen.findByRole('heading', { name: 'VAL-2' });
  await act(async () => resolver(vale));
  fireEvent.click(screen.getByRole('button', { name: 'Ver vale VAL-2' }));
  expect(screen.getByRole('heading', { name: 'VAL-2' })).toBeInTheDocument();
  expect(api.obtenerVale).toHaveBeenCalledTimes(2);
});
it('descarta listado anterior al cambiar estado', async () => {
  prepararVales();
  let resolver!: (r: PaginaVales) => void;
  vi.mocked(api.listarVales)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce({
      items: [confirmado],
      total: 1,
      pagina: 1,
      tamanoPagina: 10,
    });
  renderVales();
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'Confirmado' },
  });
  await screen.findByRole('button', { name: 'Ver vale VAL-2' });
  await act(async () => resolver({ items: [vale], total: 1, pagina: 1, tamanoPagina: 10 }));
  expect(screen.queryByRole('button', { name: 'Ver vale VAL-1' })).not.toBeInTheDocument();
});
it.each(['listado', 'resumen', 'detalle'])(
  'permite reintentar %s de forma independiente',
  async (recurso) => {
    prepararVales();
    const consulta =
      recurso === 'listado'
        ? api.listarVales
        : recurso === 'resumen'
          ? api.resumenVales
          : api.obtenerVale;
    vi.mocked(consulta).mockRejectedValueOnce(new Error('red'));
    renderVales();
    if (recurso === 'detalle')
      fireEvent.click(await screen.findByRole('button', { name: 'Ver vale VAL-1' }));
    fireEvent.click(await screen.findByRole('button', { name: `Reintentar ${recurso}` }));
    await waitFor(() =>
      expect(
        screen.queryByRole('button', { name: `Reintentar ${recurso}` }),
      ).not.toBeInTheDocument(),
    );
    expect(await screen.findByRole('button', { name: 'Ver vale VAL-1' })).toBeInTheDocument();
  },
);
it('presenta estado vacío con resumen disponible', async () => {
  prepararVales();
  vi.mocked(api.listarVales).mockResolvedValue({
    items: [],
    total: 0,
    pagina: 1,
    tamanoPagina: 10,
  });
  renderVales();
  expect(await screen.findByText('Sin vales coincidentes')).toBeInTheDocument();
  expect(screen.getByRole('region', { name: 'Resumen de vales' })).toBeInTheDocument();
});

it('combina combustible con estado y reinicia página', async () => {
  prepararVales();
  await esperarCargaInicialVales();
  fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Confirmado' } });
  await waitFor(() =>
    expect(api.listarVales).toHaveBeenLastCalledWith(
      expect.objectContaining({ estado: 'Confirmado' }),
    ),
  );
  fireEvent.change(screen.getByLabelText('Combustible'), { target: { value: 'Gasolina' } });
  await waitFor(() =>
    expect(api.listarVales).toHaveBeenLastCalledWith(
      expect.objectContaining({ estado: 'Confirmado', tipoCombustible: 'Gasolina', pagina: 1 }),
    ),
  );
});
