import { afterEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { esperarCargaInicial, limpiar, prepararApi, renderPromociones } from './promociones.testUtils';

afterEach(limpiar);

async function montar(fetchMock: ReturnType<typeof prepararApi>['fetchMock']): Promise<void> {
  renderPromociones();
  await esperarCargaInicial(fetchMock);
}

describe('PromocionesPage', () => {
  it('A3 muestra tarjetas, descripciones, estados y no evalúa hasta pulsar el botón', async () => {
    const { fetchMock, llamadas } = prepararApi();
    await montar(fetchMock);
    const resumen = screen.getByRole('region', { name: 'Resumen de promociones' });
    expect(resumen.querySelector('article strong')).toHaveTextContent('4');
    expect(screen.getAllByText('Producto: Laptop Pro').length).toBeGreaterThan(0);
    expect(screen.getByText('Compra desde S/ 3,000.00')).toBeInTheDocument();
    expect(screen.getByText('Todos los clientes')).toBeInTheDocument();
    expect(screen.getByText('Vigente')).toBeInTheDocument();
    expect(screen.getByText('No vigente')).toBeInTheDocument();
    expect(screen.getByText('Inactiva')).toBeInTheDocument();
    expect(llamadas.filter((item) => item.url.endsWith('/promociones/evaluar'))).toHaveLength(0);
  });

  it('A3 envía cliente, fecha y líneas y presenta el desglose Acme 4,037.50', async () => {
    const { fetchMock, llamadas } = prepararApi();
    await montar(fetchMock);
    fireEvent.change(screen.getByLabelText('Cliente'), { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Agregar línea' }));
    fireEvent.click(screen.getByRole('button', { name: 'Evaluar' }));
    await screen.findByRole('region', { name: 'Resultado de la evaluación' });
    const resultado = screen.getByRole('region', { name: 'Resultado de la evaluación' });
    expect(within(resultado).getByText('LAPTOP-VIP-15')).toBeInTheDocument();
    expect(within(resultado).getByText('COMPRA-3000')).toBeInTheDocument();
    expect(within(resultado).getByText(/4,037\.50/)).toBeInTheDocument();
    const llamada = llamadas.find((item) => item.url.endsWith('/promociones/evaluar'));
    expect(JSON.parse(String(llamada?.init.body))).toEqual({ clienteId: 1, fecha: expect.any(String), lineas: [{ productoId: 1, cantidad: 1, precioUnitario: 2500 }] });
  });

  it('A3 informa el resultado sin promociones con el total igual al subtotal', async () => {
    const { fetchMock } = prepararApi({ sinPromociones: true });
    await montar(fetchMock);
    fireEvent.change(screen.getByLabelText('Cliente'), { target: { value: '2' } });
    fireEvent.click(screen.getByRole('button', { name: 'Agregar línea' }));
    fireEvent.click(screen.getByRole('button', { name: 'Evaluar' }));
    expect(await screen.findByText('No hay promociones aplicables')).toBeInTheDocument();
    const resultado = screen.getByRole('region', { name: 'Resultado de la evaluación' });
    const total = within(resultado).getByText('Total').parentElement;
    expect(total).toHaveTextContent('5,000.00');
  });

  it('A3 valida localmente cliente y líneas sin llamar al evaluador', async () => {
    const { fetchMock, llamadas } = prepararApi();
    await montar(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Evaluar' }));
    expect(screen.getByText('Selecciona un cliente.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Cliente'), { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Evaluar' }));
    expect(screen.getByText('Agrega al menos una línea.')).toBeInTheDocument();
    expect(llamadas.filter((item) => item.url.endsWith('/promociones/evaluar'))).toHaveLength(0);
  });

  it('A3 rechaza cantidades y precios inválidos sin enviar la compra', async () => {
    const { fetchMock, llamadas } = prepararApi();
    await montar(fetchMock);
    fireEvent.change(screen.getByLabelText('Cliente'), { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Agregar línea' }));
    fireEvent.change(screen.getByLabelText('Cantidad'), { target: { value: '0' } });
    fireEvent.click(screen.getByRole('button', { name: 'Evaluar' }));
    expect(screen.getByText('La cantidad debe ser mayor que 0.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Cantidad'), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText('Precio unitario'), { target: { value: '0' } });
    fireEvent.click(screen.getByRole('button', { name: 'Evaluar' }));
    expect(screen.getByText('El precio unitario debe ser mayor que 0.')).toBeInTheDocument();
    expect(llamadas.filter((item) => item.url.endsWith('/promociones/evaluar'))).toHaveLength(0);
  });

  it('A3 presenta el mensaje de error devuelto por la evaluación', async () => {
    const { fetchMock } = prepararApi({ errorEvaluar: true });
    await montar(fetchMock);
    fireEvent.change(screen.getByLabelText('Cliente'), { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Agregar línea' }));
    fireEvent.click(screen.getByRole('button', { name: 'Evaluar' }));
    expect(await screen.findByText('No se pudo evaluar la compra.')).toBeInTheDocument();
  });

  it('A4 filtra y vuelve a página 1 con una búsqueda de 300 ms', async () => {
    const { fetchMock, llamadas } = prepararApi();
    await montar(fetchMock);
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText('Buscar por código o nombre'), { target: { value: 'Laptop' } });
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Producto' } });
    fireEvent.change(screen.getByLabelText('Segmento'), { target: { value: '1' } });
    fireEvent.click(screen.getByLabelText('Solo vigentes'));
    await act(async () => { await vi.advanceTimersByTimeAsync(301); });
    vi.useRealTimers();
    await waitFor(() => expect(llamadas.some((item) => item.url.includes('busqueda=Laptop') && item.url.includes('tipo=Producto') && item.url.includes('segmentoId=1') && item.url.includes('vigentes=true') && item.url.includes('pagina=1'))).toBe(true));
  });

  it('A4 alterna tipo y limpia el campo excluido, valida y crea', async () => {
    const { fetchMock, llamadas } = prepararApi();
    await montar(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva promoción' }));
    const dialogo = within(screen.getByRole('dialog'));
    expect(dialogo.getByLabelText('Producto')).toBeInTheDocument();
    fireEvent.change(dialogo.getByLabelText('Producto'), { target: { value: '1' } });
    fireEvent.click(dialogo.getByLabelText('Por monto mínimo'));
    expect(dialogo.getByLabelText('Monto mínimo')).toBeInTheDocument();
    fireEvent.change(dialogo.getByLabelText('Monto mínimo'), { target: { value: '3000' } });
    fireEvent.click(dialogo.getByLabelText('Por producto'));
    expect(dialogo.getByLabelText('Producto')).toHaveValue('');
    expect(dialogo.queryByLabelText('Monto mínimo')).not.toBeInTheDocument();
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar promoción' }));
    expect(dialogo.getByText('El código es obligatorio.')).toBeInTheDocument();
    expect(llamadas.filter((item) => item.init.method === 'POST' && item.url.endsWith('/promociones'))).toHaveLength(0);
    fireEvent.change(dialogo.getByLabelText('Código'), { target: { value: 'nuevo-10' } });
    fireEvent.change(dialogo.getByLabelText('Nombre'), { target: { value: 'Nuevo diez' } });
    fireEvent.change(dialogo.getByLabelText('Producto'), { target: { value: '1' } });
    fireEvent.change(dialogo.getByLabelText('Porcentaje de descuento'), { target: { value: '10' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar promoción' }));
    await waitFor(() => expect(llamadas.some((item) => item.init.method === 'POST' && item.url.endsWith('/promociones'))).toBe(true));
  });

  it('A4 conserva los valores escritos cuando el servidor responde 409', async () => {
    const { fetchMock } = prepararApi({ errorCrear: true });
    await montar(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva promoción' }));
    const dialogo = within(screen.getByRole('dialog'));
    fireEvent.change(dialogo.getByLabelText('Código'), { target: { value: 'LAPTOP-10' } });
    fireEvent.change(dialogo.getByLabelText('Nombre'), { target: { value: 'Promoción duplicada' } });
    fireEvent.change(dialogo.getByLabelText('Producto'), { target: { value: '1' } });
    fireEvent.change(dialogo.getByLabelText('Porcentaje de descuento'), { target: { value: '10' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar promoción' }));
    expect(await screen.findByText('Ya existe una promoción con el código LAPTOP-10.')).toBeInTheDocument();
    expect(dialogo.getByLabelText('Nombre')).toHaveValue('Promoción duplicada');
  });

  it('A4 permite editar con PUT y cerrar la baja con Escape sin petición', async () => {
    const { fetchMock, llamadas } = prepararApi();
    await montar(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'LAPTOP-VIP-15' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Editar' }));
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Nombre actualizado' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar promoción' }));
    await waitFor(() => expect(llamadas.some((item) => item.init.method === 'PUT' && item.url.endsWith('/promociones/1'))).toBe(true));
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    const antes = llamadas.length;
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(llamadas).toHaveLength(antes);
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar promoción' }));
    await waitFor(() => expect(llamadas.some((item) => item.init.method === 'DELETE' && item.url.endsWith('/promociones/1'))).toBe(true));
    expect(fetchMock).toHaveBeenCalled();
  });
});
