import { afterEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import {
  esperarCargaInicial,
  limpiarDevoluciones,
  prepararApiDevoluciones,
  renderDevoluciones,
} from './devoluciones.testUtils';

afterEach(limpiarDevoluciones);

async function montarCargado(
  fetchMock: ReturnType<typeof prepararApiDevoluciones>['fetchMock'],
): Promise<void> {
  vi.useFakeTimers();
  renderDevoluciones();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock);
}

describe('Devoluciones comerciales: bandeja, formulario y acciones', () => {
  it('A3 carga resumen y listado una sola vez, con estados legibles', async () => {
    const { fetchMock, llamadas } = prepararApiDevoluciones();
    await montarCargado(fetchMock);
    const solicitudesListado = llamadas.filter((item) => (
      item.url.includes('/devoluciones-comerciales?')
    ));
    expect(solicitudesListado).toHaveLength(1);
    expect(screen.getByText('DEV-DEV-001')).toBeInTheDocument();
    expect(screen.getAllByText('Recibida')).toHaveLength(2);
    expect(screen.getByText('Tasa de devolución')).toBeInTheDocument();
    expect(screen.getByText('10.00 % de las unidades vendidas')).toBeInTheDocument();
  });

  it('A3 filtra por búsqueda, estado, motivo y cliente regresando a página 1', async () => {
    const { fetchMock, llamadas } = prepararApiDevoluciones();
    await montarCargado(fetchMock);
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Solicitada' } });
    fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Defecto' } });
    fireEvent.change(screen.getByLabelText('Cliente'), { target: { value: '1' } });
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText('Buscar'), { target: { value: 'Acme' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(301);
    });
    vi.useRealTimers();
    await waitFor(() => {
      const filtrada = llamadas.some((item) => (
        item.url.includes('busqueda=Acme')
        && item.url.includes('estado=Solicitada')
        && item.url.includes('motivo=Defecto')
        && item.url.includes('clienteId=1')
        && item.url.includes('pagina=1')
      ));
      expect(filtrada).toBe(true);
    });
  });

  it('A3 muestra estados vacío y error del listado', async () => {
    const vacio = prepararApiDevoluciones({ listaVacia: true });
    renderDevoluciones();
    expect(await screen.findByText('No hay devoluciones que coincidan con los filtros.')).toBeInTheDocument();
    limpiarDevoluciones();
    const error = prepararApiDevoluciones({ errorLista: true });
    renderDevoluciones();
    expect(await screen.findByText('Falló el listado de devoluciones.')).toBeInTheDocument();
    expect(vacio.fetchMock).toHaveBeenCalled();
    expect(error.fetchMock).toHaveBeenCalled();
  });

  it('A4 carga órdenes y reclamos asociados, disponible e importe en vivo; excederlo no envía', async () => {
    const { fetchMock, llamadas } = prepararApiDevoluciones();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva devolución' }));
    const dialogo = within(screen.getByRole('dialog'));
    fireEvent.change(dialogo.getByLabelText('Cliente'), { target: { value: '1' } });
    expect(await dialogo.findByRole('option', { name: 'OC-001' })).toBeInTheDocument();
    fireEvent.change(dialogo.getByLabelText('Orden de compra'), { target: { value: '1' } });
    expect(await dialogo.findByRole('option', { name: 'REC-DEV-001' })).toBeInTheDocument();
    expect(await dialogo.findByText('Laptop Pro')).toBeInTheDocument();
    fireEvent.change(dialogo.getByLabelText('Cantidad de Mouse Óptico'), { target: { value: '1' } });
    expect(dialogo.getAllByText('S/ 50.00').length).toBeGreaterThan(0);
    fireEvent.change(dialogo.getByLabelText('Cantidad de Mouse Óptico'), { target: { value: '3' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar devolución' }));
    expect(await dialogo.findByText(/Solo quedan 2 unidades/)).toBeInTheDocument();
    const creaciones = llamadas.filter((item) => (
      item.init.method === 'POST' && item.url.endsWith('/devoluciones-comerciales')
    ));
    expect(creaciones).toHaveLength(0);
    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/devoluciones-comerciales/ordenes/1/disponible'), expect.anything());
  });

  it('A4 muestra el 409 literal de la API y conserva el formulario', async () => {
    const { llamadas, fetchMock } = prepararApiDevoluciones({ errorCrear: true });
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva devolución' }));
    const dialogo = within(screen.getByRole('dialog'));
    fireEvent.change(dialogo.getByLabelText('Cliente'), { target: { value: '1' } });
    fireEvent.change(await dialogo.findByLabelText('Orden de compra'), { target: { value: '1' } });
    await dialogo.findByText('Mouse Óptico');
    fireEvent.change(dialogo.getByLabelText('Cantidad de Mouse Óptico'), { target: { value: '1' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar devolución' }));
    const mensajeApi = 'Solo quedan 2 unidades de Mouse Óptico por devolver en la orden OC-001.';
    expect(await dialogo.findByText(mensajeApi)).toBeInTheDocument();
    expect(dialogo.getByLabelText('Cliente')).toHaveValue('1');
    expect(dialogo.getByLabelText('Cantidad de Mouse Óptico')).toHaveValue(1);
    const solicitudCrear = llamadas.some((item) => (
      item.url.endsWith('/devoluciones-comerciales') && item.init.method === 'POST'
    ));
    expect(solicitudCrear).toBe(true);
  });

  it('A4 valida cliente, orden, líneas y observaciones localmente sin crear', async () => {
    const { fetchMock, llamadas } = prepararApiDevoluciones();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva devolución' }));
    const dialogo = within(screen.getByRole('dialog'));
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar devolución' }));
    expect(dialogo.getByText('Selecciona un cliente.')).toBeInTheDocument();
    expect(dialogo.getByText('Selecciona una orden de compra.')).toBeInTheDocument();
    expect(dialogo.getByText('La devolución debe tener al menos una línea.')).toBeInTheDocument();
    fireEvent.change(dialogo.getByLabelText('Cliente'), { target: { value: '2' } });
    fireEvent.change(await dialogo.findByLabelText('Orden de compra'), { target: { value: '5' } });
    await dialogo.findByLabelText('Cantidad de Mouse Óptico');
    fireEvent.change(dialogo.getByLabelText('Observaciones'), { target: { value: 'X'.repeat(501) } });
    fireEvent.change(dialogo.getByLabelText('Cantidad de Mouse Óptico'), { target: { value: '1' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar devolución' }));
    expect(dialogo.getByText('Las observaciones no pueden superar 500 caracteres.')).toBeInTheDocument();
    const creaciones = llamadas.filter((item) => (
      item.url.endsWith('/devoluciones-comerciales') && item.init.method === 'POST'
    ));
    expect(creaciones).toHaveLength(0);
  });

  it('A4 edita una devolución solicitada con PUT', async () => {
    const { fetchMock, llamadas } = prepararApiDevoluciones();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'DEV-DEV-003' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Editar' }));
    const dialogo = within(screen.getByRole('dialog'));
    await dialogo.findByText('Mouse Óptico');
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar devolución' }));
    await waitFor(() => {
      const actualizada = llamadas.some((item) => (
        item.init.method === 'PUT' && item.url.endsWith('/devoluciones-comerciales/3')
      ));
      expect(actualizada).toBe(true);
    });
  });

  it('A5 ofrece las acciones según estado y aprobar y recibir van sin cuerpo', async () => {
    const { fetchMock, llamadas } = prepararApiDevoluciones();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'DEV-DEV-003' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Aprobar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar aprobar' }));
    await waitFor(() => {
      const aprobacionSinCuerpo = llamadas.some((item) => (
        item.url.endsWith('/devoluciones-comerciales/3/aprobar')
        && item.init.body === undefined
      ));
      expect(aprobacionSinCuerpo).toBe(true);
    });
    expect(await screen.findByRole('button', { name: 'Marcar recibida' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Marcar recibida' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar recibir' }));
    await waitFor(() => {
      const recepcionSinCuerpo = llamadas.some((item) => (
        item.url.endsWith('/devoluciones-comerciales/3/recibir')
        && item.init.body === undefined
      ));
      expect(recepcionSinCuerpo).toBe(true);
    });
    expect(screen.getAllByText('Recibida').length).toBeGreaterThan(0);
    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'DEV-DEV-004' }));
    await screen.findByText(/Fuera del plazo de devolución/);
    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Rechazar' })).not.toBeInTheDocument();
  });

  it('A5 exige motivo al rechazar y Escape cancela sin petición y devuelve el foco', async () => {
    const { fetchMock, llamadas } = prepararApiDevoluciones();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'DEV-DEV-003' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Rechazar' }));
    const dialogo = within(screen.getByRole('dialog'));
    const antes = llamadas.length;
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar rechazar' }));
    expect(await dialogo.findByText('El motivo de rechazo es obligatorio.')).toBeInTheDocument();
    expect(llamadas).toHaveLength(antes);
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: 'Rechazar' })).toHaveFocus();
    expect(llamadas).toHaveLength(antes);
  });

  it('A5 muestra los conflictos de transición en el diálogo', async () => {
    const { fetchMock } = prepararApiDevoluciones({ errorAccion: true });
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'DEV-DEV-002' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Marcar recibida' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar recibir' }));
    expect(await screen.findByText('La devolución DEV-DEV-002 está aprobada.')).toBeInTheDocument();
  });

  it('A5 elimina una devolución solicitada y refresca el listado', async () => {
    const { fetchMock, llamadas } = prepararApiDevoluciones();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'DEV-DEV-003' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Eliminar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar devolución' }));
    await waitFor(() => {
      const eliminada = llamadas.some((item) => (
        item.init.method === 'DELETE' && item.url.endsWith('/devoluciones-comerciales/3')
      ));
      expect(eliminada).toBe(true);
    });
  });

  it('A5 rechaza con motivo obligatorio y lo envía en el cuerpo literal', async () => {
    const { fetchMock, llamadas } = prepararApiDevoluciones();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'DEV-DEV-003' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Rechazar' }));
    fireEvent.change(screen.getByLabelText('Motivo del rechazo'), {
      target: { value: 'Producto sin defecto' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar rechazar' }));
    await waitFor(() => {
      const rechazo = llamadas.find((item) => (
        item.url.endsWith('/devoluciones-comerciales/3/rechazar')
      ));
      expect(rechazo?.init.body).toBe('{"motivo":"Producto sin defecto"}');
    });
  });
});
