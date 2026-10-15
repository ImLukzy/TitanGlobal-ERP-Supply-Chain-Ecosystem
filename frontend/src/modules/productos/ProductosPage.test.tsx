import { act, cleanup, fireEvent, render as renderBase, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import type { ReactNode } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import config, { resolveApiTarget } from '../../../vite.config';
import { ProductosPage } from './ProductosPage';

function render(ui: ReactNode) {
  return renderBase(<MemoryRouter>{ui}</MemoryRouter>);
}

interface OpcionesApi {
  vacio?: boolean;
  errorLista?: boolean;
  errorCrear?: boolean;
  errorBaja?: boolean;
}

const productos = [
  { id: 1, nombre: 'Laptop Pro', categoria: 'Computo', precio: 2500, tieneDescripcion: true },
  { id: 4, nombre: 'Teclado Mecánico', categoria: 'Perifericos', precio: 200, tieneDescripcion: false },
  { id: 9, nombre: 'Silla Ejecutiva', categoria: 'Oficina', precio: 800, tieneDescripcion: false },
  ...Array.from({ length: 17 }, (_, indice) => ({
    id: indice + 10,
    nombre: `Producto ${indice + 10}`,
    categoria: 'Accesorios',
    precio: 100 + indice,
    tieneDescripcion: true,
  })),
];

const resumen = {
  totalProductos: 20,
  productosSinDescripcion: 2,
  precioPromedio: 800,
  porCategoria: [{ categoria: 'Computo', totalProductos: 2, precioPromedio: 2500, precioMaximo: 2500 }],
  masVendidos: [
    { productoId: 1, nombre: 'Laptop Pro', unidadesVendidas: 4, montoVendido: 10000 },
    { productoId: 4, nombre: 'Teclado Mecánico', unidadesVendidas: 1, montoVendido: 200 },
  ],
};

function detalle(id: number) {
  const fila = productos.find((item) => item.id === id) ?? productos[0]!;
  const vendido = id === 1;
  return {
    ...fila,
    descripcion: fila.tieneDescripcion ? 'Equipo portátil de alto rendimiento' : null,
    unidadesVendidas: vendido ? 4 : 0,
    montoVendido: vendido ? 10000 : 0,
    clientesDistintos: vendido ? 3 : 0,
  };
}

function prepararApi(opciones: OpcionesApi = {}) {
  const llamadas: { url: string; init: RequestInit }[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    llamadas.push({ url, init });
    if (url.endsWith('/productos/categorias')) {
      return Response.json(['Accesorios', 'Computo', 'Oficina', 'Pantallas', 'Perifericos']);
    }
    if (url.endsWith('/productos/resumen')) {
      return Response.json(resumen);
    }
    if (init.method === 'POST' && opciones.errorCrear) {
      return Response.json({ StatusCode: 400, Message: 'El precio debe ser mayor que 0.' }, { status: 400 });
    }
    if (init.method === 'DELETE' && opciones.errorBaja) {
      return Response.json({ StatusCode: 409, Message: 'No se puede eliminar el producto 1 porque tiene ventas registradas.' }, { status: 409 });
    }
    if (init.method === 'DELETE') {
      return new Response(null, { status: 204 });
    }
    if (url.endsWith('/productos/1')) {
      return Response.json(detalle(1));
    }
    if (url.endsWith('/productos/4')) {
      return Response.json(detalle(4));
    }
    if (url.endsWith('/productos/9')) {
      return Response.json(detalle(9));
    }
    if (init.method === 'POST' || init.method === 'PUT') {
      return Response.json({ id: 1 });
    }
    if (url.includes('/productos?')) {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'El catálogo no está disponible.' }, { status: 500 });
      }
      if (opciones.vacio) {
        return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
      }
      const urlApi = new URL(url, 'http://localhost');
      const pagina = Number(urlApi.searchParams.get('pagina') ?? '1');
      return Response.json({
        items: productos.slice((pagina - 1) * 10, pagina * 10),
        total: productos.length,
        pagina,
        tamanoPagina: 10,
      });
    }
    return Response.json(detalle(1));
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

async function limpiarCargaInicial(fetchMock: ReturnType<typeof vi.fn>) {
  vi.useFakeTimers();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();
  fetchMock.mockClear();
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('pantalla de Productos', () => {
  it('A2 muestra listado, resumen y detalle de productos con ventas', async () => {
    prepararApi();
    render(<ProductosPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Laptop Pro' }));
    expect(await screen.findByText('Equipo portátil de alto rendimiento')).toBeInTheDocument();
    expect(screen.getByText('1 unidad')).toBeInTheDocument();
    expect(screen.getAllByText(/10,000\.00/)).toHaveLength(2);
    expect(screen.getAllByText('Teclado Mecánico').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Sin descripción').length).toBeGreaterThan(0);
  });

  it('A3 aplica búsqueda tras 300 ms y solo envía la última escritura', async () => {
    const { fetchMock } = prepararApi();
    render(<ProductosPage />);
    await screen.findByRole('button', { name: 'Laptop Pro' }, { timeout: 10000 });
    await screen.findByText('20');
    await screen.findByRole('option', { name: 'Pantallas' });
    await limpiarCargaInicial(fetchMock);
    vi.useFakeTimers();
    const busqueda = screen.getByRole('searchbox', { name: 'Buscar' });
    fireEvent.change(busqueda, { target: { value: 'T' } });
    fireEvent.change(busqueda, { target: { value: 'Te' } });
    fireEvent.change(busqueda, { target: { value: 'Teclado' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(fetchMock).not.toHaveBeenCalled();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/productos?'))).toHaveLength(1);
    expect(String(fetchMock.mock.calls.find((call) => String(call[0]).includes('/productos?'))?.[0])).toContain('busqueda=Teclado');
  });

  it('A3 envía categoría y ambos precios; un filtro vuelve a página 1', async () => {
    const { fetchMock } = prepararApi();
    render(<ProductosPage />);
    await screen.findByRole('button', { name: 'Laptop Pro' });
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(screen.getByText('Página 2 de 2')).toBeInTheDocument());
    fireEvent.change(screen.getAllByLabelText('Categoría')[0]!, { target: { value: 'Computo' } });
    fireEvent.change(screen.getByLabelText('Precio mínimo'), { target: { value: '100' } });
    fireEvent.change(screen.getByLabelText('Precio máximo'), { target: { value: '3000' } });
    await waitFor(() => expect(screen.getByText('Página 1 de 2')).toBeInTheDocument());
    const query = String(fetchMock.mock.calls.find((call) => String(call[0]).includes('precioMax=3000'))?.[0]);
    expect(query).toContain('categoria=Computo');
    expect(query).toContain('precioMin=100');
    expect(query).toContain('precioMax=3000');
    expect(query).toContain('pagina=1');
  });

  it('A3 valida precios negativos y rango incorrecto sin enviar peticiones', async () => {
    const { fetchMock } = prepararApi();
    render(<ProductosPage />);
    await screen.findByRole('button', { name: 'Laptop Pro' }, { timeout: 10000 });
    await screen.findByText('20');
    await screen.findByRole('option', { name: 'Pantallas' });
    await limpiarCargaInicial(fetchMock);
    fireEvent.change(screen.getByLabelText('Precio mínimo'), { target: { value: '-1' } });
    expect(screen.getByText('Los precios del filtro no pueden ser negativos.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/productos?'))).toHaveLength(0);
    fireEvent.change(screen.getByLabelText('Precio mínimo'), { target: { value: '900' } });
    await waitFor(() => expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/productos?'))).toHaveLength(1));
    fetchMock.mockClear();
    fireEvent.change(screen.getByLabelText('Precio máximo'), { target: { value: '100' } });
    expect(screen.getByText('El precio mínimo no puede ser mayor que el máximo.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/productos?'))).toHaveLength(0);
  });

  it('A3 muestra estados vacío y error del catálogo', async () => {
    prepararApi({ vacio: true });
    const { unmount } = render(<ProductosPage />);
    expect(await screen.findByText('No hay productos que coincidan con los filtros.')).toBeInTheDocument();
    unmount();
    prepararApi({ errorLista: true });
    render(<ProductosPage />);
    expect(await screen.findByText('El catálogo no está disponible.')).toBeInTheDocument();
  });

  it('A4 muestra ventas y el estado «Sin ventas registradas»', async () => {
    prepararApi();
    render(<ProductosPage />);
    await screen.findByRole('button', { name: 'Teclado Mecánico' });
    fireEvent.click(screen.getByRole('button', { name: 'Teclado Mecánico' }));
    expect(await screen.findByText('Sin ventas registradas')).toBeInTheDocument();
  });

  it('A4 presenta cada error local literal por campo sin llamar al API', async () => {
    const { fetchMock } = prepararApi();
    render(<ProductosPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Nuevo producto' }));
    const enviar = screen.getByRole('button', { name: 'Guardar producto' });
    fireEvent.click(enviar);
    expect(screen.getByText('El nombre del producto es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('La categoría es obligatoria.')).toBeInTheDocument();
    expect(screen.getByText('El precio debe ser mayor que 0.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre')).toHaveAttribute('aria-describedby', 'producto-nombre-error');
    expect(screen.getAllByLabelText('Categoría')[1]).toHaveAttribute('aria-describedby', 'producto-categoria-error');
    expect(screen.getByLabelText('Precio')).toHaveAttribute('aria-describedby', 'producto-precio-error');
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'P'.repeat(201) } });
    fireEvent.change(screen.getAllByLabelText('Categoría')[1]!, { target: { value: 'C'.repeat(101) } });
    fireEvent.click(enviar);
    expect(screen.getByText('El nombre del producto no puede superar 200 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('La categoría no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => (call[1] as RequestInit | undefined)?.method === 'POST')).toBe(false);
  });

  it('A4 muestra el 400 literal de la API y conserva los campos escritos', async () => {
    prepararApi({ errorCrear: true });
    render(<ProductosPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Nuevo producto' }));
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Nuevo producto' } });
    fireEvent.change(screen.getAllByLabelText('Categoría')[1]!, { target: { value: 'Computo' } });
    fireEvent.change(screen.getByLabelText('Precio'), { target: { value: '12' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar producto' }));
    expect(await screen.findByText('El precio debe ser mayor que 0.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre')).toHaveValue('Nuevo producto');
    expect(screen.getAllByLabelText('Categoría')[1]).toHaveValue('Computo');
  });

  it('A4 muestra el 409 literal con id en el diálogo de baja', async () => {
    prepararApi({ errorBaja: true });
    render(<ProductosPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Laptop Pro' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Eliminar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar producto' }));
    expect(await screen.findByText('No se puede eliminar el producto 1 porque tiene ventas registradas.')).toHaveAttribute('role', 'alert');
  });

  it('A4 una baja correcta refresca el catálogo', async () => {
    const { llamadas } = prepararApi();
    render(<ProductosPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Laptop Pro' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Eliminar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar producto' }));
    await waitFor(() => expect(llamadas.filter((call) => call.url.includes('/productos?')).length).toBeGreaterThan(1));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('A4 Escape cierra la baja sin petición y devuelve el foco al botón', async () => {
    const { fetchMock, llamadas } = prepararApi();
    render(<ProductosPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Laptop Pro' }));
    const abrirBaja = await screen.findByRole('button', { name: 'Eliminar' });
    await waitFor(() => {
      expect(llamadas.some((llamada) => llamada.url === '/api/v1/productos?pagina=1&tamanoPagina=10')).toBe(true);
      expect(llamadas.some((llamada) => llamada.url === '/api/v1/productos/resumen')).toBe(true);
      expect(llamadas.some((llamada) => llamada.url === '/api/v1/productos/categorias')).toBe(true);
      expect(llamadas.some((llamada) => llamada.url === '/api/v1/productos/1')).toBe(true);
    });
    fireEvent.click(abrirBaja);
    fetchMock.mockClear();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    await act(async () => {
      await new Promise((resolve) => window.requestAnimationFrame(resolve));
    });
    expect(document.activeElement).toBe(abrirBaja);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('A5 documenta el destino de proxy por variable de entorno', () => {
    expect(config.server?.proxy).toMatchObject({ '/api': { target: 'http://localhost:5080' } });
    expect(resolveApiTarget('http://localhost:5090')).toBe('http://localhost:5090');
  });

  it('A6 registra el módulo 21 con el catálogo de módulos', async () => {
    prepararApi();
    render(<ProductosPage />);
    expect(screen.getByRole('heading', { name: '21 · Productos' })).toBeInTheDocument();
  });
});
