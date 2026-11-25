import { fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import {
  limpiarTurnos,
  montarTurnosCargados,
  prepararApiTurnos,
  type LlamadaTurnos,
} from './turnosHorarios.testUtils';

afterEach(() => {
  limpiarTurnos();
});

function escribir(etiqueta: string, valor: string): void {
  fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } });
}

function escrituras(llamadas: LlamadaTurnos[]): LlamadaTurnos[] {
  return llamadas.filter((llamada) => llamada.metodo !== 'GET');
}

async function abrirNuevo(fetchMock: ReturnType<typeof prepararApiTurnos>['fetchMock']): Promise<void> {
  await montarTurnosCargados(fetchMock);
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo turno' }));
  await screen.findByRole('heading', { name: 'Nuevo turno' });
}

describe('formulario de turno: validación local', () => {
  it('envía vacío y muestra los mensajes 1, 3, 6, 7 y 13 sin hacer ninguna petición de escritura', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await abrirNuevo(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    expect(await screen.findByText('El código es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El nombre es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('La hora de entrada es obligatoria.')).toBeInTheDocument();
    expect(screen.getByText('La hora de salida es obligatoria.')).toBeInTheDocument();
    expect(screen.getByText('El turno fijo debe tener al menos un día laborable.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('muestra los mensajes 2, 4 y 12 y no envía', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await abrirNuevo(fetchMock);
    escribir('Código', 'C'.repeat(21));
    escribir('Nombre', 'N'.repeat(101));
    escribir('Tolerancia (minutos)', '45');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    expect(await screen.findByText('El código no puede superar 20 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('El nombre no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('La tolerancia debe estar entre 0 y 30 minutos.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('marca sola «Cruza medianoche» cuando la salida es anterior a la entrada y permite revisarla (mensaje 9)', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await abrirNuevo(fetchMock);
    escribir('Hora de entrada', '22:00');
    escribir('Hora de salida', '06:00');
    expect(screen.getByLabelText('Cruza medianoche')).toBeChecked();
    fireEvent.click(screen.getByLabelText('Cruza medianoche'));
    expect(screen.getByLabelText('Cruza medianoche')).not.toBeChecked();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    expect(await screen.findByText('Si la hora de salida es anterior a la de entrada, el turno debe cruzar medianoche.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('muestra el mensaje 8 con horas iguales y el 10 con la casilla marcada sin necesidad', async () => {
    const { fetchMock } = prepararApiTurnos();
    await abrirNuevo(fetchMock);
    escribir('Hora de entrada', '08:00');
    escribir('Hora de salida', '08:00');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    expect(await screen.findByText('La hora de salida no puede ser igual a la hora de entrada.')).toBeInTheDocument();
    escribir('Hora de salida', '17:00');
    fireEvent.click(screen.getByLabelText('Cruza medianoche'));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    expect(await screen.findByText('El turno solo cruza medianoche si la hora de salida es anterior a la de entrada.')).toBeInTheDocument();
  });

  it('con tipo Rotativo muestra el ciclo y valida los mensajes 16 y 18', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await abrirNuevo(fetchMock);
    fireEvent.click(screen.getByLabelText('Rotativo'));
    expect(screen.queryByLabelText('Lunes')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Días de trabajo')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    expect(await screen.findAllByText('El turno rotativo necesita días de trabajo y días de descanso.')).toHaveLength(2);
    expect(screen.getByText('La fecha de inicio del ciclo es obligatoria en el turno rotativo.')).toBeInTheDocument();
    escribir('Días de trabajo', '31');
    escribir('Días de descanso', '2');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    expect(await screen.findByText('Los días de trabajo y de descanso deben estar entre 1 y 30.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });
});

describe('formulario de turno: guardado', () => {
  it('crea un turno fijo con días ordenados y horas HH:mm:ss, lo selecciona y cierra el formulario', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await abrirNuevo(fetchMock);
    escribir('Código', 'tarde2');
    escribir('Nombre', 'Turno tarde 2');
    escribir('Hora de entrada', '14:00');
    escribir('Hora de salida', '22:00');
    escribir('Tolerancia (minutos)', '10');
    fireEvent.click(screen.getByLabelText('Miércoles'));
    fireEvent.click(screen.getByLabelText('Lunes'));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]).toEqual({
      url: '/api/v1/turnos',
      metodo: 'POST',
      cuerpo: {
        codigo: 'tarde2',
        nombre: 'Turno tarde 2',
        tipo: 'Fijo',
        horaEntrada: '14:00:00',
        horaSalida: '22:00:00',
        cruzaMedianoche: false,
        toleranciaMinutos: 10,
        diasLaborables: [1, 3],
      },
    });
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nuevo turno' })).not.toBeInTheDocument());
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url === '/api/v1/turnos/6')).toBe(true));
  });

  it('crea un turno rotativo con ciclo y sin días laborables', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await abrirNuevo(fetchMock);
    fireEvent.click(screen.getByLabelText('Rotativo'));
    escribir('Código', 'ROT3X1');
    escribir('Nombre', 'Rotativo 3x1');
    escribir('Hora de entrada', '22:00');
    escribir('Hora de salida', '06:00');
    escribir('Tolerancia (minutos)', '5');
    escribir('Días de trabajo', '3');
    escribir('Días de descanso', '1');
    escribir('Inicio del ciclo', '2026-12-01');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]?.cuerpo).toEqual({
      codigo: 'ROT3X1',
      nombre: 'Rotativo 3x1',
      tipo: 'Rotativo',
      horaEntrada: '22:00:00',
      horaSalida: '06:00:00',
      cruzaMedianoche: true,
      toleranciaMinutos: 5,
      diasTrabajo: 3,
      diasDescanso: 1,
      fechaInicioCiclo: '2026-12-01',
    });
  });

  it('ante el 409 del código repetido muestra el mensaje 20 y conserva lo escrito', async () => {
    const { fetchMock } = prepararApiTurnos({
      errorEscritura: { accion: 'crear-turno', status: 409, mensaje: 'Ya existe un turno con el código DIA.' },
    });
    await abrirNuevo(fetchMock);
    escribir('Código', 'DIA');
    escribir('Nombre', 'Otro turno dia');
    escribir('Hora de entrada', '08:00');
    escribir('Hora de salida', '17:00');
    fireEvent.click(screen.getByLabelText('Lunes'));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    expect(await screen.findByText('Ya existe un turno con el código DIA.')).toHaveAttribute('role', 'alert');
    expect(screen.getByLabelText('Código')).toHaveValue('DIA');
    expect(screen.getByLabelText('Nombre')).toHaveValue('Otro turno dia');
    expect(screen.getByLabelText('Lunes')).toBeChecked();
    expect(screen.getByRole('heading', { name: 'Nuevo turno' })).toBeInTheDocument();
  });

  it('edita un turno sin asignaciones con PUT y los datos precargados', async () => {
    const { fetchMock, llamadas } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'TARDE' }));
    await screen.findByRole('heading', { name: 'Turno tarde' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    await screen.findByRole('heading', { name: 'Editar turno TARDE' });
    expect(screen.getByLabelText('Hora de entrada')).toHaveValue('14:00');
    expect(screen.getByLabelText('Martes')).toBeChecked();
    expect(screen.getByLabelText('Sábado')).not.toBeChecked();
    escribir('Tolerancia (minutos)', '20');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]).toMatchObject({ url: '/api/v1/turnos/5', metodo: 'PUT' });
    expect(escrituras(llamadas)[0]?.cuerpo).toMatchObject({ codigo: 'TARDE', toleranciaMinutos: 20, diasLaborables: [1, 2, 3, 4, 5] });
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Editar turno TARDE' })).not.toBeInTheDocument());
  });

  it('muestra el mensaje 22 del backend al editar un turno que ya tiene asignaciones y conserva lo escrito', async () => {
    const { fetchMock } = prepararApiTurnos({
      errorEscritura: { accion: 'editar-turno', status: 409, mensaje: 'El turno TARDE tiene asignaciones y no se puede modificar. Crea un turno nuevo.' },
    });
    await montarTurnosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'TARDE' }));
    await screen.findByRole('heading', { name: 'Turno tarde' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    await screen.findByRole('heading', { name: 'Editar turno TARDE' });
    escribir('Nombre', 'Nombre cambiado');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar turno' }));
    expect(await screen.findByText('El turno TARDE tiene asignaciones y no se puede modificar. Crea un turno nuevo.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre')).toHaveValue('Nombre cambiado');
  });

  it('con el rol CONSULTA no ofrece el alta', async () => {
    const { fetchMock } = prepararApiTurnos();
    await montarTurnosCargados(fetchMock, ['CONSULTA']);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo turno' }));
    expect(screen.queryByRole('heading', { name: 'Nuevo turno' })).not.toBeInTheDocument();
  });
});
