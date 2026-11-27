import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import * as api from './api';
import { prepararRutas, renderRutas } from './rutasDespacho.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});

it.each(['resumen', 'zonas', 'rutas', 'detalle'])(
  'permite reintentar %s sin esconder otras consultas',
  async (recurso) => {
    prepararRutas();
    const llamada =
      recurso === 'resumen'
        ? api.resumenRutas
        : recurso === 'zonas'
          ? api.listarZonas
          : recurso === 'rutas'
            ? api.listarRutas
            : api.obtenerRuta;
    vi.mocked(llamada).mockRejectedValueOnce(new Error('red'));
    renderRutas();
    if (recurso === 'detalle')
      fireEvent.click(await screen.findByRole('button', { name: 'Ver ruta R01' }));
    fireEvent.click(await screen.findByRole('button', { name: `Reintentar ${recurso}` }));
    await waitFor(() =>
      expect(
        screen.queryByRole('button', { name: `Reintentar ${recurso}` }),
      ).not.toBeInTheDocument(),
    );
    expect(await screen.findByRole('button', { name: 'Ver ruta R01' })).toBeInTheDocument();
  },
);
it('muestra vacío sin perder el resumen', async () => {
  prepararRutas();
  vi.mocked(api.listarRutas).mockResolvedValue({
    items: [],
    total: 0,
    pagina: 1,
    tamanoPagina: 10,
  });
  renderRutas();
  expect(await screen.findByText('Sin rutas coincidentes')).toBeInTheDocument();
  expect(await screen.findByRole('region', { name: 'Resumen de rutas' })).toBeInTheDocument();
});
