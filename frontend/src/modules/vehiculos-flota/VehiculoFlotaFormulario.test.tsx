import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { esperarCargaVehiculos, prepararApiVehiculos, renderVehiculos } from './vehiculosFlota.testUtils';
import { validarVehiculoFlota } from './validacion';

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

async function abrirFormulario(errorGuardar?: 400 | 409) {
  const api = prepararApiVehiculos({ errorGuardar });
  renderVehiculos();
  await esperarCargaVehiculos(api.fetchMock);
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo vehículo' }));
  const formulario = within(screen.getByRole('region', { name: 'Nuevo vehículo' }));
  for (const [campo, valor] of [['Placa', 'NUE-001'], ['Marca', 'Volvo'], ['Modelo', 'FH'], ['Capacidad (kg)', '1200.25']]) {
    fireEvent.change(formulario.getByLabelText(campo!), { target: { value: valor } });
  }
  return { ...api, formulario };
}

describe('Formulario de vehículos de flota', () => {
  it.each([
    ['Placa', '', 'La placa es obligatoria.'],
    ['Placa', 'P'.repeat(21), 'La placa no puede superar 20 caracteres.'],
    ['Marca', ' ', 'La marca es obligatoria.'],
    ['Marca', 'M'.repeat(101), 'La marca no puede superar 100 caracteres.'],
    ['Modelo', '', 'El modelo es obligatorio.'],
    ['Modelo', 'M'.repeat(101), 'El modelo no puede superar 100 caracteres.'],
    ['Año', '1899', 'El año del vehículo no es válido.'],
    ['Año', '2025.5', 'El año del vehículo no es válido.'],
    ['Año', String(new Date().getUTCFullYear() + 2), 'El año del vehículo no es válido.'],
    ['Capacidad (kg)', '0', 'La capacidad debe ser mayor que cero.'],
    ['Capacidad (kg)', '-1', 'La capacidad debe ser mayor que cero.'],
    ['Capacidad (kg)', '1e3', 'La capacidad debe ser mayor que cero.'],
    ['Capacidad (kg)', '1.001', 'La capacidad no puede tener más de 2 decimales.'],
    ['Capacidad (kg)', '10000000000000000', 'La capacidad no puede superar 9999999999999999.99.'],
    ['Capacidad (kg)', '900719925474099.99', 'La capacidad no puede representarse con precisión. Introduce una cantidad menor.'],
    ['Observaciones (opcional)', 'O'.repeat(501), 'Las observaciones no pueden superar 500 caracteres.'],
  ])('valida %s sin enviar datos', async (campo, valor, mensaje) => {
    const { fetchMock, formulario } = await abrirFormulario();
    const input = formulario.getByLabelText(campo);
    fireEvent.change(input, { target: { value: valor } });
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar vehículo' }));
    expect(formulario.getByText(mensaje)).toBeInTheDocument();
    expect(input).toHaveAttribute('aria-invalid', 'true');
    expect(input).toHaveAccessibleDescription(mensaje);
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'POST')).toBe(false);
  });

  it('valida tipos desconocidos y vacíos con los mensajes del backend', () => {
    const base = { placa: 'ABC', marca: 'Volvo', modelo: 'FH', anio: '2025', tipo: '', capacidadKg: '1', activo: true, observaciones: '' };
    expect(validarVehiculoFlota(base).tipo).toBe('El tipo es obligatorio.');
    expect(validarVehiculoFlota({ ...base, tipo: 'Bus' }).tipo).toBe('El tipo debe ser Camion, Furgon, Moto u Otro.');
    expect(validarVehiculoFlota({ ...base, tipo: 'Camion', capacidadKg: '1.2300' })).toEqual({});
  });

  it('crea con textos recortados, activo marcado y observaciones vacías omitidas', async () => {
    const { fetchMock, filas, formulario } = await abrirFormulario();
    expect(formulario.getByLabelText('Activo')).toBeChecked();
    fireEvent.change(formulario.getByLabelText('Placa'), { target: { value: ' nue-001 ' } });
    fireEvent.change(formulario.getByLabelText('Marca'), { target: { value: ' Volvo ' } });
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar vehículo' }));
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Nuevo vehículo' })).not.toBeInTheDocument());
    const llamada = fetchMock.mock.calls.find((call) => call[1]?.method === 'POST');
    expect(JSON.parse(String(llamada?.[1]?.body))).toEqual({ placa: 'nue-001', marca: 'Volvo', modelo: 'FH', anio: new Date().getUTCFullYear(), tipo: 'Camion', capacidadKg: 1200.25, activo: true });
    expect(filas).toHaveLength(3);
  });

  it.each([400, 409] as const)('conserva lo escrito ante un error %s', async (errorGuardar) => {
    const { formulario } = await abrirFormulario(errorGuardar);
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar vehículo' }));
    await formulario.findByRole('alert');
    expect(formulario.getByLabelText('Placa')).toHaveValue('NUE-001');
    expect(formulario.getByLabelText('Capacidad (kg)')).toHaveValue('1200.25');
    expect(formulario.getByRole('alert')).toHaveTextContent(errorGuardar === 409 ? 'Ya existe un vehículo con la placa NUE-001.' : 'La capacidad debe ser mayor que cero.');
  });

  it('edita con PUT y actualiza el detalle', async () => {
    const { fetchMock } = prepararApiVehiculos();
    renderVehiculos();
    await esperarCargaVehiculos(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Editar vehículo' }));
    const formulario = within(screen.getByRole('region', { name: 'Editar vehículo' }));
    expect(formulario.getByLabelText('Placa')).toHaveValue('ABC-123');
    fireEvent.change(formulario.getByLabelText('Observaciones (opcional)'), { target: { value: 'Inspección completada' } });
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar vehículo' }));
    await screen.findByText('Inspección completada');
    const llamada = fetchMock.mock.calls.find((call) => call[1]?.method === 'PUT');
    expect(String(llamada?.[0])).toBe('/api/v1/vehiculos-flota/1');
    expect(JSON.parse(String(llamada?.[1]?.body)).observaciones).toBe('Inspección completada');
  });
});
