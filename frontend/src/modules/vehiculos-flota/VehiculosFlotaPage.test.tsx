import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { esperarCargaVehiculos, prepararApiVehiculos, renderVehiculos, vehiculosBase } from './vehiculosFlota.testUtils';

afterEach(() => { cleanup(); vi.useRealTimers(); vi.unstubAllGlobals(); });

describe('VehiculosFlotaPage', () => {
  it('muestra capacidad total, distribución, estado y detalle del vehículo', async () => {
    const { fetchMock } = prepararApiVehiculos();
    renderVehiculos();
    await esperarCargaVehiculos(fetchMock);
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Vehículos de flota');
    expect(screen.getByText('16,200.50')).toBeInTheDocument();
    expect(screen.getByText('Sin observaciones')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'XYZ-789' }));
    await screen.findByText('En mantenimiento');
    expect(screen.getByText('1,200.50 kg')).toBeInTheDocument();
  });

  it('carga una sola vez y espera 300 ms antes de buscar', async () => {
    const { fetchMock } = prepararApiVehiculos();
    renderVehiculos();
    await esperarCargaVehiculos(fetchMock);
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/vehiculos-flota?'))).toHaveLength(1);
    fetchMock.mockClear();
    vi.useFakeTimers();
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Vol' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(150); });
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Volvo' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(299); });
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/vehiculos-flota?'))).toHaveLength(0);
    await act(async () => { await vi.advanceTimersByTimeAsync(1); });
    const llamadas = fetchMock.mock.calls.filter((call) => String(call[0]).includes('/vehiculos-flota?'));
    expect(llamadas).toHaveLength(1);
    expect(String(llamadas[0]?.[0])).toContain('busqueda=Volvo');
  });

  it('combina tipo y estado sin perder activo=false', async () => {
    const { fetchMock } = prepararApiVehiculos();
    renderVehiculos();
    await esperarCargaVehiculos(fetchMock);
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Furgon' } });
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'false' } });
    await screen.findByText('En mantenimiento');
    expect(screen.queryByRole('button', { name: 'ABC-123' })).not.toBeInTheDocument();
    const ultima = fetchMock.mock.calls.filter((call) => String(call[0]).includes('/vehiculos-flota?')).at(-1);
    expect(String(ultima?.[0])).toContain('tipo=Furgon&activo=false&pagina=1');
  });

  it('vuelve a página 1 al filtrar desde la segunda página', async () => {
    const filas = Array.from({ length: 11 }, (_, indice) => ({ ...vehiculosBase[0]!, id: indice + 10, placa: `PL-${String(indice).padStart(2, '0')}` }));
    const { fetchMock } = prepararApiVehiculos({ filas });
    renderVehiculos();
    await esperarCargaVehiculos(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await screen.findByText('Página 2 de 2');
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'true' } });
    await screen.findByText('Página 1 de 2');
  });

  it('muestra la lista vacía y los errores con reintento', async () => {
    prepararApiVehiculos({ errorLista: true });
    renderVehiculos();
    await screen.findByText('Lista no disponible.');
    prepararApiVehiculos({ filas: [] });
    fireEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
    await screen.findByText('No hay vehículos de flota que coincidan con los filtros.');
  });

  it('cancela la baja con Escape y devuelve el foco', async () => {
    const { fetchMock } = prepararApiVehiculos();
    renderVehiculos();
    await esperarCargaVehiculos(fetchMock);
    const boton = screen.getByRole('button', { name: 'Eliminar vehículo' });
    fireEvent.click(boton);
    const dialogo = screen.getByRole('dialog');
    expect(within(dialogo).getByText(/ABC-123/)).toBeInTheDocument();
    fireEvent.keyDown(dialogo, { key: 'Escape' });
    await waitFor(() => expect(boton).toHaveFocus());
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'DELETE')).toBe(false);
  });

  it('confirma la baja y refresca listado y resumen', async () => {
    const { fetchMock, filas } = prepararApiVehiculos();
    renderVehiculos();
    await esperarCargaVehiculos(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar vehículo' }));
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: /Eliminar/ }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await screen.findByText('En mantenimiento');
    expect(filas).toHaveLength(1);
    expect(screen.queryByRole('button', { name: 'ABC-123' })).not.toBeInTheDocument();
    expect(within(screen.getByRole('region', { name: 'Resumen de vehículos de flota' })).getByText('1,200.50')).toBeInTheDocument();
  });
});
