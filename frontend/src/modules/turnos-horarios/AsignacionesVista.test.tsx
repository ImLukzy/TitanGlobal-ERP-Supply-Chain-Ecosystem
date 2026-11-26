import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { hoyUtc, sumarDias } from './presentacion';
import {
  limpiarTurnos,
  montarAsignacionesCargadas,
  montarSinFilas,
  prepararApiTurnos,
  urlsDeListadoAsignaciones,
  type LlamadaTurnos,
} from './turnosHorarios.testUtils';

afterEach(() => {
  limpiarTurnos();
});

const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar turnos.';
const TEXTO_SOLO_FUTURAS = 'Solo se puede eliminar una asignación que todavía no empezó.';

async function seleccionarAsignacion(nombre: string, titulo: string): Promise<void> {
  fireEvent.click(screen.getAllByRole('button', { name: nombre })[0] as HTMLElement);
  await screen.findByRole('heading', { name: titulo });
}

function escrituras(llamadas: LlamadaTurnos[]): LlamadaTurnos[] {
  return llamadas.filter((llamada) => llamada.metodo !== 'GET');
}

describe('vista de Asignaciones: listado y filtros', () => {
  it('con ?vista=asignaciones muestra la tabla con empleado, turno, inicio, fin y estado', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarAsignacionesCargadas(fetchMock);
    expect(screen.getByRole('tab', { name: 'Asignaciones' })).toHaveAttribute('aria-selected', 'true');
    expect(urlsDeListadoAsignaciones(fetchMock)).toEqual(['/api/v1/asignaciones-turno?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table');
    expect(within(tabla).getAllByText('Sin fin')).toHaveLength(4);
    expect(within(tabla).getAllByText('Vigente')).toHaveLength(3);
    expect(within(tabla).getByText('Finalizada')).toBeInTheDocument();
    expect(within(tabla).getByText('Futura')).toBeInTheDocument();
    expect(within(tabla).getByText('ROT4X2 · Rotativo 4x2')).toBeInTheDocument();
  });

  it('envía empleado, turno y estado a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiTurnos({ totalListado: 25 });
    await montarAsignacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsDeListadoAsignaciones(fetchMock).at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Empleado'), { target: { value: '2' } });
    await waitFor(() => expect(urlsDeListadoAsignaciones(fetchMock).at(-1)).toBe('/api/v1/asignaciones-turno?empleadoId=2&pagina=1&tamanoPagina=10'));
    fireEvent.change(screen.getByLabelText('Turno'), { target: { value: '3' } });
    await waitFor(() => expect(urlsDeListadoAsignaciones(fetchMock).at(-1)).toContain('turnoId=3'));
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Vigente' } });
    await waitFor(() => expect(urlsDeListadoAsignaciones(fetchMock).at(-1)).toBe('/api/v1/asignaciones-turno?empleadoId=2&turnoId=3&estado=Vigente&pagina=1&tamanoPagina=10'));
  });

  it('muestra el estado vacío y el error de carga', async () => {
    prepararApiTurnos({ listaVacia: true });
    await montarSinFilas(['RRHH'], '/modulos/64-turnos-horarios?vista=asignaciones');
    expect(await screen.findByText('No hay asignaciones con esos filtros.')).toHaveAttribute('role', 'status');
  });

  it('muestra el error del listado', async () => {
    prepararApiTurnos({ errorLista: true });
    await montarSinFilas(['RRHH'], '/modulos/64-turnos-horarios?vista=asignaciones');
    expect(await screen.findByText('No se pudo cargar la lista de asignaciones.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Asignaciones: detalle y programación', () => {
  it('pide la programación de los próximos 14 días y la muestra con textos', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await montarAsignacionesCargadas(fetchMock);
    await seleccionarAsignacion('EMP-000001 · Ana Torres Vega', 'Ana Torres Vega');
    const desde = hoyUtc();
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url
      === `/api/v1/asignaciones-turno/programacion?empleadoId=1&desde=${desde}&hasta=${sumarDias(desde, 13)}`)).toBe(true));
    const tabla = await screen.findByRole('table', { name: 'Programación del empleado' });
    expect(within(tabla).getAllByRole('row')).toHaveLength(15);
    expect(within(tabla).getAllByText('Laborable')).toHaveLength(10);
    expect(within(tabla).getAllByText('Descanso')).toHaveLength(2);
    expect(within(tabla).getByText('Cesado')).toBeInTheDocument();
    expect(within(tabla).getByText('Sin turno')).toBeInTheDocument();
    expect(within(tabla).getAllByText('08:00 – 17:00').length).toBeGreaterThan(0);
  });

  it('muestra el error de la programación sin ocultar la ficha', async () => {
    const { fetchMock } = prepararApiTurnos({ errorProgramacion: true });
    await montarAsignacionesCargadas(fetchMock);
    await seleccionarAsignacion('EMP-000001 · Ana Torres Vega', 'Ana Torres Vega');
    expect(await screen.findByText('No se pudo cargar la programación del empleado.')).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('region', { name: 'Datos de la asignación' })).toBeInTheDocument();
  });

  it('habilita Fijar fin y deshabilita Eliminar con su texto en una vigente', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarAsignacionesCargadas(fetchMock);
    await seleccionarAsignacion('EMP-000001 · Ana Torres Vega', 'Ana Torres Vega');
    expect(screen.getByRole('button', { name: 'Fijar fin' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Eliminar' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Eliminar' })).toHaveAccessibleDescription(TEXTO_SOLO_FUTURAS);
  });

  it('habilita Eliminar solo en una futura', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarAsignacionesCargadas(fetchMock);
    fireEvent.click(screen.getAllByRole('button', { name: 'EMP-000001 · Ana Torres Vega' })[1] as HTMLElement);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Eliminar' })).toBeEnabled());
    expect(screen.getByRole('button', { name: 'Fijar fin' })).toBeEnabled();
  });

  it('deshabilita Fijar fin y Eliminar en una finalizada', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarAsignacionesCargadas(fetchMock);
    fireEvent.click(screen.getAllByRole('button', { name: 'EMP-000002 · Luis Rojas Mendoza' })[0] as HTMLElement);
    await screen.findByRole('heading', { name: 'Luis Rojas Mendoza' });
    expect(screen.getByRole('button', { name: 'Fijar fin' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Eliminar' })).toBeDisabled();
  });
});

describe('vista de Asignaciones: alta', () => {
  async function abrirAlta(fetchMock: ReturnType<typeof prepararApiTurnos>['fetchMock']): Promise<void> {
    await montarAsignacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva asignación' }));
    await screen.findByRole('heading', { name: 'Nueva asignación' });
  }

  function formulario(): HTMLElement {
    return screen.getByRole('heading', { name: 'Nueva asignación' }).closest('section') as HTMLElement;
  }

  it('envía vacío y muestra los mensajes 25, 26 y 27 sin petición de escritura', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await abrirAlta(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Guardar asignación' }));
    expect(await screen.findByText('El empleado es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El turno es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('La fecha de inicio es obligatoria.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('muestra el mensaje 28 si el fin es anterior al inicio', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await abrirAlta(fetchMock);
    const alta = within(formulario());
    fireEvent.change(alta.getByLabelText('Empleado'), { target: { value: '3' } });
    fireEvent.change(alta.getByLabelText('Turno'), { target: { value: '5' } });
    fireEvent.change(alta.getByLabelText('Fecha de inicio'), { target: { value: '2026-12-10' } });
    fireEvent.change(alta.getByLabelText('Fecha de fin (opcional)'), { target: { value: '2026-12-01' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar asignación' }));
    expect(await screen.findByText('La fecha de fin no puede ser anterior a la fecha de inicio.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('ofrece solo turnos activos y avisa del cierre de la asignación abierta anterior', async () => {
    const { fetchMock } = prepararApiTurnos();
    await abrirAlta(fetchMock);
    const selector = within(formulario()).getByLabelText('Turno');
    expect(within(selector).getByRole('option', { name: 'TARDE · Turno tarde' })).toBeInTheDocument();
    expect(within(selector).queryByRole('option', { name: 'SABADO · Sabado medio dia' })).not.toBeInTheDocument();
    expect(screen.getByText('Si el empleado tiene una asignación abierta anterior, se cerrará el día antes de la fecha de inicio.')).toBeInTheDocument();
  });

  it('crea la asignación con el cuerpo exacto y cierra el formulario', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await abrirAlta(fetchMock);
    const alta = within(formulario());
    fireEvent.change(alta.getByLabelText('Empleado'), { target: { value: '3' } });
    fireEvent.change(alta.getByLabelText('Turno'), { target: { value: '5' } });
    fireEvent.change(alta.getByLabelText('Fecha de inicio'), { target: { value: '2026-12-10' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar asignación' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]).toEqual({
      url: '/api/v1/asignaciones-turno',
      metodo: 'POST',
      cuerpo: { empleadoId: 3, turnoId: 5, fechaInicio: '2026-12-10' },
    });
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nueva asignación' })).not.toBeInTheDocument());
    await waitFor(() => expect(urlsDeListadoAsignaciones(fetchMock)).toHaveLength(2));
  });

  it('ante el 409 del cruce de fechas muestra el mensaje 33 y conserva lo escrito', async () => {
    const { fetchMock } = prepararApiTurnos({
      errorEscritura: { accion: 'crear-asignacion', status: 409, mensaje: 'El empleado 3 ya tiene un turno asignado que se cruza con esas fechas.' },
    });
    await abrirAlta(fetchMock);
    const alta = within(formulario());
    fireEvent.change(alta.getByLabelText('Empleado'), { target: { value: '3' } });
    fireEvent.change(alta.getByLabelText('Turno'), { target: { value: '5' } });
    fireEvent.change(alta.getByLabelText('Fecha de inicio'), { target: { value: '2026-12-10' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar asignación' }));
    expect(await screen.findByText('El empleado 3 ya tiene un turno asignado que se cruza con esas fechas.')).toHaveAttribute('role', 'alert');
    expect(alta.getByLabelText('Fecha de inicio')).toHaveValue('2026-12-10');
    expect(alta.getByLabelText('Empleado')).toHaveValue('3');
  });
});

describe('vista de Asignaciones: fijar fin y eliminar', () => {
  it('valida el diálogo con los mensajes 36 y 28 y no envía', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await montarAsignacionesCargadas(fetchMock);
    await seleccionarAsignacion('EMP-000001 · Ana Torres Vega', 'Ana Torres Vega');
    fireEvent.click(screen.getByRole('button', { name: 'Fijar fin' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Fijar fin de la asignación' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Guardar fecha de fin' }));
    expect(await within(dialogo).findByText('La fecha de fin es obligatoria.')).toBeInTheDocument();
    fireEvent.change(within(dialogo).getByLabelText('Fecha de fin'), { target: { value: '2021-02-28' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Guardar fecha de fin' }));
    expect(await within(dialogo).findByText('La fecha de fin no puede ser anterior a la fecha de inicio.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('fija el fin con POST a /finalizacion, cierra el diálogo y recarga', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await montarAsignacionesCargadas(fetchMock);
    await seleccionarAsignacion('EMP-000001 · Ana Torres Vega', 'Ana Torres Vega');
    fireEvent.click(screen.getByRole('button', { name: 'Fijar fin' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Fijar fin de la asignación' });
    fireEvent.change(within(dialogo).getByLabelText('Fecha de fin'), { target: { value: '2099-12-31' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Guardar fecha de fin' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]).toEqual({
      url: '/api/v1/asignaciones-turno/1/finalizacion',
      metodo: 'POST',
      cuerpo: { fechaFin: '2099-12-31' },
    });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(urlsDeListadoAsignaciones(fetchMock)).toHaveLength(2));
  });

  it('muestra el mensaje 37 del backend en el diálogo ante el 409', async () => {
    const { fetchMock } = prepararApiTurnos({
      errorEscritura: { accion: 'finalizar-asignacion', status: 409, mensaje: 'La asignación ya termina en esa fecha o antes.' },
    });
    await montarAsignacionesCargadas(fetchMock);
    await seleccionarAsignacion('EMP-000001 · Ana Torres Vega', 'Ana Torres Vega');
    fireEvent.click(screen.getByRole('button', { name: 'Fijar fin' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Fijar fin de la asignación' });
    fireEvent.change(within(dialogo).getByLabelText('Fecha de fin'), { target: { value: '2099-12-31' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Guardar fecha de fin' }));
    expect(await within(dialogo).findByText('La asignación ya termina en esa fecha o antes.')).toHaveAttribute('role', 'alert');
  });

  it('elimina una futura con DELETE tras confirmar en el diálogo', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await montarAsignacionesCargadas(fetchMock);
    fireEvent.click(screen.getAllByRole('button', { name: 'EMP-000001 · Ana Torres Vega' })[1] as HTMLElement);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Eliminar' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Eliminar asignación' });
    expect(within(dialogo).getByText('TARDE de Ana Torres Vega')).toBeInTheDocument();
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Eliminar asignación' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]).toMatchObject({ url: '/api/v1/asignaciones-turno/5', metodo: 'DELETE' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(urlsDeListadoAsignaciones(fetchMock)).toHaveLength(2));
  });

  it('muestra el mensaje 35 del backend en el diálogo de eliminación', async () => {
    const { fetchMock } = prepararApiTurnos({
      errorEscritura: { accion: 'eliminar-asignacion', status: 409, mensaje: 'Solo se puede eliminar una asignación que todavía no empezó.' },
    });
    await montarAsignacionesCargadas(fetchMock);
    fireEvent.click(screen.getAllByRole('button', { name: 'EMP-000001 · Ana Torres Vega' })[1] as HTMLElement);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Eliminar' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Eliminar asignación' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Eliminar asignación' }));
    expect(await within(dialogo).findByText('Solo se puede eliminar una asignación que todavía no empezó.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Asignaciones: roles', () => {
  it('con el rol CONSULTA deshabilita el alta, Fijar fin y Eliminar y muestra el texto', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarAsignacionesCargadas(fetchMock, ['CONSULTA']);
    fireEvent.click(screen.getAllByRole('button', { name: 'EMP-000001 · Ana Torres Vega' })[1] as HTMLElement);
    await screen.findByRole('button', { name: 'Fijar fin' });
    expect(screen.getByRole('button', { name: 'Nueva asignación' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Fijar fin' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Eliminar' })).toBeDisabled();
    expect(screen.getAllByText(TEXTO_SOLO_RRHH).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByRole('button', { name: 'Nueva asignación' })).toHaveAccessibleDescription(TEXTO_SOLO_RRHH);
  });

  it.each(['RRHH', 'ADMIN'])('con el rol %s habilita el alta', async (rol) => {
    const { fetchMock } = prepararApiTurnos();
    await montarAsignacionesCargadas(fetchMock, [rol]);
    expect(screen.getByRole('button', { name: 'Nueva asignación' })).toBeEnabled();
    expect(screen.queryByText(TEXTO_SOLO_RRHH)).not.toBeInTheDocument();
  });
});
