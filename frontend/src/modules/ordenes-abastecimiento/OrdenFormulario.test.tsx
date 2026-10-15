import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { OrdenesAbastecimientoPage } from './OrdenesAbastecimientoPage';
import { esperarCargaInicial, prepararApi, renderEnRouter } from './ordenes.testUtils';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('pantalla de órdenes de abastecimiento: formulario', () => {
  it('A4 calcula subtotales y total al añadir y quitar líneas', async () => {
    prepararApi();
    renderEnRouter(<OrdenesAbastecimientoPage />);
    await esperarCargaInicial();
    fireEvent.click(screen.getByRole('button', { name: 'Nueva orden' }));
    const formulario = screen.getByRole('region', { name: 'Nueva orden' });
    fireEvent.change(within(formulario).getByLabelText('Proveedor'), { target: { value: '1' } });
    fireEvent.change(within(formulario).getByLabelText('Producto'), { target: { value: '1' } });
    fireEvent.change(within(formulario).getByLabelText('Cantidad'), { target: { value: '2' } });
    expect(screen.getAllByText(/5,000\.00/).length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole('button', { name: 'Añadir línea' }));
    fireEvent.change(within(formulario).getAllByLabelText('Producto')[1]!, { target: { value: '2' } });
    expect(screen.getAllByText(/5,900\.00/).length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole('button', { name: 'Quitar línea 2' }));
    expect(screen.getAllByText(/5,000\.00/).length).toBeGreaterThan(0);
  });

  it('A4 valida cada campo localmente sin peticiones y conserva el foco accesible', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<OrdenesAbastecimientoPage />);
    await esperarCargaInicial();
    fetchMock.mockClear();
    fireEvent.click(screen.getByRole('button', { name: 'Nueva orden' }));
    const formulario = screen.getByRole('region', { name: 'Nueva orden' });
    fireEvent.change(within(formulario).getByLabelText('Proveedor'), { target: { value: '' } });
    fireEvent.change(within(formulario).getByLabelText('Fecha de entrega estimada'), { target: { value: '' } });
    fireEvent.change(within(formulario).getByLabelText('Producto'), { target: { value: '' } });
    fireEvent.change(within(formulario).getByLabelText('Cantidad'), { target: { value: '0' } });
    fireEvent.change(within(formulario).getByLabelText('Precio unitario'), { target: { value: '0' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar orden' }));
    expect(screen.getByText('Indica el proveedor de la orden.')).toBeInTheDocument();
    expect(screen.getByText('La fecha de entrega estimada es obligatoria.')).toBeInTheDocument();
    expect(screen.getByText('Selecciona un producto.')).toBeInTheDocument();
    expect(screen.getByText('La cantidad debe ser mayor que 0.')).toBeInTheDocument();
    expect(screen.getByText('El precio unitario debe ser mayor que 0.')).toBeInTheDocument();
    expect(within(formulario).getByLabelText('Producto')).toHaveAttribute('aria-describedby', 'orden-linea-0-producto-error');
    expect(fetchMock).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Quitar línea 1' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar orden' }));
    expect(screen.getByText('La orden debe tener al menos una línea.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Añadir línea' }));
    fireEvent.change(within(formulario).getByLabelText('Proveedor'), { target: { value: '1' } });
    fireEvent.change(within(formulario).getByLabelText('Fecha de entrega estimada'), { target: { value: '2000-01-01' } });
    fireEvent.change(within(formulario).getByLabelText('Observaciones'), { target: { value: 'O'.repeat(501) } });
    fireEvent.change(within(formulario).getByLabelText('Producto'), { target: { value: '1' } });
    fireEvent.change(within(formulario).getByLabelText('Cantidad'), { target: { value: '1' } });
    fireEvent.change(within(formulario).getByLabelText('Precio unitario'), { target: { value: '2500' } });
    fireEvent.click(screen.getByRole('button', { name: 'Añadir línea' }));
    fireEvent.change(within(formulario).getAllByLabelText('Producto')[1]!, { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar orden' }));
    expect(screen.getByText('La fecha de entrega estimada no puede ser anterior a hoy.')).toBeInTheDocument();
    expect(screen.getByText('Las observaciones no pueden superar 500 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('El producto 1 está repetido en la orden.')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('A4 muestra el 400 de la API sin perder los datos escritos', async () => {
    const { fetchMock } = prepararApi({ errorCrear: true });
    renderEnRouter(<OrdenesAbastecimientoPage />);
    await esperarCargaInicial();
    fireEvent.click(screen.getByRole('button', { name: 'Nueva orden' }));
    const formulario = screen.getByRole('region', { name: 'Nueva orden' });
    fireEvent.change(within(formulario).getByLabelText('Proveedor'), { target: { value: '1' } });
    fireEvent.change(within(formulario).getByLabelText('Fecha de entrega estimada'), { target: { value: '2030-01-01' } });
    fireEvent.change(within(formulario).getByLabelText('Observaciones'), { target: { value: 'Entrega prioritaria' } });
    fireEvent.change(within(formulario).getByLabelText('Producto'), { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar orden' }));
    expect(await screen.findByText('El proveedor 1 está inactivo.')).toHaveAttribute('role', 'alert');
    expect(within(formulario).getByLabelText('Proveedor')).toHaveValue('1');
    expect(within(formulario).getByLabelText('Observaciones')).toHaveValue('Entrega prioritaria');
    expect(within(formulario).getByLabelText('Producto')).toHaveValue('1');
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/ordenes-abastecimiento' && (call[1] as RequestInit | undefined)?.method === 'POST')).toBe(true));
  });

  it('A4 precarga los datos y actualiza con PUT usando el identificador', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<OrdenesAbastecimientoPage />, ['/modulos/15-ordenes-abastecimiento?orden=1']);
    await screen.findByRole('heading', { name: 'OA-000001' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    const formulario = screen.getByRole('region', { name: 'Editar OA-000001' });
    expect(within(formulario).getByLabelText('Proveedor')).toHaveValue('1');
    expect(within(formulario).getAllByLabelText('Producto')[0]).toHaveValue('1');
    fireEvent.change(within(formulario).getByLabelText('Fecha de entrega estimada'), { target: { value: '2030-05-01' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar orden' }));
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/ordenes-abastecimiento/1' && (call[1] as RequestInit | undefined)?.method === 'PUT')).toBe(true));
  });
});
