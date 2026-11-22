import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import {
  sucursal,
  inactiva,
  prepararSucursales,
  renderSucursales,
  esperarCargaInicialSucursales,
} from './sucursales.testUtils';
import type { SucursalDetalle, PaginaSucursales } from './tipos';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.resetAllMocks();
});
it('consulta una vez y espera 300ms desde el último texto', async () => {
  prepararSucursales();
  vi.useFakeTimers();
  renderSucursales();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarSucursales).toHaveBeenCalledTimes(1);
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
  expect(api.listarSucursales).toHaveBeenCalledTimes(1);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(api.listarSucursales).toHaveBeenLastCalledWith(
    expect.objectContaining({ busqueda: 'Centro', pagina: 1 }),
  );
});
it('combina estado y paginación y vuelve a página 1', async () => {
  prepararSucursales();
  vi.mocked(api.listarSucursales).mockResolvedValue({
    items: [sucursal],
    total: 11,
    pagina: 1,
    tamanoPagina: 10,
  });
  await esperarCargaInicialSucursales();
  fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
  await waitFor(() =>
    expect(api.listarSucursales).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 2 })),
  );
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'false' },
  });
  await waitFor(() =>
    expect(api.listarSucursales).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 1, activa: false }),
    ),
  );
});
it('descarta detalle obsoleto y mantiene la misma selección sin atascarse', async () => {
  prepararSucursales();
  let resolver!: (r: SucursalDetalle) => void;
  vi.mocked(api.obtenerSucursal)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce(inactiva);
  renderSucursales();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver sucursal SUC-1' }));
  fireEvent.click(screen.getByRole('button', { name: 'Ver sucursal SUC-2' }));
  await screen.findByRole('heading', { name: 'SUC-2' });
  await act(async () => resolver(sucursal));
  fireEvent.click(screen.getByRole('button', { name: 'Ver sucursal SUC-2' }));
  expect(screen.getByRole('heading', { name: 'SUC-2' })).toBeInTheDocument();
  expect(api.obtenerSucursal).toHaveBeenCalledTimes(2);
});
it('descarta listado anterior al cambiar estado', async () => {
  prepararSucursales();
  let resolver!: (r: PaginaSucursales) => void;
  vi.mocked(api.listarSucursales)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce({
      items: [inactiva],
      total: 1,
      pagina: 1,
      tamanoPagina: 10,
    });
  renderSucursales();
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'false' },
  });
  await screen.findByRole('button', { name: 'Ver sucursal SUC-2' });
  await act(async () => resolver({ items: [sucursal], total: 1, pagina: 1, tamanoPagina: 10 }));
  expect(screen.queryByRole('button', { name: 'Ver sucursal SUC-1' })).not.toBeInTheDocument();
});
it.each(['listado', 'resumen', 'detalle'])(
  'permite reintentar %s de forma independiente',
  async (recurso) => {
    prepararSucursales();
    const consulta =
      recurso === 'listado'
        ? api.listarSucursales
        : recurso === 'resumen'
          ? api.resumenSucursales
          : api.obtenerSucursal;
    vi.mocked(consulta).mockRejectedValueOnce(new Error('red'));
    renderSucursales();
    if (recurso === 'detalle')
      fireEvent.click(await screen.findByRole('button', { name: 'Ver sucursal SUC-1' }));
    fireEvent.click(await screen.findByRole('button', { name: `Reintentar ${recurso}` }));
    await waitFor(() =>
      expect(
        screen.queryByRole('button', { name: `Reintentar ${recurso}` }),
      ).not.toBeInTheDocument(),
    );
    expect(await screen.findByRole('button', { name: 'Ver sucursal SUC-1' })).toBeInTheDocument();
  },
);
it('presenta estado vacío con resumen disponible', async () => {
  prepararSucursales();
  vi.mocked(api.listarSucursales).mockResolvedValue({
    items: [],
    total: 0,
    pagina: 1,
    tamanoPagina: 10,
  });
  renderSucursales();
  expect(await screen.findByText('Sin sucursales coincidentes')).toBeInTheDocument();
  expect(screen.getByRole('region', { name: 'Resumen de sucursales' })).toBeInTheDocument();
});

it('combina tipo con estado y reinicia página', async () => {
  prepararSucursales();
  await esperarCargaInicialSucursales();
  fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'false' } });
  await waitFor(() =>
    expect(api.listarSucursales).toHaveBeenLastCalledWith(
      expect.objectContaining({ activa: false }),
    ),
  );
  fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Oficina' } });
  await waitFor(() =>
    expect(api.listarSucursales).toHaveBeenLastCalledWith(
      expect.objectContaining({ activa: false, tipo: 'Oficina', pagina: 1 }),
    ),
  );
});

it('país espera 300ms y reinicia página sin peticiones intermedias', async () => {
  prepararSucursales();
  vi.useFakeTimers();
  renderSucursales();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarSucursales).toHaveBeenCalledTimes(1);
  fireEvent.change(screen.getByLabelText('País (coincidencia exacta)'), {
    target: { value: 'Per' },
  });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(150);
  });
  fireEvent.change(screen.getByLabelText('País (coincidencia exacta)'), {
    target: { value: 'Peru' },
  });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(299);
  });
  expect(api.listarSucursales).toHaveBeenCalledTimes(1);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(api.listarSucursales).toHaveBeenLastCalledWith(
    expect.objectContaining({ pais: 'Peru', pagina: 1 }),
  );
});
it('corrige la página cuando queda vacía después de refrescar', async () => {
  prepararSucursales();
  vi.mocked(api.listarSucursales)
    .mockResolvedValueOnce({ items: [sucursal], total: 11, pagina: 1, tamanoPagina: 10 })
    .mockResolvedValueOnce({ items: [], total: 10, pagina: 2, tamanoPagina: 10 });
  await esperarCargaInicialSucursales();
  fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
  await waitFor(() => expect(api.listarSucursales).toHaveBeenCalledTimes(3));
  expect(api.listarSucursales).toHaveBeenNthCalledWith(2, expect.objectContaining({ pagina: 2 }));
  expect(api.listarSucursales).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 1 }));
});
