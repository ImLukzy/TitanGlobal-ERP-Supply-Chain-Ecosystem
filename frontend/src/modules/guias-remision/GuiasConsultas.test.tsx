import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import {
  guia,
  emitida,
  prepararGuias,
  renderGuias,
  esperarCargaInicialGuias,
} from './guias.testUtils';
import type { GuiaDetalle, PaginaGuias } from './tipos';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.resetAllMocks();
});
it('consulta una vez y espera 300ms desde el último texto', async () => {
  prepararGuias();
  vi.useFakeTimers();
  renderGuias();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarGuias).toHaveBeenCalledTimes(1);
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Ca' } });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(150);
  });
  fireEvent.change(screen.getByRole('searchbox'), {
    target: { value: 'Carga' },
  });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(299);
  });
  expect(api.listarGuias).toHaveBeenCalledTimes(1);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(api.listarGuias).toHaveBeenLastCalledWith(
    expect.objectContaining({ busqueda: 'Carga', pagina: 1 }),
  );
});
it('combina estado y paginación y vuelve a página 1', async () => {
  prepararGuias();
  vi.mocked(api.listarGuias).mockResolvedValue({
    items: [guia],
    total: 11,
    pagina: 1,
    tamanoPagina: 10,
  });
  await esperarCargaInicialGuias();
  fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
  await waitFor(() =>
    expect(api.listarGuias).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 2 })),
  );
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'Emitida' },
  });
  await waitFor(() =>
    expect(api.listarGuias).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 1, estado: 'Emitida' }),
    ),
  );
});
it('descarta detalle obsoleto y mantiene la misma selección sin atascarse', async () => {
  prepararGuias();
  let resolver!: (r: GuiaDetalle) => void;
  vi.mocked(api.obtenerGuia)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce(emitida);
  renderGuias();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver guía T001-00000001' }));
  fireEvent.click(screen.getByRole('button', { name: 'Ver guía T001-00000002' }));
  await screen.findByRole('heading', { name: 'T001-00000002' });
  await act(async () => resolver(guia));
  fireEvent.click(screen.getByRole('button', { name: 'Ver guía T001-00000002' }));
  expect(screen.getByRole('heading', { name: 'T001-00000002' })).toBeInTheDocument();
  expect(api.obtenerGuia).toHaveBeenCalledTimes(2);
});
it('descarta listado anterior al cambiar estado', async () => {
  prepararGuias();
  let resolver!: (r: PaginaGuias) => void;
  vi.mocked(api.listarGuias)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce({
      items: [emitida],
      total: 1,
      pagina: 1,
      tamanoPagina: 10,
    });
  renderGuias();
  fireEvent.change(screen.getByLabelText('Estado'), {
    target: { value: 'Emitida' },
  });
  await screen.findByRole('button', { name: 'Ver guía T001-00000002' });
  await act(async () => resolver({ items: [guia], total: 1, pagina: 1, tamanoPagina: 10 }));
  expect(screen.queryByRole('button', { name: 'Ver guía T001-00000001' })).not.toBeInTheDocument();
});
it.each(['listado', 'resumen', 'detalle'])(
  'permite reintentar %s de forma independiente',
  async (recurso) => {
    prepararGuias();
    const consulta =
      recurso === 'listado'
        ? api.listarGuias
        : recurso === 'resumen'
          ? api.resumenGuias
          : api.obtenerGuia;
    vi.mocked(consulta).mockRejectedValueOnce(new Error('red'));
    renderGuias();
    if (recurso === 'detalle')
      fireEvent.click(await screen.findByRole('button', { name: 'Ver guía T001-00000001' }));
    fireEvent.click(await screen.findByRole('button', { name: `Reintentar ${recurso}` }));
    await waitFor(() =>
      expect(
        screen.queryByRole('button', { name: `Reintentar ${recurso}` }),
      ).not.toBeInTheDocument(),
    );
    expect(
      await screen.findByRole('button', { name: 'Ver guía T001-00000001' }),
    ).toBeInTheDocument();
  },
);
it('presenta estado vacío con resumen disponible', async () => {
  prepararGuias();
  vi.mocked(api.listarGuias).mockResolvedValue({
    items: [],
    total: 0,
    pagina: 1,
    tamanoPagina: 10,
  });
  renderGuias();
  expect(await screen.findByText('Sin guías coincidentes')).toBeInTheDocument();
  expect(screen.getByRole('region', { name: 'Resumen de guías' })).toBeInTheDocument();
});
