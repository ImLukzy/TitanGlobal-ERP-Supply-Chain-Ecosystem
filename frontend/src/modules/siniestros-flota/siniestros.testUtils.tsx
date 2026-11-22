import { act, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import * as api from './api';
import { SiniestrosFlotaPage } from './SiniestrosFlotaPage';
import type { ValoresSiniestro, ValoresReclamo, SiniestroDetalle } from './tipos';
export const valores: ValoresSiniestro = {
  codigo: 'SIN-1',
  placa: 'ABC-500',
  lugar: 'Patio Norte',
  descripcion: 'Incidente registrado manualmente',
  tipo: 'Accidente',
  fechaIncidente: '2026-11-23T10:30:12.345',
  observaciones: '',
};
export const valoresReclamo: ValoresReclamo = {
  numeroPoliza: 'POL-50',
  aseguradora: 'Aseguradora Norte',
  numeroReclamo: 'REC-50',
  moneda: 'pen',
  montoReclamado: '1250.50',
};
export const siniestro: SiniestroDetalle = {
  ...valores,
  id: 1,
  estado: 'Registrado',
  fechaIncidente: valores.fechaIncidente + 'Z',
};
export const reclamado: SiniestroDetalle = {
  ...siniestro,
  ...valoresReclamo,
  id: 2,
  codigo: 'SIN-2',
  estado: 'EnReclamo',
  moneda: 'PEN',
  fechaReclamo: '2026-11-22T12:00:00Z',
};
export function prepararSiniestros() {
  vi.mocked(api.listarSiniestros).mockResolvedValue({
    items: [siniestro, reclamado],
    total: 2,
    pagina: 1,
    tamanoPagina: 10,
  });
  vi.mocked(api.obtenerSiniestro).mockImplementation(async (id) =>
    id === 2 ? reclamado : { ...siniestro, id },
  );
  vi.mocked(api.resumenSiniestros).mockResolvedValue({
    total: 2,
    registrados: 1,
    enReclamo: 1,
    cerrados: 0,
    anulados: 0,
  });
  vi.mocked(api.crearSiniestro).mockResolvedValue({ id: 9 });
  vi.mocked(api.actualizarSiniestro).mockResolvedValue({ id: 1 });
  vi.mocked(api.reclamarSiniestro).mockResolvedValue({ id: 1 });
  vi.mocked(api.cerrarSiniestro).mockResolvedValue({ id: 1 });
  vi.mocked(api.anularSiniestro).mockResolvedValue({ id: 1 });
  vi.mocked(api.eliminarSiniestro).mockResolvedValue(undefined);
}
export function renderSiniestros() {
  return render(
    <MemoryRouter>
      <SiniestrosFlotaPage />
    </MemoryRouter>,
  );
}
export async function esperarCargaInicialSiniestros() {
  renderSiniestros();
  await act(async () => {
    await Promise.resolve();
  });
  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'Ver siniestro SIN-1' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Resumen de siniestros' })).toBeInTheDocument();
    expect(api.listarSiniestros).toHaveBeenCalledTimes(1);
    expect(api.resumenSiniestros).toHaveBeenCalledTimes(1);
  });
}
