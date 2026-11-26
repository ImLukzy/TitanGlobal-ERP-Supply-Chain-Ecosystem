import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { vi } from 'vitest';
import * as api from './api';
import { ConsolidacionCargaPage } from './ConsolidacionCargaPage';
import type { CargaDetalle } from './tipos';
export const carga: CargaDetalle = {
  id: 1,
  codigo: 'C01',
  nombre: 'Carga Norte',
  estado: 'Borrador',
  totalUnidades: 10,
  unidadesRecogidas: 10,
  totalBultos: 3,
  volumenM3: '0.180000',
  pesoKg: '6.00',
  lineas: [
    {
      id: 1,
      posicion: 1,
      sku: 'S1',
      descripcion: 'Caja',
      cantidad: 10,
      cantidadRecogida: 10,
      unidadesPorBulto: 4,
      largoCm: '50.00',
      anchoCm: '40.00',
      altoCm: '30.00',
      pesoBultoKg: '2.00',
      bultos: 3,
      volumenM3: '0.180000',
      pesoKg: '6.00',
    },
  ],
};
export const confirmada: CargaDetalle = {
  ...carga,
  id: 2,
  codigo: 'C02',
  nombre: 'Carga Centro',
  estado: 'Confirmada',
  fechaConfirmacion: '2026-11-21T12:00:00Z',
};
export function prepararCargas() {
  vi.mocked(api.listarCargas).mockResolvedValue({
    items: [carga, confirmada],
    total: 2,
    pagina: 1,
    tamanoPagina: 10,
  });
  vi.mocked(api.obtenerCarga).mockImplementation(async (id) =>
    id === 2 ? confirmada : { ...carga, id },
  );
  vi.mocked(api.resumenCargas).mockResolvedValue({
    total: 2,
    borradores: 1,
    confirmadas: 1,
    canceladas: 0,
  });
  vi.mocked(api.crearCarga).mockResolvedValue({ id: 9 });
  vi.mocked(api.actualizarCarga).mockResolvedValue({ id: 1 });
  vi.mocked(api.confirmarCarga).mockResolvedValue({ id: 1 });
  vi.mocked(api.cancelarCarga).mockResolvedValue({ id: 1 });
  vi.mocked(api.eliminarCarga).mockResolvedValue(undefined);
}
export function renderCargas() {
  return render(
    <MemoryRouter>
      <ConsolidacionCargaPage />
    </MemoryRouter>,
  );
}
