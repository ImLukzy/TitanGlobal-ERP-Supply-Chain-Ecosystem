import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  limpiarPrestamos,
  moneda,
  montarPrestamosCargados,
  prepararApiPrestamos,
  type LlamadaPrestamos,
  type OpcionesApiPrestamos,
} from './prestamos.testUtils';

afterEach(() => {
  limpiarPrestamos();
});

async function abrirAlta(fetchMock: ReturnType<typeof vi.fn>): Promise<ReturnType<typeof within>> {
  await montarPrestamosCargados(fetchMock);
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo préstamo' }));
  await screen.findByRole('option', { name: 'EMP-000001 · Torres Vega, Ana' });
  return within(screen.getByRole('region', { name: 'Nuevo préstamo' }));
}

async function abrir(opciones: OpcionesApiPrestamos = {}) {
  const api = prepararApiPrestamos(opciones);
  const formulario = await abrirAlta(api.fetchMock);
  return { ...api, formulario };
}

function escrituras(llamadas: LlamadaPrestamos[]): LlamadaPrestamos[] {
  return llamadas.filter((llamada) => llamada.metodo === 'POST');
}

function urlsDeCapacidad(llamadas: LlamadaPrestamos[]): string[] {
  return llamadas.map((llamada) => llamada.url).filter((url) => url.includes('/prestamos/capacidad/'));
}

function registrar(formulario: ReturnType<typeof within>): void {
  fireEvent.click(formulario.getByRole('button', { name: 'Registrar préstamo' }));
}

function rellenar(formulario: ReturnType<typeof within>, campos: { empleado: string; tipo: string; monto: string; cuotas?: string; fecha: string; mes: string }): void {
  fireEvent.change(formulario.getByLabelText('Empleado'), { target: { value: campos.empleado } });
  fireEvent.change(formulario.getByLabelText('Tipo'), { target: { value: campos.tipo } });
  fireEvent.change(formulario.getByLabelText('Monto'), { target: { value: campos.monto } });
  if (campos.cuotas !== undefined) {
    fireEvent.change(formulario.getByLabelText('Número de cuotas'), { target: { value: campos.cuotas } });
  }
  fireEvent.change(formulario.getByLabelText('Fecha de otorgamiento'), { target: { value: campos.fecha } });
  fireEvent.change(formulario.getByLabelText('Primer mes de descuento'), { target: { value: campos.mes } });
  fireEvent.change(formulario.getByLabelText('Motivo'), { target: { value: ' Gastos médicos ' } });
}

describe('formulario de registro de préstamo', () => {
  it('carga los empleados activos y ofrece «Legajo · Apellidos, Nombres»', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await abrirAlta(fetchMock);
    const urls = fetchMock.mock.calls.map((llamada) => String(llamada[0]));
    expect(urls).toContain('/api/v1/empleados?estado=Activo&pagina=1&tamanoPagina=100');
    expect(screen.queryByText('Se muestran los primeros 100 empleados activos.')).not.toBeInTheDocument();
  });

  it('avisa cuando hay más de 100 empleados activos', async () => {
    const { fetchMock } = prepararApiPrestamos({ totalEmpleados: 130 });
    await abrirAlta(fetchMock);
    expect(screen.getByText('Se muestran los primeros 100 empleados activos.')).toBeInTheDocument();
  });

  it('muestra los mensajes de campos obligatorios sin enviar petición', async () => {
    const { formulario, llamadas } = await abrir();
    registrar(formulario);
    expect(await formulario.findByText('El empleado es obligatorio.')).toBeInTheDocument();
    expect(formulario.getByText('El monto es obligatorio.')).toBeInTheDocument();
    expect(formulario.getByText('El primer mes de descuento es obligatorio y debe ser un mes válido.')).toBeInTheDocument();
    expect(formulario.getByText('El motivo es obligatorio.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('valida monto, cuotas y primer mes sin enviar petición', async () => {
    const { formulario, llamadas } = await abrir();
    rellenar(formulario, { empleado: '1', tipo: 'Prestamo', monto: '10.123', cuotas: '25', fecha: '2030-01-15', mes: '2029-12' });
    registrar(formulario);
    expect(await formulario.findByText('El monto admite como máximo 2 decimales y no puede superar 9999999999.99.')).toBeInTheDocument();
    expect(formulario.getByText('El número de cuotas debe estar entre 1 y 24.')).toBeInTheDocument();
    expect(formulario.getByText('El primer mes de descuento no puede ser anterior al mes de otorgamiento.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('un adelanto fija el número de cuotas en 1 y lo deshabilita', async () => {
    const { formulario } = await abrir();
    fireEvent.change(formulario.getByLabelText('Tipo'), { target: { value: 'Adelanto' } });
    const cuotas = formulario.getByLabelText('Número de cuotas');
    expect(cuotas).toBeDisabled();
    expect(cuotas).toHaveValue('1');
  });

  it('pide la capacidad a la fecha de otorgamiento y la muestra junto con la cuota máxima', async () => {
    const { formulario, llamadas } = await abrir();
    fireEvent.change(formulario.getByLabelText('Fecha de otorgamiento'), { target: { value: '2030-01-15' } });
    fireEvent.change(formulario.getByLabelText('Empleado'), { target: { value: '1' } });
    const panel = within(await screen.findByRole('region', { name: 'Capacidad de descuento' }));
    expect(await panel.findByText('Cuota máxima mensual')).toBeInTheDocument();
    expect(panel.getByText('Cuota máxima mensual').nextElementSibling).toHaveTextContent(moneda(2479.5));
    expect(panel.getByText('Neto estimado').nextElementSibling).toHaveTextContent(moneda(8265));
    expect(urlsDeCapacidad(llamadas)).toContain('/api/v1/prestamos/capacidad/1?fecha=2030-01-15');
  });

  it('muestra el cronograma previsto con el redondeo del backend: la última cuota absorbe la diferencia', async () => {
    const { formulario } = await abrir();
    rellenar(formulario, { empleado: '1', tipo: 'Prestamo', monto: '1000', cuotas: '3', fecha: '2030-11-02', mes: '2030-11' });
    const previsto = within(await screen.findByRole('region', { name: 'Tabla del cronograma previsto' }));
    await previsto.findAllByText('Dentro del máximo');
    const filas = previsto.getAllByRole('row');
    expect(filas).toHaveLength(4);
    expect(within(filas[1] as HTMLElement).getByText('11/2030')).toBeInTheDocument();
    expect(within(filas[1] as HTMLElement).getAllByText(moneda(333.33))).toHaveLength(2);
    expect(within(filas[2] as HTMLElement).getByText('12/2030')).toBeInTheDocument();
    expect(within(filas[3] as HTMLElement).getByText('01/2031')).toBeInTheDocument();
    expect(within(filas[3] as HTMLElement).getAllByText(moneda(333.34))).toHaveLength(2);
  });

  it('avisa antes de enviar si una cuota, sumada a lo comprometido, supera el máximo', async () => {
    const { formulario, llamadas } = await abrir();
    rellenar(formulario, { empleado: '2', tipo: 'Adelanto', monto: '100', fecha: '2030-01-15', mes: '2030-02' });
    const aviso = await formulario.findByRole('alert', { name: '' });
    expect(aviso).toHaveTextContent(`Aviso: la cuota de 02/2030 sumaría ${moneda(1100)} y supera el máximo de ${moneda(1096.2)} (30 % del neto estimado).`);
    expect(formulario.getByText('Supera el máximo')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('con 96.20 la suma iguala el máximo y no avisa', async () => {
    const { formulario } = await abrir();
    rellenar(formulario, { empleado: '2', tipo: 'Adelanto', monto: '96.20', fecha: '2030-01-15', mes: '2030-02' });
    expect(await formulario.findByText('Las cuotas previstas respetan el máximo mensual.')).toBeInTheDocument();
    expect(formulario.queryByText('Supera el máximo')).not.toBeInTheDocument();
  });

  it('avisa si el empleado no tiene contrato vigente (capacidad 0)', async () => {
    const { formulario } = await abrir();
    rellenar(formulario, { empleado: '3', tipo: 'Adelanto', monto: '100', fecha: '2030-01-15', mes: '2030-02' });
    expect(await formulario.findByText(/no tiene un contrato vigente en la fecha de otorgamiento/)).toBeInTheDocument();
  });

  it('muestra el error de la capacidad sin impedir el registro', async () => {
    const { formulario } = await abrir({ errorCapacidad: true });
    fireEvent.change(formulario.getByLabelText('Empleado'), { target: { value: '1' } });
    expect(await formulario.findByText('El empleado 1 no existe.')).toHaveAttribute('role', 'alert');
    expect(formulario.getByRole('button', { name: 'Registrar préstamo' })).toBeEnabled();
  });

  it('registra con el cuerpo exacto, cierra el formulario y vuelve a pedir el listado', async () => {
    const { llamadas, fetchMock, formulario } = await abrir();
    const antes = fetchMock.mock.calls.filter((llamada) => String(llamada[0]).includes('/prestamos?')).length;
    rellenar(formulario, { empleado: '1', tipo: 'Prestamo', monto: '1000,5', cuotas: '3', fecha: '2030-11-02', mes: '2030-12' });
    registrar(formulario);
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Nuevo préstamo' })).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toEqual([
      {
        url: '/api/v1/prestamos',
        metodo: 'POST',
        cuerpo: {
          empleadoId: 1,
          tipo: 'Prestamo',
          monto: 1000.5,
          numeroCuotas: 3,
          fechaOtorgamiento: '2030-11-02',
          primerAnio: 2030,
          primerMes: 12,
          motivo: 'Gastos médicos',
        },
      },
    ]);
    await waitFor(() => expect(fetchMock.mock.calls.filter((llamada) => String(llamada[0]).includes('/prestamos?')).length).toBe(antes + 1));
  });

  it('muestra el 409 del servidor junto al formulario y conserva lo escrito', async () => {
    const mensaje = 'La cuota de 02/2030 sería 1100.00 y supera el máximo de 1096.20 (30 % del neto estimado).';
    const { formulario } = await abrir({ errorEscritura: { accion: 'registrar', status: 409, mensaje } });
    rellenar(formulario, { empleado: '2', tipo: 'Adelanto', monto: '100', fecha: '2030-01-15', mes: '2030-02' });
    registrar(formulario);
    const alertas = await formulario.findAllByText(mensaje);
    expect(alertas.some((alerta: HTMLElement) => alerta.getAttribute('role') === 'alert')).toBe(true);
    expect(formulario.getByLabelText('Monto')).toHaveValue('100');
    expect(formulario.getByLabelText('Motivo')).toHaveValue(' Gastos médicos ');
    expect(formulario.getByLabelText('Empleado')).toHaveValue('2');
  });

  it('Cancelar cierra el formulario sin enviar nada', async () => {
    const { formulario, llamadas } = await abrir();
    fireEvent.click(formulario.getByRole('button', { name: 'Cancelar' }));
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Nuevo préstamo' })).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toHaveLength(0);
  });
});
