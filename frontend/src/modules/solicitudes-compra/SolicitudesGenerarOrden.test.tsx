import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { SolicitudesCompraPage } from './SolicitudesCompraPage';
import { OrdenesAbastecimientoPage } from '../ordenes-abastecimiento/OrdenesAbastecimientoPage';
import { prepararApi } from '../ordenes-abastecimiento/ordenes.testUtils';

async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>) {
  await screen.findByRole('button', { name: 'SC-000003' });
  await screen.findByRole('option', { name: 'Andes Suministros S.A.C.' });
  vi.useFakeTimers();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
  await waitFor(() => {
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/solicitudes-compra?'))).toHaveLength(2);
  });
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('generar orden desde solicitud aprobada', () => {
  it('A5 muestra el acceso solo para aprobadas, envía el cuerpo y abre la orden nueva', async () => {
    const { fetchMock } = prepararApi();
    render(
      <MemoryRouter initialEntries={['/modulos/13-solicitudes-compra']}>
        <Routes>
          <Route element={<SolicitudesCompraPage />} path="/modulos/13-solicitudes-compra" />
          <Route element={<OrdenesAbastecimientoPage />} path="/modulos/15-ordenes-abastecimiento" />
        </Routes>
      </MemoryRouter>,
    );
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'SC-000003' }));
    await screen.findByRole('heading', { name: 'SC-000003' });
    expect(screen.getByRole('button', { name: 'Generar orden' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Generar orden' }));
    const dialog = screen.getByRole('dialog', { name: 'Generar orden desde SC-000003' });
    expect(within(dialog).getByLabelText('Proveedor')).toHaveValue('1');
    fireEvent.change(within(dialog).getByLabelText('Fecha de entrega estimada'), { target: { value: '2030-01-01' } });
    fireEvent.change(within(dialog).getByLabelText('Observaciones'), { target: { value: 'Desde la solicitud aprobada' } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Generar orden' }));
    expect(await screen.findByRole('heading', { name: 'OA-000005' })).toBeInTheDocument();
    expect(screen.getAllByText('SC-000003').length).toBeGreaterThan(0);
    const llamada = fetchMock.mock.calls.find((call) => String(call[0]) === '/api/v1/ordenes-abastecimiento/desde-solicitud/3');
    expect(llamada?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ proveedorId: 1, fechaEntregaEstimada: '2030-01-01', observaciones: 'Desde la solicitud aprobada' }) });
  });

  it('A5 presenta un 409 dentro del diálogo y conserva la selección', async () => {
    const { fetchMock } = prepararApi({ errorGenerar: true });
    render(
      <MemoryRouter initialEntries={['/modulos/13-solicitudes-compra']}>
        <Routes>
          <Route element={<SolicitudesCompraPage />} path="/modulos/13-solicitudes-compra" />
          <Route element={<OrdenesAbastecimientoPage />} path="/modulos/15-ordenes-abastecimiento" />
        </Routes>
      </MemoryRouter>,
    );
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'SC-000003' }));
    await screen.findByRole('heading', { name: 'SC-000003' });
    fireEvent.click(screen.getByRole('button', { name: 'Generar orden' }));
    const dialog = screen.getByRole('dialog', { name: 'Generar orden desde SC-000003' });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Generar orden' }));
    expect(await screen.findByText('La solicitud SC-000003 ya tiene la orden OA-000001.')).toHaveAttribute('role', 'alert');
    expect(within(dialog).getByLabelText('Proveedor')).toHaveValue('1');
    expect(dialog).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'OA-000005' })).not.toBeInTheDocument();
  });

  it('A5 no muestra generar orden cuando la solicitud sigue pendiente', async () => {
    const { fetchMock } = prepararApi({ solicitudEstado: 'Pendiente' });
    render(
      <MemoryRouter initialEntries={['/modulos/13-solicitudes-compra']}>
        <Routes>
          <Route element={<SolicitudesCompraPage />} path="/modulos/13-solicitudes-compra" />
          <Route element={<OrdenesAbastecimientoPage />} path="/modulos/15-ordenes-abastecimiento" />
        </Routes>
      </MemoryRouter>,
    );
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'SC-000003' }));
    await screen.findByRole('heading', { name: 'SC-000003' });
    expect(screen.queryByRole('button', { name: 'Generar orden' })).not.toBeInTheDocument();
  });
});
