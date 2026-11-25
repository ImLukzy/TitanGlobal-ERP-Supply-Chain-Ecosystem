import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { limpiarSst, llamadasSst, montarEppCargado, prepararApiSst, urlsQueContienen } from './sst.testUtils';

afterEach(() => {
  limpiarSst();
});

function urlsEntregas(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueContienen(fetchMock, '/entregas-epp?');
}

describe('vista de EPP: entregas', () => {
  it('lista entregas con vencimiento y «Vencida» con texto', async () => {
    const { fetchMock } = prepararApiSst();
    await montarEppCargado(fetchMock);
    expect(urlsEntregas(fetchMock)).toEqual(['/api/v1/entregas-epp?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table', { name: 'Listado de entregas de EPP' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual(['Empleado', 'Equipo', 'Entrega', 'Cantidad', 'Vencimiento', 'Estado']);
    const filas = within(tabla).getAllByRole('row');
    expect(filas[1]).toHaveTextContent('10/01/2026');
    expect(filas[1]).toHaveTextContent('Vencida');
    expect(filas[2]).toHaveTextContent('30/03/2027');
    expect(filas[2]).toHaveTextContent('Vigente');
  });

  it('filtra por empleado, equipo y «Solo vencidas»', async () => {
    const { fetchMock } = prepararApiSst();
    await montarEppCargado(fetchMock);
    fireEvent.change(screen.getByLabelText('Empleado'), { target: { value: '2' } });
    await waitFor(() => expect(urlsEntregas(fetchMock).at(-1)).toBe('/api/v1/entregas-epp?empleadoId=2&pagina=1&tamanoPagina=10'));
    fireEvent.change(screen.getByLabelText('Equipo'), { target: { value: '1' } });
    fireEvent.click(screen.getByLabelText('Solo vencidas'));
    await waitFor(() => expect(urlsEntregas(fetchMock).at(-1)).toBe('/api/v1/entregas-epp?empleadoId=2&equipoId=1&vencidas=true&pagina=1&tamanoPagina=10'));
  });

  it('muestra el vencimiento calculado a la vista y registra la entrega', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarEppCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva entrega' }));
    expect(screen.getByTestId('entrega-vencimiento')).toHaveTextContent('El vencimiento se calcula al elegir el equipo y la fecha.');
    fireEvent.change(screen.getByLabelText('Equipo', { selector: '#entrega-equipo' }), { target: { value: '2' } });
    expect(screen.getByTestId('entrega-vencimiento')).toHaveTextContent('Vence el 21/05/2027');
    fireEvent.change(screen.getByLabelText('Fecha de entrega'), { target: { value: '2026-11-01' } });
    expect(screen.getByTestId('entrega-vencimiento')).toHaveTextContent('Vence el 30/04/2027');
    fireEvent.change(screen.getByLabelText('Empleado', { selector: '#entrega-empleado' }), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText('Cantidad'), { target: { value: '3' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar entrega' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'POST', '/entregas-epp')).toHaveLength(1));
    expect(llamadasSst(llamadas, 'POST', '/entregas-epp')[0]?.cuerpo).toEqual({ equipoId: 2, empleadoId: 1, fechaEntrega: '2026-11-01', cantidad: 3 });
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nueva entrega' })).not.toBeInTheDocument());
  });

  it('ofrece solo empleados y equipos activos y valida antes de enviar', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarEppCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva entrega' }));
    const empleados = within(screen.getByLabelText('Empleado', { selector: '#entrega-empleado' })).getAllByRole('option').map((opcion) => opcion.textContent);
    expect(empleados).toEqual(['Selecciona un empleado', 'EMP-000001 · Torres Vega, Ana', 'EMP-000002 · Rojas Mendoza, Luis']);
    const equipos = within(screen.getByLabelText('Equipo', { selector: '#entrega-equipo' })).getAllByRole('option').map((opcion) => opcion.textContent);
    expect(equipos).toEqual(['Selecciona un equipo', 'CASCO · Casco de seguridad', 'GUANTE · Guantes dieléctricos']);
    fireEvent.change(screen.getByLabelText('Fecha de entrega'), { target: { value: '2026-11-23' } });
    fireEvent.change(screen.getByLabelText('Cantidad'), { target: { value: '0' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar entrega' }));
    expect(screen.getByText('El empleado es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('La fecha es obligatoria y no puede ser posterior a hoy.')).toBeInTheDocument();
    expect(screen.getByText('La cantidad debe estar entre 1 y 100.')).toBeInTheDocument();
    expect(llamadasSst(llamadas, 'POST', '/entregas-epp')).toHaveLength(0);
  });

  it('muestra el mensaje literal del backend si el empleado está cesado', async () => {
    const { fetchMock } = prepararApiSst({ errorEscritura: { metodo: 'POST', sufijo: '/entregas-epp', status: 409, mensaje: 'El empleado 1 está cesado.' } });
    await montarEppCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva entrega' }));
    fireEvent.change(screen.getByLabelText('Equipo', { selector: '#entrega-equipo' }), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText('Empleado', { selector: '#entrega-empleado' }), { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar entrega' }));
    expect(await screen.findByText('El empleado 1 está cesado.')).toBeInTheDocument();
  });
});

describe('vista de EPP: catálogo de equipos', () => {
  it('lista el catálogo con el estado en texto y el filtro de estado', async () => {
    const { fetchMock } = prepararApiSst();
    await montarEppCargado(fetchMock);
    const tabla = screen.getByRole('table', { name: 'Catálogo de equipos de protección' });
    expect(within(tabla).getAllByRole('row')[3]).toHaveTextContent('Inactivo');
    expect(screen.getByRole('button', { name: 'Activar equipo ARNES' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Desactivar equipo CASCO' })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Activos' } });
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/equipos-proteccion?').at(-1)).toBe('/api/v1/equipos-proteccion?activo=true&pagina=1&tamanoPagina=10'));
  });

  it('crea un equipo con validación local y cuerpo recortado', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarEppCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo equipo' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar equipo' }));
    expect(screen.getByText('El código es obligatorio y no puede superar 20 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('El nombre es obligatorio y no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('La vida útil debe estar entre 1 y 3650 días.')).toBeInTheDocument();
    expect(llamadasSst(llamadas, 'POST', '/equipos-proteccion')).toHaveLength(0);
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: ' bota ' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: ' Botas ' } });
    fireEvent.change(screen.getByLabelText('Vida útil (días)'), { target: { value: '90' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar equipo' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'POST', '/equipos-proteccion')).toHaveLength(1));
    expect(llamadasSst(llamadas, 'POST', '/equipos-proteccion')[0]?.cuerpo).toEqual({ codigo: 'bota', nombre: 'Botas', vidaUtilDias: 90 });
    expect(await screen.findByRole('button', { name: 'Editar equipo BOTA' })).toBeInTheDocument();
  });

  it('edita un equipo con sus datos precargados', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarEppCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Editar equipo GUANTE' }));
    expect(screen.getByRole('heading', { name: 'Editar equipo GUANTE' })).toBeInTheDocument();
    expect(screen.getByLabelText('Vida útil (días)')).toHaveValue('180');
    fireEvent.change(screen.getByLabelText('Vida útil (días)'), { target: { value: '200' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar equipo' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'PUT', '/equipos-proteccion/2')).toHaveLength(1));
    expect(llamadasSst(llamadas, 'PUT', '/equipos-proteccion/2')[0]?.cuerpo).toEqual({ codigo: 'GUANTE', nombre: 'Guantes dieléctricos', vidaUtilDias: 200 });
  });

  it('desactiva y activa equipos con POST sin cuerpo', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarEppCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Desactivar equipo CASCO' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'POST', '/equipos-proteccion/1/desactivacion')).toHaveLength(1));
    expect(await screen.findByRole('button', { name: 'Activar equipo CASCO' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Activar equipo ARNES' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'POST', '/equipos-proteccion/3/activacion')).toHaveLength(1));
    expect(llamadasSst(llamadas, 'POST', '/activacion')[0]?.cuerpo).toBeUndefined();
  });

  it('muestra el mensaje del backend si el código ya existe', async () => {
    const { fetchMock } = prepararApiSst({ errorEscritura: { metodo: 'POST', sufijo: '/equipos-proteccion', status: 409, mensaje: 'Ya existe un equipo de protección con el código CASCO.' } });
    await montarEppCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo equipo' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'CASCO' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Casco' } });
    fireEvent.change(screen.getByLabelText('Vida útil (días)'), { target: { value: '10' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar equipo' }));
    expect(await screen.findByText('Ya existe un equipo de protección con el código CASCO.')).toBeInTheDocument();
    expect(screen.getByLabelText('Código')).toHaveValue('CASCO');
  });
});
