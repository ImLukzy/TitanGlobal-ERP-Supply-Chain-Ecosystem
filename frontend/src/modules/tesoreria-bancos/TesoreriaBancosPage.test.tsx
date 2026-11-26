import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { TesoreriaBancosPage } from './TesoreriaBancosPage';
import type { CuentaDetalle, Movimiento } from './tipos';

const sesion = vi.hoisted(() => ({ roles: ['FINANZAS'] }));
vi.mock('../../layout/SesionContext', () => ({ useSesionOpcional: () => ({ sesion }) }));
vi.mock('../../lib/fechaNegocioPeru', () => ({ diaDePeru: () => '2026-10-01' }));
vi.mock('./api');

const cuenta: CuentaDetalle = { id: 1, banco: 'Banco A', numero: '001', nombre: 'Cuenta principal',
  moneda: 'PEN', saldo: 100, tipo: 'Corriente', fechaRegistro: '2026-10-01T13:00:00Z' };
const destino: CuentaDetalle = { ...cuenta, id: 2, numero: '002', nombre: 'Cuenta secundaria' };
const movimiento: Movimiento = { id: 6, cuentaBancariaId: 1, tipo: 'Ingreso', importe: 100, fechaOperacion: '2026-09-30',
  referencia: 'Ingreso inicial', moneda: 'PEN', transferenciaBancariaId: null, fechaReversion: null, motivoReversion: null };
const pagina = { items: [cuenta], pagina: 1, tamanoPagina: 20, totalRegistros: 21, totalPaginas: 2 };
const historial = { ...pagina, items: [movimiento, { ...movimiento, id: 7, transferenciaBancariaId: 4 }] };
const transferencia = { id: 4, cuentaOrigenId: 1, cuentaDestinoId: 2, importe: 25,
  moneda: 'PEN' as const, fechaOperacion: '2026-10-01', movimientoEgresoId: 7, movimientoIngresoId: 8 };

afterEach(cleanup);
beforeEach(() => {
  vi.resetAllMocks();
  sesion.roles = ['FINANZAS'];
  vi.mocked(api.listarCuentas).mockResolvedValue(pagina);
  vi.mocked(api.obtenerCuenta).mockResolvedValue(cuenta);
  vi.mocked(api.listarMovimientos).mockResolvedValue(historial);
  vi.mocked(api.cuentasDeMoneda).mockResolvedValue([cuenta, destino]);
  vi.mocked(api.obtenerTransferencia).mockResolvedValue(transferencia);
});

function abrir() {
  render(<MemoryRouter><TesoreriaBancosPage /></MemoryRouter>);
}

async function seleccionar() {
  fireEvent.click(await screen.findByRole('button', { name: cuenta.nombre }));
  await screen.findByRole('heading', { name: 'Movimientos de Cuenta principal' });
}

it('presenta listado paginado, filtros y carga', async () => {
  abrir();
  expect(screen.getByText('Cargando cuentas…')).toBeInTheDocument();
  const filaVisible = await screen.findByRole('button', { name: cuenta.nombre });
  expect(filaVisible.closest('tbody > tr')).not.toBeNull();
  fireEvent.change(screen.getByLabelText('Moneda de cuenta'), { target: { value: 'PEN' } });
  fireEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));
  expect(api.listarCuentas).toHaveBeenLastCalledWith('PEN', '', 2);
});

it('muestra el vacío y reserva las escrituras', async () => {
  sesion.roles = ['CONSULTA'];
  vi.mocked(api.listarCuentas).mockResolvedValue({ ...pagina, items: [] });
  abrir();
  expect(await screen.findByText('No hay cuentas para estos filtros.')).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Nueva cuenta' })).not.toBeInTheDocument();
});

it('valida el alta de cuenta y conserva ceros iniciales', async () => {
  vi.mocked(api.guardarCuenta).mockResolvedValue(cuenta);
  abrir();
  fireEvent.click(screen.getByRole('button', { name: 'Nueva cuenta' }));
  const form = screen.getByRole('form', { name: 'Operación bancaria' });
  fireEvent.submit(form);
  expect(screen.getByRole('alert')).toHaveTextContent('Los datos de tesorería no son válidos.');
  fireEvent.change(within(form).getByLabelText('Banco'), { target: { value: 'Banco A' } });
  fireEvent.change(within(form).getByLabelText('Número de cuenta'), { target: { value: '001' } });
  fireEvent.change(within(form).getByLabelText('Nombre'), { target: { value: cuenta.nombre } });
  fireEvent.submit(form);
  await screen.findByText('Cuenta guardada.');
  expect(api.guardarCuenta).toHaveBeenCalledWith(expect.objectContaining({ numero: '001', moneda: 'PEN' }), undefined);
});

it('bloquea la edición de moneda si existe historial incluso con filtros', async () => {
  abrir();
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Editar cuenta' }));
  const form = screen.getByRole('form', { name: 'Operación bancaria' });
  expect(await within(form).findByLabelText('Moneda')).toBeDisabled();
  expect(api.listarMovimientos).toHaveBeenCalledWith(1, '', '', '', 1, 1);
});

it.each(['Ingreso', 'Egreso'] as const)('registra %s usando la fecha de Perú y refresca', async tipo => {
  vi.mocked(api.registrarMovimiento).mockResolvedValue(movimiento);
  abrir();
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Registrar ' + tipo.toLowerCase() }));
  const form = screen.getByRole('form', { name: 'Operación bancaria' });
  const campos = within(form);
  expect(campos.getByLabelText('Fecha de operación')).toHaveValue('2026-10-01');
  fireEvent.change(campos.getByLabelText('Importe'), { target: { value: '25' } });
  fireEvent.submit(form);
  await screen.findByText('Movimiento registrado.');
  expect(api.registrarMovimiento).toHaveBeenCalledWith(1, { tipo, importe: 25, fechaOperacion: '2026-10-01', referencia: null });
});

it('transfiere únicamente a otra cuenta en la moneda seleccionada', async () => {
  vi.mocked(api.registrarTransferencia).mockResolvedValue(transferencia);
  abrir();
  await seleccionar();
  fireEvent.click(screen.getByRole('button', { name: 'Transferir' }));
  const form = screen.getByRole('form', { name: 'Operación bancaria' });
  const campos = within(form);
  await campos.findByRole('option', { name: /Cuenta secundaria/ });
  expect(campos.queryByRole('option', { name: /Cuenta principal/ })).not.toBeInTheDocument();
  fireEvent.change(campos.getByLabelText('Cuenta destino'), { target: { value: '2' } });
  fireEvent.change(campos.getByLabelText('Importe'), { target: { value: '25' } });
  fireEvent.submit(form);
  await screen.findByText('Transferencia registrada.');
  expect(api.cuentasDeMoneda).toHaveBeenCalledWith('PEN');
  expect(api.registrarTransferencia).toHaveBeenCalledWith({ cuentaOrigenId: 1, cuentaDestinoId: 2,
    importe: 25, fechaOperacion: '2026-10-01', referencia: null });
});

it('reversa un movimiento con motivo y conserva la consulta de transferencias', async () => {
  vi.mocked(api.revertirMovimiento).mockResolvedValue({ ...movimiento, fechaReversion: '2026-10-01T13:00:00Z', motivoReversion: 'Corrección' });
  abrir();
  await seleccionar();
  expect(await screen.findByRole('button', { name: 'Ver transferencia 4' })).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Revertir movimiento 7' })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Revertir movimiento 6' }));
  const form = screen.getByRole('form', { name: 'Operación bancaria' });
  fireEvent.change(within(form).getByLabelText('Motivo de reversión'), { target: { value: ' Corrección ' } });
  fireEvent.submit(form);
  await screen.findByText('Movimiento revertido.');
  expect(api.revertirMovimiento).toHaveBeenCalledWith(1, 6, 'Corrección');
});

it.each([400, 401, 403, 404, 409])('muestra el literal del HTTP %s', async codigo => {
  vi.mocked(api.listarCuentas).mockRejectedValue(new ApiError(codigo, 'Literal ' + codigo));
  abrir();
  expect(await screen.findByRole('alert')).toHaveTextContent('Literal ' + codigo);
});
