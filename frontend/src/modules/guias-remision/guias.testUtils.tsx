import { act, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import * as api from './api';
import { GuiasRemisionPage } from './GuiasRemisionPage';
import type { GuiaDetalle, ValoresGuia } from './tipos';
export const valores: ValoresGuia = {
  serie: 'T001',
  numero: '1',
  fechaInicioTraslado: '2026-11-22T10:30:00.000',
  pesoBrutoKg: '12.34',
  observaciones: '',
  remitenteNombre: 'RemitenteNombre',
  remitenteDocumento: 'RemitenteDocumento',
  destinatarioNombre: 'DestinatarioNombre',
  destinatarioDocumento: 'DestinatarioDocument',
  direccionOrigen: 'DireccionOrigen',
  direccionDestino: 'DireccionDestino',
  motivoTraslado: 'MotivoTraslado',
  transportista: 'Transportista',
  conductor: 'Conductor',
  placa: 'Placa',
  lineas: [
    {
      clave: 'l1',
      sku: 'S1',
      descripcion: 'Caja',
      unidadMedida: 'UND',
      cantidad: '1.125',
    },
  ],
};
export const guia: GuiaDetalle = {
  ...valores,
  id: 1,
  numero: 1,
  estado: 'Borrador',
  fechaInicioTraslado: '2026-11-22T10:30:00Z',
  totalLineas: 1,
  lineas: [
    {
      id: 1,
      posicion: 1,
      sku: 'S1',
      descripcion: 'Caja',
      unidadMedida: 'UND',
      cantidad: '1.125',
    },
  ],
};
export const emitida: GuiaDetalle = {
  ...guia,
  id: 2,
  numero: 2,
  estado: 'Emitida',
  fechaEmision: '2026-11-21T12:00:00Z',
};
export function prepararGuias() {
  vi.mocked(api.listarGuias).mockResolvedValue({
    items: [guia, emitida],
    total: 2,
    pagina: 1,
    tamanoPagina: 10,
  });
  vi.mocked(api.obtenerGuia).mockImplementation(async (id) =>
    id === 2 ? emitida : { ...guia, id },
  );
  vi.mocked(api.resumenGuias).mockResolvedValue({
    total: 2,
    borradores: 1,
    emitidas: 1,
    anuladas: 0,
  });
  vi.mocked(api.crearGuia).mockResolvedValue({ id: 9 });
  vi.mocked(api.actualizarGuia).mockResolvedValue({ id: 1 });
  vi.mocked(api.emitirGuia).mockResolvedValue({ id: 1 });
  vi.mocked(api.anularGuia).mockResolvedValue({ id: 1 });
  vi.mocked(api.eliminarGuia).mockResolvedValue(undefined);
}
export function renderGuias() {
  return render(
    <MemoryRouter>
      <GuiasRemisionPage />
    </MemoryRouter>,
  );
}
export async function esperarCargaInicialGuias() {
  renderGuias();
  await act(async () => {
    await Promise.resolve();
  });
  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'Ver guía T001-00000001' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Resumen de guías' })).toBeInTheDocument();
    expect(api.listarGuias).toHaveBeenCalledTimes(1);
    expect(api.resumenGuias).toHaveBeenCalledTimes(1);
  });
}
