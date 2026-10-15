import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { OrdenesAbastecimientoPage } from './OrdenesAbastecimientoPage';
import { prepararApi, renderEnRouter } from './ordenes.testUtils';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('pantalla de órdenes de abastecimiento: recepción y anulación', () => {
  it('A4 confirma recepción, envía POST y refresca el detalle en modo lectura', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<OrdenesAbastecimientoPage />, ['/modulos/15-ordenes-abastecimiento?orden=1']);
    await screen.findByRole('heading', { name: 'OA-000001' });
    fireEvent.click(screen.getByRole('button', { name: 'Marcar recibida' }));
    expect(screen.getByRole('dialog', { name: 'Recibir OA-000001' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar recepción' }));
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Marcar recibida' })).not.toBeInTheDocument());
    expect(fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/ordenes-abastecimiento/1/recibir' && (call[1] as RequestInit | undefined)?.method === 'POST')).toBe(true);
    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument();
    expect(screen.getByText('14/10/2026')).toBeInTheDocument();
  });

  it('A4 exige el motivo antes de anular y muestra 409 literal dentro del diálogo', async () => {
    const { fetchMock } = prepararApi({ errorAnular: true });
    renderEnRouter(<OrdenesAbastecimientoPage />, ['/modulos/15-ordenes-abastecimiento?orden=2']);
    await screen.findByRole('heading', { name: 'OA-000002' });
    fireEvent.click(screen.getByRole('button', { name: 'Anular' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar anulación' }));
    expect(screen.getByText('El motivo de anulación es obligatorio.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).endsWith('/anular'))).toHaveLength(0);
    fireEvent.change(screen.getByLabelText('Motivo de anulación'), { target: { value: 'Cambio del plan anual' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar anulación' }));
    expect(await screen.findByText('La orden OA-000002 ya está anulada.')).toHaveAttribute('role', 'alert');
    expect(screen.getByLabelText('Motivo de anulación')).toHaveValue('Cambio del plan anual');
    const llamada = fetchMock.mock.calls.find((call) => String(call[0]).endsWith('/anular'));
    expect(JSON.parse(String((llamada?.[1] as RequestInit | undefined)?.body))).toEqual({ motivo: 'Cambio del plan anual' });
  });

  it('A4 Escape cancela la anulación sin peticiones y devuelve el foco', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<OrdenesAbastecimientoPage />, ['/modulos/15-ordenes-abastecimiento?orden=1']);
    await screen.findByRole('heading', { name: 'OA-000001' });
    const abrir = screen.getByRole('button', { name: 'Anular' });
    fireEvent.click(abrir);
    fetchMock.mockClear();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    await act(async () => {
      await new Promise((resolve) => window.requestAnimationFrame(resolve));
    });
    expect(document.activeElement).toBe(abrir);
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
