import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { semanaActual } from './presentacion';
import {
  limpiarAsistencia,
  montarJornadasCargadas,
  prepararApiAsistencia,
  renderAsistencia,
  urlsDeListadoJornadas,
  type LlamadaAsistencia,
} from './asistencia.testUtils';

afterEach(() => {
  limpiarAsistencia();
});

const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar la asistencia.';

function escrituras(llamadas: LlamadaAsistencia[]): LlamadaAsistencia[] {
  return llamadas.filter((llamada) => llamada.metodo !== 'GET');
}

async function seleccionarJornada(nombre: string, indice: number, titulo: string): Promise<void> {
  fireEvent.click(screen.getAllByRole('button', { name: nombre })[indice] as HTMLElement);
  await screen.findByRole('heading', { name: titulo });
}

function celdasDeFila(fila: number): string {
  const tabla = screen.getByRole('table', { name: 'Listado de jornadas de asistencia' });
  return within(tabla).getAllByRole('row')[fila]?.textContent ?? '';
}

describe('vista de Jornadas: listado, resumen y filtros', () => {
  it('pide la semana actual por defecto y muestra la tabla con horas en UTC−5', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    const { desde, hasta } = semanaActual();
    expect(urlsDeListadoJornadas(fetchMock)).toEqual([`/api/v1/jornadas-asistencia?desde=${desde}&hasta=${hasta}&pagina=1&tamanoPagina=10`]);
    expect(fetchMock.mock.calls.map((llamada) => String(llamada[0]))).toContain(`/api/v1/jornadas-asistencia/resumen?desde=${desde}&hasta=${hasta}`);
    expect(screen.getByLabelText('Desde')).toHaveValue(desde);
    expect(screen.getByLabelText('Hasta')).toHaveValue(hasta);
    const tabla = screen.getByRole('table', { name: 'Listado de jornadas de asistencia' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual([
      'Fecha', 'Empleado', 'Turno', 'Entrada', 'Salida', 'Trabajado', 'Tardanza', 'Extra', 'Estado', 'Horas extra',
    ]);
    const primera = celdasDeFila(1);
    expect(primera).toContain('08:15');
    expect(primera).toContain('17:00');
    expect(primera).toContain('8 h 45 min');
    expect(primera).toContain('15 min');
    expect(primera).toContain('Tardanza');
    expect(primera).toContain('No aplica');
    const segunda = celdasDeFila(2);
    expect(segunda).toContain('07:55');
    expect(segunda).toContain('18:30');
    expect(segunda).toContain('10 h 35 min');
    expect(segunda).toContain('1 h 30 min');
    expect(segunda).toContain('Pendiente');
    expect(celdasDeFila(3)).toContain('Sin marcación');
    expect(celdasDeFila(4)).toContain('Aprobada');
  });

  it('marca con (+1) la salida de un turno nocturno que termina al día siguiente', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    const nocturna = celdasDeFila(5);
    expect(nocturna).toContain('22:00');
    expect(nocturna).toContain('06:00 (+1)');
    expect(nocturna).toContain('NOCHE');
  });

  it('muestra el resumen del rango con minutos en horas', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    const resumen = screen.getByRole('region', { name: 'Resumen de asistencia del rango' });
    expect(within(resumen).getAllByRole('article').map((tarjeta) => tarjeta.textContent)).toEqual([
      'Asistencias3',
      'Tardanzas1',
      'Sin marcación1',
      'Faltas0',
      'Extra pendiente1 h 30 min',
      'Extra aprobada1 h',
    ]);
  });

  it('envía empleado, rango, estado y horas extra a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiAsistencia({ totalListado: 25 });
    await montarJornadasCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsDeListadoJornadas(fetchMock).at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Empleado'), { target: { value: '2' } });
    await waitFor(() => expect(urlsDeListadoJornadas(fetchMock).at(-1)).toContain('empleadoId=2&'));
    expect(urlsDeListadoJornadas(fetchMock).at(-1)).toContain('pagina=1');
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-11-01' } });
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-11-30' } });
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Asistencia' } });
    fireEvent.change(screen.getByLabelText('Horas extra'), { target: { value: 'Pendiente' } });
    await waitFor(() => expect(urlsDeListadoJornadas(fetchMock).at(-1)).toBe(
      '/api/v1/jornadas-asistencia?empleadoId=2&desde=2026-11-01&hasta=2026-11-30&estado=Asistencia&estadoHorasExtra=Pendiente&pagina=1&tamanoPagina=10',
    ));
    await waitFor(() => expect(fetchMock.mock.calls.map((llamada) => String(llamada[0]))).toContain('/api/v1/jornadas-asistencia/resumen?desde=2026-11-01&hasta=2026-11-30'));
  });

  it('muestra el estado vacío y los errores del listado y del resumen', async () => {
    prepararApiAsistencia({ listaVacia: true });
    renderAsistencia(['RRHH']);
    expect(await screen.findByText('No hay jornadas con esos filtros.')).toHaveAttribute('role', 'status');
    limpiarAsistencia();
    prepararApiAsistencia({ errorLista: true, errorResumen: true });
    renderAsistencia(['RRHH']);
    expect(await screen.findByText('No se pudo cargar la lista de jornadas.')).toHaveAttribute('role', 'alert');
    expect(await screen.findByText('No se pudo cargar el resumen de asistencia.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Jornadas: detalle', () => {
  it('muestra los datos programados y las marcaciones del día y del siguiente en hora local', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    await seleccionarJornada('EMP-000002 · Luis Rojas Mendoza', 0, 'Luis Rojas Mendoza');
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url
      === '/api/v1/marcaciones?empleadoId=2&desde=2026-11-04&hasta=2026-11-05&pagina=1&tamanoPagina=100')).toBe(true));
    const programados = screen.getByRole('region', { name: 'Datos programados' });
    expect(within(programados).getByText('Entrada programada').nextElementSibling).toHaveTextContent('08:00');
    expect(within(programados).getByText('Salida programada').nextElementSibling).toHaveTextContent('17:00');
    expect(within(programados).getByText('Tolerancia').nextElementSibling).toHaveTextContent('10 min');
    const tabla = await screen.findByRole('table', { name: 'Marcaciones del empleado' });
    expect(within(tabla).getAllByRole('row')).toHaveLength(2);
    expect(within(tabla).getByText('18:30')).toBeInTheDocument();
    expect(within(tabla).getByText('Olvidó marcar la salida')).toBeInTheDocument();
    const resultado = screen.getByRole('region', { name: 'Resultado del día' });
    expect(within(resultado).getByText('Horas extra').nextElementSibling).toHaveTextContent('1 h 30 min');
  });

  it('muestra quién decidió, cuándo y el motivo en una jornada decidida, sin botones de decisión', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    await seleccionarJornada('EMP-000001 · Ana Torres Vega', 1, 'Ana Torres Vega');
    const resultado = screen.getByRole('region', { name: 'Resultado del día' });
    expect(within(resultado).getByText('Decidido por').nextElementSibling).toHaveTextContent('rrhh');
    expect(within(resultado).getByText('Fecha de decisión').nextElementSibling).toHaveTextContent('07/11/2026 10:30');
    expect(screen.queryByRole('button', { name: 'Aprobar horas extra' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Rechazar' })).not.toBeInTheDocument();
  });

  it('no ofrece decidir horas extra que no están pendientes', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    await seleccionarJornada('EMP-000001 · Ana Torres Vega', 0, 'Ana Torres Vega');
    expect(screen.queryByRole('button', { name: 'Aprobar horas extra' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Rechazar' })).not.toBeInTheDocument();
  });

  it('muestra el error de las marcaciones del día sin romper la ficha', async () => {
    const { fetchMock } = prepararApiAsistencia({ errorMarcacionesDia: true });
    await montarJornadasCargadas(fetchMock);
    await seleccionarJornada('EMP-000002 · Luis Rojas Mendoza', 0, 'Luis Rojas Mendoza');
    expect(await screen.findByText('No se pudieron cargar las marcaciones del día.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Jornadas: aprobar y rechazar horas extra', () => {
  it('aprueba las horas extra pendientes, recarga y deja de ofrecer los botones', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    await seleccionarJornada('EMP-000002 · Luis Rojas Mendoza', 0, 'Luis Rojas Mendoza');
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar horas extra' }));
    await waitFor(() => expect(escrituras(llamadas)).toEqual([
      { url: '/api/v1/jornadas-asistencia/2/horas-extra/aprobacion', metodo: 'POST', cuerpo: undefined },
    ]));
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Aprobar horas extra' })).not.toBeInTheDocument());
    expect(screen.queryByRole('button', { name: 'Rechazar' })).not.toBeInTheDocument();
    await waitFor(() => expect(urlsDeListadoJornadas(fetchMock)).toHaveLength(2));
  });

  it('muestra el mensaje del servidor si las horas extra ya no están pendientes (409)', async () => {
    const { fetchMock } = prepararApiAsistencia({ errorEscritura: { accion: 'aprobar', status: 409, mensaje: 'La jornada 2 no tiene horas extra pendientes.' } });
    await montarJornadasCargadas(fetchMock);
    await seleccionarJornada('EMP-000002 · Luis Rojas Mendoza', 0, 'Luis Rojas Mendoza');
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar horas extra' }));
    expect(await screen.findByText('La jornada 2 no tiene horas extra pendientes.')).toHaveAttribute('role', 'alert');
    await waitFor(() => expect(urlsDeListadoJornadas(fetchMock)).toHaveLength(2));
  });

  it('rechaza con motivo: valida los mensajes 21 y 22 antes de enviar', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    await seleccionarJornada('EMP-000002 · Luis Rojas Mendoza', 0, 'Luis Rojas Mendoza');
    fireEvent.click(screen.getByRole('button', { name: 'Rechazar' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Rechazar horas extra' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Rechazar horas extra' }));
    expect(await within(dialogo).findByText('El motivo del rechazo es obligatorio.')).toBeInTheDocument();
    fireEvent.change(within(dialogo).getByLabelText('Motivo del rechazo'), { target: { value: 'a'.repeat(301) } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Rechazar horas extra' }));
    expect(await within(dialogo).findByText('El motivo del rechazo no puede superar 300 caracteres.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('rechaza con el motivo recortado, cierra el diálogo y muestra el motivo en la ficha', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    await seleccionarJornada('EMP-000002 · Luis Rojas Mendoza', 0, 'Luis Rojas Mendoza');
    fireEvent.click(screen.getByRole('button', { name: 'Rechazar' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Rechazar horas extra' });
    fireEvent.change(within(dialogo).getByLabelText('Motivo del rechazo'), { target: { value: '  No autorizadas  ' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Rechazar horas extra' }));
    await waitFor(() => expect(escrituras(llamadas)).toEqual([
      { url: '/api/v1/jornadas-asistencia/2/horas-extra/rechazo', metodo: 'POST', cuerpo: { motivo: 'No autorizadas' } },
    ]));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(screen.getByText('Motivo del rechazo').nextElementSibling).toHaveTextContent('No autorizadas');
    expect(screen.queryByRole('button', { name: 'Aprobar horas extra' })).not.toBeInTheDocument();
  });

  it('Escape cierra el diálogo de rechazo sin enviar', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    await seleccionarJornada('EMP-000002 · Luis Rojas Mendoza', 0, 'Luis Rojas Mendoza');
    fireEvent.click(screen.getByRole('button', { name: 'Rechazar' }));
    await screen.findByRole('dialog', { name: 'Rechazar horas extra' });
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('muestra el error del servidor dentro del diálogo de rechazo', async () => {
    const { fetchMock } = prepararApiAsistencia({ errorEscritura: { accion: 'rechazar', status: 409, mensaje: 'La jornada 2 no tiene horas extra pendientes.' } });
    await montarJornadasCargadas(fetchMock);
    await seleccionarJornada('EMP-000002 · Luis Rojas Mendoza', 0, 'Luis Rojas Mendoza');
    fireEvent.click(screen.getByRole('button', { name: 'Rechazar' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Rechazar horas extra' });
    fireEvent.change(within(dialogo).getByLabelText('Motivo del rechazo'), { target: { value: 'No autorizadas' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Rechazar horas extra' }));
    expect(await within(dialogo).findByText('La jornada 2 no tiene horas extra pendientes.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Jornadas: cálculo', () => {
  it('calcula el rango del filtro y muestra calculadas, conservadas y eliminadas', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Calcular jornadas' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Calcular jornadas' });
    const { desde, hasta } = semanaActual();
    expect(within(dialogo).getByLabelText('Desde')).toHaveValue(desde);
    expect(within(dialogo).getByLabelText('Hasta')).toHaveValue(hasta);
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Calcular' }));
    await waitFor(() => expect(escrituras(llamadas)).toEqual([
      { url: '/api/v1/jornadas-asistencia/calculo', metodo: 'POST', cuerpo: { desde, hasta } },
    ]));
    expect(await within(dialogo).findByText('Calculadas')).toBeInTheDocument();
    expect(within(dialogo).getByText('Calculadas').nextElementSibling).toHaveTextContent('12');
    expect(within(dialogo).getByText('Conservadas').nextElementSibling).toHaveTextContent('1');
    expect(within(dialogo).getByText('Eliminadas').nextElementSibling).toHaveTextContent('0');
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Cerrar' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(urlsDeListadoJornadas(fetchMock)).toHaveLength(2));
  });

  it('envía el empleado elegido y valida el rango con los mensajes 16 y 18 sin enviar', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Calcular jornadas' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Calcular jornadas' });
    fireEvent.change(within(dialogo).getByLabelText('Desde'), { target: { value: '' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Calcular' }));
    expect(await within(dialogo).findByText('Las fechas desde y hasta son obligatorias y desde no puede ser posterior a hasta.')).toBeInTheDocument();
    fireEvent.change(within(dialogo).getByLabelText('Desde'), { target: { value: '2099-01-01' } });
    fireEvent.change(within(dialogo).getByLabelText('Hasta'), { target: { value: '2099-01-02' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Calcular' }));
    expect(await within(dialogo).findByText('No se pueden calcular jornadas futuras.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
    fireEvent.change(within(dialogo).getByLabelText('Desde'), { target: { value: '2026-11-02' } });
    fireEvent.change(within(dialogo).getByLabelText('Hasta'), { target: { value: '2026-11-04' } });
    fireEvent.change(within(dialogo).getByLabelText('Empleado (opcional)'), { target: { value: '2' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Calcular' }));
    await waitFor(() => expect(escrituras(llamadas)).toEqual([
      { url: '/api/v1/jornadas-asistencia/calculo', metodo: 'POST', cuerpo: { desde: '2026-11-02', hasta: '2026-11-04', empleadoId: 2 } },
    ]));
  });

  it('muestra el error del servidor en el diálogo de cálculo', async () => {
    const { fetchMock } = prepararApiAsistencia({ errorEscritura: { accion: 'calcular', status: 400, mensaje: 'El rango de cálculo no puede superar 31 días.' } });
    await montarJornadasCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Calcular jornadas' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Calcular jornadas' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Calcular' }));
    expect(await within(dialogo).findByText('El rango de cálculo no puede superar 31 días.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Jornadas: roles', () => {
  it('sin RRHH ni ADMIN deshabilita calcular, aprobar y rechazar con el aviso', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock, ['CONSULTA']);
    expect(screen.getByRole('button', { name: 'Calcular jornadas' })).toBeDisabled();
    expect(screen.getByText(TEXTO_SOLO_RRHH)).toBeInTheDocument();
    await seleccionarJornada('EMP-000002 · Luis Rojas Mendoza', 0, 'Luis Rojas Mendoza');
    expect(screen.getByRole('button', { name: 'Aprobar horas extra' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Rechazar' })).toBeDisabled();
    expect(screen.getAllByText(TEXTO_SOLO_RRHH)).toHaveLength(2);
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('con ADMIN las acciones están habilitadas', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarJornadasCargadas(fetchMock, ['ADMIN']);
    expect(screen.getByRole('button', { name: 'Calcular jornadas' })).toBeEnabled();
    await seleccionarJornada('EMP-000002 · Luis Rojas Mendoza', 0, 'Luis Rojas Mendoza');
    expect(screen.getByRole('button', { name: 'Aprobar horas extra' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Rechazar' })).toBeEnabled();
    expect(screen.queryByText(TEXTO_SOLO_RRHH)).not.toBeInTheDocument();
  });
});
