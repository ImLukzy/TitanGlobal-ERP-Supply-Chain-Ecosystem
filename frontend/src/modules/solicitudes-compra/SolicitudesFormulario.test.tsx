import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { esperarCargaInicial, prepararApi, renderPagina } from './solicitudes.testUtils';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('SolicitudesCompra: formulario', () => {
  it('A4 añade y quita líneas y calcula subtotales y total en vivo', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva solicitud' }));
    const productos = screen.getAllByLabelText('Producto');
    fireEvent.change(productos[0]!, { target: { value: '1' } });
    fireEvent.change(screen.getAllByLabelText('Cantidad')[0]!, { target: { value: '2' } });
    expect(screen.getAllByText(/5,000\.00/).length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole('button', { name: 'Añadir línea' }));
    fireEvent.change(screen.getAllByLabelText('Producto')[1]!, { target: { value: '2' } });
    expect(screen.getAllByText(/5,900\.00/).length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole('button', { name: 'Quitar línea 2' }));
    expect(screen.getAllByText(/5,000\.00/).length).toBeGreaterThan(0);
  });

  it('A4 muestra los errores locales literales por campo y no envía solicitudes inválidas', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva solicitud' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar solicitud' }));
    expect(screen.getByText('El solicitante es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('Selecciona un producto.')).toBeInTheDocument();
    expect(screen.getByLabelText('Solicitante')).toHaveAttribute('aria-describedby', 'solicitud-solicitante-error');
    expect(screen.getByLabelText('Producto')).toHaveAttribute('aria-describedby', 'linea-0-producto-error');
    fireEvent.click(screen.getByRole('button', { name: 'Quitar línea 1' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar solicitud' }));
    expect(screen.getByText('La solicitud debe tener al menos una línea.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Añadir línea' }));
    fireEvent.change(screen.getByLabelText('Solicitante'), { target: { value: 'S'.repeat(101) } });
    fireEvent.change(screen.getByLabelText('Observaciones'), { target: { value: 'O'.repeat(501) } });
    fireEvent.change(screen.getByLabelText('Producto'), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText('Cantidad'), { target: { value: '0' } });
    fireEvent.change(screen.getByLabelText('Precio estimado'), { target: { value: '-1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Añadir línea' }));
    fireEvent.change(screen.getAllByLabelText('Producto')[1]!, { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar solicitud' }));
    expect(screen.getByText('El solicitante no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('Las observaciones no pueden superar 500 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('La cantidad debe ser mayor que 0.')).toBeInTheDocument();
    expect(screen.getByText('El precio estimado no puede ser negativo.')).toBeInTheDocument();
    expect(screen.getByText('El producto 1 está repetido en la solicitud.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => (call[1] as RequestInit | undefined)?.method === 'POST')).toBe(false);
  });

  it('A4 conserva los datos al mostrar un 400 literal de la API', async () => {
    const { fetchMock } = prepararApi({ errorCrear: true });
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva solicitud' }));
    fireEvent.change(screen.getByLabelText('Solicitante'), { target: { value: 'Elena Rojas' } });
    fireEvent.change(screen.getByLabelText('Producto'), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText('Precio estimado'), { target: { value: '2600' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar solicitud' }));
    expect(await screen.findByText('El solicitante no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(screen.getByLabelText('Solicitante')).toHaveValue('Elena Rojas');
    expect(screen.getByLabelText('Producto')).toHaveValue('1');
    await waitFor(() => {
      expect(fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/solicitudes-compra'
        && (call[1] as RequestInit | undefined)?.method === 'POST')).toBe(true);
    });
  });

  it('A4 registra una solicitud nueva, muestra su número y refresca detalle y resumen', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva solicitud' }));
    fireEvent.change(screen.getByLabelText('Solicitante'), { target: { value: 'Marta Ríos' } });
    fireEvent.change(screen.getByLabelText('Producto'), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText('Cantidad'), { target: { value: '2' } });
    fireEvent.change(screen.getByLabelText('Precio estimado'), { target: { value: '2500' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar solicitud' }));
    expect(await screen.findByRole('heading', { name: 'SC-000006' })).toBeInTheDocument();
    const llamada = fetchMock.mock.calls.find((call) => String(call[0]) === '/api/v1/solicitudes-compra'
      && (call[1] as RequestInit | undefined)?.method === 'POST');
    expect(llamada).toBeDefined();
    expect(JSON.parse(String((llamada?.[1] as RequestInit).body))).toMatchObject({
      solicitante: 'Marta Ríos',
      lineas: [{ productoId: 1, cantidad: 2, precioEstimado: 2500 }],
    });
    await waitFor(() => {
      expect(fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/solicitudes-compra/resumen')).toBe(true);
    });
  });

  it('A4 precarga edición y envía PUT con el id correspondiente', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'SC-000001' }));
    await screen.findByText('Entrega para la planta principal');
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    expect(screen.getByLabelText('Solicitante')).toHaveValue('Ana Torres');
    expect(screen.getAllByLabelText('Producto')[0]).toHaveValue('1');
    fireEvent.change(screen.getByLabelText('Solicitante'), { target: { value: 'Ana Torres actualizada' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar solicitud' }));
    await waitFor(() => {
      const llamada = fetchMock.mock.calls.find((call) => String(call[0]) === '/api/v1/solicitudes-compra/1'
        && (call[1] as RequestInit | undefined)?.method === 'PUT');
      expect(llamada).toBeDefined();
      expect(JSON.parse(String((llamada?.[1] as RequestInit).body))).toMatchObject({ solicitante: 'Ana Torres actualizada' });
    });
  });
});
