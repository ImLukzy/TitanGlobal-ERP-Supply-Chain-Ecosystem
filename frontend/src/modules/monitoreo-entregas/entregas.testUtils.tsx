import { act, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import * as api from './api';
import { MonitoreoEntregasPage } from './MonitoreoEntregasPage';
import type { EntregaDetalle, ValoresEntrega } from './tipos';
export const valores: ValoresEntrega = {
  codigo: 'ENV-1',
  referenciaGuia: 'T045-1',
  destinatario: 'Centro Sur',
  direccionDestino: 'Avenida 100',
  fechaProgramada: '2026-11-22T10:30:12.345',
  observaciones: '',
};
export const entrega: EntregaDetalle = {
  ...valores,
  id: 1,
  estado: 'Programada',
  fechaProgramada: '2026-11-22T10:30:12.345Z',
  historial: [
    {
      id: 1,
      estado: 'Programada',
      fecha: '2026-11-21T12:00:00Z',
      descripcion: 'Entrega programada.',
    },
  ],
};
export const enRuta: EntregaDetalle = {
  ...entrega,
  id: 2,
  codigo: 'ENV-2',
  estado: 'EnRuta',
  fechaSalida: '2026-11-21T13:00:00Z',
  historial: [
    ...entrega.historial,
    {
      id: 2,
      estado: 'EnRuta',
      fecha: '2026-11-21T13:00:00Z',
      descripcion: 'Salida a ruta registrada.',
    },
  ],
};
export function prepararEntregas() {
  vi.mocked(api.listarEntregas).mockResolvedValue({
    items: [entrega, enRuta],
    total: 2,
    pagina: 1,
    tamanoPagina: 10,
  });
  vi.mocked(api.obtenerEntrega).mockImplementation(async (id) =>
    id === 2 ? enRuta : { ...entrega, id },
  );
  vi.mocked(api.resumenEntregas).mockResolvedValue({
    total: 2,
    programadas: 1,
    enRuta: 1,
    entregadas: 0,
    canceladas: 0,
  });
  vi.mocked(api.crearEntrega).mockResolvedValue({ id: 9 });
  vi.mocked(api.actualizarEntrega).mockResolvedValue({ id: 1 });
  vi.mocked(api.eliminarEntrega).mockResolvedValue(undefined);
  vi.mocked(api.iniciarEntrega).mockResolvedValue({ id: 1 });
  vi.mocked(api.completarEntrega).mockResolvedValue({ id: 2 });
  vi.mocked(api.cancelarEntrega).mockResolvedValue({ id: 1 });
}
export function renderEntregas() {
  return render(
    <MemoryRouter>
      <MonitoreoEntregasPage />
    </MemoryRouter>,
  );
}
export async function esperarCargaInicialEntregas() {
  renderEntregas();
  await act(async () => {
    await Promise.resolve();
  });
  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'Ver entrega ENV-1' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Resumen de entregas' })).toBeInTheDocument();
    expect(api.listarEntregas).toHaveBeenCalledTimes(1);
    expect(api.resumenEntregas).toHaveBeenCalledTimes(1);
  });
}
