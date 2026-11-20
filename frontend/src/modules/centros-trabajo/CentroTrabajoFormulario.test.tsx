import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { esperarCargaCentros, prepararApiCentros, renderCentros } from './centrosTrabajo.testUtils';

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

async function abrirFormulario(errorGuardar?: 400 | 409) {
  const api = prepararApiCentros({ errorGuardar });
  renderCentros();
  await esperarCargaCentros(api.fetchMock);
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo centro de trabajo' }));
  const formulario = within(screen.getByRole('region', { name: 'Nuevo centro de trabajo' }));
  fireEvent.change(formulario.getByLabelText('Código'), { target: { value: 'ct-new-01' } });
  fireEvent.change(formulario.getByLabelText('Nombre'), { target: { value: 'Celda nueva' } });
  fireEvent.change(formulario.getByLabelText('Planta'), { target: { value: 'Planta Cusco' } });
  fireEvent.change(formulario.getByLabelText('Capacidad (h/día)'), { target: { value: '8' } });
  return { ...api, formulario };
}

describe('Formulario de centros de trabajo', () => {
  it.each([
    ['Código', '', 'El código es obligatorio.'],
    ['Código', 'A', 'El código solo admite letras, números y guiones (2 a 20 caracteres).'],
    ['Nombre', ' ', 'El nombre del centro de trabajo es obligatorio.'],
    ['Nombre', 'N'.repeat(201), 'El nombre del centro de trabajo no puede superar 200 caracteres.'],
    ['Planta', ' ', 'La planta es obligatoria.'],
    ['Planta', 'P'.repeat(101), 'La planta no puede superar 100 caracteres.'],
    ['Capacidad (h/día)', '0', 'La capacidad debe ser mayor que 0 y como máximo 24 horas por día.'],
    ['Capacidad (h/día)', '24.01', 'La capacidad debe ser mayor que 0 y como máximo 24 horas por día.'],
    ['Capacidad (h/día)', '0.001', 'La capacidad admite como máximo 2 decimales.'],
    ['Costo por hora (S/)', '-0.01', 'El costo por hora no puede ser negativo.'],
    ['Costo por hora (S/)', '1.001', 'El costo por hora admite como máximo 2 decimales.'],
    ['Costo por hora (S/)', '10000000000000000', 'El costo por hora no puede superar 9999999999999999.99.'],
    ['Costo por hora (S/)', '', 'Introduce un costo por hora válido.'],
    ['Costo por hora (S/)', 'NaN', 'Introduce un costo por hora válido.'],
    ['Costo por hora (S/)', '900719925474099.99', 'El costo por hora no puede representarse con precisión. Introduce un importe menor.'],
    ['Responsable (opcional)', 'R'.repeat(101), 'El responsable no puede superar 100 caracteres.'],
  ])('valida %s = %s sin enviar datos', async (campo, valor, mensaje) => {
    const { fetchMock, formulario } = await abrirFormulario();
    const input = formulario.getByLabelText(campo);
    fireEvent.change(input, { target: { value: valor } });
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar centro de trabajo' }));
    expect(formulario.getByText(mensaje)).toBeInTheDocument();
    expect(input).toHaveAttribute('aria-invalid', 'true');
    expect(input).toHaveAccessibleDescription(mensaje);
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'POST')).toBe(false);
  });

  it('rechaza un tipo manipulado sin enviar petición', async () => {
    const { fetchMock, formulario } = await abrirFormulario();
    fireEvent.change(formulario.getByLabelText('Tipo'), { target: { value: 'Taller' } });
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar centro de trabajo' }));
    expect(formulario.getByText('El tipo de centro de trabajo no es válido.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'POST')).toBe(false);
  });

  it('crea con código normalizado, capacidad 24, costo 0 y responsable vacío omitido', async () => {
    const { fetchMock, filas, formulario } = await abrirFormulario();
    expect(formulario.getByLabelText('Código')).toHaveValue('CT-NEW-01');
    expect(formulario.getByLabelText('Activo')).toBeChecked();
    fireEvent.change(formulario.getByLabelText('Nombre'), { target: { value: ' Celda nueva ' } });
    fireEvent.change(formulario.getByLabelText('Capacidad (h/día)'), { target: { value: '24' } });
    fireEvent.change(formulario.getByLabelText('Responsable (opcional)'), { target: { value: '   ' } });
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar centro de trabajo' }));
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Nuevo centro de trabajo' })).not.toBeInTheDocument());
    const llamada = fetchMock.mock.calls.find((call) => call[1]?.method === 'POST');
    expect(JSON.parse(String(llamada?.[1]?.body))).toEqual({
      codigo: 'CT-NEW-01', nombre: 'Celda nueva', tipo: 'Linea', planta: 'Planta Cusco', capacidadHorasDia: 24, costoHora: 0, activo: true,
    });
    expect(filas).toHaveLength(6);
    await screen.findByRole('option', { name: 'Planta Cusco' });
    expect(await screen.findByText('92.00')).toBeInTheDocument();
  });

  it.each([400, 409] as const)('conserva valores y muestra error %s', async (codigo) => {
    const { formulario } = await abrirFormulario(codigo);
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar centro de trabajo' }));
    const alerta = await formulario.findByRole('alert');
    expect(alerta).toHaveTextContent(codigo === 400 ? 'La planta no puede superar 100 caracteres.' : 'Ya existe un centro de trabajo con el código CT-NEW-01.');
    expect(formulario.getByLabelText('Nombre')).toHaveValue('Celda nueva');
    expect(formulario.getByLabelText('Código')).toHaveValue('CT-NEW-01');
    expect(formulario.getByLabelText('Planta')).toHaveValue('Planta Cusco');
  });

  it('edita todos los campos con PUT y refresca el detalle', async () => {
    const { fetchMock } = prepararApiCentros();
    renderCentros();
    await esperarCargaCentros(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Editar centro de trabajo' }));
    const formulario = within(screen.getByRole('region', { name: 'Editar centro de trabajo' }));
    fireEvent.change(formulario.getByLabelText('Nombre'), { target: { value: 'Centro actualizado' } });
    fireEvent.change(formulario.getByLabelText('Tipo'), { target: { value: 'Linea' } });
    fireEvent.change(formulario.getByLabelText('Costo por hora (S/)'), { target: { value: '85.50' } });
    fireEvent.change(formulario.getByLabelText('Responsable (opcional)'), { target: { value: ' Rosa ' } });
    fireEvent.click(formulario.getByLabelText('Activo'));
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar centro de trabajo' }));
    await screen.findByRole('heading', { name: 'Centro actualizado' });
    const llamada = fetchMock.mock.calls.find((call) => call[1]?.method === 'PUT');
    expect(String(llamada?.[0])).toBe('/api/v1/centros-trabajo/5');
    expect(JSON.parse(String(llamada?.[1]?.body))).toMatchObject({ nombre: 'Centro actualizado', costoHora: 85.5, tipo: 'Linea', responsable: 'Rosa', activo: false });
  });

  it('cancelar no guarda y devuelve el foco al alta', async () => {
    const { fetchMock, formulario } = await abrirFormulario();
    fireEvent.click(formulario.getByRole('button', { name: 'Cancelar' }));
    expect(screen.queryByRole('region', { name: 'Nuevo centro de trabajo' })).not.toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'POST')).toBe(false);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Nuevo centro de trabajo' })).toHaveFocus());
  });
});
