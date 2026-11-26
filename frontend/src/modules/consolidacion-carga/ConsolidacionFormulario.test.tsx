import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { ConsolidacionFormulario } from './ConsolidacionFormulario';
import { carga, prepararCargas, renderCargas } from './cargas.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
function rellenar() {
  fireEvent.change(screen.getByLabelText('Código'), { target: { value: ' NUEVA ' } });
  fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: ' Carga nueva ' } });
  const linea = within(screen.getByRole('group', { name: 'Línea 1' }));
  fireEvent.change(linea.getByLabelText('SKU'), { target: { value: ' SKU ' } });
  fireEvent.change(linea.getByLabelText('Descripción'), { target: { value: ' Caja ' } });
}
it('enfoca código y el primer error, sin enviar datos inválidos', async () => {
  const guardar = vi.fn();
  render(
    <ConsolidacionFormulario
      inicial={null}
      ocupado={false}
      error=""
      guardar={guardar}
      cerrar={vi.fn()}
    />,
  );
  expect(screen.getByLabelText('Código')).toHaveFocus();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
  expect(screen.getByText('El código de la consolidación es obligatorio.')).toBeInTheDocument();
  expect(screen.getByText('Línea 1: el SKU es obligatorio.')).toBeInTheDocument();
  await waitFor(() => expect(screen.getByLabelText('Código')).toHaveFocus());
  expect(guardar).not.toHaveBeenCalled();
});
it('guarda un borrador con packing incompleto normalizando textos sin claves de UI', () => {
  const guardar = vi.fn();
  render(
    <ConsolidacionFormulario
      inicial={null}
      ocupado={false}
      error=""
      guardar={guardar}
      cerrar={vi.fn()}
    />,
  );
  rellenar();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
  expect(guardar).toHaveBeenCalledWith({
    codigo: 'NUEVA',
    nombre: 'Carga nueva',
    observaciones: undefined,
    lineas: [
      {
        sku: 'SKU',
        descripcion: 'Caja',
        cantidad: 1,
        cantidadRecogida: 0,
        unidadesPorBulto: 0,
        largoCm: 0,
        anchoCm: 0,
        altoCm: 0,
        pesoBultoKg: 0,
      },
    ],
  });
});
it('agrega y quita presentaciones sin perder la primera, con foco en la nueva línea', async () => {
  render(
    <ConsolidacionFormulario
      inicial={carga}
      ocupado={false}
      error=""
      guardar={vi.fn()}
      cerrar={vi.fn()}
    />,
  );
  fireEvent.click(screen.getByRole('button', { name: 'Agregar línea' }));
  const segunda = within(screen.getByRole('group', { name: 'Línea 2' }));
  await waitFor(() => expect(segunda.getByLabelText('SKU')).toHaveFocus());
  expect(within(screen.getByRole('group', { name: 'Línea 1' })).getByLabelText('SKU')).toHaveValue(
    'S1',
  );
  fireEvent.click(segunda.getByRole('button', { name: 'Quitar línea 2' }));
  expect(screen.queryByRole('group', { name: 'Línea 2' })).not.toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Quitar línea 1' })).toBeDisabled();
});
it('calcula inmediatamente picking y packing con bulto parcial', () => {
  render(
    <ConsolidacionFormulario
      inicial={carga}
      ocupado={false}
      error=""
      guardar={vi.fn()}
      cerrar={vi.fn()}
    />,
  );
  expect(screen.getByText('Total estimado: 3 bultos · 0.180000 m³ · 6.00 kg')).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('Cantidad prevista'), { target: { value: '12' } });
  fireEvent.change(screen.getByLabelText('Unidades por bulto'), { target: { value: '5' } });
  expect(screen.getByText('Total estimado: 3 bultos · 0.180000 m³ · 6.00 kg')).toBeInTheDocument();
});
it('enfoca el error de una línea y no trunca cantidades ni dimensiones', async () => {
  const guardar = vi.fn();
  render(
    <ConsolidacionFormulario
      inicial={carga}
      ocupado={false}
      error=""
      guardar={guardar}
      cerrar={vi.fn()}
    />,
  );
  fireEvent.change(screen.getByLabelText('Largo (cm)'), { target: { value: '1.001' } });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
  expect(
    screen.getByText('Línea 1: el largo no puede tener más de 2 decimales.'),
  ).toBeInTheDocument();
  await waitFor(() => expect(screen.getByLabelText('Largo (cm)')).toHaveFocus());
  expect(guardar).not.toHaveBeenCalled();
});
it.each([400, 409])('conserva cabecera y líneas si guardar devuelve %s', async (estado) => {
  prepararCargas();
  vi.mocked(api.crearCarga).mockRejectedValue(new ApiError(estado, 'Conflicto literal.'));
  renderCargas();
  fireEvent.click(screen.getByRole('button', { name: 'Nueva consolidación' }));
  rellenar();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Conflicto literal.');
  expect(screen.getByLabelText('Código')).toHaveValue(' NUEVA ');
  expect(screen.getByLabelText('SKU')).toHaveValue(' SKU ');
});
it('impide doble guardado y refresca listado, detalle y resumen', async () => {
  prepararCargas();
  let resolver!: (r: { id: number }) => void;
  vi.mocked(api.crearCarga).mockReturnValue(
    new Promise((resolve) => {
      resolver = resolve;
    }),
  );
  renderCargas();
  await screen.findByRole('button', { name: 'Ver carga C01' });
  fireEvent.click(screen.getByRole('button', { name: 'Nueva consolidación' }));
  rellenar();
  const boton = screen.getByRole('button', { name: 'Guardar borrador' });
  fireEvent.click(boton);
  fireEvent.submit(boton.closest('form')!);
  expect(api.crearCarga).toHaveBeenCalledTimes(1);
  expect(screen.getByLabelText('SKU')).toBeDisabled();
  await act(async () => resolver({ id: 9 }));
  await screen.findByRole('heading', { name: 'Carga Norte' });
  expect(api.obtenerCarga).toHaveBeenCalledWith(9);
  await waitFor(() => expect(api.resumenCargas).toHaveBeenCalledTimes(2));
});
it('edita las líneas existentes en vez de crear otra consolidación', async () => {
  prepararCargas();
  renderCargas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver carga C01' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Editar carga' }));
  fireEvent.change(screen.getByLabelText('Cantidad recogida'), { target: { value: '8' } });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
  await waitFor(() =>
    expect(api.actualizarCarga).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ lineas: [expect.objectContaining({ cantidadRecogida: 8 })] }),
    ),
  );
  expect(api.crearCarga).not.toHaveBeenCalled();
});
