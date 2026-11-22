import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import {
  reclamado,
  prepararSiniestros,
  esperarCargaInicialSiniestros,
} from './siniestros.testUtils';
vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});
async function seleccionar(id = 1) {
  prepararSiniestros();
  await esperarCargaInicialSiniestros();
  fireEvent.click(screen.getByRole('button', { name: `Ver siniestro SIN-${id}` }));
  await screen.findByRole('heading', { name: `SIN-${id}` });
}
function rellenarReclamo() {
  for (const [l, v] of [
    ['Número de póliza', ' POL-50 '],
    ['Aseguradora', ' Aseguradora Norte '],
    ['Número de reclamo', ' REC-50 '],
    ['Moneda (3 letras)', 'pen'],
    ['Monto reclamado', '1250.50'],
  ])
    fireEvent.change(screen.getByLabelText(l), { target: { value: v } });
}
it('Escape no reclama y devuelve foco', async () => {
  await seleccionar();
  const b = screen.getByRole('button', { name: 'Abrir reclamo' });
  b.focus();
  fireEvent.click(b);
  expect(screen.getByLabelText('Número de póliza')).toHaveFocus();
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
  expect(api.reclamarSiniestro).not.toHaveBeenCalled();
});
it('valida reclamo antes de enviar', async () => {
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Abrir reclamo' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar reclamo' }));
  expect(api.reclamarSiniestro).not.toHaveBeenCalled();
  expect(screen.getByText('El campo número de póliza es obligatorio.')).toBeInTheDocument();
});
it('409 conserva reclamo y foco para Escape', async () => {
  await seleccionar();
  const b = screen.getByRole('button', { name: 'Abrir reclamo' });
  b.focus();
  fireEvent.click(b);
  rellenarReclamo();
  vi.mocked(api.reclamarSiniestro).mockRejectedValueOnce(new ApiError(409, 'Estado cambiado'));
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar reclamo' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Estado cambiado');
  expect(screen.getByLabelText('Monto reclamado')).toHaveValue('1250.50');
  expect(screen.getByRole('dialog')).toHaveFocus();
  fireEvent.keyDown(document.activeElement!, { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
});
it('bloquea doble envío y cierre mientras reclama', async () => {
  await seleccionar();
  let resolver!: (d: { id: number }) => void;
  vi.mocked(api.reclamarSiniestro).mockReturnValue(
    new Promise((r) => {
      resolver = r;
    }),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Abrir reclamo' }));
  rellenarReclamo();
  const b = screen.getByRole('button', { name: 'Confirmar reclamo' });
  fireEvent.click(b);
  fireEvent.click(b);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  expect(api.reclamarSiniestro).toHaveBeenCalledTimes(1);
  expect(api.reclamarSiniestro).toHaveBeenCalledWith(1, {
    numeroPoliza: 'POL-50',
    aseguradora: 'Aseguradora Norte',
    numeroReclamo: 'REC-50',
    moneda: 'PEN',
    montoReclamado: 1250.5,
  });
  expect(screen.getByRole('dialog')).toHaveFocus();
  expect(fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Tab' })).toBe(false);
  expect(fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Tab', shiftKey: true })).toBe(false);
  expect(screen.getByRole('dialog')).toHaveFocus();
  vi.mocked(api.obtenerSiniestro).mockResolvedValue({ ...reclamado, id: 1, codigo: 'SIN-1' });
  await act(async () => resolver({ id: 1 }));
  expect(await screen.findByRole('button', { name: 'Cerrar reclamo' })).toBeInTheDocument();
});
it('cierre enfoca select y atrapa ShiftTab y Escape', async () => {
  await seleccionar(2);
  const b = screen.getByRole('button', { name: 'Cerrar reclamo' });
  b.focus();
  fireEvent.click(b);
  const primero = screen.getByLabelText('Resultado'),
    ultimo = screen.getByRole('button', { name: 'Confirmar cierre' });
  expect(primero).toHaveFocus();
  fireEvent.keyDown(primero, { key: 'Tab', shiftKey: true });
  expect(ultimo).toHaveFocus();
  fireEvent.keyDown(ultimo, { key: 'Tab' });
  expect(primero).toHaveFocus();
  fireEvent.keyDown(primero, { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
  expect(api.cerrarSiniestro).not.toHaveBeenCalled();
});
it('cierre excesivo no hace petición ni pierde datos', async () => {
  await seleccionar(2);
  fireEvent.click(screen.getByRole('button', { name: 'Cerrar reclamo' }));
  fireEvent.change(screen.getByLabelText('Monto reconocido'), { target: { value: '1251' } });
  fireEvent.change(screen.getByLabelText('Resolución'), { target: { value: 'Referencia' } });
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar cierre' }));
  expect(
    screen.getByText('El monto reconocido no puede superar el reclamado.'),
  ).toBeInTheDocument();
  expect(api.cerrarSiniestro).not.toHaveBeenCalled();
});
it.each([
  ['Aceptado', '1000.25', '1000.25 PEN'],
  ['Rechazado', '0', '0.00 PEN'],
] as const)(
  'cierra %s conservando referencia e importe',
  async (resultado, montoReconocido, visible) => {
    await seleccionar(2);
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar reclamo' }));
    fireEvent.change(screen.getByLabelText('Resultado'), { target: { value: resultado } });
    fireEvent.change(screen.getByLabelText('Monto reconocido'), {
      target: { value: montoReconocido },
    });
    fireEvent.change(screen.getByLabelText('Resolución'), {
      target: { value: ' Referencia manual ' },
    });
    vi.mocked(api.obtenerSiniestro).mockResolvedValue({
      ...reclamado,
      estado: 'Cerrado',
      resultado,
      montoReconocido: resultado === 'Aceptado' ? '1000.25' : '0.00',
      resolucion: 'Referencia manual',
      fechaCierre: '2026-11-22T13:00:00Z',
    });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cierre' }));
    await waitFor(() =>
      expect(
        within(screen.getByRole('region', { name: 'SIN-2' })).getByText('Referencia manual'),
      ).toBeInTheDocument(),
    );
    expect(screen.getByText(visible)).toBeInTheDocument();
    expect(screen.getByText('1250.50 PEN')).toBeInTheDocument();
    expect(screen.getByText('Reclamo (UTC)')).toBeInTheDocument();
    expect(api.cerrarSiniestro).toHaveBeenCalledWith(2, {
      resultado,
      montoReconocido: Number(montoReconocido),
      resolucion: 'Referencia manual',
    });
    expect(screen.queryByRole('button', { name: 'Anular siniestro' })).not.toBeInTheDocument();
  },
);
it('anula con motivo conservando reclamo', async () => {
  await seleccionar(2);
  fireEvent.click(screen.getByRole('button', { name: 'Anular siniestro' }));
  const m = screen.getByLabelText('Motivo de anulación');
  expect(m).toHaveFocus();
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar anulación' }));
  expect(api.anularSiniestro).not.toHaveBeenCalled();
  fireEvent.change(m, { target: { value: ' Cambio ' } });
  vi.mocked(api.obtenerSiniestro).mockResolvedValue({
    ...reclamado,
    estado: 'Anulado',
    motivoAnulacion: 'Cambio',
    fechaAnulacion: '2026-11-22T14:00:00Z',
  });
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar anulación' }));
  await waitFor(() =>
    expect(
      within(screen.getByRole('region', { name: 'SIN-2' })).getByText('Cambio'),
    ).toBeInTheDocument(),
  );
  expect(screen.getByText('1250.50 PEN')).toBeInTheDocument();
  expect(screen.getByText('POL-50')).toBeInTheDocument();
  expect(screen.getByText('Reclamo (UTC)')).toBeInTheDocument();
});
it('baja accesible no escribe con Escape y confirma después', async () => {
  await seleccionar();
  const b = screen.getByRole('button', { name: 'Eliminar siniestro' });
  b.focus();
  fireEvent.click(b);
  fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
  await waitFor(() => expect(b).toHaveFocus());
  expect(api.eliminarSiniestro).not.toHaveBeenCalled();
  fireEvent.click(b);
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar siniestro' }),
  );
  expect(
    await screen.findByText('Selecciona un siniestro para ver el detalle.'),
  ).toBeInTheDocument();
});
