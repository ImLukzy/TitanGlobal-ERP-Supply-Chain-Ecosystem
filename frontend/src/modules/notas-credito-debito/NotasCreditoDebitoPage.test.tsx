import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { NotasCreditoDebitoPage } from './NotasCreditoDebitoPage';
import type { NotaDetalle } from './tipos';

const sesion = vi.hoisted(() => ({ roles: ['FINANZAS'] }));
vi.mock('../../layout/SesionContext', () => ({ useSesionOpcional: () => ({ sesion }) }));
vi.mock('./api');

const nota: NotaDetalle = {
  id: 7, facturaId: 3, tipo: 'Credito', numero: 'NC-000001', moneda: 'USD', total: 11.8, estado: 'Borrador',
  fechaCreacion: '2026-10-01T13:00:00Z', fechaEmision: null, clienteId: 1, clienteNombre: 'Cliente A',
  facturaTipoComprobante: 'Factura', facturaSerie: 'F001', facturaNumero: 1,
  motivo: 'Ajuste de prueba', baseImponible: 10, importeImpuesto: 1.8, fechaAnulacion: null, motivoAnulacion: null,
  detalles: [{ id: 1, detalleFacturaId: 22, cantidad: 1, precioUnitario: 10, porcentajeImpuesto: 18,
    baseImponible: 10, importeImpuesto: 1.8, total: 11.8 }],
};
const factura = { id: 3, numeroComprobante: 'F001-00000001', tipoComprobante: 'Factura' as const, clienteNombre: 'Cliente A',
  estado: 'Emitida', moneda: 'USD', detalles: [{ id: 22, descripcion: 'Producto del origen', cantidad: 2, precioUnitario: 10, porcentajeImpuesto: 18 }] };
const pagina = { items: [nota], pagina: 1, tamanoPagina: 20, totalRegistros: 21, totalPaginas: 2 };

afterEach(cleanup);

beforeEach(() => {
  vi.resetAllMocks();
  sesion.roles = ['FINANZAS'];
  vi.mocked(api.listarNotas).mockResolvedValue(pagina);
  vi.mocked(api.obtenerNota).mockResolvedValue(nota);
  vi.mocked(api.facturasElegibles).mockResolvedValue([factura]);
  vi.mocked(api.facturaParaNota).mockResolvedValue(factura);
});

function abrir() {
  render(<MemoryRouter><NotasCreditoDebitoPage /></MemoryRouter>);
}

async function consultar() {
  fireEvent.click(await screen.findByRole('button', { name: nota.numero }));
  await screen.findByText(/Ajuste de prueba/);
}

it('carga, filtra y pagina notas', async () => {
  abrir();
  expect(screen.getByText('Cargando notas…')).toBeInTheDocument();
  const filaVisible = await screen.findByRole('button', { name: nota.numero });
  expect(filaVisible.closest('tbody > tr')).not.toBeNull();
  fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Emitida' } });
  expect(api.listarNotas).toHaveBeenLastCalledWith('', '', 'Emitida', 1);
  fireEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));
  expect(api.listarNotas).toHaveBeenLastCalledWith('', '', 'Emitida', 2);
});

it('muestra el vacío del servidor', async () => {
  vi.mocked(api.listarNotas).mockResolvedValue({ ...pagina, items: [], totalRegistros: 0 });
  abrir();
  expect(await screen.findByText('No hay notas para estos filtros.')).toBeInTheDocument();
});

it('permite leer a CONSULTA y reserva acciones para FINANZAS y ADMIN', async () => {
  sesion.roles = ['CONSULTA'];
  abrir();
  await consultar();
  expect(screen.getByText(/Impuesto histórico 18%/)).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Nueva nota' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Emitir nota' })).not.toBeInTheDocument();
});

it('crea débito con comprobante y línea reales y valida el motivo', async () => {
  vi.mocked(api.crearNota).mockResolvedValue(nota);
  abrir();
  fireEvent.click(screen.getByRole('button', { name: 'Nueva nota' }));
  const form = screen.getByRole('form', { name: 'Datos de la nota' });
  fireEvent.submit(form);
  expect(screen.getByRole('alert')).toHaveTextContent('El motivo debe tener entre 3 y 300 caracteres.');
  await screen.findByRole('option', { name: /F001-00000001/ });
  fireEvent.change(within(form).getByLabelText('Comprobante emitido'), { target: { value: '3' } });
  fireEvent.change(await within(form).findByLabelText('Línea de origen'), { target: { value: '22' } });
  fireEvent.change(within(form).getByLabelText('Cantidad'), { target: { value: '1.25' } });
  fireEvent.change(within(form).getByLabelText('Tipo de nota'), { target: { value: 'Debito' } });
  fireEvent.change(within(form).getByLabelText('Motivo'), { target: { value: ' Ajuste ' } });
  fireEvent.submit(form);
  await screen.findByText('Borrador guardado.');
  expect(api.crearNota).toHaveBeenCalledWith({ facturaId: 3, tipo: 'Debito', motivo: 'Ajuste',
    detalles: [{ detalleFacturaId: 22, cantidad: 1.25, precioUnitario: 10 }] });
});

it('edita el borrador sin sustituir el comprobante, tipo ni folio', async () => {
  sesion.roles = ['ADMIN'];
  vi.mocked(api.editarNota).mockResolvedValue(nota);
  abrir();
  await consultar();
  fireEvent.click(screen.getByRole('button', { name: 'Editar borrador' }));
  const form = screen.getByRole('form', { name: 'Datos de la nota' });
  expect(within(form).queryByLabelText('Tipo de nota')).not.toBeInTheDocument();
  expect(await within(form).findByLabelText('Línea de origen')).toHaveValue('22');
  fireEvent.submit(form);
  await screen.findByText('Borrador guardado.');
  expect(api.editarNota).toHaveBeenCalledWith(7, { motivo: nota.motivo,
    detalles: [{ detalleFacturaId: 22, cantidad: 1, precioUnitario: 10 }] });
});

it('confirma emisión y anulación según el estado', async () => {
  const emitida = { ...nota, estado: 'Emitida' as const, fechaEmision: '2026-10-01' };
  const anulada = { ...emitida, estado: 'Anulada' as const };
  vi.mocked(api.emitirNota).mockResolvedValue(emitida);
  vi.mocked(api.anularNota).mockResolvedValue(anulada);
  abrir();
  await consultar();
  vi.mocked(api.obtenerNota).mockResolvedValue(emitida);
  fireEvent.click(screen.getByRole('button', { name: 'Emitir nota' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar emisión' }));
  await screen.findByText('Nota emitida.');
  expect(api.emitirNota).toHaveBeenCalledWith(7);
  fireEvent.click(await screen.findByRole('button', { name: 'Anular nota' }));
  const form = screen.getByRole('form', { name: 'Confirmar anulación' });
  fireEvent.change(within(form).getByLabelText('Motivo de anulación'), { target: { value: 'Comprobante duplicado' } });
  vi.mocked(api.obtenerNota).mockResolvedValue(anulada);
  fireEvent.submit(form);
  await screen.findByText('Nota anulada.');
  expect(api.anularNota).toHaveBeenCalledWith(7, 'Comprobante duplicado');
});

it.each([400, 401, 403, 404, 409])('presenta el literal HTTP %s del backend', async codigo => {
  vi.mocked(api.listarNotas).mockRejectedValue(new ApiError(codigo, 'Literal ' + codigo));
  abrir();
  expect(await screen.findByRole('alert')).toHaveTextContent('Literal ' + codigo);
});
