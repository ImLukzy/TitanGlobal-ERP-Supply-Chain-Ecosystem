import { act, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import * as api from './api';
import { LogisticaInversaPage } from './LogisticaInversaPage';
import type { ValoresRetorno, RetornoDetalle } from './tipos';
export const valores: ValoresRetorno = {
  codigo: 'RET-1',
  referenciaOrigen: 'GUIA-49',
  contraparte: 'Cliente Norte',
  puntoRecogida: 'Muelle Norte',
  puntoRecepcion: 'Almacén Central',
  descripcion: 'Pallet reutilizable',
  unidad: 'unidad',
  tipo: 'Pallet',
  fechaProgramada: '2026-11-22T10:30:12.345',
  cantidadProgramada: '10.125',
  observaciones: '',
};
export const retorno: RetornoDetalle = {
  ...valores,
  id: 1,
  estado: 'Programado',
  fechaProgramada: valores.fechaProgramada + 'Z',
};
export const recogido: RetornoDetalle = {
  ...retorno,
  id: 2,
  codigo: 'RET-2',
  estado: 'Recogido',
  cantidadRecogida: '9.125',
  noRecogida: '1.000',
  responsableRecogida: 'Ana',
  fechaRecogida: '2026-11-21T12:00:00Z',
};
export function prepararRetornos() {
  vi.mocked(api.listarRetornos).mockResolvedValue({
    items: [retorno, recogido],
    total: 2,
    pagina: 1,
    tamanoPagina: 10,
  });
  vi.mocked(api.obtenerRetorno).mockImplementation(async (id) =>
    id === 2 ? recogido : { ...retorno, id },
  );
  vi.mocked(api.resumenRetornos).mockResolvedValue({
    total: 2,
    programados: 1,
    recogidos: 1,
    recibidos: 0,
    cancelados: 0,
  });
  vi.mocked(api.crearRetorno).mockResolvedValue({ id: 9 });
  vi.mocked(api.actualizarRetorno).mockResolvedValue({ id: 1 });
  vi.mocked(api.recogerRetorno).mockResolvedValue({ id: 1 });
  vi.mocked(api.recibirRetorno).mockResolvedValue({ id: 1 });
  vi.mocked(api.cancelarRetorno).mockResolvedValue({ id: 1 });
  vi.mocked(api.eliminarRetorno).mockResolvedValue(undefined);
}
export function renderRetornos() {
  return render(
    <MemoryRouter>
      <LogisticaInversaPage />
    </MemoryRouter>,
  );
}
export async function esperarCargaInicialRetornos() {
  renderRetornos();
  await act(async () => {
    await Promise.resolve();
  });
  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'Ver retorno RET-1' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Resumen de retornos' })).toBeInTheDocument();
    expect(api.listarRetornos).toHaveBeenCalledTimes(1);
    expect(api.resumenRetornos).toHaveBeenCalledTimes(1);
  });
}
