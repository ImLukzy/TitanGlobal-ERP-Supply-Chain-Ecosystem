import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ValeFormulario } from './ValeFormulario';
import { vale } from './vales.testUtils';
afterEach(cleanup);
it('valida sin enviar y enfoca el primer error', async () => {
  const guardar = vi.fn();
  render(
    <ValeFormulario inicial={null} ocupado={false} error="" guardar={guardar} cerrar={() => {}} />,
  );
  fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
  await waitFor(() => expect(screen.getByLabelText('Código')).toHaveFocus());
  expect(guardar).not.toHaveBeenCalled();
  expect(screen.getByText('La fecha de carga es obligatoria.')).toBeInTheDocument();
});
it('calcula exactamente y envía final vacío como null', () => {
  const guardar = vi.fn();
  render(
    <ValeFormulario inicial={vale} ocupado={false} error="" guardar={guardar} cerrar={() => {}} />,
  );
  expect(screen.getByText('Importe estimado: 256.25 PEN')).toBeInTheDocument();
  expect(screen.getByText('500 km · 10.000 km/L · 10.000 L/100 km')).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('Odómetro final (km, opcional)'), {
    target: { value: '' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
  expect(guardar).toHaveBeenCalledWith({
    codigo: 'VAL-1',
    placa: 'ABC-470',
    estacion: 'Estación Norte',
    fechaCarga: '2026-11-22T10:30:12.345Z',
    tipoCombustible: 'Diesel',
    litros: 50,
    precioLitro: 5.125,
    moneda: 'PEN',
    odometroInicial: 12000,
    odometroFinal: null,
    observaciones: undefined,
  });
  expect(
    screen.getByText('Rendimiento pendiente: registra el odómetro final.'),
  ).toBeInTheDocument();
});
it('conserva valores con error del servidor y bloquea doble envío', () => {
  const guardar = vi.fn();
  const { rerender } = render(
    <ValeFormulario
      inicial={vale}
      ocupado={false}
      error="Conflicto"
      guardar={guardar}
      cerrar={() => {}}
    />,
  );
  expect(screen.getByRole('alert')).toHaveTextContent('Conflicto');
  expect(screen.getByLabelText('Litros (L)')).toHaveValue('50.000');
  rerender(
    <ValeFormulario
      inicial={vale}
      ocupado={true}
      error="Conflicto"
      guardar={guardar}
      cerrar={() => {}}
    />,
  );
  expect(screen.getByLabelText('Litros (L)')).toBeDisabled();
  expect(screen.getByRole('button', { name: 'Guardando…' })).toBeDisabled();
  expect(guardar).not.toHaveBeenCalled();
});
