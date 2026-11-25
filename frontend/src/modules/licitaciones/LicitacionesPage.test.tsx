import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { LicitacionesPage } from './LicitacionesPage';
import { esperarCargaInicial, llamadasLista, prepararApi, renderEnRouter, seleccionar } from './licitaciones.testUtils';

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date('2026-11-15T12:00:00'));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('pantalla de licitaciones: resumen, filtros y tabla', () => {
  it('A3 muestra el resumen con montos y el conteo por estado en orden fijo', async () => {
    prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    const resumen = screen.getByRole('region', { name: 'Resumen de licitaciones' });
    expect(within(resumen).getByText('Licitaciones').nextElementSibling).toHaveTextContent('5');
    expect(within(resumen).getByText('Ofertas').nextElementSibling).toHaveTextContent('7');
    expect(within(resumen).getByText('Monto adjudicado').nextElementSibling).toHaveTextContent('28,000.00');
    const items = within(within(resumen).getByRole('list', { name: 'Licitaciones por estado' })).getAllByRole('listitem');
    expect(items.map((item) => item.textContent)).toEqual(['Abierta2', 'Cerrada1', 'Adjudicada1', 'Cancelada1']);
  });

  it('A3 la tabla marca «Plazo vencido» solo en la abierta vencida', async () => {
    prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    const filas = screen.getAllByRole('row');
    expect(filas.filter((fila) => within(fila).queryByText('Plazo vencido') !== null)).toHaveLength(1);
    expect(within(screen.getByRole('button', { name: 'LIC-2026-006' }).closest('tr') as HTMLElement).getByText('Plazo vencido')).toBeInTheDocument();
  });

  it('A3 cambiar estado y tipo filtra de inmediato y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Cerrada' } });
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'SubastaInversa' } });
    await waitFor(() => expect(llamadasLista(fetchMock).at(-1)).toBe('/api/v1/licitaciones?pagina=1&tamanoPagina=10&estado=Cerrada&tipo=SubastaInversa'));
  });

  it('A3 la búsqueda espera 300 ms antes de consultar', async () => {
    vi.useRealTimers();
    vi.useFakeTimers({ toFake: ['Date', 'setTimeout', 'clearTimeout'] });
    const { fetchMock } = prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await vi.waitFor(() => expect(llamadasLista(fetchMock)).toHaveLength(1));
    fireEvent.change(screen.getByLabelText('Buscar'), { target: { value: 'cobre' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(299); });
    expect(llamadasLista(fetchMock)).toHaveLength(1);
    await act(async () => { await vi.advanceTimersByTimeAsync(1); });
    await vi.waitFor(() => expect(llamadasLista(fetchMock).at(-1)).toContain('busqueda=cobre'));
  });

  it('A3 muestra estado vacío y error de lista', async () => {
    prepararApi({ vacio: true });
    const primera = renderEnRouter(<LicitacionesPage />);
    expect(await screen.findByText('No hay licitaciones para mostrar.')).toBeInTheDocument();
    primera.unmount();
    prepararApi({ errorLista: true });
    renderEnRouter(<LicitacionesPage />);
    expect(await screen.findByText('No se pudieron cargar las licitaciones.')).toBeInTheDocument();
  });
});

describe('pantalla de licitaciones: detalle y acciones por estado', () => {
  it('A4 abierta sin ofertas: Editar, Presentar oferta, Cerrar, Cancelar y Eliminar', async () => {
    prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-001');
    expect(screen.getByText('Compra de cobre refinado.')).toBeInTheDocument();
    expect(screen.getByText('Aún no hay ofertas.')).toBeInTheDocument();
    for (const nombre of ['Editar', 'Presentar oferta', 'Cerrar', 'Cancelar', 'Eliminar']) {
      expect(screen.getByRole('button', { name: nombre })).toBeInTheDocument();
    }
  });

  it('A4 subasta abierta con ofertas: mejora por oferta y sin Eliminar', async () => {
    prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-002');
    expect(screen.getByRole('button', { name: 'Mejorar oferta de Andes Suministros S.A.C.' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Mejorar oferta de Cobre Austral SpA' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Eliminar' })).not.toBeInTheDocument();
    expect(screen.getByText('Entrega en almacén central.')).toBeInTheDocument();
    expect(screen.getByText('Sin observaciones')).toBeInTheDocument();
  });

  it('A4 cerrada ofrece Adjudicar y Cancelar; adjudicada y cancelada son de solo lectura', async () => {
    prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-004');
    expect(screen.getByRole('button', { name: 'Adjudicar' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument();
    await seleccionar('LIC-2026-003');
    expect(screen.getByText('Ganadora')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Cancelar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Adjudicar' })).not.toBeInTheDocument();
    await seleccionar('LIC-2026-005');
    expect(screen.queryByRole('button', { name: 'Eliminar' })).not.toBeInTheDocument();
  });

  it('A4 abierta con plazo vencido no permite ofertar pero sí cerrar y cancelar', async () => {
    prepararApi();
    renderEnRouter(<LicitacionesPage />);
    await esperarCargaInicial();
    await seleccionar('LIC-2026-006');
    expect(screen.getAllByText('Plazo vencido').length).toBeGreaterThan(0);
    expect(screen.queryByRole('button', { name: 'Presentar oferta' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Cerrar' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Cancelar' })).toBeInTheDocument();
  });
});
