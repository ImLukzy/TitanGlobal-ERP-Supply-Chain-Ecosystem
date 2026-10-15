import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { montar, prepararFetch } from './cotizaciones.testUtils';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

describe('acciones de cotizaciones', () => {
  it('A5 confirma aceptación y muestra el enlace a la orden generada', async () => {
    const fetchMock = prepararFetch();
    montar();
    fireEvent.click(await screen.findByRole('button', { name: 'COT-DEV-001' }));
    expect(
      await screen.findByRole('heading', { name: 'COT-DEV-001' }),
    ).toBeInTheDocument();
    fireEvent.click(await screen.findByRole('button', { name: 'Aceptar' }));
    expect(
      await screen.findByText('Se generará la orden de compra OC-COT-DEV-001.'),
    ).toBeInTheDocument();
    fireEvent.click(
      screen.getByRole('button', { name: 'Confirmar aceptación' }),
    );
    await waitFor(() =>
      expect(
        fetchMock.mock.calls.some(
          (call) =>
            String(call[0]).endsWith('/cotizaciones/1/aceptar') &&
            call[1]?.method === 'POST' &&
            !('body' in (call[1] ?? {})),
        ),
      ).toBe(true),
    );
    expect(
      await screen.findByRole('link', { name: 'Ver orden OC-COT-000001' }),
    ).toHaveAttribute('href', '/modulos/04-ordenes-compra');
  });

  it('A5 exige motivo local, muestra 409 y conserva el motivo escrito', async () => {
    const fetchMock = prepararFetch();
    montar();
    fireEvent.click(await screen.findByRole('button', { name: 'COT-DEV-001' }));
    expect(
      await screen.findByRole('heading', { name: 'COT-DEV-001' }),
    ).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Rechazar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar rechazo' }));
    expect(
      screen.getByText('El motivo de rechazo es obligatorio.'),
    ).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.some((call) =>
        String(call[0]).endsWith('/rechazar'),
      ),
    ).toBe(false);
    fireEvent.change(
      screen.getByLabelText('Motivo de rechazo', { exact: false }),
      { target: { value: 'El cliente eligió otra alternativa.' } },
    );
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar rechazo' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'La cotización COT-DEV-001 ya está aceptada.',
    );
    expect(
      screen.getByLabelText('Motivo de rechazo', { exact: false }),
    ).toHaveValue('El cliente eligió otra alternativa.');
  });

  it('A5 Escape cierra aceptación y devuelve el foco al botón', async () => {
    prepararFetch();
    montar();
    const botonAceptar = await screen.findByRole('button', {
      name: 'COT-DEV-001',
    });
    fireEvent.click(botonAceptar);
    expect(
      await screen.findByRole('heading', { name: 'COT-DEV-001' }),
    ).toBeInTheDocument();
    const accionAceptar = screen.getByRole('button', { name: 'Aceptar' });
    fireEvent.click(accionAceptar);
    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() =>
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument(),
    );
    await waitFor(() => expect(document.activeElement).toBe(accionAceptar));
  });

  it('A5 una cotización decidida no presenta acciones de edición ni decisión', async () => {
    prepararFetch();
    montar();
    fireEvent.click(await screen.findByRole('button', { name: 'COT-DEV-004' }));
    expect(
      await screen.findByRole('heading', { name: 'COT-DEV-004' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Editar' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Aceptar' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Rechazar' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Eliminar' }),
    ).not.toBeInTheDocument();
  });
});
