import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  limpiarContratos,
  montarContratosCargados,
  prepararApiContratos,
  type LlamadaContratos,
} from './contratosLaborales.testUtils';

afterEach(() => {
  limpiarContratos();
});

async function abrirAlta(fetchMock: ReturnType<typeof vi.fn>): Promise<ReturnType<typeof within>> {
  await montarContratosCargados(fetchMock);
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo contrato' }));
  await screen.findByRole('option', { name: 'EMP-000001 · Torres Vega, Ana' });
  return within(screen.getByRole('region', { name: 'Nuevo contrato' }));
}

function escritura(llamadas: LlamadaContratos[]): LlamadaContratos[] {
  return llamadas.filter((llamada) => llamada.metodo === 'POST');
}

function rellenarValido(formulario: ReturnType<typeof within>): void {
  fireEvent.change(formulario.getByLabelText('Empleado'), { target: { value: '3' } });
  fireEvent.change(formulario.getByLabelText('Modalidad'), { target: { value: 'PlazoFijo' } });
  fireEvent.change(formulario.getByLabelText('Fecha de inicio'), { target: { value: '2026-12-01' } });
  fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2027-11-30' } });
  fireEvent.change(formulario.getByLabelText('Sueldo básico (PEN)'), { target: { value: '2500.50' } });
  fireEvent.change(formulario.getByLabelText('Cargo'), { target: { value: ' Operario ' } });
}

function guardar(formulario: ReturnType<typeof within>): void {
  fireEvent.click(formulario.getByRole('button', { name: 'Guardar contrato' }));
}

describe('formulario de alta de contrato laboral', () => {
  it('carga los empleados activos y ofrece «Legajo · Apellidos, Nombres»', async () => {
    const { fetchMock } = prepararApiContratos();
    await abrirAlta(fetchMock);
    const urls = fetchMock.mock.calls.map((llamada) => String(llamada[0]));
    expect(urls).toContain('/api/v1/empleados?estado=Activo&pagina=1&tamanoPagina=100');
    expect(screen.queryByText('Se muestran los primeros 100 empleados activos.')).not.toBeInTheDocument();
  });

  it('avisa cuando hay más de 100 empleados activos', async () => {
    const { fetchMock } = prepararApiContratos({ totalEmpleados: 130 });
    await abrirAlta(fetchMock);
    expect(screen.getByText('Se muestran los primeros 100 empleados activos.')).toBeInTheDocument();
  });

  it('muestra los mensajes de campos obligatorios sin enviar petición', async () => {
    const { fetchMock, llamadas } = prepararApiContratos();
    const formulario = await abrirAlta(fetchMock);
    guardar(formulario);
    expect(await formulario.findByText('El empleado es obligatorio.')).toBeInTheDocument();
    expect(formulario.getByText('La fecha de inicio es obligatoria.')).toBeInTheDocument();
    expect(formulario.getByText('El sueldo básico es obligatorio.')).toBeInTheDocument();
    expect(formulario.getByText('El cargo es obligatorio.')).toBeInTheDocument();
    expect(escritura(llamadas)).toHaveLength(0);
  });

  it('exige fecha de fin en plazo fijo y que sea posterior al inicio', async () => {
    const { fetchMock, llamadas } = prepararApiContratos();
    const formulario = await abrirAlta(fetchMock);
    rellenarValido(formulario);
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '' } });
    guardar(formulario);
    expect(await formulario.findByText('La fecha de fin es obligatoria para esta modalidad.')).toBeInTheDocument();
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2026-12-01' } });
    guardar(formulario);
    expect(await formulario.findByText('La fecha de fin debe ser posterior a la fecha de inicio.')).toBeInTheDocument();
    expect(escritura(llamadas)).toHaveLength(0);
  });

  it('deshabilita y vacía la fecha de fin con la modalidad Indeterminado', async () => {
    const { fetchMock } = prepararApiContratos();
    const formulario = await abrirAlta(fetchMock);
    expect(formulario.getByLabelText('Fecha de fin')).toBeDisabled();
    fireEvent.change(formulario.getByLabelText('Modalidad'), { target: { value: 'Formativo' } });
    expect(formulario.getByLabelText('Fecha de fin')).toBeEnabled();
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2027-01-01' } });
    fireEvent.change(formulario.getByLabelText('Modalidad'), { target: { value: 'Indeterminado' } });
    expect(formulario.getByLabelText('Fecha de fin')).toBeDisabled();
    expect(formulario.getByLabelText('Fecha de fin')).toHaveValue('');
  });

  it.each([
    ['0', 'El sueldo básico debe ser mayor que cero.'],
    ['0.001', 'El sueldo básico admite como máximo 2 decimales.'],
    ['123456789012', 'El sueldo básico no puede superar 9999999999.99.'],
  ])('rechaza el sueldo %s sin enviar petición', async (sueldo, mensaje) => {
    const { fetchMock, llamadas } = prepararApiContratos();
    const formulario = await abrirAlta(fetchMock);
    rellenarValido(formulario);
    fireEvent.change(formulario.getByLabelText('Sueldo básico (PEN)'), { target: { value: sueldo } });
    guardar(formulario);
    expect(await formulario.findByText(mensaje)).toBeInTheDocument();
    expect(escritura(llamadas)).toHaveLength(0);
  });

  it('rechaza un cargo de más de 100 caracteres y muestra el contador', async () => {
    const { fetchMock, llamadas } = prepararApiContratos();
    const formulario = await abrirAlta(fetchMock);
    rellenarValido(formulario);
    fireEvent.change(formulario.getByLabelText('Cargo'), { target: { value: 'x'.repeat(101) } });
    expect(formulario.getByText('101/100')).toBeInTheDocument();
    guardar(formulario);
    expect(await formulario.findByText('El cargo no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(escritura(llamadas)).toHaveLength(0);
  });

  it('guarda con el cuerpo exacto, selecciona el contrato creado y recarga', async () => {
    const { fetchMock, llamadas } = prepararApiContratos();
    const formulario = await abrirAlta(fetchMock);
    rellenarValido(formulario);
    guardar(formulario);
    await waitFor(() => expect(escritura(llamadas)).toHaveLength(1));
    expect(escritura(llamadas)[0]).toMatchObject({
      url: '/api/v1/contratos-laborales',
      cuerpo: {
        empleadoId: 3,
        modalidad: 'PlazoFijo',
        fechaInicio: '2026-12-01',
        fechaFin: '2027-11-30',
        sueldoBasico: 2500.5,
        cargo: 'Operario',
      },
    });
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Nuevo contrato' })).not.toBeInTheDocument());
    await waitFor(() => {
      expect(llamadas.some((llamada) => llamada.url === '/api/v1/contratos-laborales/9')).toBe(true);
    });
  });

  it('muestra el 409 del servidor y conserva lo escrito', async () => {
    const mensaje = 'El empleado 3 ya tiene un contrato vigente.';
    const { fetchMock } = prepararApiContratos({ errorEscritura: { accion: 'crear', status: 409, mensaje } });
    const formulario = await abrirAlta(fetchMock);
    rellenarValido(formulario);
    guardar(formulario);
    expect(await formulario.findByText(mensaje)).toHaveAttribute('role', 'alert');
    expect(formulario.getByLabelText('Cargo')).toHaveValue(' Operario ');
    expect(formulario.getByLabelText('Sueldo básico (PEN)')).toHaveValue('2500.50');
    expect(formulario.getByLabelText('Empleado')).toHaveValue('3');
  });
});
