import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { LicitacionesPage } from './LicitacionesPage';
import { cambiarValor, esperarCargaInicial, llamadasMetodo, prepararApi, renderEnRouter, seleccionar } from './licitaciones.testUtils';

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date('2026-11-15T12:00:00'));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

async function abrirNueva(): Promise<void> {
  await esperarCargaInicial();
  fireEvent.click(screen.getByRole('button', { name: 'Nueva licitación' }));
  await screen.findByRole('heading', { name: 'Nueva licitación' });
}

describe('pantalla de licitaciones: formularios', () => {
  it('A5 valida la licitación con sus literales y no envía nada', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await abrirNueva();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar licitación' }));
    expect(await screen.findByText('El número es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El título es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El presupuesto referencial debe ser mayor a 0.')).toBeInTheDocument();
    expect(screen.getByText('La fecha de cierre debe ser futura.')).toBeInTheDocument();
    expect(screen.getByLabelText('Número')).toHaveAttribute('aria-invalid', 'true');
    expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(0);
  });

  it('A5 crea la licitación con número en mayúsculas y fecha ISO', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await abrirNueva();
    cambiarValor('Número', 'lic-2026-010');
    expect(screen.getByLabelText('Número')).toHaveValue('LIC-2026-010');
    cambiarValor('Título', '  Insumos de oficina ');
    fireEvent.change(screen.getByLabelText('Tipo', { selector: '#licitacion-tipo' }), { target: { value: 'SubastaInversa' } });
    cambiarValor('Presupuesto referencial', '15000.50');
    cambiarValor('Fecha de cierre', '2099-01-10T10:00');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar licitación' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(1));
    const [url, init] = llamadasMetodo(fetchMock, 'POST')[0] ?? ['', undefined];
    expect(url).toBe('/api/v1/licitaciones');
    expect(JSON.parse(String(init?.body))).toEqual({
      numero: 'LIC-2026-010', titulo: 'Insumos de oficina', tipo: 'SubastaInversa', presupuestoReferencial: 15000.5,
      fechaCierre: new Date('2099-01-10T10:00').toISOString(),
    });
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nueva licitación' })).not.toBeInTheDocument());
  });

  it('A5 un 409 al crear se muestra y conserva lo escrito', async () => {
    prepararApi({ errorCrear: true });
    renderEnRouter(<LicitacionesPage />);
    await abrirNueva();
    cambiarValor('Número', 'LIC-2026-001');
    cambiarValor('Título', 'Duplicada');
    cambiarValor('Presupuesto referencial', '100');
    cambiarValor('Fecha de cierre', '2099-01-10T10:00');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar licitación' }));
    expect(await screen.findByText('Ya existe una licitación con el número LIC-2026-001.')).toBeInTheDocument();
    expect(screen.getByLabelText('Título')).toHaveValue('Duplicada');
  });

  it('A5 editar deja número y tipo de solo lectura y envía solo los campos editables', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    await screen.findByRole('heading', { name: 'Editar licitación LIC-2026-001' });
    expect(screen.queryByLabelText('Número')).not.toBeInTheDocument();
    cambiarValor('Título', 'Cobre refinado grado A');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar licitación' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'PUT')).toHaveLength(1));
    const [url, init] = llamadasMetodo(fetchMock, 'PUT')[0] ?? ['', undefined];
    expect(url).toBe('/api/v1/licitaciones/1');
    expect(Object.keys(JSON.parse(String(init?.body))).sort()).toEqual(['descripcion', 'fechaCierre', 'presupuestoReferencial', 'titulo']);
  });

  it('A5 presenta una oferta validando el monto contra el presupuesto', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Presentar oferta' }));
    await screen.findByRole('heading', { name: 'Presentar oferta a LIC-2026-001' });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar oferta' }));
    expect(await screen.findByText('El proveedor es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El plazo de entrega debe estar entre 1 y 365 días.')).toBeInTheDocument();
    cambiarValor('Proveedor', '2');
    cambiarValor('Monto', '40000');
    cambiarValor('Plazo de entrega (días)', '10');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar oferta' }));
    expect(await screen.findByText('El monto no puede superar el presupuesto referencial.')).toBeInTheDocument();
    cambiarValor('Monto', '29000');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar oferta' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(1));
    const [url, init] = llamadasMetodo(fetchMock, 'POST')[0] ?? ['', undefined];
    expect(url).toBe('/api/v1/licitaciones/1/ofertas');
    expect(JSON.parse(String(init?.body))).toEqual({ proveedorId: 2, monto: 29000, plazoEntregaDias: 10 });
  });
});

describe('pantalla de licitaciones: cerrar, mejorar, adjudicar, cancelar y eliminar', () => {
  it('A6 mejora una oferta solo si el nuevo monto es menor al actual', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-002');
    fireEvent.click(screen.getByRole('button', { name: 'Mejorar oferta de Andes Suministros S.A.C.' }));
    await screen.findByRole('dialog', { name: 'Mejorar oferta' });
    cambiarValor('Nuevo monto', '25000');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar mejora' }));
    expect(await screen.findByText('La nueva oferta debe ser menor a la actual.')).toBeInTheDocument();
    cambiarValor('Nuevo monto', '24000');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar mejora' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'PUT')).toHaveLength(1));
    const [url, init] = llamadasMetodo(fetchMock, 'PUT')[0] ?? ['', undefined];
    expect(url).toBe('/api/v1/licitaciones/2/ofertas/1');
    expect(JSON.parse(String(init?.body))).toEqual({ monto: 24000 });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('A6 cerrar pide confirmación; Escape cancela y devuelve el foco al botón', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-001');
    const abrir = screen.getByRole('button', { name: 'Cerrar' });
    fireEvent.click(abrir);
    await screen.findByRole('dialog', { name: 'Cerrar licitación' });
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(abrir).toHaveFocus());
    expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(0);
    fireEvent.click(abrir);
    fireEvent.click(await screen.findByRole('button', { name: 'Cerrar licitación' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'POST').map((c) => c[0])).toEqual(['/api/v1/licitaciones/1/cerrar']));
  });

  it('A6 adjudicar propone la oferta menor y permite elegir otra', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-004');
    fireEvent.click(screen.getByRole('button', { name: 'Adjudicar' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Adjudicar licitación' });
    expect(dialogo).toHaveTextContent('puedes elegir otra');
    expect(screen.getByRole('radio', { name: /Cobre Austral SpA/ })).toBeChecked();
    fireEvent.click(screen.getByRole('radio', { name: /Andes Suministros S.A.C./ }));
    fireEvent.click(screen.getByRole('button', { name: 'Adjudicar licitación' }));
    await waitFor(() => expect(llamadasMetodo(fetchMock, 'POST')).toHaveLength(1));
    const [url, init] = llamadasMetodo(fetchMock, 'POST')[0] ?? ['', undefined];
    expect(url).toBe('/api/v1/licitaciones/4/adjudicar');
    expect(JSON.parse(String(init?.body))).toEqual({ ofertaId: 4 });
  });

  it('A6 un 409 de la acción se muestra en el diálogo y no lo cierra', async () => {
    prepararApi({ errorAccion: true });
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar' }));
    await screen.findByRole('dialog', { name: 'Cancelar licitación' });
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar licitación' }));
    expect(await screen.findByText('La licitación 2 no está abierta.')).toBeInTheDocument();
    expect(screen.getByRole('dialog', { name: 'Cancelar licitación' })).toBeInTheDocument();
  });

  it('A6 elimina con DialogoBaja y muestra el error del backend si lo hay', async () => {
    const { fetchMock } = prepararApi({ errorBaja: true });
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    expect(await screen.findByRole('heading', { name: 'Eliminar licitación' })).toBeInTheDocument();
    expect(screen.getByText('LIC-2026-001 · Cobre refinado')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar licitación' }));
    expect(await screen.findByText('La licitación 1 tiene ofertas y no se puede eliminar; cancélela.')).toBeInTheDocument();
    expect(llamadasMetodo(fetchMock, 'DELETE')).toHaveLength(1);
  });
});
