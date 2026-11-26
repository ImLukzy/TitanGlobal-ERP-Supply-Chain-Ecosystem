import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { ImpuestosTasasPage } from './ImpuestosTasasPage';
import type { ImpuestoDetalle, ImpuestoListado } from './tipos';

vi.mock('./api');

const igv: ImpuestoDetalle = {
  id: 1, codigo: 'IGV', nombre: 'Impuesto general a las ventas', tipo: 'IGV', pais: 'PER', activo: true,
  descripcion: null, tasas: [{ id: 1, activo: true, porcentaje: 18, fechaInicio: '2026-01-01T00:00:00Z',
    fechaFin: null, observacion: 'Tasa de prueba' }],
};
const lista: ImpuestoListado[] = [{
  id: 1, codigo: igv.codigo, nombre: igv.nombre, tipo: 'IGV', pais: 'PER', activo: true,
  porcentajeVigente: 18, fechaInicioVigencia: '2026-01-01T00:00:00Z',
}, {
  id: 2, codigo: 'IVA', nombre: 'Impuesto al valor agregado', tipo: 'IVA', pais: 'CHL', activo: false,
  porcentajeVigente: null, fechaInicioVigencia: null,
}];

function abrir() {
  render(<MemoryRouter><ImpuestosTasasPage /></MemoryRouter>);
}

async function detalle() {
  fireEvent.click(await screen.findByRole('button', { name: 'IGV' }));
  await screen.findByText('Tasa de prueba');
}

afterEach(cleanup);

beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(api.listarImpuestos).mockResolvedValue(lista);
  vi.mocked(api.obtenerImpuesto).mockResolvedValue(igv);
});

describe('Impuestos y tasas', () => {
  it('distingue carga, catálogo y resultado vacío filtrado', async () => {
    abrir();
    expect(screen.getByRole('status')).toHaveTextContent('Cargando impuestos');
    fireEvent.change(await screen.findByLabelText('Tipo de impuesto'), { target: { value: 'IVA' } });
    const filaVisible = await screen.findByRole('button', { name: 'IVA' });
    expect(filaVisible.closest('tbody > tr')).not.toBeNull();
    expect(screen.queryByRole('button', { name: 'IGV' })).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Actividad'), { target: { value: 'activo' } });
    expect(screen.getByText('No hay impuestos para estos filtros.')).toBeInTheDocument();
  });

  it('pagina localmente porque el contrato devuelve el catálogo completo', async () => {
    vi.mocked(api.listarImpuestos).mockResolvedValue(Array.from({ length: 11 }, (_, indice) =>
      ({ ...lista[0], id: indice + 1, codigo: 'T-' + (indice + 1) })));
    abrir();
    expect(await screen.findByText('Página 1 de 2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    expect(screen.getByRole('button', { name: 'T-11' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'T-1' })).not.toBeInTheDocument();
  });

  it('valida campos y guarda un impuesto con posterior actualización', async () => {
    vi.mocked(api.crearImpuesto).mockResolvedValue({ id: 1 });
    abrir();
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo impuesto' }));
    const form = screen.getByRole('form', { name: 'Datos del impuesto' });
    fireEvent.submit(form);
    expect(screen.getByRole('alert')).toHaveTextContent('Indique el código');
    fireEvent.change(within(form).getByLabelText('Código'), { target: { value: 'IGV' } });
    fireEvent.change(within(form).getByLabelText('Nombre'), { target: { value: igv.nombre } });
    fireEvent.submit(form);
    await screen.findByText('Impuesto guardado.');
    expect(api.crearImpuesto).toHaveBeenCalledWith({ codigo: 'IGV', nombre: igv.nombre, tipo: 'IGV',
      descripcion: null, pais: 'PER', activo: true });
    await waitFor(() => expect(api.listarImpuestos).toHaveBeenCalledTimes(2));
  });

  it('valida el rango de fechas y guarda el día calendario de la tasa', async () => {
    vi.mocked(api.registrarTasa).mockResolvedValue({ id: 2, impuestoId: 1 });
    abrir();
    await detalle();
    fireEvent.click(screen.getByRole('button', { name: 'Registrar tasa' }));
    const form = screen.getByRole('form', { name: 'Registrar tasa' });
    fireEvent.change(within(form).getByLabelText('Porcentaje'), { target: { value: '19' } });
    fireEvent.change(within(form).getByLabelText('Fecha de inicio'), { target: { value: '2026-10-01' } });
    fireEvent.change(within(form).getByLabelText('Fecha final (opcional)'), { target: { value: '2026-09-30' } });
    fireEvent.submit(form);
    expect(screen.getByRole('alert')).toHaveTextContent('La fecha final no puede ser anterior');
    fireEvent.change(within(form).getByLabelText('Fecha final (opcional)'), { target: { value: '2026-10-31' } });
    fireEvent.submit(form);
    await screen.findByText('Tasa registrada.');
    expect(api.registrarTasa).toHaveBeenCalledWith(1, {
      porcentaje: 19, fechaInicio: '2026-10-01', fechaFin: '2026-10-31', observacion: null,
    });
  });

  it('muestra el cálculo que devuelve la API', async () => {
    vi.mocked(api.calcularImpuesto).mockResolvedValue({ impuestoId: 1, codigo: 'IGV', nombre: igv.nombre, baseImponible: 100,
      porcentajeAplicado: 18, importeImpuesto: 18, total: 118, fechaOperacion: '2026-10-01T00:00:00Z' });
    abrir();
    await detalle();
    fireEvent.click(screen.getByRole('button', { name: 'Calcular impuesto' }));
    const form = screen.getByRole('form', { name: 'Calcular impuesto' });
    fireEvent.change(within(form).getByLabelText('Base imponible'), { target: { value: '100' } });
    fireEvent.change(within(form).getByLabelText('Fecha de operación'), { target: { value: '2026-10-01' } });
    fireEvent.submit(form);
    expect(await screen.findByText('118.00')).toBeInTheDocument();
    expect(api.calcularImpuesto).toHaveBeenCalledWith({ impuestoId: 1, baseImponible: 100, fechaOperacion: '2026-10-01' });
  });

  it('confirma la desactivación y permite reactivar desde la edición', async () => {
    vi.mocked(api.obtenerImpuesto).mockResolvedValueOnce(igv).mockResolvedValueOnce({ ...igv, activo: false });
    vi.mocked(api.desactivarImpuesto).mockResolvedValue(undefined);
    vi.mocked(api.actualizarImpuesto).mockResolvedValue(undefined);
    abrir();
    await detalle();
    fireEvent.click(screen.getByRole('button', { name: 'Desactivar impuesto' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
    await screen.findByText('Impuesto desactivado.');
    expect(api.desactivarImpuesto).toHaveBeenCalledWith(1);
    fireEvent.click(await screen.findByRole('button', { name: 'Activar impuesto' }));
    expect(screen.queryByRole('button', { name: 'Registrar tasa' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
    await screen.findByText('Impuesto activado.');
    expect(api.actualizarImpuesto).toHaveBeenCalledWith(1, expect.objectContaining({ activo: true }));
  });

  it.each([400, 401, 403, 404, 409])('conserva el literal de un error HTTP %s', async estado => {
    vi.mocked(api.listarImpuestos).mockRejectedValue(new ApiError(estado, 'Mensaje de la API ' + estado));
    abrir();
    expect(await screen.findByRole('alert')).toHaveTextContent('Mensaje de la API ' + estado);
  });

  it('mantiene disponible el formulario tras una numeración duplicada', async () => {
    vi.mocked(api.actualizarImpuesto).mockRejectedValue(new ApiError(409, 'Ya existe un impuesto con el código IGV.'));
    abrir();
    await detalle();
    fireEvent.click(screen.getByRole('button', { name: 'Editar impuesto' }));
    const form = screen.getByRole('form', { name: 'Datos del impuesto' });
    fireEvent.submit(form);
    expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe un impuesto con el código IGV.');
    expect(form).toBeInTheDocument();
  });
});
