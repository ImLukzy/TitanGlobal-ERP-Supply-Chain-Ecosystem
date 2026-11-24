import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { MemoryRouter } from 'react-router';
import { ApiError } from '../../api/client';
import * as api from './api';
import { TransportistasTercerosPage } from './TransportistasTercerosPage';
import { DialogoCancelarFlete } from './DialogoCancelarFlete';
import { FleteFormulario } from './FleteFormulario';
import type { FleteDetalle, TransportistaDetalle } from './tipos';

vi.mock('./api');
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.resetAllMocks();
});
const transportista: TransportistaDetalle = {
  id: 1,
  razonSocial: 'Andes',
  documentoFiscal: 'DOC1',
  pais: 'Perú',
  tipoServicio: 'Ambos',
  activo: true,
};
const flete: FleteDetalle = {
  id: 1,
  transportistaId: 1,
  transportistaRazonSocial: 'Andes',
  referencia: 'F1',
  fecha: '2026-11-21T00:00:00Z',
  origen: 'Lima',
  destino: 'Callao',
  importe: '9999999999999999.99',
  moneda: 'USD',
  estado: 'Registrado',
};
function preparar() {
  vi.mocked(api.listarTransportistas).mockResolvedValue({
    items: [transportista],
    total: 1,
    pagina: 1,
    tamanoPagina: 10,
  });
  vi.mocked(api.resumenTransportistas).mockResolvedValue({
    totalTransportistas: 1,
    activos: 1,
    inactivos: 0,
    porTipoServicio: [{ tipoServicio: 'Ambos', totalTransportistas: 1, activos: 1 }],
  });
  vi.mocked(api.obtenerTransportista).mockResolvedValue(transportista);
  vi.mocked(api.listarFletes).mockResolvedValue({
    items: [flete],
    total: 1,
    pagina: 1,
    tamanoPagina: 10,
  });
  vi.mocked(api.obtenerFlete).mockResolvedValue(flete);
}
it('permite reintentar el resumen sin ocultar el listado disponible', async () => {
  preparar();
  vi.mocked(api.resumenTransportistas).mockRejectedValueOnce(new Error('red'));
  pagina();
  await screen.findByRole('button', { name: 'Ver transportista Andes' });
  fireEvent.click(await screen.findByRole('button', { name: 'Reintentar resumen' }));
  await screen.findByRole('region', { name: 'Resumen de transportistas' });
  expect(screen.queryByRole('button', { name: 'Reintentar resumen' })).not.toBeInTheDocument();
});
function pagina(vista = 'transportistas') {
  return render(
    <MemoryRouter initialEntries={[`/modulos/48-transportistas-terceros?vista=${vista}`]}>
      <TransportistasTercerosPage />
    </MemoryRouter>,
  );
}
it('carga el maestro una vez y espera 300ms al buscar', async () => {
  preparar();
  vi.useFakeTimers();
  pagina();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
  });
  expect(api.listarTransportistas).toHaveBeenCalledTimes(1);
  fireEvent.change(screen.getByLabelText('Buscar razón social o documento'), {
    target: { value: 'Sur' },
  });
  await act(async () => {
    await vi.advanceTimersByTimeAsync(299);
  });
  expect(api.listarTransportistas).toHaveBeenCalledTimes(1);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(api.listarTransportistas).toHaveBeenLastCalledWith(
    expect.objectContaining({ busqueda: 'Sur', pagina: 1 }),
  );
});
it('combina filtros, paginación y vuelve al principio', async () => {
  preparar();
  vi.mocked(api.listarTransportistas).mockResolvedValue({
    items: [transportista],
    total: 12,
    pagina: 1,
    tamanoPagina: 10,
  });
  pagina();
  fireEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));
  await waitFor(() =>
    expect(api.listarTransportistas).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 2 }),
    ),
  );
  fireEvent.change(screen.getByLabelText('Estado del transportista'), {
    target: { value: 'false' },
  });
  fireEvent.change(screen.getByLabelText('Servicio'), { target: { value: 'Flete' } });
  await waitFor(() =>
    expect(api.listarTransportistas).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 1, activo: false, tipoServicio: 'Flete' }),
    ),
  );
});
it('mantiene datos del formulario y muestra un conflicto literal', async () => {
  preparar();
  vi.mocked(api.crearTransportista).mockRejectedValue(new ApiError(409, 'Documento duplicado.'));
  pagina();
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo transportista' }));
  for (const [label, valor] of [
    ['Razón social', ' Nuevo '],
    ['Documento fiscal', ' D1 '],
    ['País', ' Perú '],
  ])
    fireEvent.change(screen.getByLabelText(label!), { target: { value: valor } });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar transportista' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Documento duplicado.');
  expect(screen.getByLabelText('Razón social')).toHaveValue(' Nuevo ');
  expect(api.crearTransportista).toHaveBeenCalledWith(
    expect.objectContaining({ razonSocial: 'Nuevo', documentoFiscal: 'D1', activo: true }),
  );
});
it('impide guardar inválidos y enfoca el primer error', async () => {
  preparar();
  pagina();
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo transportista' }));
  fireEvent.click(screen.getByRole('button', { name: 'Guardar transportista' }));
  expect(screen.getByText('La razón social es obligatoria.')).toBeInTheDocument();
  await waitFor(() => expect(screen.getByLabelText('Razón social')).toHaveFocus());
  expect(api.crearTransportista).not.toHaveBeenCalled();
});
it('edita el maestro y refresca el resumen', async () => {
  preparar();
  vi.mocked(api.actualizarTransportista).mockResolvedValue({ id: 1 });
  pagina();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver transportista Andes' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Editar transportista' }));
  fireEvent.change(screen.getByLabelText('Razón social'), { target: { value: 'Andes nueva' } });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar transportista' }));
  await waitFor(() =>
    expect(api.actualizarTransportista).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ razonSocial: 'Andes nueva' }),
    ),
  );
  await waitFor(() => expect(api.resumenTransportistas).toHaveBeenCalledTimes(2));
});
it('muestra el 409 de baja sin cerrar el diálogo y Escape devuelve foco', async () => {
  preparar();
  vi.mocked(api.eliminarTransportista).mockRejectedValue(
    new ApiError(409, 'No se puede eliminar un transportista con fletes registrados.'),
  );
  pagina();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver transportista Andes' }));
  const boton = await screen.findByRole('button', { name: 'Eliminar transportista' });
  boton.focus();
  fireEvent.click(boton);
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar transportista' }),
  );
  expect(await screen.findByRole('alert')).toHaveTextContent('No se puede eliminar');
  fireEvent.keyDown(document, { key: 'Escape' });
  await waitFor(() => expect(boton).toHaveFocus());
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});
it('corrige una última página vacía tras eliminar', async () => {
  preparar();
  vi.mocked(api.listarTransportistas).mockImplementation(async (filtros) => ({
    items: [transportista],
    total: 11,
    pagina: filtros.pagina!,
    tamanoPagina: 10,
  }));
  vi.mocked(api.eliminarTransportista).mockResolvedValue(undefined);
  pagina();
  fireEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));
  await waitFor(() =>
    expect(api.listarTransportistas).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 2 }),
    ),
  );
  fireEvent.click(screen.getByRole('button', { name: 'Ver transportista Andes' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Eliminar transportista' }));
  vi.mocked(api.listarTransportistas).mockImplementation(async (filtros) => ({
    items: filtros.pagina === 2 ? [] : [transportista],
    total: 10,
    pagina: filtros.pagina!,
    tamanoPagina: 10,
  }));
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar transportista' }),
  );
  await waitFor(() =>
    expect(api.listarTransportistas).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 1 }),
    ),
  );
});
it('presenta error, reintento y vacío', async () => {
  preparar();
  vi.mocked(api.listarTransportistas).mockRejectedValueOnce(new Error('red'));
  pagina();
  expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo completar');
  vi.mocked(api.listarTransportistas).mockResolvedValue({
    items: [],
    total: 0,
    pagina: 1,
    tamanoPagina: 10,
  });
  fireEvent.click(screen.getByRole('button', { name: 'Reintentar transportistas' }));
  expect(await screen.findByText('Sin transportistas coincidentes')).toBeInTheDocument();
});
it('ignora un detalle antiguo después de cambiar selección', async () => {
  preparar();
  vi.mocked(api.listarTransportistas).mockResolvedValue({
    items: [transportista, { ...transportista, id: 2, razonSocial: 'Sur' }],
    total: 2,
    pagina: 1,
    tamanoPagina: 10,
  });
  let resolver!: (dato: TransportistaDetalle) => void;
  vi.mocked(api.obtenerTransportista)
    .mockReturnValueOnce(
      new Promise((resolve) => {
        resolver = resolve;
      }),
    )
    .mockResolvedValueOnce({ ...transportista, id: 2, razonSocial: 'Sur' });
  pagina();
  fireEvent.click(await screen.findByRole('button', { name: 'Ver transportista Andes' }));
  fireEvent.click(screen.getByRole('button', { name: 'Ver transportista Sur' }));
  await screen.findByRole('heading', { name: 'Sur' });
  await act(async () => resolver(transportista));
  expect(screen.queryByRole('heading', { name: 'Andes' })).not.toBeInTheDocument();
});
it('navega mediante URL y conserva la precisión textual del flete', async () => {
  preparar();
  pagina();
  fireEvent.click(screen.getByRole('link', { name: 'Fletes' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Ver flete F1' }));
  await screen.findByRole('heading', { name: 'Flete F1' });
  expect(screen.getAllByText('9999999999999999.99 USD')).toHaveLength(2);
  expect(
    screen.queryByRole('button', { name: /Editar flete|Eliminar flete/ }),
  ).not.toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Fletes' })).toHaveAttribute('aria-current', 'page');
});
it('cancela flete y muestra el motivo sin permitir otra cancelación', async () => {
  preparar();
  vi.mocked(api.cancelarFlete).mockResolvedValue({ id: 1 });
  pagina('fletes');
  fireEvent.click(await screen.findByRole('button', { name: 'Ver flete F1' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Cancelar flete' }));
  fireEvent.change(screen.getByLabelText('Motivo de cancelación'), {
    target: { value: ' Ruta suspendida ' },
  });
  vi.mocked(api.obtenerFlete).mockResolvedValue({
    ...flete,
    estado: 'Cancelado',
    motivoCancelacion: 'Ruta suspendida',
    fechaCancelacion: '2026-11-21T15:00:00Z',
  });
  fireEvent.click(
    within(screen.getByRole('dialog')).getByRole('button', { name: 'Cancelar flete' }),
  );
  await waitFor(() =>
    expect(screen.queryByRole('button', { name: 'Cancelar flete' })).not.toBeInTheDocument(),
  );
  expect(screen.getByText('Ruta suspendida')).toBeInTheDocument();
  expect(api.cancelarFlete).toHaveBeenCalledWith(1, 'Ruta suspendida');
});
it('diálogo conserva el motivo, atrapa Tab y restaura foco con Escape', () => {
  const cerrar = vi.fn();
  const confirmar = vi.fn();
  const origen = document.createElement('button');
  document.body.append(origen);
  origen.focus();
  const vista = render(
    <DialogoCancelarFlete
      referencia="F1"
      ocupado={false}
      error="Conflicto"
      cerrar={cerrar}
      confirmar={confirmar}
    />,
  );
  const motivo = screen.getByLabelText('Motivo de cancelación');
  expect(motivo).toHaveFocus();
  fireEvent.click(screen.getByRole('button', { name: 'Cancelar flete' }));
  expect(screen.getByText('El motivo de cancelación es obligatorio.')).toBeInTheDocument();
  expect(confirmar).not.toHaveBeenCalled();
  fireEvent.change(motivo, { target: { value: 'Motivo conservado' } });
  fireEvent.keyDown(motivo, { key: 'Tab', shiftKey: true });
  expect(screen.getByRole('button', { name: 'Cancelar flete' })).toHaveFocus();
  fireEvent.keyDown(document.activeElement!, { key: 'Tab' });
  expect(motivo).toHaveFocus();
  fireEvent.keyDown(motivo, { key: 'Escape' });
  expect(cerrar).toHaveBeenCalled();
  expect(motivo).toHaveValue('Motivo conservado');
  vista.unmount();
  expect(origen).toHaveFocus();
  origen.remove();
});
it('busca y pagina transportistas activos, registra una fecha UTC', async () => {
  preparar();
  vi.mocked(api.listarTransportistas).mockResolvedValue({
    items: [transportista],
    total: 111,
    pagina: 1,
    tamanoPagina: 10,
  });
  const guardar = vi.fn();
  render(<FleteFormulario ocupado={false} error="" guardar={guardar} cerrar={vi.fn()} />);
  fireEvent.click(await screen.findByRole('button', { name: 'Más transportistas' }));
  await waitFor(() =>
    expect(api.listarTransportistas).toHaveBeenLastCalledWith(
      expect.objectContaining({ activo: true, pagina: 2 }),
    ),
  );
  fireEvent.change(screen.getByLabelText('Transportista'), { target: { value: '1' } });
  for (const [label, valor] of [
    ['Referencia', ' F2 '],
    ['Fecha UTC', '2026-11-21'],
    ['Origen', ' Lima '],
    ['Destino', ' Callao '],
    ['Importe', '12.30'],
    ['Moneda', 'usd'],
  ])
    fireEvent.change(screen.getByLabelText(label!), { target: { value: valor } });
  fireEvent.click(screen.getByRole('button', { name: 'Guardar flete' }));
  expect(guardar).toHaveBeenCalledWith(
    expect.objectContaining({
      transportistaId: 1,
      referencia: 'F2',
      fecha: '2026-11-21T00:00:00Z',
      importe: 12.3,
      moneda: 'USD',
    }),
  );
});
