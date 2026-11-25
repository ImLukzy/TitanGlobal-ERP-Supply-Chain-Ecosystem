import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  limpiarAusencias,
  montarAusenciasCargadas,
  prepararApiAusencias,
  urlsDeListado,
  type LlamadaAusencias,
  type OpcionesApiAusencias,
} from './ausencias.testUtils';

afterEach(() => {
  limpiarAusencias();
});

async function abrirAlta(fetchMock: ReturnType<typeof vi.fn>): Promise<ReturnType<typeof within>> {
  await montarAusenciasCargadas(fetchMock);
  fireEvent.click(screen.getByRole('button', { name: 'Nueva ausencia' }));
  await screen.findByRole('option', { name: 'EMP-000001 · Torres Vega, Ana' });
  return within(screen.getByRole('region', { name: 'Nueva ausencia' }));
}

async function abrir(opciones: OpcionesApiAusencias = {}) {
  const api = prepararApiAusencias(opciones);
  const formulario = await abrirAlta(api.fetchMock);
  return { ...api, formulario };
}

function escrituras(llamadas: LlamadaAusencias[]): LlamadaAusencias[] {
  return llamadas.filter((llamada) => llamada.metodo === 'POST');
}

function urlsDeSaldo(llamadas: LlamadaAusencias[]): string[] {
  return llamadas.map((llamada) => llamada.url).filter((url) => url.includes('saldo-vacacional'));
}

function registrar(formulario: ReturnType<typeof within>): void {
  fireEvent.click(formulario.getByRole('button', { name: 'Registrar ausencia' }));
}

function rellenarPermiso(formulario: ReturnType<typeof within>): void {
  fireEvent.change(formulario.getByLabelText('Empleado'), { target: { value: '2' } });
  fireEvent.change(formulario.getByLabelText('Tipo'), { target: { value: 'PermisoConGoce' } });
  fireEvent.change(formulario.getByLabelText('Fecha de inicio'), { target: { value: '2099-02-02' } });
  fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2099-02-03' } });
  fireEvent.change(formulario.getByLabelText('Motivo'), { target: { value: ' Trámite bancario ' } });
}

describe('formulario de registro de ausencia', () => {
  it('carga los empleados activos y ofrece «Legajo · Apellidos, Nombres»', async () => {
    const { fetchMock } = prepararApiAusencias();
    await abrirAlta(fetchMock);
    const urls = fetchMock.mock.calls.map((llamada) => String(llamada[0]));
    expect(urls).toContain('/api/v1/empleados?estado=Activo&pagina=1&tamanoPagina=100');
    expect(screen.queryByText('Se muestran los primeros 100 empleados activos.')).not.toBeInTheDocument();
  });

  it('avisa cuando hay más de 100 empleados activos', async () => {
    const { fetchMock } = prepararApiAusencias({ totalEmpleados: 130 });
    await abrirAlta(fetchMock);
    expect(screen.getByText('Se muestran los primeros 100 empleados activos.')).toBeInTheDocument();
  });

  it('muestra los mensajes de campos obligatorios sin enviar petición', async () => {
    const { formulario, llamadas } = await abrir();
    registrar(formulario);
    expect(await formulario.findByText('El empleado es obligatorio.')).toBeInTheDocument();
    expect(formulario.getByText('La fecha de inicio es obligatoria.')).toBeInTheDocument();
    expect(formulario.getByText('La fecha de fin es obligatoria.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('exige motivo según el tipo y limita su longitud sin enviar petición', async () => {
    const { formulario, llamadas } = await abrir();
    rellenarPermiso(formulario);
    fireEvent.change(formulario.getByLabelText('Motivo'), { target: { value: '   ' } });
    registrar(formulario);
    expect(await formulario.findByText('El motivo es obligatorio para este tipo de ausencia.')).toBeInTheDocument();
    fireEvent.change(formulario.getByLabelText('Motivo'), { target: { value: 'a'.repeat(301) } });
    registrar(formulario);
    expect(await formulario.findByText('El motivo no puede superar 300 caracteres.')).toBeInTheDocument();
    expect(formulario.getByText('301/300')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('cambia la ayuda del motivo según el tipo', async () => {
    const { formulario } = await abrir();
    expect(formulario.getByText('Opcional para este tipo de ausencia.')).toBeInTheDocument();
    fireEvent.change(formulario.getByLabelText('Tipo'), { target: { value: 'DescansoMedico' } });
    expect(formulario.getByText('Obligatorio para este tipo de ausencia.')).toBeInTheDocument();
  });

  it('valida el orden de las fechas y los topes de maternidad y paternidad', async () => {
    const { formulario, llamadas } = await abrir();
    fireEvent.change(formulario.getByLabelText('Empleado'), { target: { value: '1' } });
    fireEvent.change(formulario.getByLabelText('Fecha de inicio'), { target: { value: '2099-02-10' } });
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2099-02-09' } });
    registrar(formulario);
    expect(await formulario.findByText('La fecha de fin no puede ser anterior a la fecha de inicio.')).toBeInTheDocument();
    fireEvent.change(formulario.getByLabelText('Tipo'), { target: { value: 'LicenciaPaternidad' } });
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2099-02-20' } });
    registrar(formulario);
    expect(await formulario.findByText('La licencia de paternidad no puede superar 10 días.')).toBeInTheDocument();
    fireEvent.change(formulario.getByLabelText('Tipo'), { target: { value: 'LicenciaMaternidad' } });
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2099-07-10' } });
    registrar(formulario);
    expect(await formulario.findByText('La licencia de maternidad no puede superar 98 días.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('calcula en vivo los días de la solicitud con ambos extremos incluidos', async () => {
    const { formulario } = await abrir();
    expect(formulario.getByText('Días de la solicitud: indica las fechas')).toBeInTheDocument();
    fireEvent.change(formulario.getByLabelText('Fecha de inicio'), { target: { value: '2099-12-30' } });
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2100-01-02' } });
    expect(formulario.getByText('Días de la solicitud: 4')).toBeInTheDocument();
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2099-12-30' } });
    expect(formulario.getByText('Días de la solicitud: 1')).toBeInTheDocument();
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2099-12-01' } });
    expect(formulario.getByText('Días de la solicitud: indica las fechas')).toBeInTheDocument();
  });

  it('muestra el saldo vacacional al elegir empleado con el tipo Vacaciones y los días que pide la solicitud', async () => {
    const { formulario, llamadas } = await abrir();
    expect(formulario.queryByRole('group', { name: 'Saldo vacacional' })).not.toBeInTheDocument();
    fireEvent.change(formulario.getByLabelText('Fecha de inicio'), { target: { value: '2099-01-04' } });
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2099-01-15' } });
    fireEvent.change(formulario.getByLabelText('Empleado'), { target: { value: '1' } });
    const saldo = within(await formulario.findByRole('group', { name: 'Saldo vacacional' }));
    expect(saldo.getByText('Saldo').nextElementSibling).toHaveTextContent('6.5');
    expect(saldo.getByText('Días devengados').nextElementSibling).toHaveTextContent('30.0');
    expect(saldo.getByText('La solicitud pide 12 días.')).toBeInTheDocument();
    await waitFor(() => expect(urlsDeSaldo(llamadas).at(-1)).toBe('/api/v1/ausencias/saldo-vacacional/1?fecha=2099-01-04'));
  });

  it('pide el saldo sin fecha mientras no hay fecha de inicio válida y lo oculta con otro tipo', async () => {
    const { formulario, llamadas } = await abrir();
    fireEvent.change(formulario.getByLabelText('Empleado'), { target: { value: '1' } });
    await formulario.findByRole('group', { name: 'Saldo vacacional' });
    expect(urlsDeSaldo(llamadas)).toEqual(['/api/v1/ausencias/saldo-vacacional/1']);
    fireEvent.change(formulario.getByLabelText('Tipo'), { target: { value: 'PermisoSinGoce' } });
    expect(formulario.queryByRole('group', { name: 'Saldo vacacional' })).not.toBeInTheDocument();
  });

  it('muestra el error del saldo sin impedir el registro', async () => {
    const { formulario } = await abrir({ errorSaldo: true });
    fireEvent.change(formulario.getByLabelText('Empleado'), { target: { value: '1' } });
    expect(await formulario.findByText('El empleado 1 no existe.')).toHaveAttribute('role', 'alert');
    expect(formulario.getByRole('button', { name: 'Registrar ausencia' })).toBeEnabled();
  });

  it('registra con el cuerpo exacto, selecciona la nueva ausencia y recarga el listado', async () => {
    const { fetchMock, llamadas, formulario } = await abrir();
    const antes = urlsDeListado(fetchMock).length;
    rellenarPermiso(formulario);
    registrar(formulario);
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Nueva ausencia' })).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toEqual([
      {
        url: '/api/v1/ausencias',
        metodo: 'POST',
        cuerpo: { empleadoId: 2, tipo: 'PermisoConGoce', fechaInicio: '2099-02-02', fechaFin: '2099-02-03', motivo: 'Trámite bancario' },
      },
    ]);
    await waitFor(() => expect(urlsDeListado(fetchMock)).toHaveLength(antes + 1));
    expect(llamadas.map((llamada) => llamada.url)).toContain('/api/v1/ausencias/8');
  });

  it('envía el motivo como nulo en unas vacaciones sin motivo', async () => {
    const { llamadas, formulario } = await abrir();
    fireEvent.change(formulario.getByLabelText('Empleado'), { target: { value: '1' } });
    fireEvent.change(formulario.getByLabelText('Fecha de inicio'), { target: { value: '2099-02-02' } });
    fireEvent.change(formulario.getByLabelText('Fecha de fin'), { target: { value: '2099-02-04' } });
    registrar(formulario);
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]?.cuerpo).toEqual({ empleadoId: 1, tipo: 'Vacaciones', fechaInicio: '2099-02-02', fechaFin: '2099-02-04', motivo: null });
  });

  it.each([
    ['El saldo vacacional es de 6.5 días y la solicitud pide 12.'],
    ['La ausencia se cruza con otra ausencia pendiente o aprobada del empleado.'],
    ['El empleado 2 está cesado y no admite ausencias.'],
  ])('muestra el 409 «%s» junto al formulario y conserva lo escrito', async (mensaje) => {
    const { formulario } = await abrir({ errorEscritura: { accion: 'registrar', status: 409, mensaje } });
    rellenarPermiso(formulario);
    registrar(formulario);
    expect(await formulario.findByText(mensaje)).toHaveAttribute('role', 'alert');
    expect(formulario.getByLabelText('Empleado')).toHaveValue('2');
    expect(formulario.getByLabelText('Tipo')).toHaveValue('PermisoConGoce');
    expect(formulario.getByLabelText('Fecha de inicio')).toHaveValue('2099-02-02');
    expect(formulario.getByLabelText('Fecha de fin')).toHaveValue('2099-02-03');
    expect(formulario.getByLabelText('Motivo')).toHaveValue(' Trámite bancario ');
    expect(formulario.getByRole('button', { name: 'Registrar ausencia' })).toBeEnabled();
  });

  it('el botón Cancelar cierra el formulario sin enviar nada', async () => {
    const { llamadas, formulario } = await abrir();
    fireEvent.click(formulario.getByRole('button', { name: 'Cancelar' }));
    expect(screen.queryByRole('region', { name: 'Nueva ausencia' })).not.toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });
});
