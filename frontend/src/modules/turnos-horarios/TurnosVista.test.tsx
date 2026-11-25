import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  limpiarTurnos,
  montarSinFilas,
  montarTurnosCargados,
  prepararApiTurnos,
  urlsDeListadoTurnos,
} from './turnosHorarios.testUtils';

afterEach(() => {
  limpiarTurnos();
});

async function seleccionarTurno(codigo: string, nombre: string): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: codigo }));
  await screen.findByRole('heading', { name: nombre });
}

const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar turnos.';
const TEXTO_CON_ASIGNACIONES = 'El turno tiene asignaciones: crea uno nuevo.';

describe('pantalla de Turnos: listado, filtros y resumen', () => {
  it('carga una sola vez el listado en página 1 y muestra horario, tolerancia, días y estado', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    expect(screen.getByRole('heading', { name: '64 · Turnos y horarios' })).toBeInTheDocument();
    expect(urlsDeListadoTurnos(fetchMock)).toEqual(['/api/v1/turnos?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table');
    expect(within(tabla).getByText('08:00 – 17:00')).toBeInTheDocument();
    expect(within(tabla).getAllByText('L M X J V')).toHaveLength(2);
    expect(within(tabla).getByText('L M X J V S')).toBeInTheDocument();
    expect(within(tabla).getByText('4 x 2')).toBeInTheDocument();
    expect(within(tabla).getByText('15 min')).toBeInTheDocument();
    expect(within(tabla).getAllByText('Activo')).toHaveLength(4);
    expect(within(tabla).getByText('Inactivo')).toBeInTheDocument();
  });

  it('marca con (+1) el horario que cruza la medianoche', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    expect(within(screen.getByRole('table')).getByText('22:00 – 06:00 (+1)')).toBeInTheDocument();
  });

  it('muestra el resumen: activos, fijos, rotativos y empleados sin turno hoy', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    const resumen = screen.getByRole('region', { name: 'Resumen de turnos' });
    expect(within(resumen).getByText('Turnos activos').nextElementSibling).toHaveTextContent('4');
    expect(within(resumen).getByText('Fijos').nextElementSibling).toHaveTextContent('4');
    expect(within(resumen).getByText('Rotativos').nextElementSibling).toHaveTextContent('1');
    expect(within(resumen).getByText('Empleados sin turno hoy').nextElementSibling).toHaveTextContent('2');
  });

  it('envía tipo y estado a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Rotativo' } });
    await waitFor(() => expect(urlsDeListadoTurnos(fetchMock).at(-1)).toContain('tipo=Rotativo'));
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Inactivos' } });
    await waitFor(() => expect(urlsDeListadoTurnos(fetchMock).at(-1)).toContain('activo=false'));
    expect(urlsDeListadoTurnos(fetchMock).at(-1)).toBe('/api/v1/turnos?tipo=Rotativo&activo=false&pagina=1&tamanoPagina=10');
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Activos' } });
    await waitFor(() => expect(urlsDeListadoTurnos(fetchMock).at(-1)).toContain('activo=true'));
  });

  it('vuelve a la página 1 al filtrar estando en la página 2', async () => {
    const { fetchMock } = prepararApiTurnos({ totalListado: 25 });
    await montarTurnosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsDeListadoTurnos(fetchMock).at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Fijo' } });
    await waitFor(() => expect(urlsDeListadoTurnos(fetchMock).at(-1)).toBe('/api/v1/turnos?tipo=Fijo&pagina=1&tamanoPagina=10'));
  });

  it('aplica la búsqueda 300 ms después del último cambio con una sola consulta del último texto', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    const antes = urlsDeListadoTurnos(fetchMock).length;
    vi.useFakeTimers();
    const buscador = screen.getByLabelText('Buscar por código o nombre');
    fireEvent.change(buscador, { target: { value: 'N' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(200);
    });
    fireEvent.change(buscador, { target: { value: 'NOCHE' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsDeListadoTurnos(fetchMock)).toHaveLength(antes);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsDeListadoTurnos(fetchMock)).toHaveLength(antes + 1));
    expect(urlsDeListadoTurnos(fetchMock).at(-1)).toBe('/api/v1/turnos?busqueda=NOCHE&pagina=1&tamanoPagina=10');
  });

  it('muestra el estado vacío cuando no hay turnos', async () => {
    prepararApiTurnos({ listaVacia: true });
    await montarSinFilas();
    expect(await screen.findByText('No hay turnos con esos filtros.')).toHaveAttribute('role', 'status');
  });

  it('muestra el error de carga del listado', async () => {
    prepararApiTurnos({ errorLista: true });
    await montarSinFilas();
    expect(await screen.findByText('No se pudo cargar la lista de turnos.')).toHaveAttribute('role', 'alert');
  });

  it('muestra el error del resumen sin ocultar el listado', async () => {
    prepararApiTurnos({ errorResumen: true });
    await montarSinFilas();
    expect(await screen.findByText('No se pudo cargar el resumen de turnos.')).toHaveAttribute('role', 'alert');
    expect(await screen.findByRole('button', { name: 'DIA' })).toBeInTheDocument();
  });
});

describe('pantalla de Turnos: detalle y acciones', () => {
  it('muestra los datos del turno fijo con asignaciones y empleados con turno hoy', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    await seleccionarTurno('DIA', 'Turno dia');
    const datos = screen.getByRole('region', { name: 'Datos del turno' });
    expect(within(datos).getByText('Horario').nextElementSibling).toHaveTextContent('08:00 – 17:00');
    expect(within(datos).getByText('Días laborables').nextElementSibling).toHaveTextContent('Lunes, Martes, Miércoles, Jueves, Viernes');
    expect(within(datos).getByText('Asignaciones totales').nextElementSibling).toHaveTextContent('3');
    expect(within(datos).getByText('Empleados con turno hoy').nextElementSibling).toHaveTextContent('3');
  });

  it('muestra el ciclo y el inicio del turno rotativo', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    await seleccionarTurno('ROT4X2', 'Rotativo 4x2');
    const datos = screen.getByRole('region', { name: 'Datos del turno' });
    expect(within(datos).getByText('Ciclo (trabajo x descanso)').nextElementSibling).toHaveTextContent('4 x 2');
    expect(within(datos).getByText('Inicio del ciclo')).toBeInTheDocument();
  });

  it('con asignaciones deshabilita Editar y muestra el texto, pero permite desactivar', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    await seleccionarTurno('DIA', 'Turno dia');
    expect(screen.getByRole('button', { name: 'Editar' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Editar' })).toHaveAccessibleDescription(TEXTO_CON_ASIGNACIONES);
    expect(screen.getByText(TEXTO_CON_ASIGNACIONES)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Desactivar' })).toBeEnabled();
  });

  it('sin asignaciones habilita Editar', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    await seleccionarTurno('TARDE', 'Turno tarde');
    expect(screen.getByRole('button', { name: 'Editar' })).toBeEnabled();
    expect(screen.queryByText(TEXTO_CON_ASIGNACIONES)).not.toBeInTheDocument();
  });

  it('desactiva un turno activo con POST sin cuerpo y recarga la pantalla', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    await seleccionarTurno('TARDE', 'Turno tarde');
    fireEvent.click(screen.getByRole('button', { name: 'Desactivar' }));
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url === '/api/v1/turnos/5/desactivacion' && llamada.metodo === 'POST')).toBe(true));
    await waitFor(() => expect(urlsDeListadoTurnos(fetchMock)).toHaveLength(2));
  });

  it('activa un turno inactivo con POST a /activacion', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    await seleccionarTurno('SABADO', 'Sabado medio dia');
    fireEvent.click(screen.getByRole('button', { name: 'Activar' }));
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url === '/api/v1/turnos/4/activacion' && llamada.metodo === 'POST')).toBe(true));
  });

  it('muestra el mensaje 23 del backend junto al botón y recarga ante el 409', async () => {
    const { fetchMock } = prepararApiTurnos({
      errorEscritura: { accion: 'desactivar-turno', status: 409, mensaje: 'El turno TARDE ya está inactivo.' },
    });
    await montarTurnosCargados(fetchMock);
    await seleccionarTurno('TARDE', 'Turno tarde');
    fireEvent.click(screen.getByRole('button', { name: 'Desactivar' }));
    expect(await screen.findByText('El turno TARDE ya está inactivo.')).toHaveAttribute('role', 'alert');
    await waitFor(() => expect(urlsDeListadoTurnos(fetchMock)).toHaveLength(2));
  });

  it('muestra el error de detalle sin ocultar el listado', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    fetchMock.mockImplementationOnce(async () => Response.json({ StatusCode: 500, Message: 'No se pudo cargar el detalle del turno.' }, { status: 500 }));
    fireEvent.click(screen.getByRole('button', { name: 'DIA' }));
    expect(await screen.findByText('No se pudo cargar el detalle del turno.')).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('button', { name: 'NOCHE' })).toBeInTheDocument();
  });
});

describe('pantalla de Turnos: roles y pestañas', () => {
  it('con el rol CONSULTA deshabilita los botones de escritura y muestra el texto', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock, ['CONSULTA']);
    await seleccionarTurno('TARDE', 'Turno tarde');
    expect(screen.getByRole('button', { name: 'Nuevo turno' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Editar' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Desactivar' })).toBeDisabled();
    expect(screen.getAllByText(TEXTO_SOLO_RRHH).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByRole('button', { name: 'Nuevo turno' })).toHaveAccessibleDescription(TEXTO_SOLO_RRHH);
    expect(screen.getByRole('button', { name: 'Desactivar' })).toHaveAccessibleDescription(TEXTO_SOLO_RRHH);
  });

  it.each(['RRHH', 'ADMIN'])('con el rol %s habilita los botones de escritura', async (rol) => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock, [rol]);
    await seleccionarTurno('TARDE', 'Turno tarde');
    expect(screen.getByRole('button', { name: 'Nuevo turno' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Editar' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Desactivar' })).toBeEnabled();
    expect(screen.queryByText(TEXTO_SOLO_RRHH)).not.toBeInTheDocument();
  });

  it('cambia de vista con las pestañas y con las flechas del teclado', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    const pestanaTurnos = screen.getByRole('tab', { name: 'Turnos' });
    const pestanaAsignaciones = screen.getByRole('tab', { name: 'Asignaciones' });
    expect(pestanaTurnos).toHaveAttribute('aria-selected', 'true');
    fireEvent.click(pestanaAsignaciones);
    expect(await screen.findByRole('heading', { name: 'Todas las asignaciones' })).toBeInTheDocument();
    expect(pestanaAsignaciones).toHaveAttribute('aria-selected', 'true');
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowLeft' });
    expect(await screen.findByRole('heading', { name: 'Todos los turnos' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Turnos' })).toHaveAttribute('aria-selected', 'true');
  });
});
