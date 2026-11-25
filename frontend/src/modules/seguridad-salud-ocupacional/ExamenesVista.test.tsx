import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { limpiarSst, llamadasSst, montarExamenesCargados, prepararApiSst, urlsQueContienen } from './sst.testUtils';

afterEach(() => {
  limpiarSst();
});

function urlsExamenes(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueContienen(fetchMock, '/examenes-medicos?');
}

describe('vista de Exámenes médicos: listado y filtros', () => {
  it('lista empleado, tipo, fecha, resultado y vencimiento con texto', async () => {
    const { fetchMock } = prepararApiSst();
    await montarExamenesCargados(fetchMock);
    expect(urlsExamenes(fetchMock)).toEqual(['/api/v1/examenes-medicos?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table', { name: 'Listado de exámenes médicos' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual(['Empleado', 'Tipo', 'Fecha', 'Resultado', 'Vencimiento', 'Estado']);
    const filas = within(tabla).getAllByRole('row');
    expect(filas[1]).toHaveTextContent('Periódico');
    expect(filas[1]).toHaveTextContent('Apto con restricciones');
    expect(filas[1]).toHaveTextContent('Sin trabajo en altura');
    expect(filas[1]).toHaveTextContent('Vencido');
    expect(filas[2]).toHaveTextContent('Vigente');
    expect(filas[3]).toHaveTextContent('No apto');
    expect(filas[3]).toHaveTextContent('No aplica');
  });

  it('filtra por empleado, tipo, resultado y «Solo vencidos»', async () => {
    const { fetchMock } = prepararApiSst();
    await montarExamenesCargados(fetchMock);
    fireEvent.change(screen.getByLabelText('Empleado'), { target: { value: '1' } });
    await waitFor(() => expect(urlsExamenes(fetchMock).at(-1)).toBe('/api/v1/examenes-medicos?empleadoId=1&pagina=1&tamanoPagina=10'));
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Periodico' } });
    fireEvent.change(screen.getByLabelText('Resultado'), { target: { value: 'AptoConRestricciones' } });
    fireEvent.click(screen.getByLabelText('Solo vencidos'));
    await waitFor(() => expect(urlsExamenes(fetchMock).at(-1)).toBe('/api/v1/examenes-medicos?empleadoId=1&tipo=Periodico&resultado=AptoConRestricciones&vencidos=true&pagina=1&tamanoPagina=10'));
  });

  it('aplica los filtros de la URL al abrir la pestaña', async () => {
    const { fetchMock } = prepararApiSst();
    await montarExamenesCargados(fetchMock, ['RRHH'], '&resultado=AptoConRestricciones&vencidos=true');
    expect(urlsExamenes(fetchMock)).toEqual(['/api/v1/examenes-medicos?resultado=AptoConRestricciones&vencidos=true&pagina=1&tamanoPagina=10']);
    expect(screen.getByLabelText('Resultado')).toHaveValue('AptoConRestricciones');
    expect(screen.getByLabelText('Solo vencidos')).toBeChecked();
  });
});

describe('vista de Exámenes médicos: alta', () => {
  it('muestra restricciones solo con AptoConRestricciones y oculta el vencimiento en Retiro', async () => {
    const { fetchMock } = prepararApiSst();
    await montarExamenesCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo examen' }));
    expect(screen.queryByLabelText('Restricciones')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Fecha de vencimiento')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Resultado', { selector: '#examen-resultado' }), { target: { value: 'AptoConRestricciones' } });
    expect(screen.getByLabelText('Restricciones')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Resultado', { selector: '#examen-resultado' }), { target: { value: 'Apto' } });
    expect(screen.queryByLabelText('Restricciones')).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Tipo de examen'), { target: { value: 'Retiro' } });
    expect(screen.queryByLabelText('Fecha de vencimiento')).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Tipo de examen'), { target: { value: 'Ingreso' } });
    expect(screen.getByLabelText('Fecha de vencimiento')).toBeInTheDocument();
  });

  it('valida los mensajes locales y no envía nada con datos inválidos', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarExamenesCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo examen' }));
    fireEvent.click(screen.getByRole('button', { name: 'Registrar examen' }));
    expect(screen.getByText('El empleado es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El tipo de examen debe ser Ingreso, Periodico o Retiro.')).toBeInTheDocument();
    expect(screen.getByText('El resultado debe ser Apto, AptoConRestricciones o NoApto.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Tipo de examen'), { target: { value: 'Periodico' } });
    fireEvent.change(screen.getByLabelText('Resultado', { selector: '#examen-resultado' }), { target: { value: 'AptoConRestricciones' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar examen' }));
    expect(screen.getByText('Las restricciones son obligatorias si el resultado es AptoConRestricciones y no pueden superar 300 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('La fecha de vencimiento es obligatoria y posterior al examen en los exámenes de ingreso y periódicos.')).toBeInTheDocument();
    expect(llamadasSst(llamadas, 'POST', '/examenes-medicos')).toHaveLength(0);
  });

  it('registra un examen periódico con restricciones', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarExamenesCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo examen' }));
    fireEvent.change(screen.getByLabelText('Empleado', { selector: '#examen-empleado' }), { target: { value: '2' } });
    fireEvent.change(screen.getByLabelText('Tipo de examen'), { target: { value: 'Periodico' } });
    fireEvent.change(screen.getByLabelText('Fecha del examen'), { target: { value: '2026-11-10' } });
    fireEvent.change(screen.getByLabelText('Resultado', { selector: '#examen-resultado' }), { target: { value: 'AptoConRestricciones' } });
    fireEvent.change(screen.getByLabelText('Restricciones'), { target: { value: ' No cargar peso ' } });
    fireEvent.change(screen.getByLabelText('Fecha de vencimiento'), { target: { value: '2027-11-10' } });
    fireEvent.change(screen.getByLabelText('Clínica (opcional)'), { target: { value: ' Clínica Sur ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar examen' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'POST', '/examenes-medicos')).toHaveLength(1));
    expect(llamadasSst(llamadas, 'POST', '/examenes-medicos')[0]?.cuerpo).toEqual({ empleadoId: 2, tipo: 'Periodico', fecha: '2026-11-10', resultado: 'AptoConRestricciones', restricciones: 'No cargar peso', fechaVencimiento: '2027-11-10', clinica: 'Clínica Sur' });
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nuevo examen médico' })).not.toBeInTheDocument());
  });

  it('registra un examen de retiro sin vencimiento ni restricciones', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarExamenesCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo examen' }));
    fireEvent.change(screen.getByLabelText('Empleado', { selector: '#examen-empleado' }), { target: { value: '3' } });
    fireEvent.change(screen.getByLabelText('Tipo de examen'), { target: { value: 'Retiro' } });
    fireEvent.change(screen.getByLabelText('Fecha del examen'), { target: { value: '2026-11-10' } });
    fireEvent.change(screen.getByLabelText('Resultado', { selector: '#examen-resultado' }), { target: { value: 'Apto' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar examen' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'POST', '/examenes-medicos')).toHaveLength(1));
    expect(llamadasSst(llamadas, 'POST', '/examenes-medicos')[0]?.cuerpo).toEqual({ empleadoId: 3, tipo: 'Retiro', fecha: '2026-11-10', resultado: 'Apto', restricciones: null, fechaVencimiento: null, clinica: null });
  });

  it('muestra el mensaje literal del backend sobre el cese y conserva lo escrito', async () => {
    const mensaje = 'El examen de retiro se registra hasta 30 días después del cese del empleado 3.';
    const { fetchMock } = prepararApiSst({ errorEscritura: { metodo: 'POST', sufijo: '/examenes-medicos', status: 409, mensaje } });
    await montarExamenesCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo examen' }));
    fireEvent.change(screen.getByLabelText('Empleado', { selector: '#examen-empleado' }), { target: { value: '3' } });
    fireEvent.change(screen.getByLabelText('Tipo de examen'), { target: { value: 'Retiro' } });
    fireEvent.change(screen.getByLabelText('Fecha del examen'), { target: { value: '2026-11-10' } });
    fireEvent.change(screen.getByLabelText('Resultado', { selector: '#examen-resultado' }), { target: { value: 'Apto' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar examen' }));
    expect(await screen.findByText(mensaje)).toBeInTheDocument();
    expect(screen.getByLabelText('Fecha del examen')).toHaveValue('2026-11-10');
  });
});
