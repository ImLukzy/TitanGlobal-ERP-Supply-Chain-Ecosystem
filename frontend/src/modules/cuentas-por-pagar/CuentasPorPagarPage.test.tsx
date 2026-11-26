import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import type { ProveedorListado } from '../proveedores/tipos';
import * as api from './api';
import { CuentasPorPagarPage } from './CuentasPorPagarPage';
import type { CuentaDetalle, Pago } from './tipos';

const sesion = vi.hoisted(() => ({ roles: ['FINANZAS'] }));
vi.mock('../../layout/SesionContext', () => ({ useSesionOpcional: () => ({ sesion }) }));
vi.mock('../../lib/fechaNegocioPeru', () => ({ diaDePeru: () => '2026-10-01' }));
vi.mock('./api');

const cuenta: CuentaDetalle = {
  id: 3, proveedorId: 1, serie: 'F01', numeroDocumento: '010', moneda: 'PEN', total: 100,
  saldo: 70, fechaVencimiento: '2026-11-01', anulada: false, vencida: false, ordenAbastecimientoId: null,
  fechaDocumento: '2026-10-01', baseImponible: 80, importeImpuesto: 20, importePagado: 30,
  fechaAnulacion: null, motivoAnulacion: null,
};
const activa = { id: 1, razonSocial: 'Proveedor activo SAC', activo: true } as ProveedorListado;
const inactiva = { id: 2, razonSocial: 'Proveedor archivado', activo: false } as ProveedorListado;
const pagina = { items: [cuenta], pagina: 1, tamanoPagina: 20, totalRegistros: 21, totalPaginas: 2 };
const pago: Pago = {
  id: 4, cuentaPorPagarId: 3, importe: 30, moneda: 'PEN', fechaPago: '2026-10-01',
  fechaReversion: null, motivoReversion: null,
};

afterEach(cleanup);
beforeEach(() => {
  vi.resetAllMocks();
  sesion.roles = ['FINANZAS'];
  vi.mocked(api.listarCuentas).mockResolvedValue(pagina);
  vi.mocked(api.detalleCuenta).mockResolvedValue(cuenta);
  vi.mocked(api.listarPagos).mockResolvedValue({ ...pagina, items: [pago] });
  vi.mocked(api.proveedoresCuenta).mockResolvedValue([activa, inactiva]);
  vi.mocked(api.ordenesProveedor).mockResolvedValue([]);
  const grupo = { proveedorId: 1, moneda: 'PEN' as const, saldoAlDia: 70, saldoVencido: 15,
    vencido0A30: 10, vencido31A60: 3, vencido61A90: 2, vencidoMasDe90: 0 };
  vi.mocked(api.resumenCuentas).mockResolvedValue({
    fechaCorte: '2026-10-01', grupos: [grupo, { ...grupo, moneda: 'USD' }],
  });
});

function abrir() {
  render(<MemoryRouter><CuentasPorPagarPage /></MemoryRouter>);
}

async function seleccionar() {
  fireEvent.click(await screen.findByRole('button', { name: 'F01-010' }));
  await screen.findByRole('heading', { name: 'F01-010' });
}

it('carga la cartera, muestra el saldo y pagina sus filtros', async () => {
  abrir();
  expect(screen.getByText('Cargando cuentas por pagar…')).toBeInTheDocument();
  const filaVisible = await screen.findByRole('button', { name: 'F01-010' });
  expect(filaVisible.closest('tbody > tr')).not.toBeNull();
  fireEvent.change(screen.getByLabelText('Filtrar por proveedor'), { target: { value: '1' } });
  fireEvent.change(screen.getByLabelText('Moneda de la cartera'), { target: { value: 'PEN' } });
  fireEvent.click(screen.getByLabelText('Solo vencidas'));
  fireEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));
  expect(api.listarCuentas).toHaveBeenLastCalledWith('1', 'PEN', true, 2);
  expect(api.resumenCuentas).toHaveBeenLastCalledWith('1', 'PEN');
});

it('muestra el vacío del listado y del reporte', async () => {
  vi.mocked(api.listarCuentas).mockResolvedValue({ ...pagina, items: [] });
  vi.mocked(api.resumenCuentas).mockResolvedValue({ fechaCorte: '2026-10-01', grupos: [] });
  abrir();
  await screen.findByText('No hay cuentas para estos filtros.');
  expect(screen.getByText('Sin vencimientos para el proveedor y la moneda seleccionados.')).toBeInTheDocument();
});

it('consulta vencimientos por separado en PEN y USD', async () => {
  abrir();
  const tabla = await screen.findByRole('table', { name: 'Importes por tramo y moneda' });
  expect(tabla).toHaveTextContent('PEN');
  expect(tabla).toHaveTextContent('USD');
  expect(tabla).toHaveTextContent('Más de 90 días');
  expect(screen.getByText('Fecha de corte 2026-10-01')).toBeInTheDocument();
});

it('registra la factura de un proveedor activo conservando el vencimiento futuro', async () => {
  vi.mocked(api.registrarCuenta).mockResolvedValue(cuenta);
  abrir();
  fireEvent.click(screen.getByRole('button', { name: 'Registrar factura' }));
  const proveedor = await screen.findByLabelText('Proveedor activo');
  expect(within(proveedor).queryByRole('option', { name: inactiva.razonSocial })).not.toBeInTheDocument();
  expect(proveedor).toBeRequired();
  fireEvent.change(proveedor, { target: { value: '1' } });
  fireEvent.change(screen.getByLabelText('Serie'), { target: { value: ' F01 ' } });
  fireEvent.change(screen.getByLabelText('Número'), { target: { value: '010' } });
  fireEvent.change(screen.getByLabelText('Fecha de vencimiento'), { target: { value: '2026-11-01' } });
  fireEvent.change(screen.getByLabelText('Base imponible'), { target: { value: '80' } });
  fireEvent.change(screen.getByLabelText('Impuesto'), { target: { value: '20' } });
  fireEvent.submit(screen.getByRole('form', { name: 'Factura de proveedor' }));
  await screen.findByText('Factura registrada.');
  expect(api.registrarCuenta).toHaveBeenCalledWith({
    serie: 'F01', numeroDocumento: '010', proveedorId: 1, ordenAbastecimientoId: null, moneda: 'PEN',
    fechaDocumento: '2026-10-01', fechaVencimiento: '2026-11-01', baseImponible: 80, importeImpuesto: 20,
  });
});

it('valida la referencia de factura y evita un sobrepago antes de llamar al backend', async () => {
  abrir();
  fireEvent.click(screen.getByRole('button', { name: 'Registrar factura' }));
  const alta = await screen.findByRole('form', { name: 'Factura de proveedor' });
  fireEvent.submit(alta);
  expect(screen.getByRole('alert')).toHaveTextContent('La serie y el número');
  expect(api.registrarCuenta).not.toHaveBeenCalled();
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Registrar pago' }));
  const importe = screen.getByLabelText('Importe del pago');
  expect(importe).toHaveAttribute('max', '70');
  fireEvent.change(importe, { target: { value: '71' } });
  fireEvent.submit(screen.getByRole('form', { name: 'Registrar pago' }));
  expect(screen.getByRole('alert')).toHaveTextContent('hasta el saldo pendiente');
  expect(api.registrarPago).not.toHaveBeenCalled();
});

it('registra un pago con fecha de Perú y refresca la información de la cuenta', async () => {
  vi.mocked(api.registrarPago).mockResolvedValue(pago);
  abrir();
  await seleccionar();
  expect(screen.queryByRole('button', { name: 'Anular cuenta' })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Registrar pago' }));
  fireEvent.change(screen.getByLabelText('Importe del pago'), { target: { value: '15.25' } });
  expect(screen.getByLabelText('Fecha de pago')).toHaveValue('2026-10-01');
  fireEvent.submit(screen.getByRole('form', { name: 'Registrar pago' }));
  await screen.findByText('Pago registrado.');
  expect(api.registrarPago).toHaveBeenCalledWith(3, { importe: 15.25, fechaPago: '2026-10-01' });
  expect(api.detalleCuenta).toHaveBeenCalledTimes(2);
  expect(api.listarPagos).toHaveBeenCalledTimes(2);
});

it('revierte un pago con motivo y vuelve a consultar su historial', async () => {
  vi.mocked(api.revertirPago).mockResolvedValue(pago);
  abrir();
  await seleccionar();
  fireEvent.click(await screen.findByRole('button', { name: 'Revertir pago 4' }));
  const motivo = screen.getByLabelText('Motivo de reversión');
  expect(motivo).toBeRequired();
  fireEvent.change(motivo, { target: { value: 'Duplicado' } });
  fireEvent.submit(screen.getByRole('form', { name: 'Confirmar operación' }));
  await screen.findByText('Pago revertido.');
  expect(api.revertirPago).toHaveBeenCalledWith(3, 4, 'Duplicado');
});

it('anula una cuenta sin pagos vigentes usando un motivo', async () => {
  vi.mocked(api.detalleCuenta).mockResolvedValue({ ...cuenta, saldo: 100, importePagado: 0 });
  vi.mocked(api.anularCuenta).mockResolvedValue({ ...cuenta, anulada: true, importePagado: 0 });
  abrir();
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Anular cuenta' }));
  fireEvent.change(screen.getByLabelText('Motivo de anulación'), { target: { value: 'Documento duplicado' } });
  fireEvent.submit(screen.getByRole('form', { name: 'Confirmar operación' }));
  await screen.findByText('Cuenta anulada.');
  expect(api.anularCuenta).toHaveBeenCalledWith(3, 'Documento duplicado');
});

it('permite consultar a un usuario sin rol de escritura', async () => {
  sesion.roles = ['CONSULTA'];
  abrir();
  await seleccionar();
  await screen.findByText('Aplicado');
  expect(screen.queryByRole('button', { name: 'Registrar factura' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Registrar pago' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Revertir pago 4' })).not.toBeInTheDocument();
});

it.each([400, 401, 403, 404, 409])('presenta el mensaje HTTP %i de la API', async codigo => {
  vi.mocked(api.listarCuentas).mockRejectedValue(new ApiError(codigo, 'Error de la API ' + codigo));
  abrir();
  expect(await screen.findByRole('alert')).toHaveTextContent('Error de la API ' + codigo);
});
