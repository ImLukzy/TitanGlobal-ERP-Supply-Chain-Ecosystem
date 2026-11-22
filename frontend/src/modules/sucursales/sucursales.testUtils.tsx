import { act, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import * as api from './api';
import { SucursalesPage } from './SucursalesPage';
import type { ValoresSucursal, SucursalDetalle } from './tipos';
export const valores: ValoresSucursal = {
  codigo: 'SUC-1',
  nombre: 'Sucursal Norte',
  tipo: 'Oficina',
  pais: 'Peru',
  ciudad: 'Lima',
  direccion: 'Avenida Norte 123',
  contacto: '',
  email: '',
  telefono: '',
  observaciones: '',
  fechaApertura: '2026-11-23T10:30:12.345',
};
export const sucursal: SucursalDetalle = {
  ...valores,
  id: 1,
  activa: true,
  contacto: null,
  email: null,
  telefono: null,
  observaciones: null,
  fechaApertura: valores.fechaApertura + 'Z',
  fechaRegistro: '2026-11-22T10:00:00Z',
  fechaCambioEstado: null,
};
export const inactiva: SucursalDetalle = {
  ...sucursal,
  id: 2,
  codigo: 'SUC-2',
  tipo: 'Filial',
  activa: false,
  fechaCambioEstado: '2026-11-22T12:00:00Z',
};
export function prepararSucursales() {
  vi.mocked(api.listarSucursales).mockResolvedValue({
    items: [sucursal, inactiva],
    total: 2,
    pagina: 1,
    tamanoPagina: 10,
  });
  vi.mocked(api.obtenerSucursal).mockImplementation(async (id) =>
    id === 2 ? inactiva : { ...sucursal, id },
  );
  vi.mocked(api.resumenSucursales).mockResolvedValue({
    total: 2,
    activas: 1,
    inactivas: 1,
    filiales: 1,
    plantas: 0,
    oficinas: 1,
  });
  vi.mocked(api.crearSucursal).mockResolvedValue({ id: 9 });
  vi.mocked(api.actualizarSucursal).mockResolvedValue({ id: 1 });
  vi.mocked(api.activarSucursal).mockResolvedValue({ id: 1 });
  vi.mocked(api.desactivarSucursal).mockResolvedValue({ id: 1 });
  vi.mocked(api.eliminarSucursal).mockResolvedValue(undefined);
}
export function renderSucursales() {
  return render(
    <MemoryRouter>
      <SucursalesPage />
    </MemoryRouter>,
  );
}
export async function esperarCargaInicialSucursales() {
  renderSucursales();
  await act(async () => {
    await Promise.resolve();
  });
  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'Ver sucursal SUC-1' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Resumen de sucursales' })).toBeInTheDocument();
    expect(api.listarSucursales).toHaveBeenCalledTimes(1);
    expect(api.resumenSucursales).toHaveBeenCalledTimes(1);
  });
}
