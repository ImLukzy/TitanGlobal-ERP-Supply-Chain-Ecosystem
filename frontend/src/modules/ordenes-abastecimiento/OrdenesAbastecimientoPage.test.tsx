import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { OrdenesAbastecimientoPage } from './OrdenesAbastecimientoPage';
import { cambiarValor, esperarCargaInicial, prepararApi, renderEnRouter } from './ordenes.testUtils';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('pantalla de órdenes de abastecimiento: filtros y tabla', () => {
  it('A3 busca tras 300 ms con temporizadores falsos y solo consulta la última escritura', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<OrdenesAbastecimientoPage />);
    await esperarCargaInicial();
    fetchMock.mockClear();
    vi.useFakeTimers();
    const busqueda = screen.getByRole('textbox', { name: 'Buscar' });
    fireEvent.change(busqueda, { target: { value: 'A' } });
    fireEvent.change(busqueda, { target: { value: 'An' } });
    fireEvent.change(busqueda, { target: { value: 'Andes' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(fetchMock).not.toHaveBeenCalled();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/ordenes-abastecimiento?'))).toHaveLength(1);
    expect(String(fetchMock.mock.calls[0]?.[0])).toContain('busqueda=Andes');
    expect(String(fetchMock.mock.calls[0]?.[0])).toContain('pagina=1');
  });

  it('A3 envía filtros de estado, proveedor y fechas; un rango inválido no consulta', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<OrdenesAbastecimientoPage />);
    await esperarCargaInicial();
    fetchMock.mockClear();
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Emitida' } });
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
    fireEvent.change(screen.getByLabelText('Proveedor'), { target: { value: '1' } });
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
    cambiarValor('Fecha desde', '2026-10-10');
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3));
    cambiarValor('Fecha hasta', '2026-10-14');
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(4));
    const query = String(fetchMock.mock.calls[3]?.[0]);
    expect(query).toContain('estado=Emitida');
    expect(query).toContain('proveedorId=1');
    expect(query).toContain('fechaDesde=2026-10-10');
    expect(query).toContain('fechaHasta=2026-10-14');
    cambiarValor('Fecha desde', '2026-10-20');
    expect(screen.getByText('La fecha desde no puede ser posterior a la fecha hasta.')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(4);
  });

  it('A3 vuelve a página 1 cuando cambia otro filtro', async () => {
    const { fetchMock } = prepararApi({ totalFilas: 21 });
    renderEnRouter(<OrdenesAbastecimientoPage />);
    await esperarCargaInicial();
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    expect(await screen.findByText('Página 2 de 3')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Emitida' } });
    expect(await screen.findByText('Página 1 de 3')).toBeInTheDocument();
    const consulta = String(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/ordenes-abastecimiento?')).at(-1)?.[0]);
    expect(consulta).toContain('pagina=1');
    expect(consulta).toContain('estado=Emitida');
  });

  it('A3 abre ?orden=2, muestra la etiqueta Atrasada y la tabla identifica origen manual', async () => {
    prepararApi();
    renderEnRouter(<OrdenesAbastecimientoPage />, ['/modulos/15-ordenes-abastecimiento?orden=2']);
    expect(await screen.findByRole('heading', { name: 'OA-000002' })).toBeInTheDocument();
    expect(screen.getAllByText('Atrasada').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Manual').length).toBeGreaterThan(0);
    expect(screen.getByRole('button', { name: 'Marcar recibida' })).toBeInTheDocument();
  });

  it('A3 presenta estados vacío y error de listado', async () => {
    prepararApi({ vacio: true });
    renderEnRouter(<OrdenesAbastecimientoPage />);
    expect(await screen.findByText('No hay órdenes de abastecimiento para mostrar.')).toBeInTheDocument();
    cleanup();
    prepararApi({ errorLista: true });
    renderEnRouter(<OrdenesAbastecimientoPage />);
    expect(await screen.findByText('No se pudieron cargar las órdenes.')).toHaveAttribute('role', 'alert');
  });
});
