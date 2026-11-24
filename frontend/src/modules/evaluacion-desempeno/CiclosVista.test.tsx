import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  limpiarEvaluacion,
  montarCiclosCargados,
  prepararApiEvaluacion,
  renderEvaluacion,
  urlsQueContienen,
} from './evaluacion.testUtils';

afterEach(() => {
  limpiarEvaluacion();
});

async function seleccionarCiclo(codigo: string, nombre: string): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: codigo }));
  await waitFor(() => {
    expect(screen.getByRole('heading', { name: nombre })).toBeInTheDocument();
    expect(screen.getByText(/^Suma de pesos:/)).toBeInTheDocument();
  });
}

function dialogo(): HTMLElement {
  return screen.getByRole('dialog');
}

describe('vista de Ciclos: listado y filtros', () => {
  it('lista código, nombre, periodo, fechas y estado con texto', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    expect(urlsQueContienen(fetchMock, '/ciclos-evaluacion?')).toEqual(['/api/v1/ciclos-evaluacion?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table', { name: 'Listado de ciclos de evaluación' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual(['Código', 'Nombre', 'Periodo', 'Fechas', 'Estado']);
    const filas = within(tabla).getAllByRole('row');
    expect(filas).toHaveLength(5);
    expect(filas[1]).toHaveTextContent('2026-S1');
    expect(filas[1]).toHaveTextContent('Cerrado');
    expect(filas[2]).toHaveTextContent('01/06/2026 - 30/11/2026');
    expect(filas[2]).toHaveTextContent('Abierto');
    expect(filas[3]).toHaveTextContent('Configuración');
  });

  it('envía el estado a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiEvaluacion({ totalListado: 25 });
    await montarCiclosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/ciclos-evaluacion?').at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Configuracion' } });
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/ciclos-evaluacion?').at(-1)).toBe('/api/v1/ciclos-evaluacion?estado=Configuracion&pagina=1&tamanoPagina=10'));
  });

  it('aplica la búsqueda 300 ms después del último cambio', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    const antes = urlsQueContienen(fetchMock, '/ciclos-evaluacion?').length;
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText('Buscar por código o nombre'), { target: { value: '2027' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsQueContienen(fetchMock, '/ciclos-evaluacion?')).toHaveLength(antes);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/ciclos-evaluacion?').at(-1)).toBe('/api/v1/ciclos-evaluacion?busqueda=2027&pagina=1&tamanoPagina=10'));
  });

  it('muestra los estados vacío y de error', async () => {
    prepararApiEvaluacion({ listaVacia: true });
    renderEvaluacion();
    expect(await screen.findByText('No hay ciclos con esos filtros.')).toHaveAttribute('role', 'status');
    limpiarEvaluacion();
    prepararApiEvaluacion({ errorLista: true });
    renderEvaluacion();
    expect(await screen.findByText('No se pudo cargar la lista de ciclos.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Ciclos: detalle, competencias y resultados', () => {
  it('un ciclo abierto muestra competencias, suma de pesos, resultados y «Cerrar ciclo»', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2026-S2', 'Segundo semestre 2026');
    expect(screen.getByText('Suma de pesos: 100,00 de 100')).toBeInTheDocument();
    const tabla = screen.getByRole('table', { name: 'Competencias del ciclo' });
    expect(within(tabla).getByText('Liderazgo').closest('tr')).toHaveTextContent('45,50 %');
    expect(within(tabla).getByText('Trabajo en equipo').closest('tr')).toHaveTextContent('54,50 %');
    expect(screen.getByRole('button', { name: 'Cerrar ciclo' })).toBeEnabled();
    expect(screen.queryByRole('button', { name: 'Abrir ciclo' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Agregar competencia' })).not.toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Resultados' })).toBeInTheDocument();
    expect(await screen.findByRole('list', { name: 'Distribución por etiqueta' })).toBeInTheDocument();
    const distribucion = screen.getByRole('list', { name: 'Distribución por etiqueta' });
    expect(within(distribucion).getAllByRole('listitem')).toHaveLength(5);
    expect(within(distribucion).getByText('Competente').closest('li')).toHaveTextContent('2');
    const promedios = screen.getByRole('list', { name: 'Promedio por competencia (de 1 a 5)' });
    expect(within(promedios).getByText('Liderazgo').closest('li')).toHaveTextContent('3,50');
    expect(within(promedios).getByText('Trabajo en equipo').closest('li')).toHaveTextContent('4,00');
    expect(screen.getByText('Puntaje promedio').nextSibling).toHaveTextContent('3,55');
  });

  it('un ciclo en configuración con suma 85 bloquea «Abrir ciclo» y explica el motivo', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2027-S1', 'Primer semestre 2027');
    expect(screen.getByText('Suma de pesos: 85,00 de 100')).toBeInTheDocument();
    const abrir = screen.getByRole('button', { name: 'Abrir ciclo' });
    expect(abrir).toBeDisabled();
    expect(abrir).toHaveAccessibleDescription('Los pesos suman 85,00 de 100: ajusta las competencias para poder abrir el ciclo.');
    expect(screen.getByRole('button', { name: 'Agregar competencia' })).toBeEnabled();
    expect(urlsQueContienen(fetchMock, '/resultados')).toHaveLength(0);
  });

  it('abre un ciclo con suma 100 y pasa a Abierto', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2027-S2', 'Segundo semestre 2027');
    fireEvent.click(screen.getByRole('button', { name: 'Abrir ciclo' }));
    expect(await screen.findByRole('button', { name: 'Cerrar ciclo' })).toBeInTheDocument();
    expect(llamadas.some((llamada) => llamada.metodo === 'POST' && llamada.url.endsWith('/ciclos-evaluacion/4/apertura'))).toBe(true);
    expect(screen.queryByRole('button', { name: 'Abrir ciclo' })).not.toBeInTheDocument();
  });

  it('muestra el mensaje del servidor si la apertura falla', async () => {
    const { fetchMock } = prepararApiEvaluacion({ errorEscritura: { metodo: 'POST', sufijo: '/ciclos-evaluacion/4/apertura', status: 409, mensaje: 'Los pesos de las competencias suman 99.00 y deben sumar 100.' } });
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2027-S2', 'Segundo semestre 2027');
    fireEvent.click(screen.getByRole('button', { name: 'Abrir ciclo' }));
    expect(await screen.findByText('Los pesos de las competencias suman 99.00 y deben sumar 100.')).toHaveAttribute('role', 'alert');
  });

  it('cierra un ciclo abierto solo después de confirmar', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2026-S2', 'Segundo semestre 2026');
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar ciclo' }));
    expect(within(dialogo()).getByRole('heading', { name: 'Cerrar ciclo' })).toBeInTheDocument();
    expect(llamadas.some((llamada) => llamada.url.endsWith('/cierre'))).toBe(false);
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Cancelar' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar ciclo' }));
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Cerrar ciclo' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(llamadas.filter((llamada) => llamada.metodo === 'POST' && llamada.url.endsWith('/ciclos-evaluacion/2/cierre'))).toHaveLength(1);
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Cerrar ciclo' })).not.toBeInTheDocument());
  });

  it('un ciclo cerrado no ofrece acciones de escritura', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2026-S1', 'Primer semestre 2026');
    expect(screen.queryByRole('button', { name: 'Cerrar ciclo' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Abrir ciclo' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Editar ciclo' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Agregar competencia' })).not.toBeInTheDocument();
  });

  it('muestra el error de los resultados', async () => {
    const { fetchMock } = prepararApiEvaluacion({ errorResultados: true });
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2026-S2', 'Segundo semestre 2026');
    expect(await screen.findByText('No se pudieron cargar los resultados del ciclo.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Ciclos: competencias y formulario de ciclo', () => {
  it('valida la competencia con los mensajes 2 y 11 y no envía', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2027-S1', 'Primer semestre 2027');
    fireEvent.click(screen.getByRole('button', { name: 'Agregar competencia' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar competencia' }));
    expect(await screen.findByText('El nombre es obligatorio y no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('El peso debe ser mayor que 0 y como máximo 100, con 2 decimales.')).toBeInTheDocument();
    expect(llamadas.some((llamada) => llamada.metodo === 'POST' && llamada.url.endsWith('/competencias'))).toBe(false);
  });

  it('agrega una competencia y la suma llega a 100', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2027-S1', 'Primer semestre 2027');
    fireEvent.click(screen.getByRole('button', { name: 'Agregar competencia' }));
    fireEvent.change(screen.getByLabelText('Nombre de la competencia'), { target: { value: ' Orden ' } });
    fireEvent.change(screen.getByLabelText('Peso (%)'), { target: { value: '15' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar competencia' }));
    expect(await screen.findByText('Suma de pesos: 100,00 de 100')).toBeInTheDocument();
    const llamada = llamadas.find((item) => item.metodo === 'POST' && item.url.endsWith('/ciclos-evaluacion/3/competencias'));
    expect(llamada?.cuerpo).toEqual({ nombre: 'Orden', descripcion: null, peso: 15 });
    expect(screen.getByRole('button', { name: 'Abrir ciclo' })).toBeEnabled();
  });

  it('edita el peso de una competencia', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2027-S1', 'Primer semestre 2027');
    fireEvent.click(screen.getByRole('button', { name: 'Editar competencia Puntualidad' }));
    expect(screen.getByLabelText('Peso (%)')).toHaveValue('25,00');
    fireEvent.change(screen.getByLabelText('Peso (%)'), { target: { value: '40,5' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar competencia' }));
    expect(await screen.findByText('Suma de pesos: 100,50 de 100')).toBeInTheDocument();
    expect(llamadas.find((item) => item.metodo === 'PUT' && item.url.endsWith('/competencias/6'))?.cuerpo).toEqual({ nombre: 'Puntualidad', descripcion: null, peso: 40.5 });
  });

  it('quita una competencia después de confirmar', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2027-S1', 'Primer semestre 2027');
    fireEvent.click(screen.getByRole('button', { name: 'Quitar competencia Puntualidad' }));
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Quitar competencia' }));
    expect(await screen.findByText('Suma de pesos: 60,00 de 100')).toBeInTheDocument();
    expect(llamadas.some((llamada) => llamada.metodo === 'DELETE' && llamada.url.endsWith('/ciclos-evaluacion/3/competencias/6'))).toBe(true);
  });

  it('crea un ciclo: valida fechas y muestra el 409 del servidor dentro del formulario', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion({ errorEscritura: { metodo: 'POST', sufijo: '/ciclos-evaluacion', status: 409, mensaje: 'Ya existe un ciclo con el código 2028-S1.' } });
    await montarCiclosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo ciclo' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar ciclo' }));
    expect(await screen.findByText('El código es obligatorio y no puede superar 20 caracteres.')).toBeInTheDocument();
    expect(screen.getAllByText('Las fechas del periodo, de apertura y de cierre son obligatorias.')).toHaveLength(4);
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: '2028-S1' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Primer semestre 2028' } });
    fireEvent.change(screen.getByLabelText('Inicio del periodo'), { target: { value: '2028-01-01' } });
    fireEvent.change(screen.getByLabelText('Fin del periodo'), { target: { value: '2028-06-30' } });
    fireEvent.change(screen.getByLabelText('Apertura de evaluaciones'), { target: { value: '2028-06-01' } });
    fireEvent.change(screen.getByLabelText('Cierre de evaluaciones'), { target: { value: '2028-06-01' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar ciclo' }));
    expect(await screen.findByText('La fecha de cierre debe ser posterior a la de apertura.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Cierre de evaluaciones'), { target: { value: '2028-07-31' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar ciclo' }));
    expect(await screen.findByText('Ya existe un ciclo con el código 2028-S1.')).toHaveAttribute('role', 'alert');
    expect(llamadas.filter((llamada) => llamada.metodo === 'POST' && llamada.url.endsWith('/ciclos-evaluacion'))).toHaveLength(1);
    expect(screen.getByLabelText('Código')).toHaveValue('2028-S1');
  });

  it('edita un ciclo en configuración con sus datos precargados', async () => {
    const { fetchMock, llamadas } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    await seleccionarCiclo('2027-S1', 'Primer semestre 2027');
    fireEvent.click(screen.getByRole('button', { name: 'Editar ciclo' }));
    expect(screen.getByLabelText('Código')).toHaveValue('2027-S1');
    expect(screen.getByLabelText('Inicio del periodo')).toHaveValue('2026-06-01');
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Semestre uno 2027' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar ciclo' }));
    expect(await screen.findByRole('heading', { name: 'Semestre uno 2027' })).toBeInTheDocument();
    expect(llamadas.some((llamada) => llamada.metodo === 'PUT' && llamada.url.endsWith('/ciclos-evaluacion/3'))).toBe(true);
  });
});
