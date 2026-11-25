import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  limpiarEvaluacion,
  montarEvaluacionesCargadas,
  prepararApiEvaluacion,
  renderEvaluacion,
  urlsQueContienen,
} from './evaluacion.testUtils';

afterEach(() => {
  limpiarEvaluacion();
});

async function seleccionarEvaluacion(empleado: string): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: empleado }));
  await waitFor(() => {
    expect(screen.getByRole('heading', { name: empleado })).toBeInTheDocument();
    expect(screen.queryByText('Cargando evaluación…')).not.toBeInTheDocument();
  });
}

function grupo(competencia: string): HTMLElement {
  return screen.getByRole('group', { name: new RegExp(`^${competencia} · peso`) });
}

function calificar(competencia: string, etiqueta: string): void {
  fireEvent.click(within(grupo(competencia)).getByRole('radio', { name: etiqueta }));
}

function dialogo(): HTMLElement {
  return screen.getByRole('dialog');
}

describe('vista de Evaluaciones: listado y filtros', () => {
  it('lista empleado, evaluador, ciclo, estado y puntaje con etiqueta', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    expect(urlsQueContienen(fetchMock, '/evaluaciones?')).toEqual(['/api/v1/evaluaciones?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table', { name: 'Listado de evaluaciones' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual(['Empleado', 'Evaluador', 'Ciclo', 'Estado', 'Puntaje y etiqueta']);
    const filas = within(tabla).getAllByRole('row');
    expect(filas).toHaveLength(4);
    expect(filas[1]).toHaveTextContent('Borrador');
    expect(filas[1]).toHaveTextContent('Sin puntaje');
    expect(filas[2]).toHaveTextContent('Enviada');
    expect(filas[2]).toHaveTextContent('3,55 · Competente');
    expect(filas[3]).toHaveTextContent('2026-S1');
    expect(filas[3]).toHaveTextContent('4,20 · Destacado');
  });

  it('envía ciclo y estado a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiEvaluacion({ totalListado: 25 });
    await montarEvaluacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/evaluaciones?').at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Ciclo'), { target: { value: '2' } });
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Enviada' } });
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/evaluaciones?').at(-1)).toBe('/api/v1/evaluaciones?cicloId=2&estado=Enviada&pagina=1&tamanoPagina=10'));
  });

  it('aplica la búsqueda 300 ms después del último cambio', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    const antes = urlsQueContienen(fetchMock, '/evaluaciones?').length;
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText('Buscar por empleado'), { target: { value: 'Maria' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsQueContienen(fetchMock, '/evaluaciones?')).toHaveLength(antes);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/evaluaciones?').at(-1)).toBe('/api/v1/evaluaciones?busqueda=Maria&pagina=1&tamanoPagina=10'));
  });

  it('muestra los estados vacío y de error', async () => {
    prepararApiEvaluacion({ listaVacia: true });
    renderEvaluacion(['RRHH'], '/modulos/68-evaluacion-desempeno?vista=evaluaciones');
    expect(await screen.findByText('No hay evaluaciones con esos filtros.')).toHaveAttribute('role', 'status');
    limpiarEvaluacion();
    prepararApiEvaluacion({ errorLista: true });
    renderEvaluacion(['RRHH'], '/modulos/68-evaluacion-desempeno?vista=evaluaciones');
    expect(await screen.findByText('No se pudo cargar la lista de evaluaciones.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Evaluaciones: calificación', () => {
  it('muestra un fieldset por competencia con cinco opciones con texto y el puntaje estimado en vivo', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    await seleccionarEvaluacion('Ana Torres Vega');
    expect(within(grupo('Liderazgo')).getAllByRole('radio').map((radio) => radio.parentElement?.textContent)).toEqual([
      '1 · Deficiente', '2 · Regular', '3 · Bueno', '4 · Muy bueno', '5 · Sobresaliente',
    ]);
    expect(grupo('Trabajo en equipo')).toHaveTextContent('peso 54,50 %');
    expect(screen.getByText('Faltan calificar 2 competencias para estimar el puntaje.')).toBeInTheDocument();
    calificar('Liderazgo', '3 · Bueno');
    expect(screen.getByText('Faltan calificar 1 competencias para estimar el puntaje.')).toBeInTheDocument();
    calificar('Trabajo en equipo', '4 · Muy bueno');
    expect(screen.getByText('Puntaje estimado: 3,55 · Competente')).toBeInTheDocument();
    calificar('Trabajo en equipo', '5 · Sobresaliente');
    expect(screen.getByText('Puntaje estimado: 4,09 · Destacado')).toBeInTheDocument();
  });

  it('guarda las calificaciones y el comentario con el cuerpo esperado', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    await seleccionarEvaluacion('Ana Torres Vega');
    calificar('Liderazgo', '3 · Bueno');
    fireEvent.change(screen.getByLabelText('Comentario'), { target: { value: ' Buen semestre ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByText('Cambios guardados.')).toBeInTheDocument();
    const llamada = llamadas.find((item) => item.metodo === 'PUT' && item.url.endsWith('/evaluaciones/11/calificaciones'));
    expect(llamada?.cuerpo).toEqual({ calificaciones: [{ competenciaId: 3, calificacion: 3 }], comentario: 'Buen semestre' });
  });

  it('no envía si el comentario supera 500 caracteres (mensaje 27)', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    await seleccionarEvaluacion('Ana Torres Vega');
    fireEvent.change(screen.getByLabelText('Comentario'), { target: { value: 'x'.repeat(501) } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByText('El comentario no puede superar 500 caracteres.')).toBeInTheDocument();
    expect(llamadas.some((llamada) => llamada.metodo === 'PUT')).toBe(false);
  });

  it('envía la evaluación: guarda, la envía y pasa a Enviada con «Cerrar evaluación»', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    await seleccionarEvaluacion('Ana Torres Vega');
    calificar('Liderazgo', '3 · Bueno');
    calificar('Trabajo en equipo', '4 · Muy bueno');
    fireEvent.click(screen.getByRole('button', { name: 'Enviar' }));
    expect(await screen.findByRole('button', { name: 'Cerrar evaluación' })).toBeInTheDocument();
    const escrituras = llamadas.filter((llamada) => llamada.metodo !== 'GET').map((llamada) => `${llamada.metodo} ${llamada.url}`);
    expect(escrituras).toEqual(['PUT /api/v1/evaluaciones/11/calificaciones', 'POST /api/v1/evaluaciones/11/envio']);
    const tabla = screen.getByRole('table', { name: 'Calificaciones por competencia' });
    expect(within(tabla).getByText('Liderazgo').closest('tr')).toHaveTextContent('3');
    expect(screen.queryByRole('radio')).not.toBeInTheDocument();
    expect(screen.getByText('3,55 · Competente', { selector: 'dd' })).toBeInTheDocument();
  });

  it('muestra el mensaje 28 del servidor si faltan calificaciones y conserva lo marcado', async () => {
    const { fetchMock } = prepararApiEvaluacion({ errorEscritura: { metodo: 'POST', sufijo: '/evaluaciones/11/envio', status: 409, mensaje: 'Faltan calificar 1 competencias para enviar la evaluación.' } });
    await montarEvaluacionesCargadas(fetchMock);
    await seleccionarEvaluacion('Ana Torres Vega');
    calificar('Liderazgo', '3 · Bueno');
    fireEvent.click(screen.getByRole('button', { name: 'Enviar' }));
    expect(await screen.findByText('Faltan calificar 1 competencias para enviar la evaluación.')).toHaveAttribute('role', 'alert');
    expect(within(grupo('Liderazgo')).getByRole('radio', { name: '3 · Bueno' })).toBeChecked();
  });
});

describe('vista de Evaluaciones: cierre y lectura', () => {
  it('cierra una evaluación enviada solo después de confirmar', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    await seleccionarEvaluacion('Maria Salazar Quispe');
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar evaluación' }));
    expect(llamadas.some((llamada) => llamada.url.endsWith('/cierre'))).toBe(false);
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Cancelar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar evaluación' }));
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Cerrar evaluación' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(llamadas.filter((llamada) => llamada.metodo === 'POST' && llamada.url.endsWith('/evaluaciones/12/cierre'))).toHaveLength(1);
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Cerrar evaluación' })).not.toBeInTheDocument());
  });

  it('una evaluación cerrada es de solo lectura', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    await seleccionarEvaluacion('Pedro Díaz Lara');
    expect(screen.getByRole('table', { name: 'Calificaciones por competencia' })).toBeInTheDocument();
    expect(screen.queryByRole('radio')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Cerrar evaluación' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Enviar' })).not.toBeInTheDocument();
  });
});

describe('vista de Evaluaciones: nueva evaluación', () => {
  it('solo ofrece ciclos abiertos y valida los mensajes 17 y 18', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva evaluación' }));
    const cuadro = within(dialogo());
    expect(cuadro.getByRole('option', { name: '2026-S2 · Segundo semestre 2026' })).toBeInTheDocument();
    expect(cuadro.queryByRole('option', { name: /2026-S1/ })).not.toBeInTheDocument();
    expect(cuadro.queryByRole('option', { name: /2027-S1/ })).not.toBeInTheDocument();
    fireEvent.click(cuadro.getByRole('button', { name: 'Registrar evaluación' }));
    expect(await cuadro.findAllByText('El ciclo, el empleado y el evaluador son obligatorios.')).toHaveLength(3);
    fireEvent.change(cuadro.getByLabelText('Ciclo abierto'), { target: { value: '2' } });
    fireEvent.change(cuadro.getByLabelText('Empleado evaluado'), { target: { value: '1' } });
    fireEvent.change(cuadro.getByLabelText('Evaluador'), { target: { value: '1' } });
    fireEvent.click(cuadro.getByRole('button', { name: 'Registrar evaluación' }));
    expect(await cuadro.findByText('El evaluador no puede ser el propio evaluado.')).toBeInTheDocument();
    expect(llamadas.some((llamada) => llamada.metodo === 'POST')).toBe(false);
  });

  it('registra la evaluación, cierra el diálogo y la selecciona', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva evaluación' }));
    const cuadro = within(dialogo());
    fireEvent.change(cuadro.getByLabelText('Ciclo abierto'), { target: { value: '2' } });
    fireEvent.change(cuadro.getByLabelText('Empleado evaluado'), { target: { value: '2' } });
    fireEvent.change(cuadro.getByLabelText('Evaluador'), { target: { value: '1' } });
    fireEvent.click(cuadro.getByRole('button', { name: 'Registrar evaluación' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(llamadas.find((item) => item.metodo === 'POST' && item.url.endsWith('/evaluaciones'))?.cuerpo).toEqual({ cicloId: 2, empleadoId: 2, evaluadorId: 1 });
    expect(await screen.findByRole('heading', { name: 'Luis Rojas Mendoza' })).toBeInTheDocument();
    expect(await screen.findByRole('group', { name: /^Liderazgo · peso/ })).toBeInTheDocument();
  });

  it('muestra el 409 del servidor dentro del diálogo', async () => {
    const { fetchMock } = prepararApiEvaluacion({ errorEscritura: { metodo: 'POST', sufijo: '/evaluaciones', status: 409, mensaje: 'El empleado 2 ya tiene una evaluación en el ciclo 2026-S2.' } });
    await montarEvaluacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva evaluación' }));
    const cuadro = within(dialogo());
    fireEvent.change(cuadro.getByLabelText('Ciclo abierto'), { target: { value: '2' } });
    fireEvent.change(cuadro.getByLabelText('Empleado evaluado'), { target: { value: '2' } });
    fireEvent.change(cuadro.getByLabelText('Evaluador'), { target: { value: '1' } });
    fireEvent.click(cuadro.getByRole('button', { name: 'Registrar evaluación' }));
    expect(await cuadro.findByText('El empleado 2 ya tiene una evaluación en el ciclo 2026-S2.')).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });
});
