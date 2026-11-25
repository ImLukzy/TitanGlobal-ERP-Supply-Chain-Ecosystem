import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { prepararRutas, renderRutas, ruta, rutaDos } from './rutasDespacho.testUtils';
import type { RutaDetalle, PaginaRutas } from './tipos';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.resetAllMocks();
});

it('muestra resumen, detalle y cifras textuales exactas con moneda', async () => {
  prepararRutas();
  renderRutas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver ruta R02' }));
  await screen.findByRole('heading', { name: 'Corredor Sur' });
  expect(screen.getAllByText('9999999999999999.99 USD')).toHaveLength(2);
  expect(screen.getByRole('region', { name: 'Resumen de rutas' })).toBeInTheDocument();
  expect(screen.getAllByText('Inactiva')).toHaveLength(2);
});
it('consulta una vez al montar y espera 300ms desde el último texto', async () => {
  prepararRutas();
  vi.useFakeTimers();
  renderRutas();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarRutas).toHaveBeenCalledTimes(1);
  const busqueda = screen.getByRole('searchbox');
  fireEvent.change(busqueda, { target: { value: 'Li' } });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(150);
  });
  fireEvent.change(busqueda, { target: { value: 'Lima' } });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(299);
  });
  expect(api.listarRutas).toHaveBeenCalledTimes(1);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(api.listarRutas).toHaveBeenLastCalledWith(
    expect.objectContaining({ busqueda: 'Lima', pagina: 1 }),
  );
});
it('no queda cargando al deshacer una búsqueda antes de enviarla', async () => {
  prepararRutas();
  vi.useFakeTimers();
  renderRutas();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'x' } });
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: '' } });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarRutas).toHaveBeenCalledTimes(1);
  expect(screen.queryByText('Cargando rutas…')).not.toBeInTheDocument();
});
it.each(['Zona', 'Estado', 'búsqueda'])('vuelve a página 1 al cambiar %s', async (campo) => {
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
  if (campo === 'búsqueda') {
    vi.useFakeTimers();
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Lima' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });
    vi.useRealTimers();
  } else
    fireEvent.change(screen.getByLabelText(campo), {
      target: { value: campo === 'Zona' ? 'Norte' : 'false' },
    });
  await waitFor(() =>
    expect(api.listarRutas).toHaveBeenLastCalledWith(
      expect.objectContaining({
        pagina: 1,
        ...(campo === 'Zona'
          ? { zona: 'Norte' }
          : campo === 'Estado'
            ? { activo: false }
            : { busqueda: 'Lima' }),
      }),
    ),
  );
});
it('ignora un detalle anterior y volver a seleccionar la misma ruta no bloquea el panel', async () => {
  prepararRutas();
  let resolver!: (datos: RutaDetalle) => void;
  vi.mocked(api.obtenerRuta)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce(rutaDos);
  renderRutas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver ruta R01' }));
  fireEvent.click(screen.getByRole('button', { name: 'Ver ruta R02' }));
  await screen.findByRole('heading', { name: 'Corredor Sur' });
  await act(async () => resolver(ruta));
  fireEvent.click(screen.getByRole('button', { name: 'Ver ruta R02' }));
  expect(screen.getByRole('heading', { name: 'Corredor Sur' })).toBeInTheDocument();
  expect(api.obtenerRuta).toHaveBeenCalledTimes(2);
});
it('ignora un listado anterior al cambiar los filtros', async () => {
  prepararRutas();
  let resolver!: (datos: PaginaRutas) => void;
  vi.mocked(api.listarRutas)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce({ items: [rutaDos], total: 1, pagina: 1, tamanoPagina: 10 });
  renderRutas();
  fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'false' } });
  await screen.findByRole('button', { name: 'Ver ruta R02' });
  await act(async () => resolver({ items: [ruta], total: 1, pagina: 1, tamanoPagina: 10 }));
  expect(screen.queryByRole('button', { name: 'Ver ruta R01' })).not.toBeInTheDocument();
});
