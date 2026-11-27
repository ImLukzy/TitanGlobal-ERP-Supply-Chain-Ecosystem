import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AcuerdosAbastecimientoPage } from './AcuerdosAbastecimientoPage';
import { cambiarValor, esperarCargaInicial, llamadasMetodo, prepararApi, renderEnRouter, seleccionar } from './acuerdos.testUtils';

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date('2026-11-15T12:00:00'));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

async function abrirNuevo(): Promise<void> {
  await esperarCargaInicial();
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo acuerdo' }));
  await screen.findByRole('heading', { name: 'Nuevo acuerdo' });
}

function llenarAcuerdo(numero: string): void {
  cambiarValor('Número', numero);
  fireEvent.change(screen.getByLabelText('Proveedor', { selector: '#acuerdo-proveedor' }), { target: { value: '2' } });
  cambiarValor('Fecha de inicio', '2026-12-01');
  cambiarValor('Fecha de fin', '2027-12-01');
  cambiarValor('Monto máximo', '15000.50');
}

describe('pantalla de acuerdos: formularios', () => {
  it('A5 valida el acuerdo con sus literales y no envía nada', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await abrirNuevo();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar acuerdo' }));
    expect(await screen.findByText('El número es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El proveedor es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El monto máximo debe ser mayor a 0.')).toBeInTheDocument();
    expect(screen.getByText('La fecha de fin debe ser posterior a la de inicio.')).toBeInTheDocument();
    expect(screen.getByLabelText('Número')).toHaveAttribute('aria-invalid', 'true');
    cambiarValor('Fecha de inicio', '2026-01-01');
    cambiarValor('Fecha de fin', '2026-11-01');
    cambiarValor('Monto máximo', '10.123');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar acuerdo' }));
    expect(await screen.findByText('La fecha de fin debe ser futura.')).toBeInTheDocument();
    expect(screen.getByText('El monto máximo no es válido.')).toBeInTheDocument();
    expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(0);
  });

  it('A5 crea el acuerdo con número en mayúsculas y fechas ISO', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await abrirNuevo();
    llenarAcuerdo('am-2026-010');
    expect(screen.getByLabelText('Número')).toHaveValue('AM-2026-010');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar acuerdo' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(1));
    const [url, init] = llamadasMetodo(fetchMock, 'POST')[0] ?? ['', undefined];
    expect(url).toBe('/api/v1/acuerdos-abastecimiento');
    expect(JSON.parse(String(init?.body))).toEqual({
      numero: 'AM-2026-010', proveedorId: 2, montoMaximo: 15000.5,
      fechaInicio: new Date('2026-12-01T00:00:00').toISOString(), fechaFin: new Date('2027-12-01T23:59:59').toISOString(),
    });
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nuevo acuerdo' })).not.toBeInTheDocument());
  });

  it('A5 un 409 al crear se muestra y conserva lo escrito', async () => {
    prepararApi({ errorCrear: true });
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await abrirNuevo();
    llenarAcuerdo('AM-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar acuerdo' }));
    expect(await screen.findByText('Ya existe un acuerdo con el número AM-2026-001.')).toBeInTheDocument();
    expect(screen.getByLabelText('Monto máximo')).toHaveValue('15000.50');
  });

  it('A5 editar deja número y proveedor de solo lectura y hace PUT con su id', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    await screen.findByRole('heading', { name: 'Editar acuerdo AM-2026-001' });
    expect(screen.queryByLabelText('Número')).not.toBeInTheDocument();
    cambiarValor('Monto máximo', '120000');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar acuerdo' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'PUT')).toHaveLength(1));
    const [url, init] = llamadasMetodo(fetchMock, 'PUT')[0] ?? ['', undefined];
    expect(url).toBe('/api/v1/acuerdos-abastecimiento/1');
    expect(Object.keys(JSON.parse(String(init?.body))).sort()).toEqual(['descripcion', 'fechaFin', 'fechaInicio', 'montoMaximo']);
    expect(JSON.parse(String(init?.body)).montoMaximo).toBe(120000);
  });

  it('A5 la línea deshabilita productos repetidos, valida y muestra la vista previa', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Agregar línea' }));
    await screen.findByRole('heading', { name: 'Agregar línea a AM-2026-001' });
    expect(screen.getByRole('option', { name: 'Cobre refinado (ya en el acuerdo)' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar línea' }));
    expect(await screen.findByText('El producto es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El precio acordado debe ser mayor a 0.')).toBeInTheDocument();
    expect(screen.getByText('La cantidad comprometida debe ser mayor a 0.')).toBeInTheDocument();
    cambiarValor('Producto', '3');
    cambiarValor('Precio acordado', '100');
    cambiarValor('Cantidad comprometida', '2000000');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar línea' }));
    expect(await screen.findByText('La cantidad comprometida no puede superar 1000000.')).toBeInTheDocument();
    cambiarValor('Cantidad comprometida', '10');
    expect(screen.getByText(/Compromiso tras agregar: .*91,000\.00 de .*100,000\.00/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar línea' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(1));
    const [url, init] = llamadasMetodo(fetchMock, 'POST')[0] ?? ['', undefined];
    expect(url).toBe('/api/v1/acuerdos-abastecimiento/1/lineas');
    expect(JSON.parse(String(init?.body))).toEqual({ productoId: 3, precioAcordado: 100, cantidadComprometida: 10 });
  });

  it('A5 un 400 de la API al agregar línea se muestra sin perder lo escrito', async () => {
    prepararApi({ errorLinea: true });
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Agregar línea' }));
    await screen.findByRole('heading', { name: 'Agregar línea a AM-2026-001' });
    cambiarValor('Producto', '3');
    cambiarValor('Precio acordado', '100');
    cambiarValor('Cantidad comprometida', '10');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar línea' }));
    expect(await screen.findByText('El compromiso de las líneas supera el monto máximo del acuerdo.')).toBeInTheDocument();
    expect(screen.getByLabelText('Precio acordado')).toHaveValue('100');
  });
});

describe('pantalla de acuerdos: consumo y cambios de estado', () => {
  it('A6 registra consumo y rechaza localmente una cantidad mayor al saldo', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Registrar consumo de Cobre refinado' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Registrar consumo' });
    expect(dialogo).toHaveTextContent('Saldo disponible: 600');
    cambiarValor('Cantidad a consumir', '700');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar consumo' }));
    expect(await screen.findByText('La cantidad supera el saldo de la línea (600).')).toBeInTheDocument();
    expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(0);
    cambiarValor('Cantidad a consumir', '100');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar consumo' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(1));
    const [url, init] = llamadasMetodo(fetchMock, 'POST')[0] ?? ['', undefined];
    expect(url).toBe('/api/v1/acuerdos-abastecimiento/1/lineas/1/consumos');
    expect(JSON.parse(String(init?.body))).toEqual({ cantidad: 100 });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('A6 un 409 del consumo se muestra dentro del diálogo', async () => {
    prepararApi({ errorAccion: true });
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Registrar consumo de Cobre refinado' }));
    await screen.findByRole('dialog', { name: 'Registrar consumo' });
    cambiarValor('Cantidad a consumir', '10');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar consumo' }));
    expect(await screen.findByText('El acuerdo 1 no está vigente.')).toBeInTheDocument();
    expect(screen.getByRole('dialog', { name: 'Registrar consumo' })).toBeInTheDocument();
  });

  it('A6 suspender pide confirmación; Escape cancela y devuelve el foco', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-001');
    const abrir = screen.getByRole('button', { name: 'Suspender' });
    fireEvent.click(abrir);
    await screen.findByRole('dialog', { name: 'Suspender acuerdo' });
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(abrir).toHaveFocus());
    expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(0);
    fireEvent.click(abrir);
    fireEvent.click(await screen.findByRole('button', { name: 'Suspender acuerdo' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'POST').map((c) => c[0])).toEqual(['/api/v1/acuerdos-abastecimiento/1/suspender']));
  });

  it('A6 reactivar un acuerdo suspendido', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-003');
    fireEvent.click(screen.getByRole('button', { name: 'Reactivar' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Reactivar acuerdo' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'POST').map((c) => c[0])).toEqual(['/api/v1/acuerdos-abastecimiento/3/reactivar']));
  });

  it('A6 un 409 al cancelar se muestra en el diálogo y no lo cierra', async () => {
    prepararApi({ errorAccion: true });
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar' }));
    await screen.findByRole('dialog', { name: 'Cancelar acuerdo' });
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar acuerdo' }));
    expect(await screen.findByText('El acuerdo 1 no está vigente.')).toBeInTheDocument();
    expect(screen.getByRole('dialog', { name: 'Cancelar acuerdo' })).toBeInTheDocument();
  });

  it('A6 quita una línea sin consumo con confirmación', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Quitar línea de Lingote de zinc' }));
    await screen.findByRole('dialog', { name: 'Quitar línea' });
    fireEvent.click(screen.getByRole('button', { name: 'Quitar línea' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'DELETE').map((c) => c[0])).toEqual(['/api/v1/acuerdos-abastecimiento/1/lineas/2']));
  });

  it('A6 elimina con DialogoBaja y muestra el error del backend si lo hay', async () => {
    const { fetchMock } = prepararApi({ errorBaja: true });
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-005');
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    expect(await screen.findByRole('heading', { name: 'Eliminar acuerdo' })).toBeInTheDocument();
    expect(screen.getByText('AM-2026-005 · Andes Suministros S.A.C.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar acuerdo' }));
    expect(await screen.findByText('El acuerdo 5 tiene consumos y no se puede eliminar; cancélelo.')).toBeInTheDocument();
    expect(llamadasMetodo(fetchMock, 'DELETE')).toHaveLength(1);
  });
});
