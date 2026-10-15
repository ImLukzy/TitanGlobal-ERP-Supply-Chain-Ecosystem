import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  esperarCargaInicial,
  prepararApi,
  renderPagina,
} from './solicitudes.testUtils';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('SolicitudesCompra: resumen, filtros y tabla', () => {
  it('A3 presenta resumen, tabla, fechas y detalle con líneas y subtotales', async () => {
    const { fetchMock } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock);
    expect(screen.getByText('Pendientes')).toBeInTheDocument();
    expect(screen.getByText('Aprobadas')).toBeInTheDocument();
    expect(screen.getByText('Rechazadas')).toBeInTheDocument();
    expect(screen.getAllByText('Laptop Pro').length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole('button', { name: 'SC-000001' }));
    expect(await screen.findByText('Entrega para la planta principal')).toBeInTheDocument();
    expect(screen.getAllByText('2/1/2026').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Andes Suministros S.A.C.').length).toBeGreaterThan(0);
    expect(screen.getAllByText(/3,400\.00/).length).toBeGreaterThan(0);
    expect(screen.getByText('Sin proveedor')).toBeInTheDocument();
  });

  it('A3 usa temporizadores falsos para el debounce de 300 ms', async () => {
    const { fetchMock, llamadas } = prepararApi();
    renderPagina();
    await esperarCargaInicial(fetchMock, llamadas);
    vi.useFakeTimers();
    fireEvent.change(screen.getByRole('searchbox', { name: 'Buscar por número o solicitante' }), { target: { value: 'S' } });
    fireEvent.change(screen.getByRole('searchbox', { name: 'Buscar por número o solicitante' }), { target: { value: 'SC-' } });
    fireEvent.change(screen.getByRole('searchbox', { name: 'Buscar por número o solicitante' }), { target: { value: 'SC-000001' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(llamadas.filter((llamada) => llamada.url.includes('/solicitudes-compra?'))).toHaveLength(0);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    expect(llamadas.filter((llamada) => llamada.url.includes('/solicitudes-compra?'))).toHaveLength(1);
    expect(llamadas.find((llamada) => llamada.url.includes('/solicitudes-compra?'))?.url).toContain('busqueda=SC-000001');
    expect(llamadas.find((llamada) => llamada.url.includes('/solicitudes-compra?'))?.url).toContain('pagina=1');
  });

  it('A3 aplica estado, proveedor y fechas; fecha inválida no consulta y los filtros vuelven a página 1', async () => {
    const { fetchMock, llamadas } = prepararApi({ paginado: true });
    renderPagina();
    await esperarCargaInicial(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    expect(await screen.findByText('Página 2 de 2')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Pendiente' } });
    expect(await screen.findByText('Página 1 de 2')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Proveedor'), { target: { value: '2' } });
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-01-02' } });
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-01-12' } });
    await waitFor(() => {
      expect(llamadas.some((llamada) => llamada.url.includes('estado=Pendiente')
        && llamada.url.includes('proveedorId=2')
        && llamada.url.includes('fechaDesde=2026-01-02')
        && llamada.url.includes('fechaHasta=2026-01-12')
        && llamada.url.includes('pagina=1'))).toBe(true);
    });
    const cantidadAntes = llamadas.filter((llamada) => llamada.url.includes('/solicitudes-compra?')).length;
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-01-20' } });
    expect(screen.getByText('La fecha desde no puede ser posterior a la fecha hasta.')).toBeInTheDocument();
    expect(llamadas.filter((llamada) => llamada.url.includes('/solicitudes-compra?'))).toHaveLength(cantidadAntes);
  });

  it('A3 muestra los estados vacío y error de la lista', async () => {
    const vacio = prepararApi({ listaVacia: true });
    renderPagina();
    expect(await screen.findByText('No hay solicitudes que coincidan con los filtros.')).toBeInTheDocument();
    cleanup();
    vi.unstubAllGlobals();
    const error = prepararApi({ errorLista: true });
    renderPagina();
    expect(await screen.findByText('No se pudo cargar la lista de solicitudes.')).toBeInTheDocument();
    expect(vacio.fetchMock).toHaveBeenCalled();
    expect(error.fetchMock).toHaveBeenCalled();
  });
});
