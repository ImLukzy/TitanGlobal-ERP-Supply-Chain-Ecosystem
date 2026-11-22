import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import {
  siniestro,
  reclamado,
  prepararSiniestros,
  renderSiniestros,
  esperarCargaInicialSiniestros,
} from './siniestros.testUtils';
import type { SiniestroDetalle, PaginaSiniestros } from './tipos';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.resetAllMocks();
});
it('consulta una vez y espera 300ms desde el último texto', async () => {
  prepararSiniestros();
  vi.useFakeTimers();
  renderSiniestros();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarSiniestros).toHaveBeenCalledTimes(1);
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
  expect(api.listarSiniestros).toHaveBeenCalledTimes(1);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(api.listarSiniestros).toHaveBeenLastCalledWith(
    expect.objectContaining({ busqueda: 'Centro', pagina: 1 }),
  );
});
it('combina estado y paginación y vuelve a página 1', async () => {
  prepararSiniestros();
  vi.mocked(api.listarSiniestros).mockResolvedValue({
    items: [siniestro],
    total: 11,
    pagina: 1,
    tamanoPagina: 10,
  });
  await esperarCargaInicialSiniestros();
  fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
  await waitFor(() =>
    expect(api.listarSiniestros).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 2 })),
  );
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'EnReclamo' },
  });
  await waitFor(() =>
    expect(api.listarSiniestros).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 1, estado: 'EnReclamo' }),
    ),
  );
});
it('descarta detalle obsoleto y mantiene la misma selección sin atascarse', async () => {
  prepararSiniestros();
  let resolver!: (r: SiniestroDetalle) => void;
  vi.mocked(api.obtenerSiniestro)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce(reclamado);
  renderSiniestros();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver siniestro SIN-1' }));
  fireEvent.click(screen.getByRole('button', { name: 'Ver siniestro SIN-2' }));
  await screen.findByRole('heading', { name: 'SIN-2' });
  await act(async () => resolver(siniestro));
  fireEvent.click(screen.getByRole('button', { name: 'Ver siniestro SIN-2' }));
  expect(screen.getByRole('heading', { name: 'SIN-2' })).toBeInTheDocument();
  expect(api.obtenerSiniestro).toHaveBeenCalledTimes(2);
});
it('descarta listado anterior al cambiar estado', async () => {
  prepararSiniestros();
  let resolver!: (r: PaginaSiniestros) => void;
  vi.mocked(api.listarSiniestros)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce({
      items: [reclamado],
      total: 1,
      pagina: 1,
      tamanoPagina: 10,
    });
  renderSiniestros();
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'EnReclamo' },
  });
  await screen.findByRole('button', { name: 'Ver siniestro SIN-2' });
  await act(async () => resolver({ items: [siniestro], total: 1, pagina: 1, tamanoPagina: 10 }));
  expect(screen.queryByRole('button', { name: 'Ver siniestro SIN-1' })).not.toBeInTheDocument();
});
it.each(['listado', 'resumen', 'detalle'])(
  'permite reintentar %s de forma independiente',
  async (recurso) => {
    prepararSiniestros();
    const consulta =
      recurso === 'listado'
        ? api.listarSiniestros
        : recurso === 'resumen'
          ? api.resumenSiniestros
          : api.obtenerSiniestro;
    vi.mocked(consulta).mockRejectedValueOnce(new Error('red'));
    renderSiniestros();
    if (recurso === 'detalle')
      fireEvent.click(await screen.findByRole('button', { name: 'Ver siniestro SIN-1' }));
    fireEvent.click(await screen.findByRole('button', { name: `Reintentar ${recurso}` }));
    await waitFor(() =>
      expect(
        screen.queryByRole('button', { name: `Reintentar ${recurso}` }),
      ).not.toBeInTheDocument(),
    );
    expect(await screen.findByRole('button', { name: 'Ver siniestro SIN-1' })).toBeInTheDocument();
  },
);
it('presenta estado vacío con resumen disponible', async () => {
  prepararSiniestros();
  vi.mocked(api.listarSiniestros).mockResolvedValue({
    items: [],
    total: 0,
    pagina: 1,
    tamanoPagina: 10,
  });
  renderSiniestros();
  expect(await screen.findByText('Sin siniestros coincidentes')).toBeInTheDocument();
  expect(screen.getByRole('region', { name: 'Resumen de siniestros' })).toBeInTheDocument();
});

it('combina combustible con estado y reinicia página', async () => {
  prepararSiniestros();
  await esperarCargaInicialSiniestros();
  fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'EnReclamo' } });
  await waitFor(() =>
    expect(api.listarSiniestros).toHaveBeenLastCalledWith(
      expect.objectContaining({ estado: 'EnReclamo' }),
    ),
  );
  fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Robo' } });
  await waitFor(() =>
    expect(api.listarSiniestros).toHaveBeenLastCalledWith(
      expect.objectContaining({ estado: 'EnReclamo', tipo: 'Robo', pagina: 1 }),
    ),
  );
});
