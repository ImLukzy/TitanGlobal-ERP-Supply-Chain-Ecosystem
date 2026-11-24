import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { conductoresBase, esperarCargaConductores, prepararApiConductores, renderConductores } from './conductores.testUtils';

afterEach(() => { cleanup(); vi.useRealTimers(); vi.unstubAllGlobals(); });

describe('ConductoresPage', () => {
  it('muestra las coincidencias y separa vigencia, habilitación y estado', async () => {
    prepararApiConductores();
    renderConductores();
    await esperarCargaConductores();
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('42');
    expect(screen.getByText('3 conductores en el resultado')).toBeInTheDocument();
    expect(screen.getByText('Sin observaciones')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'PLT-0002' }));
    await screen.findByRole('heading', { name: 'Maria Quispe' });
    expect(screen.getByText('Licencia vencida. La habilitación administrativa no acredita una licencia vigente.')).toBeInTheDocument();
    const panel = within(screen.getByRole('region', { name: 'Maria Quispe' }));
    expect(panel.getByText('Habilitado')).toBeInTheDocument();
    expect(panel.getByText('Activo')).toBeInTheDocument();
  });

  it('carga una sola vez y espera 300 ms tras el último cambio de búsqueda', async () => {
    const { fetchMock } = prepararApiConductores();
    renderConductores();
    await esperarCargaConductores();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/conductores?'))).toHaveLength(1);
    fetchMock.mockClear();
    vi.useFakeTimers();
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Mar' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(150); });
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Maria' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(299); });
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/conductores?'))).toHaveLength(0);
    await act(async () => { await vi.advanceTimersByTimeAsync(1); });
    const llamadas = fetchMock.mock.calls.filter((call) => String(call[0]).includes('/conductores?'));
    expect(llamadas).toHaveLength(1);
    expect(String(llamadas[0]?.[0])).toContain('busqueda=Maria');
  });

  it('filtra no habilitados con habilitado=false', async () => {
    const { fetchMock } = prepararApiConductores();
    renderConductores();
    await esperarCargaConductores();
    fireEvent.change(screen.getByLabelText('Habilitación'), { target: { value: 'false' } });
    await screen.findByRole('heading', { name: 'Jose Flores' });
    expect(screen.getByText('1 conductor en el resultado')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'PLT-0001' })).not.toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => String(call[0]).includes('habilitado=false'))).toBe(true);
  });

  it('vuelve a página 1 al cambiar habilitación desde la segunda página', async () => {
    const filas = Array.from({ length: 11 }, (_, indice) => ({ ...conductoresBase[0]!, id: indice + 10, documentoLicencia: `PLT-${String(indice).padStart(3, '0')}` }));
    prepararApiConductores({ filas });
    renderConductores();
    await esperarCargaConductores();
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await screen.findByText('Página 2 de 2');
    fireEvent.change(screen.getByLabelText('Habilitación'), { target: { value: 'true' } });
    await screen.findByText('Página 1 de 2');
  });

  it('muestra error recuperable y lista vacía', async () => {
    prepararApiConductores({ errorLista: true });
    renderConductores();
    await screen.findByText('Lista no disponible.');
    prepararApiConductores({ filas: [] });
    fireEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
    await screen.findByText('No hay conductores que coincidan con los filtros.');
  });

  it('cancela la baja con Escape y devuelve el foco', async () => {
    const { fetchMock } = prepararApiConductores();
    renderConductores();
    await esperarCargaConductores();
    const boton = screen.getByRole('button', { name: 'Eliminar conductor' });
    fireEvent.click(boton);
    const dialogo = screen.getByRole('dialog');
    expect(within(dialogo).getByText(/Carlos Ruiz/)).toBeInTheDocument();
    fireEvent.keyDown(dialogo, { key: 'Escape' });
    await waitFor(() => expect(boton).toHaveFocus());
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'DELETE')).toBe(false);
  });

  it('elimina y refresca el total y la selección', async () => {
    const { filas } = prepararApiConductores();
    renderConductores();
    await esperarCargaConductores();
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar conductor' }));
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar conductor' }));
    await screen.findByText('2 conductores en el resultado');
    await screen.findByRole('heading', { name: 'Maria Quispe' });
    expect(filas).toHaveLength(2);
    expect(screen.queryByRole('button', { name: 'PLT-0001' })).not.toBeInTheDocument();
  });

  it('conserva el diálogo con el mensaje literal si la baja falla', async () => {
    prepararApiConductores({ errorEliminar: true });
    renderConductores();
    await esperarCargaConductores();
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar conductor' }));
    const dialogo = within(screen.getByRole('dialog'));
    fireEvent.click(dialogo.getByRole('button', { name: 'Eliminar conductor' }));
    await dialogo.findByText('El conductor 1 no existe.');
    expect(dialogo.getByRole('button', { name: 'Eliminar conductor' })).toBeEnabled();
  });

  it('ignora un detalle antiguo que termina después de cambiar la selección', async () => {
    const { fetchMock } = prepararApiConductores();
    const responder = fetchMock.getMockImplementation()!;
    let resolver: (respuesta: Response) => void = () => { throw new Error('Detalle no solicitado'); };
    fetchMock.mockImplementation(async (input, init) => {
      if (String(input) === '/api/v1/conductores/1') { return new Promise<Response>((resolve) => { resolver = resolve; }); }
      return responder(input, init);
    });
    renderConductores();
    await screen.findByRole('button', { name: 'PLT-0002' });
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/conductores/1'))).toBe(true));
    fireEvent.click(screen.getByRole('button', { name: 'PLT-0002' }));
    await screen.findByRole('heading', { name: 'Maria Quispe' });
    await act(async () => { resolver(Response.json(conductoresBase[0])); });
    expect(screen.getByRole('heading', { name: 'Maria Quispe' })).toBeInTheDocument();
  });
});
