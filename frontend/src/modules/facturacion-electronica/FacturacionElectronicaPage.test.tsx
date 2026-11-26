import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { FacturacionElectronicaPage } from './FacturacionElectronicaPage';
import type { FacturaDetalle, FacturaFila } from './tipos';

vi.mock('./api', async importOriginal => {
  const original = await importOriginal<typeof import('./api')>();
  return { ...original, listarFacturas: vi.fn(), obtenerFactura: vi.fn(), resumenFacturas: vi.fn(),
    guardarFactura: vi.fn(), emitirFactura: vi.fn(), anularFactura: vi.fn(), cargarCatalogosFactura: vi.fn() };
});
vi.mock('../ordenes-compra/api', () => ({ obtenerJerarquiaCliente: vi.fn().mockResolvedValue([]) }));

const fila: FacturaFila = { id: 1, clienteId: 1, clienteNombre: 'Cliente registrado', clienteRuc: '20111111111',
  tipoComprobante: 'Factura', serie: 'F001', numero: 1, numeroComprobante: 'F001-00000001', fechaEmision: '2026-10-01T05:00:00Z',
  fechaVencimiento: null, moneda: 'PEN', estado: 'Borrador', subtotal: 10, totalDescuento: 0, totalImpuesto: 1.8, total: 11.8,
  cantidadDetalles: 1 };
const detalle: FacturaDetalle = { ...fila, ordenCompraId: null, numeroOrdenCompra: null, observaciones: null,
  fechaCreacion: '2026-10-01T08:00:00Z', fechaAnulacion: null, motivoAnulacion: null, detalles: [
    { id: 1, productoId: 1, productoNombre: 'Producto A', impuestoId: 1, impuestoNombre: 'IGV',
      porcentajeImpuesto: 18, descripcion: 'Detalle A', cantidad: 1, precioUnitario: 10, descuentoPorcentaje: 0,
      subtotal: 10, importeDescuento: 0, baseImponible: 10, importeImpuesto: 1.8, total: 11.8 },
  ] };
const pagina = { items: [fila], pagina: 1, tamanoPagina: 20, totalRegistros: 21, totalPaginas: 2 };

afterEach(cleanup);

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(api.listarFacturas).mockImplementation(async (_filtros, numero) => ({ ...pagina, pagina: numero }));
  vi.mocked(api.obtenerFactura).mockResolvedValue(detalle);
  vi.mocked(api.resumenFacturas).mockResolvedValue({ moneda: 'PEN', totalComprobantes: 1, totalFacturas: 1, totalBoletas: 0,
    totalBorradores: 1, totalEmitidas: 0, totalAnuladas: 0, subtotalFacturado: 0, totalDescuentos: 0, totalImpuestos: 0, totalFacturado: 0 });
  vi.mocked(api.guardarFactura).mockResolvedValue(detalle);
  vi.mocked(api.cargarCatalogosFactura).mockResolvedValue({
    clientes: [{ id: 1, nombre: fila.clienteNombre, ruc: fila.clienteRuc, totalOrdenes: 1 }],
    productos: [{ id: 1, nombre: 'Producto A', categoria: 'Pruebas', precio: 10, tieneDescripcion: false }],
    impuestos: [{ id: 1, codigo: 'IGV', nombre: 'Impuesto histórico', activo: true }],
  });
});

function abrir() {
  render(<MemoryRouter><FacturacionElectronicaPage /></MemoryRouter>);
}

async function consultar() {
  fireEvent.click(await screen.findByRole('button', { name: fila.numeroComprobante }));
  await screen.findByText('Detalle A');
}

it('carga la lista, permite filtrar y navegar a la segunda página', async () => {
  abrir();
  expect(screen.getByText('Cargando comprobantes…')).toBeInTheDocument();
  const filaVisible = await screen.findByRole('button', { name: fila.numeroComprobante });
  expect(filaVisible.closest('tbody > tr')).not.toBeNull();
  fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Boleta' } });
  expect(api.listarFacturas).toHaveBeenLastCalledWith(expect.objectContaining({ tipoComprobante: 'Boleta' }), 1);
  fireEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));
  expect(api.listarFacturas).toHaveBeenLastCalledWith(expect.objectContaining({ tipoComprobante: 'Boleta' }), 2);
  expect(await screen.findByText('Página 2 de 2')).toBeInTheDocument();
});

it('muestra el resumen del servidor y el estado vacío', async () => {
  vi.mocked(api.listarFacturas).mockResolvedValue({ ...pagina, items: [], totalRegistros: 0, totalPaginas: 0 });
  abrir();
  expect(await screen.findByText('No hay comprobantes para estos filtros.')).toBeInTheDocument();
  expect(screen.getByLabelText('Resumen de facturación')).toHaveTextContent('1 borradores');
});

it('valida los datos y crea una boleta con fecha de Perú y una línea', async () => {
  abrir();
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo comprobante' }));
  const form = await screen.findByRole('form', { name: 'Datos del comprobante' });
  fireEvent.submit(form);
  expect(screen.getByRole('alert')).toHaveTextContent('Indica el cliente');
  const datos = within(form);
  fireEvent.change(datos.getByLabelText('Cliente'), { target: { value: '1' } });
  fireEvent.change(datos.getByLabelText('Tipo de comprobante'), { target: { value: 'Boleta' } });
  fireEvent.change(datos.getByLabelText('Serie'), { target: { value: 'B001' } });
  fireEvent.change(datos.getByLabelText('Número'), { target: { value: '1' } });
  fireEvent.change(datos.getByLabelText('Fecha de emisión'), { target: { value: '2026-10-01' } });
  fireEvent.change(datos.getByLabelText('Producto'), { target: { value: '1' } });
  fireEvent.change(datos.getByLabelText('Descripción'), { target: { value: 'Detalle B' } });
  fireEvent.change(datos.getByLabelText('Precio unitario'), { target: { value: '10' } });
  fireEvent.submit(form);
  await screen.findByText('Borrador guardado.');
  expect(api.guardarFactura).toHaveBeenCalledWith(expect.objectContaining({ clienteId: 1, tipoComprobante: 'Boleta',
    fechaEmision: '2026-10-01T05:00:00Z', detalles: [expect.objectContaining({ productoId: 1, cantidad: 1 })] }), undefined);
});

it('conserva los datos previos al editar y solo confirma la emisión en borrador', async () => {
  abrir();
  await consultar();
  fireEvent.click(screen.getByRole('button', { name: 'Editar borrador' }));
  const form = await screen.findByRole('form', { name: 'Datos del comprobante' });
  expect(within(form).getByLabelText('Producto')).toHaveValue('1');
  fireEvent.submit(form);
  await screen.findByText('Borrador guardado.');
  expect(api.guardarFactura).toHaveBeenCalledWith(expect.objectContaining({ fechaEmision: detalle.fechaEmision }), 1);
  const emitida = { ...detalle, estado: 'Emitida' as const };
  vi.mocked(api.emitirFactura).mockResolvedValue(emitida);
  vi.mocked(api.obtenerFactura).mockResolvedValue(emitida);
  fireEvent.click(await screen.findByRole('button', { name: 'Emitir comprobante' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar emisión' }));
  await screen.findByText('Comprobante emitido.');
  expect(api.emitirFactura).toHaveBeenCalledWith(1);
  await screen.findByText('Detalle A');
  expect(screen.queryByRole('button', { name: 'Editar borrador' })).not.toBeInTheDocument();
});

it('exige motivo para anular y conserva el literal 409 de la API', async () => {
  vi.mocked(api.anularFactura).mockRejectedValue(new ApiError(409, 'La factura ya se encuentra anulada.'));
  abrir();
  await consultar();
  fireEvent.click(screen.getByRole('button', { name: 'Anular comprobante' }));
  const form = screen.getByRole('form', { name: 'Confirmar anulación' });
  fireEvent.submit(form);
  expect(screen.getByRole('alert')).toHaveTextContent('El motivo de anulación es obligatorio.');
  fireEvent.change(within(form).getByLabelText('Motivo de anulación'), { target: { value: '  Duplicado  ' } });
  fireEvent.submit(form);
  expect(await screen.findByText('La factura ya se encuentra anulada.')).toBeInTheDocument();
  expect(api.anularFactura).toHaveBeenCalledWith(1, 'Duplicado');
});

it.each([400, 401, 403, 404, 409])('presenta el error HTTP %s del backend', async codigo => {
  vi.mocked(api.listarFacturas).mockRejectedValue(new ApiError(codigo, 'Literal ' + codigo));
  abrir();
  expect(await screen.findByRole('alert')).toHaveTextContent('Literal ' + codigo);
});
