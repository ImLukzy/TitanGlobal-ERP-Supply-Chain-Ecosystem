import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { setSelectedRole } from '../../api/roleStore';
import { NuevaOrdenForm } from './NuevaOrdenForm';

afterEach(() => {
  cleanup();
  setSelectedRole(null);
  vi.unstubAllGlobals();
});

describe('formulario Nueva orden', () => {
  it('valida número obligatorio, cliente positivo y total no negativo sin llamar la API', () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    const { container } = render(<NuevaOrdenForm />);
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Cliente ID' }), { target: { value: '0' } });
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Total' }), { target: { value: '-1' } });
    const form = container.querySelector('form');
    if (form === null) {
      throw new Error('No se encontró el formulario.');
    }
    fireEvent.submit(form);

    expect(screen.getByText('El número de orden es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El cliente debe ser un entero mayor que 0.')).toBeInTheDocument();
    expect(screen.getByText('El total debe ser un número mayor o igual a 0.')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('crea la orden, anuncia el id y limpia todos los campos', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: 7 }), { status: 201 }));
    vi.stubGlobal('fetch', fetchMock);
    render(<NuevaOrdenForm />);
    fireEvent.change(screen.getByRole('textbox', { name: 'Número de orden' }), { target: { value: 'OC-007' } });
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Cliente ID' }), { target: { value: '1' } });
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Total' }), { target: { value: '2500' } });
    fireEvent.change(screen.getByLabelText(/Descripción/), { target: { value: 'Compra de prueba' } });
    fireEvent.click(screen.getByRole('button', { name: 'Crear orden' }));

    expect(await screen.findByText('Orden creada con Id 7')).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Número de orden' })).toHaveValue('');
    expect(screen.getByRole('spinbutton', { name: 'Cliente ID' })).toHaveValue(null);
    expect(screen.getByRole('spinbutton', { name: 'Total' })).toHaveValue(null);
    expect(screen.getByLabelText(/Descripción/)).toHaveValue('');
    expect(fetchMock.mock.calls[0]?.[1]?.body).toBe(JSON.stringify({
      numeroOrden: 'OC-007',
      clienteId: 1,
      total: 2500,
      descripcion: 'Compra de prueba',
    }));
  });

  it.each([
    [409, 'Ya existe una orden de compra con el número OC-001.'],
    [400, 'El cliente 9999 no existe.'],
    [403, 'Error 403 del API'],
  ])('muestra el Message del API en una respuesta %i y conserva el formulario', async (statusCode, message) => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ StatusCode: statusCode, Message: message }), { status: statusCode })));
    render(<NuevaOrdenForm />);
    fireEvent.change(screen.getByRole('textbox', { name: 'Número de orden' }), { target: { value: statusCode === 409 ? 'OC-001' : 'OC-008' } });
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Cliente ID' }), { target: { value: statusCode === 400 ? '9999' : '1' } });
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Total' }), { target: { value: '50' } });
    const button = screen.getByRole('button', { name: 'Crear orden' });
    fireEvent.click(button);

    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Número de orden' })).toHaveValue(statusCode === 409 ? 'OC-001' : 'OC-008');
    expect(screen.getByRole('spinbutton', { name: 'Cliente ID' })).toHaveValue(statusCode === 400 ? 9999 : 1);
    expect(screen.getByRole('spinbutton', { name: 'Total' })).toHaveValue(50);
    expect(button).toBeEnabled();
  });
});
