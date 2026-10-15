import { afterEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { alertasMock, esperarCargaInicial, limpiarAlertas, montarAlertasCargadas, prepararApiAlertas, renderAlertas } from './alertas.testUtils';

afterEach(limpiarAlertas);

describe('centro de notificaciones y alertas', () => {
  it('A3 carga una lista, muestra las tres severidades y el enlace correcto', async () => {
    const { fetchMock } = prepararApiAlertas();
    await montarAlertasCargadas(fetchMock);
    const tabla = screen.getByRole('table', { name: 'Centro de alertas' });
    expect(within(tabla).getAllByText('Alta')).toHaveLength(3);
    expect(within(tabla).getAllByText('Media')).toHaveLength(2);
    expect(within(tabla).getAllByText('Baja')).toHaveLength(2);
    fireEvent.click(screen.getByRole('button', { name: 'Ver detalle: Reclamo vencido' }));
    expect(screen.getByRole('heading', { name: 'Reclamo vencido' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Ver registro relacionado' })).toHaveAttribute('href', '/modulos/09-reclamos-postventa?reclamoId=3');
  });

  it('A3 refleja filtros en URL y consulta, y vuelve a página 1', async () => {
    const { fetchMock, llamadas } = prepararApiAlertas();
    await montarAlertasCargadas(fetchMock, '/modulos/78-notificaciones-alertas?pagina=2');
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'ContratoExcedido' } });
    fireEvent.change(screen.getByLabelText('Severidad'), { target: { value: 'alta' } });
    expect(screen.getByLabelText('Tipo')).toHaveValue('ContratoExcedido');
    expect(screen.getByLabelText('Severidad')).toHaveValue('alta');
    expect(screen.getByTestId('ruta-actual')).toHaveTextContent('tipo=ContratoExcedido');
    expect(screen.getByTestId('ruta-actual')).toHaveTextContent('severidad=alta');
    await waitFor(() => {
      expect(llamadas.some((llamada) => llamada.url.includes('tipo=ContratoExcedido') && llamada.url.includes('severidad=alta') && llamada.url.includes('pagina=1'))).toBe(true);
    });
    fireEvent.click(screen.getByLabelText('Mostrar descartadas'));
    await waitFor(() => {
      expect(llamadas.some((llamada) => llamada.url.includes('incluirDescartadas=true') && llamada.url.includes('tipo=ContratoExcedido'))).toBe(true);
    });
  });

  it('A3 muestra estados vacío y error del listado', async () => {
    const vacia = prepararApiAlertas({ listaVacia: true });
    vi.useFakeTimers();
    renderAlertas();
    await vi.advanceTimersByTimeAsync(1);
    vi.useRealTimers();
    await esperarCargaInicial(vacia.fetchMock);
    expect(screen.getByText('No hay alertas pendientes.')).toBeInTheDocument();
    limpiarAlertas();
    const error = prepararApiAlertas({ errorLista: true });
    vi.useFakeTimers();
    renderAlertas();
    await vi.advanceTimersByTimeAsync(1);
    vi.useRealTimers();
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('No se pudo cargar el listado de alertas.'));
    expect(error.fetchMock.mock.calls.filter((llamada) => String(llamada[0]).includes('/alertas?'))).toHaveLength(1);
  });

  it('A4 confirma descarte y refresca el listado y el resumen', async () => {
    const { fetchMock, llamadas } = prepararApiAlertas();
    await montarAlertasCargadas(fetchMock);
    fireEvent.click(screen.getAllByRole('button', { name: 'Descartar' })[0]);
    const dialogo = within(screen.getByRole('dialog', { name: 'Descartar alerta' }));
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar descartar' }));
    await waitFor(() => {
      expect(llamadas.some((llamada) => llamada.url.endsWith('/alertas/descartar') && llamada.init.method === 'POST')).toBe(true);
      expect(llamadas.filter((llamada) => llamada.url.includes('/alertas?'))).toHaveLength(2);
      expect(llamadas.filter((llamada) => llamada.url.endsWith('/alertas/resumen'))).toHaveLength(2);
    });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('A4 restaura alertas descartadas y conserva errores 404/409 en el diálogo', async () => {
    const restauracion = prepararApiAlertas();
    await montarAlertasCargadas(restauracion.fetchMock);
    fireEvent.click(screen.getByLabelText('Mostrar descartadas'));
    await screen.findByRole('button', { name: 'Restaurar' });
    fireEvent.click(screen.getByRole('button', { name: 'Restaurar' }));
    const dialogo = within(screen.getByRole('dialog', { name: 'Restaurar alerta' }));
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar restaurar' }));
    await waitFor(() => expect(restauracion.llamadas.some((llamada) => llamada.url.endsWith('/alertas/restaurar'))).toBe(true));
    limpiarAlertas();
    const error = prepararApiAlertas({ errorDescarte: 409 });
    await montarAlertasCargadas(error.fetchMock);
    fireEvent.click(screen.getAllByRole('button', { name: 'Descartar' })[0]);
    const dialogoError = within(screen.getByRole('dialog'));
    fireEvent.click(dialogoError.getByRole('button', { name: 'Confirmar descartar' }));
    expect(await dialogoError.findByRole('alert')).toHaveTextContent('La alerta reclamo-vencido:3 ya está descartada.');
    expect(dialogoError.getByText(alertasMock[0].mensaje)).toBeInTheDocument();
    limpiarAlertas();
    const noExiste = prepararApiAlertas({ errorDescarte: 404 });
    await montarAlertasCargadas(noExiste.fetchMock);
    fireEvent.click(screen.getAllByRole('button', { name: 'Descartar' })[0]);
    const dialogo404 = within(screen.getByRole('dialog'));
    fireEvent.click(dialogo404.getByRole('button', { name: 'Confirmar descartar' }));
    expect(await dialogo404.findByRole('alert')).toHaveTextContent('La alerta reclamo-vencido:3 no existe o ya no está activa.');
    limpiarAlertas();
    const restaurarNoExiste = prepararApiAlertas({ errorRestauracion: 404 });
    await montarAlertasCargadas(restaurarNoExiste.fetchMock);
    fireEvent.click(screen.getByLabelText('Mostrar descartadas'));
    await screen.findByRole('button', { name: 'Restaurar' });
    fireEvent.click(screen.getByRole('button', { name: 'Restaurar' }));
    const dialogoRestaurar404 = within(screen.getByRole('dialog'));
    fireEvent.click(dialogoRestaurar404.getByRole('button', { name: 'Confirmar restaurar' }));
    expect(await dialogoRestaurar404.findByRole('alert')).toHaveTextContent('La alerta reclamo-vencido:8 no está descartada.');
  });

  it('A4 Escape cancela la confirmación y devuelve el foco al botón', async () => {
    const { fetchMock, llamadas } = prepararApiAlertas();
    await montarAlertasCargadas(fetchMock);
    const boton = screen.getAllByRole('button', { name: 'Descartar' })[0];
    fireEvent.click(boton);
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(boton).toHaveFocus();
    expect(llamadas.some((llamada) => llamada.url.endsWith('/alertas/descartar'))).toBe(false);
  });
});
