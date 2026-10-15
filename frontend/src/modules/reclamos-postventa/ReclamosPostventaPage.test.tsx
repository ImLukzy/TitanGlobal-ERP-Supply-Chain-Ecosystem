import { act, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { limpiar, prepararApi, renderPagina, esperarCargaInicial, reclamosMock } from './reclamos.testUtils';

describe('bandeja de reclamos postventa', () => {
  afterEach(limpiar);

  it('carga una sola bandeja y presenta vencido, prioridad y resumen textual', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    expect(screen.getByText('Vencido')).toBeInTheDocument();
    expect(screen.getAllByText('Alta').length).toBeGreaterThan(1);
    expect(screen.getByText('80.00 %')).toBeInTheDocument();
    expect(screen.getByText('atendidos a tiempo')).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/reclamos-postventa?'))).toHaveLength(1);
  });

  it('aplica filtros y búsqueda con temporizadores falsos y restablece página', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.change(screen.getByRole('combobox', { name: 'Tipo' }), { target: { value: 'Defecto' } });
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]).includes('tipo=Defecto'))).toBe(true));
    fetchMock.mockClear();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Solo vencidos' }));
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]).includes('vencidos=true'))).toBe(true));
    fetchMock.mockClear();
    vi.useFakeTimers();
    fireEvent.change(screen.getByRole('textbox', { name: 'Buscar' }), { target: { value: 'REC-DEV' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(300); });
    vi.useRealTimers();
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]).includes('busqueda=REC-DEV'))).toBe(true));
    const url = String(fetchMock.mock.calls.find((call) => String(call[0]).includes('/reclamos-postventa?'))?.[0]);
    expect(url).toContain('pagina=1');
  });

  it('muestra estados vacío y error de listado', async () => {
    const vacia = prepararApi({ vacia: true });
    renderPagina();
    await screen.findByText('No hay reclamos que coincidan con los filtros.');
    await screen.findByText('Reclamos por tipo');
    await waitFor(() => expect(vacia.fetchMock.mock.calls.filter((call) => String(call[0]).includes('/reclamos-postventa?'))).toHaveLength(1));
    expect(screen.getByText('No hay reclamos que coincidan con los filtros.')).toBeInTheDocument();
    limpiar();
    const fallida = prepararApi({ errorLista: true });
    renderPagina();
    await waitFor(() => expect(fallida.fetchMock.mock.calls.filter((call) => String(call[0]).includes('/reclamos-postventa?'))).toHaveLength(1));
    expect(screen.getByText('Falló la bandeja.')).toBeInTheDocument();
    expect(fallida.fetchMock).toHaveBeenCalled();
  });

  it('muestra ficha, línea de tiempo accesible y resultado de plazo', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'REC-DEV-003' }));
    expect(await screen.findByText('Atendido a tiempo')).toBeInTheDocument();
    expect(screen.getByLabelText('Seguimiento del reclamo').querySelector('ol')).toBeInTheDocument();
    expect(screen.getByText('Se coordinó una nueva entrega.')).toBeInTheDocument();
  });

  it('presenta las acciones permitidas según estado del reclamo', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'REC-DEV-001' }));
    await screen.findByText('La laptop presenta una falla de encendido.');
    expect(screen.getByRole('button', { name: 'Editar' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Iniciar' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Resolver' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Rechazar' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'REC-DEV-002' }));
    await screen.findByText('Falta un accesorio en el envío.');
    expect(screen.queryByRole('button', { name: 'Iniciar' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Resolver' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'REC-DEV-003' }));
    await screen.findByText('Se coordinó una nueva entrega.');
    expect(screen.getByRole('button', { name: 'Cerrar' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'REC-DEV-004' }));
    await screen.findByText('Factura corregida.');
    expect(screen.queryByRole('button', { name: 'Cerrar' })).not.toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/reclamos-postventa?'))).toHaveLength(1);
  });

  it('exige resolución antes de enviarla y Escape devuelve el foco al disparador', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'REC-DEV-002' }));
    await screen.findByText('Falta un accesorio en el envío.');
    const resolver = screen.getByRole('button', { name: 'Resolver' });
    fireEvent.click(resolver);
    const dialogo = screen.getByRole('dialog');
    fireEvent.click(dialogo.querySelector('button.primary-button') as HTMLButtonElement);
    expect(screen.getByText('La resolución es obligatoria.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/reclamos-postventa/2/resolver'))).toBe(false);
    fireEvent.keyDown(dialogo, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(document.activeElement).toBe(resolver));
  });

  it('limita órdenes y productos en el formulario, cambia el plazo y conserva errores API', async () => {
    const { fetchMock } = prepararApi({ errorCrear: true });
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo reclamo' }));
    fireEvent.change(screen.getByLabelText('Cliente', { selector: '#reclamo-cliente' }), { target: { value: '1' } });
    expect(await screen.findByRole('option', { name: 'OC-001' })).toBeInTheDocument();
    fireEvent.change(screen.getByRole('combobox', { name: 'Orden de compra' }), { target: { value: '1' } });
    expect(screen.getByRole('option', { name: 'Laptop Pro' })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Prioridad', { selector: '#reclamo-prioridad' }), { target: { value: 'Alta' } });
    expect(screen.getByText('Plazo: 2 días')).toBeInTheDocument();
    fireEvent.change(screen.getByRole('textbox', { name: 'Descripción' }), { target: { value: 'Producto defectuoso' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar reclamo' }));
    expect(await screen.findByText('El cliente 9999 no existe.')).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Descripción' })).toHaveValue('Producto defectuoso');
    expect(reclamosMock).toHaveLength(5);
  });

  it('valida campos localmente y confirma una acción mostrando el conflicto 409', async () => {
    const { fetchMock } = prepararApi({ errorAccion: true });
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo reclamo' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar reclamo' }));
    expect(screen.getByText('Selecciona un cliente.')).toBeInTheDocument();
    expect(screen.getByText('Selecciona una orden de compra.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'POST')).toBe(false);
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar' }));
    fireEvent.click(screen.getByRole('button', { name: 'REC-DEV-001' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Iniciar' }));
    fireEvent.click(screen.getByRole('dialog').querySelector('button.primary-button') as HTMLButtonElement);
    expect(await screen.findByText('El reclamo REC-DEV-001 está cerrado.')).toBeInTheDocument();
  });
});
