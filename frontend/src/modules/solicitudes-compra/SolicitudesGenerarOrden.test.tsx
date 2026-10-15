import { act, cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { SolicitudesCompraPage } from './SolicitudesCompraPage';
import { OrdenesAbastecimientoPage } from '../ordenes-abastecimiento/OrdenesAbastecimientoPage';
import { prepararApi } from '../ordenes-abastecimiento/ordenes.testUtils';

async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>) {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();
  await screen.findByRole('button', { name: 'SC-000003' });
  await screen.findByRole('option', { name: 'Andes Suministros S.A.C.' });
  await screen.findByRole('region', { name: 'Resumen de solicitudes de compra' });
  const urls = fetchMock.mock.calls.map((call) => String(call[0]));
  expect(urls.filter((url) => url.includes('/solicitudes-compra?'))).toHaveLength(1);
  expect(urls.some((url) => url.includes('/solicitudes-compra/resumen'))).toBe(true);
  expect(urls.some((url) => url.includes('/proveedores?'))).toBe(true);
  expect(urls.some((url) => url.includes('/productos?'))).toBe(true);
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('generar orden desde solicitud aprobada', () => {
  it('A5 muestra el acceso solo para aprobadas, envía el cuerpo y abre la orden nueva', async () => {
    const { fetchMock } = prepararApi();
    vi.useFakeTimers();
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
    vi.useFakeTimers();
    fireEvent.click(within(dialog).getByRole('button', { name: 'Generar orden' }));
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
      await vi.advanceTimersByTimeAsync(301);
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    });
    vi.useRealTimers();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]) === '/api/v1/ordenes-abastecimiento/desde-solicitud/3' && call[1]?.method === 'POST')).toHaveLength(1);
    expect(screen.getByRole('heading', { name: 'OA-000005' })).toBeInTheDocument();
    expect(screen.getAllByText('SC-000003').length).toBeGreaterThan(0);
    const llamada = fetchMock.mock.calls.find((call) => String(call[0]) === '/api/v1/ordenes-abastecimiento/desde-solicitud/3');
    expect(llamada?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ proveedorId: 1, fechaEntregaEstimada: '2030-01-01', observaciones: 'Desde la solicitud aprobada' }) });
  });

  it('A5 presenta un 409 dentro del diálogo y conserva la selección', async () => {
    const { fetchMock } = prepararApi({ errorGenerar: true });
    vi.useFakeTimers();
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
    expect(fetchMock.mock.calls.filter((call) => String(call[0]) === '/api/v1/ordenes-abastecimiento/desde-solicitud/3' && call[1]?.method === 'POST')).toHaveLength(1);
    expect(within(dialog).getByLabelText('Proveedor')).toHaveValue('1');
    expect(dialog).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'OA-000005' })).not.toBeInTheDocument();
  });

  it('A5 no muestra generar orden cuando la solicitud sigue pendiente', async () => {
    const { fetchMock } = prepararApi({ solicitudEstado: 'Pendiente' });
    vi.useFakeTimers();
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
