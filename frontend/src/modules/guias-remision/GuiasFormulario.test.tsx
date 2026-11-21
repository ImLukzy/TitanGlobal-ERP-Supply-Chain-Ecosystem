import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { GuiaFormulario } from './GuiaFormulario';
import { guia } from './guias.testUtils';
afterEach(cleanup);
it('valida sin guardar y enfoca el primer error', async () => {
  const guardar = vi.fn();
  render(
    <GuiaFormulario inicial={null} ocupado={false} error="" guardar={guardar} cerrar={() => {}} />,
  );
  fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
  await waitFor(() => expect(screen.getByLabelText('Serie')).toHaveFocus());
  expect(guardar).not.toHaveBeenCalled();
  expect(screen.getByText('La fecha de inicio del traslado es obligatoria.')).toBeInTheDocument();
});
it('edita dos líneas y envía solo el contrato, preservando UTC', () => {
  const guardar = vi.fn();
  render(
    <GuiaFormulario inicial={guia} ocupado={false} error="" guardar={guardar} cerrar={() => {}} />,
  );
  fireEvent.click(screen.getByRole('button', { name: 'Agregar línea' }));
  const segunda = within(screen.getByRole('group', { name: 'Línea 2' }));
  for (const [label, value] of [
    ['SKU', 'S2'],
    ['Descripción', 'Segunda'],
    ['Unidad de medida', 'KG'],
    ['Cantidad', '0.001'],
  ])
    fireEvent.change(segunda.getByLabelText(label), { target: { value } });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
  expect(guardar).toHaveBeenCalledWith(
    expect.objectContaining({
      serie: 'T001',
      numero: 1,
      fechaInicioTraslado: '2026-11-22T10:30:00.000Z',
      pesoBrutoKg: 12.34,
      lineas: [
        {
          sku: 'S1',
          descripcion: 'Caja',
          unidadMedida: 'UND',
          cantidad: 1.125,
        },
        {
          sku: 'S2',
          descripcion: 'Segunda',
          unidadMedida: 'KG',
          cantidad: 0.001,
        },
      ],
    }),
  );
  expect(guardar.mock.calls[0][0]).not.toHaveProperty('id');
  expect(guardar.mock.calls[0][0]).not.toHaveProperty('estado');
});
it('conserva campos con error del servidor y bloquea mientras guarda', () => {
  const guardar = vi.fn();
  const { rerender } = render(
    <GuiaFormulario
      inicial={guia}
      ocupado={false}
      error="Conflicto"
      guardar={guardar}
      cerrar={() => {}}
    />,
  );
  expect(screen.getByRole('alert')).toHaveTextContent('Conflicto');
  expect(screen.getByLabelText('Serie')).toHaveValue('T001');
  rerender(
    <GuiaFormulario
      inicial={guia}
      ocupado={true}
      error="Conflicto"
      guardar={guardar}
      cerrar={() => {}}
    />,
  );
  expect(screen.getByRole('button', { name: 'Guardando…' })).toBeDisabled();
  expect(screen.getByLabelText('Serie')).toBeDisabled();
  expect(guardar).not.toHaveBeenCalled();
});
it('agrega y quita sin intercambiar las líneas', () => {
  render(
    <GuiaFormulario inicial={guia} ocupado={false} error="" guardar={() => {}} cerrar={() => {}} />,
  );
  fireEvent.click(screen.getByRole('button', { name: 'Agregar línea' }));
  fireEvent.change(within(screen.getByRole('group', { name: 'Línea 2' })).getByLabelText('SKU'), {
    target: { value: 'S2' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Quitar línea 1' }));
  expect(screen.getByLabelText('SKU')).toHaveValue('S2');
  expect(screen.getByRole('button', { name: 'Quitar línea 1' })).toBeDisabled();
});
