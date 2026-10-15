import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { limpiarComisiones, montarComisionesCargadas, prepararApiComisiones, renderComisiones } from './comisiones.testUtils';

afterEach(limpiarComisiones);

describe('Modulo08_ComisionesVendedores', () => {
  it('muestra las tres pestañas y cambia por teclado con query vista', async () => {
    prepararApiComisiones();
    renderComisiones();
    expect(await screen.findByRole('button', { name: 'Nuevo vendedor' })).toBeInTheDocument();
    fireEvent.keyDown(screen.getByRole('tablist', { name: 'Vistas de comisiones de vendedores' }), { key: 'ArrowRight' });
    expect(await screen.findByRole('tabpanel', { name: 'Asignación de ventas' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Asignación de ventas' })).toHaveAttribute('aria-selected', 'true');
  });

  it('A2 carga una sola lista de vendedores y aplica búsqueda tras 300 ms', async () => {
    const { fetchMock, llamadas } = prepararApiComisiones();
    await montarComisionesCargadas(fetchMock);
    expect(llamadas.filter((llamada) => llamada.url.includes('/vendedores?'))).toHaveLength(1);
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText('Buscar por código o nombre'), { target: { value: 'Luis' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(299); });
    expect(llamadas.filter((llamada) => llamada.url.includes('/vendedores?'))).toHaveLength(1);
    await act(async () => { await vi.advanceTimersByTimeAsync(1); });
    vi.useRealTimers();
    expect(await screen.findByRole('button', { name: 'V-LUIS' })).toBeInTheDocument();
    expect(llamadas.at(-1)?.url).toContain('busqueda=Luis');
  });

  it('A3 filtra vendedores por estado y conserva la página 1', async () => {
    const { fetchMock, llamadas } = prepararApiComisiones();
    await montarComisionesCargadas(fetchMock);
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'false' } });
    expect(await screen.findByRole('button', { name: 'V-OLD' })).toBeInTheDocument();
    expect(llamadas.at(-1)?.url).toContain('activo=false&pagina=1&tamanoPagina=10');
  });

  it('A3 valida campos localmente y no escribe cuando están vacíos', async () => {
    const { fetchMock } = prepararApiComisiones();
    await montarComisionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo vendedor' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar vendedor' }));
    expect(await screen.findByText('El código es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El nombre del vendedor es obligatorio.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((llamada) => (llamada[1] as RequestInit).method === 'POST')).toBe(false);
  });

  it('A3 conserva lo escrito y muestra el mensaje literal al duplicar código', async () => {
    const { fetchMock } = prepararApiComisiones({ codigoDuplicado: true });
    await montarComisionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo vendedor' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'V-NUEVO' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Vendedora nueva' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar vendedor' }));
    expect(await screen.findByText('Ya existe un vendedor con el código V-NUEVO.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre')).toHaveValue('Vendedora nueva');
  });

  it('A3 edita un vendedor con PUT y el cuerpo del formulario', async () => {
    const { fetchMock, llamadas } = prepararApiComisiones();
    await montarComisionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'V-ANA' }));
    expect(await screen.findByText('ana@titanglobal.example')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Ana Torres editada' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar vendedor' }));
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url.endsWith('/vendedores/1') && llamada.init.method === 'PUT')).toBe(true));
    const llamada = llamadas.find((elemento) => elemento.url.endsWith('/vendedores/1') && elemento.init.method === 'PUT');
    expect(JSON.parse(String(llamada?.init.body))).toMatchObject({ codigo: 'V-ANA', nombre: 'Ana Torres editada', porcentajeComision: 5, activo: true });
  });

  it('A3 muestra el 409 de baja en DialogoBaja y cierra con Escape', async () => {
    const { fetchMock } = prepararApiComisiones({ bajaConVentas: true });
    await montarComisionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'V-ANA' }));
    expect(await screen.findByText('ana@titanglobal.example')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar vendedor' }));
    expect(await screen.findByText('No se puede eliminar el vendedor V-ANA porque tiene ventas asignadas.')).toBeInTheDocument();
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('A4 filtra órdenes por mes, vendedor y sin asignar, y ofrece solo vendedores activos', async () => {
    const { fetchMock, llamadas } = prepararApiComisiones();
    await montarComisionesCargadas(fetchMock, '/modulos/08-comisiones-vendedores?vista=asignacion', 'asignacion');
    const selectorOrden = screen.getByLabelText('Vendedor para OC-2026-000101');
    expect(within(selectorOrden).queryByRole('option', { name: /V-OLD/ })).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Año de la orden'), { target: { value: '2026' } });
    fireEvent.change(screen.getByLabelText('Mes de la orden'), { target: { value: '2' } });
    fireEvent.change(screen.getByLabelText('Vendedor'), { target: { value: '1' } });
    fireEvent.click(screen.getByLabelText('Solo sin asignar'));
    await waitFor(() => expect(llamadas.at(-1)?.url).toContain('vendedorId=1&sinAsignar=true&anio=2026&mes=2&pagina=1&tamanoPagina=10'));
  });

  it('A4 asigna y reasigna una orden con PUT y el vendedor elegido', async () => {
    const { fetchMock, llamadas } = prepararApiComisiones();
    await montarComisionesCargadas(fetchMock, '/modulos/08-comisiones-vendedores?vista=asignacion', 'asignacion');
    const selectorOrden = screen.getByLabelText('Vendedor para OC-2026-000102');
    fireEvent.change(selectorOrden, { target: { value: '2' } });
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url.endsWith('/ordenes/102/vendedor') && llamada.init.method === 'PUT')).toBe(true));
    const llamada = llamadas.find((elemento) => elemento.url.endsWith('/ordenes/102/vendedor') && elemento.init.method === 'PUT');
    expect(llamada?.init.body).toBe('{"vendedorId":2}');
    expect(fetchMock).toHaveBeenCalled();
  });

  it('A4 quita una asignación con DELETE', async () => {
    const { llamadas, fetchMock } = prepararApiComisiones();
    await montarComisionesCargadas(fetchMock, '/modulos/08-comisiones-vendedores?vista=asignacion', 'asignacion');
    fireEvent.click(screen.getAllByRole('button', { name: 'Quitar' })[0]!);
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url.endsWith('/ordenes/101/vendedor') && llamada.init.method === 'DELETE')).toBe(true));
  });

  it('A4 conserva la tabla y muestra el 409 de mes liquidado junto a la orden', async () => {
    const { fetchMock } = prepararApiComisiones({ asignacionMesLiquidado: true });
    await montarComisionesCargadas(fetchMock, '/modulos/08-comisiones-vendedores?vista=asignacion', 'asignacion');
    fireEvent.change(screen.getByLabelText('Vendedor para OC-2026-000102'), { target: { value: '1' } });
    expect(await screen.findByText('El mes 01/2026 ya está liquidado; no se pueden cambiar sus asignaciones.')).toBeInTheDocument();
    expect(screen.getByText('OC-2026-000101')).toBeInTheDocument();
  });

  it('A5 muestra totales exactos y no ofrece liquidar un mes ya liquidado', async () => {
    const { fetchMock, llamadas } = prepararApiComisiones();
    await montarComisionesCargadas(fetchMock, '/modulos/08-comisiones-vendedores?vista=comisiones', 'comisiones');
    expect(screen.getByText('S/ 20,000.00')).toBeInTheDocument();
    expect(screen.getByText('S/ 800.00')).toBeInTheDocument();
    expect(screen.getByText('Mes liquidado: los importes ya no cambian.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Liquidar mes' })).not.toBeInTheDocument();
    expect(llamadas[0]?.url).toBe('/api/v1/comisiones-vendedores/comisiones?anio=2026&mes=1');
  });

  it('A5 confirma la liquidación del periodo y muestra el 409 literal en el diálogo', async () => {
    const { fetchMock } = prepararApiComisiones({ liquidacionDuplicada: true });
    await montarComisionesCargadas(fetchMock, '/modulos/08-comisiones-vendedores?vista=comisiones', 'comisiones');
    fireEvent.change(screen.getByLabelText('Mes'), { target: { value: '3' } });
    await screen.findByRole('button', { name: 'Liquidar mes' });
    fireEvent.click(screen.getByRole('button', { name: 'Liquidar mes' }));
    const dialogo = screen.getByRole('dialog');
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Confirmar liquidación' }));
    expect(await within(dialogo).findByText('El mes 03/2026 ya está liquidado.')).toBeInTheDocument();
  });

  it('A5 liquida mes pendiente con confirmación y actualiza el estado', async () => {
    const { fetchMock, llamadas } = prepararApiComisiones();
    await montarComisionesCargadas(fetchMock, '/modulos/08-comisiones-vendedores?vista=comisiones', 'comisiones');
    fireEvent.change(screen.getByLabelText('Mes'), { target: { value: '3' } });
    await screen.findByRole('button', { name: 'Liquidar mes' });
    fireEvent.click(screen.getByRole('button', { name: 'Liquidar mes' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar liquidación' }));
    expect(await screen.findByText('Mes liquidado: los importes ya no cambian.')).toBeInTheDocument();
    expect(llamadas.some((llamada) => llamada.url.endsWith('/liquidaciones') && llamada.init.method === 'POST' && llamada.init.body === '{"anio":2026,"mes":3}')).toBe(true);
  });

  it('A5 confirma el pago con POST sin cuerpo y refresca el periodo', async () => {
    const { fetchMock, llamadas } = prepararApiComisiones();
    await montarComisionesCargadas(fetchMock, '/modulos/08-comisiones-vendedores?vista=comisiones', 'comisiones');
    fireEvent.change(screen.getByLabelText('Mes'), { target: { value: '2' } });
    fireEvent.click(await screen.findByRole('button', { name: 'Marcar pagada' }));
    const dialogo = screen.getByRole('dialog');
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Marcar pagada' }));
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url.endsWith('/liquidaciones/21/pagar') && llamada.init.method === 'POST')).toBe(true));
    const llamadaPago = llamadas.find((llamada) => llamada.url.endsWith('/liquidaciones/21/pagar'));
    expect(llamadaPago?.init.body).toBeUndefined();
    await waitFor(() => expect(llamadas.filter((llamada) => llamada.url.includes('/comisiones?')).length).toBe(3));
  });

  it('A5 conserva el diálogo de pago y muestra el 409 literal del backend', async () => {
    const { fetchMock } = prepararApiComisiones({ pagoDuplicado: true });
    await montarComisionesCargadas(fetchMock, '/modulos/08-comisiones-vendedores?vista=comisiones', 'comisiones');
    fireEvent.change(screen.getByLabelText('Mes'), { target: { value: '2' } });
    fireEvent.click(await screen.findByRole('button', { name: 'Marcar pagada' }));
    const dialogo = screen.getByRole('dialog');
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Marcar pagada' }));
    expect(await within(dialogo).findByText('La liquidación 21 ya está pagada.')).toBeInTheDocument();
  });
});
