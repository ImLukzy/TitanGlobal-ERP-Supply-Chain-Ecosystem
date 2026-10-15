import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { esperarCargaInicial, prepararApi, renderPagina } from './solicitudes.testUtils';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

async function seleccionarPendiente(): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: 'SC-000001' }));
  await screen.findByText('Entrega para la planta principal');
}

describe('SolicitudesCompra: decisiones y baja', () => {
  it('A5 aprueba con POST y refresca listado, detalle y resumen', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    await seleccionarPendiente();
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar' }));
    const dialogo = screen.getByRole('dialog', { name: 'Aprobar SC-000001' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Confirmar aprobación' }));
    await waitFor(() => {
      expect(fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/solicitudes-compra/1/aprobar'
        && (call[1] as RequestInit | undefined)?.method === 'POST')).toBe(true);
    });
    expect(await screen.findByRole('heading', { name: 'SC-000001' })).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]) === '/api/v1/solicitudes-compra/resumen').length).toBeGreaterThan(0);
    expect(fetchMock.mock.calls.filter((call) => String(call[0]) === '/api/v1/solicitudes-compra/1').length).toBeGreaterThan(1);
  });

  it('A5 valida motivo de rechazo y envía { motivo }', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    await seleccionarPendiente();
    fireEvent.click(screen.getByRole('button', { name: 'Rechazar' }));
    const dialogo = screen.getByRole('dialog', { name: 'Rechazar SC-000001' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Confirmar rechazo' }));
    expect(screen.getByText('El motivo de rechazo es obligatorio.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/1/rechazar'))).toBe(false);
    fireEvent.change(screen.getByLabelText(/Motivo de rechazo/), { target: { value: 'M'.repeat(501) } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Confirmar rechazo' }));
    expect(screen.getByText('El motivo de rechazo no puede superar 500 caracteres.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/1/rechazar'))).toBe(false);
    fireEvent.change(screen.getByLabelText(/Motivo de rechazo/), { target: { value: 'Presupuesto agotado' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Confirmar rechazo' }));
    await waitFor(() => {
      const llamada = fetchMock.mock.calls.find((call) => String(call[0]) === '/api/v1/solicitudes-compra/1/rechazar');
      expect(llamada).toBeDefined();
      expect(JSON.parse(String((llamada?.[1] as RequestInit).body))).toEqual({ motivo: 'Presupuesto agotado' });
    });
  });

  it('A5 muestra el error 409 dentro del diálogo de aprobación', async () => {
    const { fetchMock } = prepararApi({ errorDecidir: true });
    renderPagina();
    await esperarCargaInicial(fetchMock);
    await seleccionarPendiente();
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar' }));
    const dialogo = screen.getByRole('dialog', { name: 'Aprobar SC-000001' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Confirmar aprobación' }));
    expect(await screen.findByText('La solicitud SC-000001 ya está aprobada.')).toBeInTheDocument();
  });

  it('A5 deja solo lectura una solicitud aprobada', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'SC-000003' }));
    const titulo = await screen.findByRole('heading', { name: 'SC-000003' });
    const panel = titulo.closest('section');
    expect(panel).not.toBeNull();
    expect(within(panel as HTMLElement).queryByRole('button', { name: 'Aprobar' })).not.toBeInTheDocument();
    expect(within(panel as HTMLElement).queryByRole('button', { name: 'Eliminar' })).not.toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalled();
  });

  it('A5 Escape cierra el diálogo de rechazo y devuelve el foco', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    await seleccionarPendiente();
    const abrirRechazo = screen.getByRole('button', { name: 'Rechazar' });
    fireEvent.click(abrirRechazo);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await act(async () => {
      await new Promise((resolver) => window.requestAnimationFrame(resolver));
    });
    expect(document.activeElement).toBe(abrirRechazo);
    expect(fetchMock.mock.calls.some((call) => String(call[0]).includes('/aprobar') || String(call[0]).includes('/rechazar'))).toBe(false);
  });

  it('A5 muestra el 409 en el diálogo de baja y refresca tras eliminar correctamente', async () => {
    const conflict = prepararApi({ errorBaja: true });
    renderPagina();
    await esperarCargaInicial(conflict.fetchMock);
    await seleccionarPendiente();
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    const dialogo = screen.getByRole('dialog', { name: 'Eliminar solicitud' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Eliminar solicitud' }));
    expect(await within(dialogo).findByText('La solicitud SC-000001 ya está aprobada.')).toBeInTheDocument();
    cleanup();
    vi.unstubAllGlobals();
    const correcto = prepararApi();
    renderPagina();
    await esperarCargaInicial(correcto.fetchMock);
    await seleccionarPendiente();
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar solicitud' }));
    await waitFor(() => {
      expect(correcto.fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/solicitudes-compra/1'
        && (call[1] as RequestInit | undefined)?.method === 'DELETE')).toBe(true);
    });
    await waitFor(() => {
      expect(correcto.fetchMock.mock.calls.filter((call) => String(call[0]) === '/api/v1/solicitudes-compra/resumen').length).toBeGreaterThan(0);
    });
  });
});
