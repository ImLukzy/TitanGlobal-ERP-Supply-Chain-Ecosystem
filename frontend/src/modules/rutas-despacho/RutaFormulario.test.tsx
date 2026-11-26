import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { RutaFormulario } from './RutaFormulario';
import { prepararRutas, renderRutas, ruta } from './rutasDespacho.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
function rellenar() {
  for (const [campo, valor] of [
    ['Código', ' R-NUEVA '],
    ['Nombre', ' Ruta nueva '],
    ['Zona de la ruta', ' Centro '],
    ['Origen', ' Lima '],
    ['Destino', ' Callao '],
    ['Distancia (km)', '25.50'],
    ['Peaje estimado', '0'],
    ['Moneda', 'usd'],
  ])
    fireEvent.change(screen.getByLabelText(campo!), { target: { value: valor } });
}
it('enfoca código, valida obligatorios y enfoca el primer error', async () => {
  const guardar = vi.fn();
  render(
    <RutaFormulario inicial={null} ocupado={false} error="" guardar={guardar} cerrar={vi.fn()} />,
  );
  expect(screen.getByLabelText('Código')).toHaveFocus();
  expect(screen.getByLabelText('Activa')).toBeChecked();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar ruta' }));
  expect(screen.getByText('El código de la ruta es obligatorio.')).toBeInTheDocument();
  await waitFor(() => expect(screen.getByLabelText('Código')).toHaveFocus());
  expect(guardar).not.toHaveBeenCalled();
});
it('envía textos normalizados, peaje cero y moneda original, sin observaciones vacías', () => {
  const guardar = vi.fn();
  render(
    <RutaFormulario inicial={null} ocupado={false} error="" guardar={guardar} cerrar={vi.fn()} />,
  );
  rellenar();
  fireEvent.click(screen.getByLabelText('Activa'));
  fireEvent.click(screen.getByRole('button', { name: 'Guardar ruta' }));
  expect(guardar).toHaveBeenCalledWith({
    codigo: 'R-NUEVA',
    nombre: 'Ruta nueva',
    zona: 'Centro',
    origen: 'Lima',
    destino: 'Callao',
    distanciaKm: 25.5,
    peajeEstimado: 0,
    moneda: 'USD',
    activo: false,
    observaciones: undefined,
  });
});
it('no envía un peaje que perdería precisión', () => {
  const guardar = vi.fn();
  render(
    <RutaFormulario inicial={ruta} ocupado={false} error="" guardar={guardar} cerrar={vi.fn()} />,
  );
  fireEvent.change(screen.getByLabelText('Peaje estimado'), {
    target: { value: '9999999999999999.99' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar ruta' }));
  expect(screen.getByText(/no puede enviarse con precisión/)).toBeInTheDocument();
  expect(guardar).not.toHaveBeenCalled();
});
it.each([400, 409])('conserva los campos después de un %s', async (estado) => {
  prepararRutas();
  vi.mocked(api.crearRuta).mockRejectedValue(new ApiError(estado, 'Mensaje literal del servidor.'));
  renderRutas();
  fireEvent.click(screen.getByRole('button', { name: 'Nueva ruta' }));
  rellenar();
  fireEvent.click(screen.getByRole('button', { name: 'Guardar ruta' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Mensaje literal del servidor.');
  expect(screen.getByLabelText('Código')).toHaveValue(' R-NUEVA ');
  expect(screen.getByLabelText('Distancia (km)')).toHaveValue('25.50');
});
it('impide altas repetidas y refresca zonas, listado y resumen al guardar', async () => {
  prepararRutas();
  let resolver!: (datos: { id: number }) => void;
  vi.mocked(api.crearRuta).mockReturnValue(
    new Promise((resolve) => {
      resolver = resolve;
    }),
  );
  renderRutas();
  await screen.findByRole('button', { name: 'Ver ruta R01' });
  fireEvent.click(screen.getByRole('button', { name: 'Nueva ruta' }));
  rellenar();
  const boton = screen.getByRole('button', { name: 'Guardar ruta' });
  fireEvent.click(boton);
  fireEvent.submit(boton.closest('form')!);
  expect(api.crearRuta).toHaveBeenCalledTimes(1);
  expect(screen.getByLabelText('Código')).toBeDisabled();
  await act(async () => resolver({ id: 9 }));
  await screen.findByRole('heading', { name: 'Corredor Norte' });
  expect(api.obtenerRuta).toHaveBeenCalledWith(9);
  await waitFor(() => expect(api.listarZonas).toHaveBeenCalledTimes(2));
  expect(api.resumenRutas).toHaveBeenCalledTimes(2);
});
it('edita la ruta seleccionada y permite desactivarla', async () => {
  prepararRutas();
  renderRutas();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver ruta R01' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Editar ruta' }));
  expect(screen.getByLabelText('Código')).toHaveValue('R01');
  fireEvent.click(screen.getByLabelText('Activa'));
  fireEvent.change(screen.getByLabelText('Observaciones'), { target: { value: ' Suspendida ' } });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar ruta' }));
  await waitFor(() =>
    expect(api.actualizarRuta).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ activo: false, observaciones: 'Suspendida' }),
    ),
  );
});
