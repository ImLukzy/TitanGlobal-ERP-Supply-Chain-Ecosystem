import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { vi } from 'vitest';
import * as api from './api';
import { RutasDespachoPage } from './RutasDespachoPage';
import type { RutaDetalle } from './tipos';
export const ruta: RutaDetalle = {
  id: 1,
  codigo: 'R01',
  nombre: 'Corredor Norte',
  zona: 'Norte',
  origen: 'Lima',
  destino: 'Trujillo',
  distanciaKm: '558.50',
  peajeEstimado: '98.00',
  moneda: 'PEN',
  activo: true,
};
export const rutaDos: RutaDetalle = {
  ...ruta,
  id: 2,
  codigo: 'R02',
  nombre: 'Corredor Sur',
  zona: 'Sur',
  destino: 'Ica',
  peajeEstimado: '9999999999999999.99',
  moneda: 'USD',
  activo: false,
};
export function prepararRutas() {
  vi.mocked(api.listarRutas).mockResolvedValue({
    items: [ruta, rutaDos],
    total: 2,
    pagina: 1,
    tamanoPagina: 10,
  });
  vi.mocked(api.listarZonas).mockResolvedValue(['Norte', 'Sur']);
  vi.mocked(api.resumenRutas).mockResolvedValue({
    totalRutas: 2,
    activas: 1,
    inactivas: 1,
    porZona: [
      { zona: 'Norte', totalRutas: 1, activas: 1 },
      { zona: 'Sur', totalRutas: 1, activas: 0 },
    ],
  });
  vi.mocked(api.obtenerRuta).mockImplementation(async (id) =>
    id === 2 ? rutaDos : { ...ruta, id },
  );
  vi.mocked(api.crearRuta).mockResolvedValue({ id: 9 });
  vi.mocked(api.actualizarRuta).mockResolvedValue({ id: 1 });
  vi.mocked(api.eliminarRuta).mockResolvedValue(undefined);
}
export function renderRutas() {
  return render(
    <MemoryRouter>
      <RutasDespachoPage />
    </MemoryRouter>,
  );
}
