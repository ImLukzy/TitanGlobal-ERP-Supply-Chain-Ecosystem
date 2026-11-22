import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { EntregaFormulario } from './EntregaFormulario';
import { entrega } from './entregas.testUtils';
afterEach(cleanup);
it('valida sin guardar y enfoca el primer error', async () => {
  const guardar = vi.fn();
  render(
    <EntregaFormulario
      inicial={null}
      ocupado={false}
      error=""
      guardar={guardar}
      cerrar={() => {}}
    />,
  );
  fireEvent.click(screen.getByRole('button', { name: 'Guardar programación' }));
  await waitFor(() => expect(screen.getByLabelText('Código')).toHaveFocus());
  expect(guardar).not.toHaveBeenCalled();
  expect(screen.getByText('La fecha programada es obligatoria.')).toBeInTheDocument();
});
it('edita sin perder UTC y envía únicamente campos editables', () => {
  const guardar = vi.fn();
  render(
    <EntregaFormulario
      inicial={entrega}
      ocupado={false}
      error=""
      guardar={guardar}
      cerrar={() => {}}
    />,
  );
  fireEvent.change(screen.getByLabelText('Destinatario'), { target: { value: ' Otro ' } });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar programación' }));
  expect(guardar).toHaveBeenCalledWith({
    codigo: 'ENV-1',
    referenciaGuia: 'T045-1',
    destinatario: 'Otro',
    direccionDestino: 'Avenida 100',
    fechaProgramada: '2026-11-22T10:30:12.345Z',
    observaciones: undefined,
  });
});
it('conserva datos ante error y bloquea mientras guarda', () => {
  const guardar = vi.fn();
  const { rerender } = render(
    <EntregaFormulario
      inicial={entrega}
      ocupado={false}
      error="Conflicto"
      guardar={guardar}
      cerrar={() => {}}
    />,
  );
  expect(screen.getByRole('alert')).toHaveTextContent('Conflicto');
  expect(screen.getByLabelText('Código')).toHaveValue('ENV-1');
  rerender(
    <EntregaFormulario
      inicial={entrega}
      ocupado={true}
      error="Conflicto"
      guardar={guardar}
      cerrar={() => {}}
    />,
  );
  expect(screen.getByLabelText('Código')).toBeDisabled();
  expect(screen.getByRole('button', { name: 'Guardando…' })).toBeDisabled();
  expect(guardar).not.toHaveBeenCalled();
});
