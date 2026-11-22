import { act, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import * as api from './api';
import { ControlCombustiblePage } from './ControlCombustiblePage';
import type { ValoresVale, ValeDetalle } from './tipos';
export const valores: ValoresVale = {
  codigo: 'VAL-1',
  placa: 'ABC-470',
  estacion: 'Estación Norte',
  fechaCarga: '2026-11-22T10:30:12.345',
  tipoCombustible: 'Diesel',
  litros: '50',
  precioLitro: '5.125',
  moneda: 'PEN',
  odometroInicial: '12000',
  odometroFinal: '12500',
  observaciones: '',
};
export const vale: ValeDetalle = {
  ...valores,
  id: 1,
  estado: 'Borrador',
  fechaCarga: valores.fechaCarga + 'Z',
  litros: '50.000',
  precioLitro: '5.125',
  odometroInicial: 12000,
  odometroFinal: 12500,
  importe: '256.25',
  kilometrosRecorridos: 500,
  kmPorLitro: '10.000',
  litrosPor100Km: '10.000',
};
export const confirmado: ValeDetalle = {
  ...vale,
  id: 2,
  codigo: 'VAL-2',
  estado: 'Confirmado',
  fechaConfirmacion: '2026-11-21T12:00:00Z',
};
export function prepararVales() {
  vi.mocked(api.listarVales).mockResolvedValue({
    items: [vale, confirmado],
    total: 2,
    pagina: 1,
    tamanoPagina: 10,
  });
  vi.mocked(api.obtenerVale).mockImplementation(async (id) =>
    id === 2 ? confirmado : { ...vale, id },
  );
  vi.mocked(api.resumenVales).mockResolvedValue({
    total: 2,
    borradores: 1,
    confirmados: 1,
    anulados: 0,
  });
  vi.mocked(api.crearVale).mockResolvedValue({ id: 9 });
  vi.mocked(api.actualizarVale).mockResolvedValue({ id: 1 });
  vi.mocked(api.confirmarVale).mockResolvedValue({ id: 1 });
  vi.mocked(api.anularVale).mockResolvedValue({ id: 1 });
  vi.mocked(api.eliminarVale).mockResolvedValue(undefined);
}
export function renderVales() {
  return render(
    <MemoryRouter>
      <ControlCombustiblePage />
    </MemoryRouter>,
  );
}
export async function esperarCargaInicialVales() {
  renderVales();
  await act(async () => {
    await Promise.resolve();
  });
  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'Ver vale VAL-1' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Resumen de vales' })).toBeInTheDocument();
    expect(api.listarVales).toHaveBeenCalledTimes(1);
    expect(api.resumenVales).toHaveBeenCalledTimes(1);
  });
}
