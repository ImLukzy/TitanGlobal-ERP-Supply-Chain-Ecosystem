import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import {
  entrega,
  enRuta,
  prepararEntregas,
  esperarCargaInicialEntregas,
} from './entregas.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
async function seleccionar(ruta = false) {
  prepararEntregas();
  await esperarCargaInicialEntregas();
  fireEvent.click(
    screen.getByRole('button', { name: ruta ? 'Ver entrega ENV-2' : 'Ver entrega ENV-1' }),
  );
  await screen.findByRole('heading', { name: ruta ? 'ENV-2' : 'ENV-1' });
}
it('confirma salida, Escape no escribe y el foco retorna', async () => {
  await seleccionar();
  const boton = screen.getByRole('button', { name: 'Registrar salida' });
  boton.focus();
  fireEvent.click(boton);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(boton).toHaveFocus());
  expect(api.iniciarEntrega).not.toHaveBeenCalled();
  fireEvent.click(boton);
  vi.mocked(api.iniciarEntrega).mockRejectedValueOnce(
    new ApiError(409, 'Solo se puede iniciar una entrega programada.'),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar salida' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Solo se puede iniciar');
  expect(screen.getByRole('heading', { name: 'ENV-1' })).toBeInTheDocument();
});
it('evita doble salida y refresca las acciones de En ruta', async () => {
  await seleccionar();
  let resolver!: (r: { id: number }) => void;
  vi.mocked(api.iniciarEntrega).mockReturnValue(
    new Promise((r) => {
      resolver = r;
    }),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Registrar salida' }));
  const boton = screen.getByRole('button', { name: 'Confirmar salida' });
  fireEvent.click(boton);
  fireEvent.click(boton);
  expect(api.iniciarEntrega).toHaveBeenCalledTimes(1);
  vi.mocked(api.obtenerEntrega).mockResolvedValue({ ...enRuta, id: 1, codigo: 'ENV-1' });
  await act(async () => resolver({ id: 1 }));
  await screen.findByRole('button', { name: 'Registrar recepción' });
  expect(screen.queryByRole('button', { name: 'Editar entrega' })).not.toBeInTheDocument();
});
it('valida POD y conserva sus datos con 409 antes de registrar recepción', async () => {
  await seleccionar(true);
  fireEvent.click(screen.getByRole('button', { name: 'Registrar recepción' }));
  const d = within(screen.getByRole('dialog'));
  expect(d.getByLabelText('Receptor')).toHaveFocus();
  fireEvent.click(d.getByRole('button', { name: 'Confirmar recepción' }));
  expect(api.completarEntrega).not.toHaveBeenCalled();
  expect(screen.getByText('El campo receptor es obligatorio.')).toBeInTheDocument();
  for (const [label, value] of [
    ['Receptor', ' Persona '],
    ['Documento del receptor', ' DOC '],
    ['Referencia de constancia', ' POD '],
  ])
    fireEvent.change(d.getByLabelText(label), { target: { value } });
  vi.mocked(api.completarEntrega).mockRejectedValueOnce(
    new ApiError(409, 'Solo se puede completar una entrega en ruta.'),
  );
  fireEvent.click(d.getByRole('button', { name: 'Confirmar recepción' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Solo se puede completar');
  expect(d.getByLabelText('Receptor')).toHaveValue(' Persona ');
  vi.mocked(api.obtenerEntrega).mockResolvedValue({
    ...enRuta,
    estado: 'Entregada',
    receptor: 'Persona',
    documentoReceptor: 'DOC',
    referenciaConstancia: 'POD',
    fechaEntrega: '2026-11-21T14:00:00Z',
    historial: [
      ...enRuta.historial,
      {
        id: 3,
        estado: 'Entregada',
        fecha: '2026-11-21T14:00:00Z',
        descripcion: 'Recepción registrada mediante constancia manual.',
      },
    ],
  });
  fireEvent.click(d.getByRole('button', { name: 'Confirmar recepción' }));
  await screen.findByText('Recepción registrada mediante constancia manual.');
  expect(api.completarEntrega).toHaveBeenLastCalledWith(2, {
    receptor: 'Persona',
    documentoReceptor: 'DOC',
    referenciaConstancia: 'POD',
    observaciones: undefined,
  });
  expect(screen.queryByRole('button', { name: 'Cancelar entrega' })).not.toBeInTheDocument();
  expect(screen.getByText('POD')).toBeInTheDocument();
});
it('valida cancelación y preserva el historial terminal', async () => {
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Cancelar entrega' }));
  const d = within(screen.getByRole('dialog'));
  fireEvent.click(d.getByRole('button', { name: 'Confirmar cancelación' }));
  expect(api.cancelarEntrega).not.toHaveBeenCalled();
  expect(screen.getByText('El motivo de cancelación es obligatorio.')).toBeInTheDocument();
  fireEvent.change(d.getByLabelText('Motivo de cancelación'), { target: { value: ' Cambio ' } });
  vi.mocked(api.obtenerEntrega).mockResolvedValue({
    ...entrega,
    estado: 'Cancelada',
    motivoCancelacion: 'Cambio',
    fechaCancelacion: '2026-11-21T14:00:00Z',
    historial: [
      ...entrega.historial,
      { id: 3, estado: 'Cancelada', fecha: '2026-11-21T14:00:00Z', descripcion: 'Cambio' },
    ],
  });
  fireEvent.click(d.getByRole('button', { name: 'Confirmar cancelación' }));
  await waitFor(() => expect(api.cancelarEntrega).toHaveBeenCalledWith(1, 'Cambio'));
  await screen.findByText('Cancelada (UTC)');
  expect(screen.getByText('Entrega programada.')).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Editar entrega' })).not.toBeInTheDocument();
});
it('Escape en baja no elimina; la baja confirmada limpia la selección', async () => {
  await seleccionar();
  const boton = screen.getByRole('button', { name: 'Eliminar entrega' });
  boton.focus();
  fireEvent.click(boton);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(boton).toHaveFocus());
  expect(api.eliminarEntrega).not.toHaveBeenCalled();
  fireEvent.click(boton);
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar entrega' }),
  );
  await waitFor(() => expect(api.eliminarEntrega).toHaveBeenCalledWith(1));
  expect(
    await screen.findByText('Selecciona una entrega para ver el detalle.'),
  ).toBeInTheDocument();
});
