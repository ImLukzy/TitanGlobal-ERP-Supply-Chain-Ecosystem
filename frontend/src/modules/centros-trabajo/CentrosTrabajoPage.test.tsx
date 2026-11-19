import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { formateadorMoneda } from '../../lib/formato';
import { centrosBase, esperarCargaCentros, prepararApiCentros, renderCentros } from './centrosTrabajo.testUtils';

afterEach(() => { cleanup(); vi.useRealTimers(); vi.unstubAllGlobals(); });

describe('CentrosTrabajoPage', () => {
  it('muestra el resumen exacto del seed, detalle en soles y estados', async () => {
    const { fetchMock } = prepararApiCentros();
    renderCentros();
    await esperarCargaCentros(fetchMock);
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('33');
    expect(screen.getByText('44.00')).toBeInTheDocument();
    expect(screen.getByText('48.00 h/día')).toBeInTheDocument();
    expect(screen.getByText('20.00 h/día')).toBeInTheDocument();
    expect(screen.getByText('Sin responsable')).toBeInTheDocument();
    expect(screen.getByText(formateadorMoneda.format(60), { normalizer: (texto) => texto })).toBeInTheDocument();
    expect(screen.getAllByText('Inactivo')).toHaveLength(1);
  });

  it('carga una sola vez el listado y espera exactamente 300 ms al buscar', async () => {
    vi.useFakeTimers();
    const { fetchMock } = prepararApiCentros();
    renderCentros();
    await act(async () => { await vi.advanceTimersByTimeAsync(301); });
    vi.useRealTimers();
    await esperarCargaCentros(fetchMock);
    fetchMock.mockClear();
    vi.useFakeTimers();
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Env' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(150); });
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Envasado' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(299); });
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/centros-trabajo?'))).toHaveLength(0);
    await act(async () => { await vi.advanceTimersByTimeAsync(1); });
    const listados = fetchMock.mock.calls.filter((call) => String(call[0]).includes('/centros-trabajo?'));
    expect(listados).toHaveLength(1);
    expect(String(listados[0]?.[0])).toContain('busqueda=Envasado');
  });

  it('combina filtros de planta, tipo y estado', async () => {
    const { fetchMock } = prepararApiCentros();
    renderCentros();
    await esperarCargaCentros(fetchMock);
    fireEvent.change(screen.getByLabelText('Planta'), { target: { value: 'Planta Arequipa' } });
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Linea' } });
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'false' } });
    await screen.findByRole('heading', { name: 'Línea de mezclado' });
    const llamada = fetchMock.mock.calls.filter((call) => String(call[0]).includes('/centros-trabajo?')).at(-1);
    const consulta = new URL(String(llamada?.[0]), 'http://localhost').searchParams;
    expect(Object.fromEntries(consulta)).toMatchObject({ planta: 'Planta Arequipa', tipo: 'Linea', activo: 'false', pagina: '1' });
  });

  it.each(['Planta', 'Tipo', 'Estado', 'búsqueda'])('vuelve a página 1 al cambiar %s desde página 2', async (filtro) => {
    const filas = Array.from({ length: 12 }, (_, indice) => ({ ...centrosBase[0]!, id: indice + 10, codigo: `CT-${String(indice).padStart(2, '0')}` }));
    const { fetchMock } = prepararApiCentros({ filas });
    renderCentros();
    await screen.findByRole('button', { name: 'CT-00' });
    await screen.findByRole('button', { name: 'Editar centro de trabajo' });
    await screen.findByRole('option', { name: 'Planta Lima' });
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await screen.findByText('Página 2 de 2');
    await screen.findByRole('heading', { name: 'Línea de envasado 1' });
    if (filtro === 'búsqueda') {
      vi.useFakeTimers();
      fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'CT' } });
      await act(async () => { await vi.advanceTimersByTimeAsync(300); });
      vi.useRealTimers();
    } else {
      const valor = filtro === 'Planta' ? 'Planta Lima' : filtro === 'Tipo' ? 'Linea' : 'true';
      fireEvent.change(screen.getByLabelText(filtro), { target: { value: valor } });
    }
    await screen.findByText('Página 1 de 2');
    expect(String(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/centros-trabajo?')).at(-1)?.[0])).toContain('pagina=1');
  });

  it('muestra carga y listado vacío', async () => {
    prepararApiCentros({ filas: [] });
    renderCentros();
    expect(screen.getByText('Cargando centros de trabajo…')).toBeInTheDocument();
    expect(await screen.findByText('No hay centros de trabajo que coincidan con los filtros.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Eliminar centro de trabajo' })).not.toBeInTheDocument();
  });

  it.each([
    ['errorLista', 'No se pudo cargar la lista de centros de trabajo.'],
    ['errorResumen', 'No se pudo cargar el resumen de centros de trabajo.'],
    ['errorPlantas', 'No se pudieron cargar las plantas.'],
    ['errorDetalle', 'El centro de trabajo 5 no existe.'],
  ] as const)('muestra %s con el mensaje de API', async (opcion, mensaje) => {
    prepararApiCentros({ [opcion]: true });
    renderCentros();
    expect(await screen.findByText(mensaje)).toHaveAttribute('role', 'alert');
  });

  it('elimina con confirmación y refresca listado, resumen y plantas', async () => {
    const { fetchMock, filas } = prepararApiCentros();
    renderCentros();
    await esperarCargaCentros(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar centro de trabajo' }));
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar centro de trabajo' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await screen.findByText('56.00');
    expect(filas.some((centro) => centro.id === 5)).toBe(false);
    expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/5') && call[1]?.method === 'DELETE')).toBe(true);
    for (const ruta of ['/centros-trabajo?', '/centros-trabajo/resumen', '/centros-trabajo/plantas']) {
      expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes(ruta))).toHaveLength(2);
    }
  });

  it('mantiene el diálogo abierto si la baja falla', async () => {
    const { fetchMock } = prepararApiCentros({ errorEliminar: true });
    renderCentros();
    await esperarCargaCentros(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar centro de trabajo' }));
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar centro de trabajo' }));
    expect(await screen.findByText('El centro de trabajo 5 no existe.')).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('Escape cancela sin DELETE y devuelve el foco', async () => {
    const { fetchMock } = prepararApiCentros();
    renderCentros();
    await esperarCargaCentros(fetchMock);
    const boton = screen.getByRole('button', { name: 'Eliminar centro de trabajo' });
    fireEvent.click(boton);
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'DELETE')).toBe(false);
    await waitFor(() => expect(boton).toHaveFocus());
  });
});
