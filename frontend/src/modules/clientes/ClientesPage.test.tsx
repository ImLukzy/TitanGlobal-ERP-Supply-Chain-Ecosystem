import { act, cleanup, fireEvent, render as renderBase, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import type { ReactNode } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ClientesPage } from './ClientesPage';

function render(ui: ReactNode) {
  return renderBase(<MemoryRouter>{ui}</MemoryRouter>);
}

interface ClientePrueba {
  id: number;
  nombre: string;
  ruc: string;
  totalOrdenes: number;
}

function cliente(id: number, nombre: string, totalOrdenes: number): ClientePrueba {
  const rucs = ['20111111111', '20222222222', '20333333333', '20444444444', '20555555555'];
  return { id, nombre, ruc: rucs[id - 1] ?? '20666666666', totalOrdenes };
}

const clientes: ClientePrueba[] = [
  cliente(1, 'Acme Industrial', 4),
  cliente(2, 'Beta Comercio', 2),
  cliente(3, 'Gamma Manufactura', 1),
  cliente(4, 'Delta Logística', 1),
  cliente(5, 'Cliente sin órdenes', 0),
];

function detalle(id: number): Record<string, unknown> {
  const seleccionado = clientes.find((item) => item.id === id) ?? clientes[0];
  return {
    ...seleccionado,
    descripcion: id === 1 ? 'Distribuidor principal' : null,
    montoTotal: id === 1 ? 10000 : 0,
    ultimaOrdenFecha: id === 1 ? '2026-01-02T00:00:00Z' : null,
  };
}

function respuestaJson(contenido: unknown, status = 200): Response {
  return new Response(status === 204 ? null : JSON.stringify(contenido), { status });
}

function prepararApi(opciones: { errorCrear?: number; errorEliminar?: number; errorLista?: boolean; vacio?: boolean } = {}) {
  const llamadas: Array<{ url: string; init: RequestInit }> = [];
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    llamadas.push({ url, init });
    if (url.endsWith('/clientes/resumen')) {
      return Promise.resolve(respuestaJson({
        totalClientes: 5,
        clientesConOrdenes: 4,
        clientesSinDescripcion: 1,
        topClientesPorMonto: [
          { clienteId: 1, nombre: 'Acme Industrial', montoTotal: 10000, totalOrdenes: 4 },
          { clienteId: 3, nombre: 'Gamma Manufactura', montoTotal: 800, totalOrdenes: 1 },
        ],
      }));
    }
    if (url.includes('/clientes?')) {
      if (opciones.errorLista) {
        return Promise.resolve(respuestaJson({ StatusCode: 500, Message: 'No se pudo cargar el directorio.' }, 500));
      }
      if (opciones.vacio) {
        return Promise.resolve(respuestaJson({ items: [], total: 0, pagina: 1, tamanoPagina: 10 }));
      }
      const consulta = new URLSearchParams(url.split('?')[1] ?? '');
      const pagina = Number(consulta.get('pagina'));
      const items = pagina === 2 ? [cliente(11, 'Omega Comercio', 0)] : clientes;
      return Promise.resolve(respuestaJson({ items, total: pagina === 2 ? 11 : 11, pagina, tamanoPagina: 10 }));
    }
    if (url.endsWith('/clientes') && init.method === 'POST' && opciones.errorCrear !== undefined) {
      return Promise.resolve(respuestaJson({ StatusCode: opciones.errorCrear, Message: 'Ya existe un cliente con el RUC 20111111111.' }, opciones.errorCrear));
    }
    if (url.endsWith('/clientes') && init.method === 'POST') {
      return Promise.resolve(respuestaJson({ id: 6 }));
    }
    if (url.endsWith('/clientes/1') && init.method === 'PUT') {
      return Promise.resolve(respuestaJson({ id: 1 }));
    }
    if (url.endsWith('/clientes/1') && init.method === 'DELETE' && opciones.errorEliminar !== undefined) {
      return Promise.resolve(respuestaJson({ StatusCode: opciones.errorEliminar, Message: 'No se puede eliminar el cliente 1 porque tiene órdenes de compra.' }, opciones.errorEliminar));
    }
    if (url.endsWith('/clientes/1') && init.method === 'DELETE') {
      return Promise.resolve(respuestaJson(undefined, 204));
    }
    const id = Number(url.slice(url.lastIndexOf('/') + 1));
    return Promise.resolve(respuestaJson(detalle(Number.isNaN(id) ? 1 : id)));
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('pantalla de Clientes', () => {
  it('espera 300 ms para buscar y vuelve a página 1 al cambiar búsqueda', async () => {
    const { fetchMock, llamadas } = prepararApi();
    render(<ClientesPage />);
    expect(await screen.findByRole('button', { name: 'Acme Industrial' }, { timeout: 10000 })).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Acme Industrial' }, { timeout: 10000 })).toBeInTheDocument();
    expect(screen.getByText('4 órdenes')).toBeInTheDocument();
    expect(screen.getByText('1 orden')).toBeInTheDocument();
    vi.useFakeTimers();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    vi.useRealTimers();
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url.includes('pagina=2'))).toBe(true));
    expect(await screen.findByRole('button', { name: 'Omega Comercio' })).toBeInTheDocument();
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url === '/api/v1/clientes/11')).toBe(true));
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(screen.getByText('Página 2 de 2')).toBeInTheDocument();
    vi.useFakeTimers();
    fetchMock.mockClear();
    const busqueda = screen.getByRole('searchbox', { name: 'Buscar por nombre o RUC' });
    fireEvent.change(busqueda, { target: { value: 'A' } });
    fireEvent.change(busqueda, { target: { value: 'Ac' } });
    fireEvent.change(busqueda, { target: { value: 'Acme' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(fetchMock).not.toHaveBeenCalled();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    expect(fetchMock.mock.calls.filter((llamada) => String(llamada[0]).includes('/clientes?'))).toHaveLength(1);
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(fetchMock.mock.calls.find((llamada) => String(llamada[0]).includes('/clientes?'))?.[0]).toBe('/api/v1/clientes?busqueda=Acme&pagina=1&tamanoPagina=10');
    expect(screen.getByText('Página 1 de 2')).toBeInTheDocument();
  });

  it('muestra estados vacío y error de la tabla', async () => {
    prepararApi({ vacio: true });
    const { unmount } = render(<ClientesPage />);
    expect(await screen.findByText('No hay clientes que coincidan')).toBeInTheDocument();
    unmount();
    prepararApi({ errorLista: true });
    render(<ClientesPage />);
    expect(await screen.findByText('No se pudo cargar el directorio.')).toBeInTheDocument();
  });

  it('valida nombre y RUC localmente sin llamar al API', async () => {
    const { fetchMock } = prepararApi();
    render(<ClientesPage />);
    await screen.findByRole('heading', { name: 'Acme Industrial' });
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo cliente' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar cliente' }));
    expect(await screen.findByText('El nombre del cliente es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El RUC debe tener 11 dígitos numéricos.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => (call[1] as RequestInit | undefined)?.method === 'POST')).toBe(false);
  });

  it('crea un cliente y refresca el directorio y resumen', async () => {
    const { llamadas } = prepararApi();
    render(<ClientesPage />);
    await screen.findByRole('heading', { name: 'Acme Industrial' });
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo cliente' }));
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Nuevo Cliente' } });
    fireEvent.change(screen.getByLabelText('RUC'), { target: { value: '20666666666' } });
    fireEvent.change(screen.getByLabelText(/Descripción/), { target: { value: 'Nueva cuenta' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar cliente' }));
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nuevo cliente' })).not.toBeInTheDocument());
    expect(llamadas.some((llamada) => llamada.url === '/api/v1/clientes' && llamada.init.method === 'POST' && llamada.init.body === JSON.stringify({ nombre: 'Nuevo Cliente', ruc: '20666666666', descripcion: 'Nueva cuenta' }))).toBe(true);
    await waitFor(() => expect(llamadas.filter((llamada) => llamada.url.includes('/clientes?')).length).toBeGreaterThan(1));
    expect(llamadas.filter((llamada) => llamada.url.endsWith('/clientes/resumen')).length).toBeGreaterThan(1);
  });

  it('muestra un 409 de alta sin perder los campos escritos', async () => {
    prepararApi({ errorCrear: 409 });
    render(<ClientesPage />);
    await screen.findByRole('heading', { name: 'Acme Industrial' });
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo cliente' }));
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Cliente repetido' } });
    fireEvent.change(screen.getByLabelText('RUC'), { target: { value: '20111111111' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar cliente' }));
    expect(await screen.findByText('Ya existe un cliente con el RUC 20111111111.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre')).toHaveValue('Cliente repetido');
    expect(screen.getByLabelText('RUC')).toHaveValue('20111111111');
  });

  it('precarga y actualiza un cliente mediante PUT con su id', async () => {
    const { llamadas } = prepararApi();
    render(<ClientesPage />);
    await screen.findByRole('heading', { name: 'Acme Industrial' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    expect(screen.getByLabelText('Nombre')).toHaveValue('Acme Industrial');
    expect(screen.getByLabelText('RUC')).toHaveValue('20111111111');
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Acme Industrial SAC' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar cliente' }));
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url === '/api/v1/clientes/1' && llamada.init.method === 'PUT' && llamada.init.body === JSON.stringify({ nombre: 'Acme Industrial SAC', ruc: '20111111111', descripcion: 'Distribuidor principal' }))).toBe(true));
  });

  it('cancela la baja con Escape, no envía DELETE y devuelve el foco', async () => {
    const { llamadas } = prepararApi();
    render(<ClientesPage />);
    await screen.findByRole('heading', { name: 'Acme Industrial' });
    const botonEliminar = screen.getByRole('button', { name: /^Eliminar$/ });
    fireEvent.click(botonEliminar);
    const dialogo = screen.getByRole('dialog', { name: 'Eliminar cliente' });
    expect(within(dialogo).getByText(/Acme Industrial/)).toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(document.activeElement).toBe(botonEliminar));
    expect(llamadas.some((llamada) => llamada.init.method === 'DELETE')).toBe(false);
  });

  it('confirma la baja y muestra un 409 dentro del diálogo', async () => {
    const { llamadas } = prepararApi({ errorEliminar: 409 });
    render(<ClientesPage />);
    await screen.findByRole('heading', { name: 'Acme Industrial' });
    fireEvent.click(screen.getByRole('button', { name: /^Eliminar$/ }));
    const dialogo = screen.getByRole('dialog');
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Eliminar cliente' }));
    expect(await within(dialogo).findByText('No se puede eliminar el cliente 1 porque tiene órdenes de compra.')).toBeInTheDocument();
    expect(llamadas.some((llamada) => llamada.url === '/api/v1/clientes/1' && llamada.init.method === 'DELETE')).toBe(true);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('elimina con 204 y refresca el listado y resumen', async () => {
    const { llamadas } = prepararApi();
    render(<ClientesPage />);
    await screen.findByRole('heading', { name: 'Acme Industrial' });
    fireEvent.click(screen.getByRole('button', { name: /^Eliminar$/ }));
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar cliente' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(llamadas.filter((llamada) => llamada.url.includes('/clientes?')).length).toBeGreaterThan(1));
    expect(llamadas.filter((llamada) => llamada.url.endsWith('/clientes/resumen')).length).toBeGreaterThan(1);
  });
});
