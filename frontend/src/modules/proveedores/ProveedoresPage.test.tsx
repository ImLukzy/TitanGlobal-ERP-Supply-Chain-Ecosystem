import { act, cleanup, fireEvent, render as renderBase, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import type { ReactNode } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ProveedoresPage } from './ProveedoresPage';

interface OpcionesApi {
  vacio?: boolean;
  errorLista?: boolean;
  errorGuardar?: boolean;
  paginado?: boolean;
}

interface ProveedorPrueba {
  id: number;
  razonSocial: string;
  ruc: string;
  pais: string;
  email: string | null;
  telefono: string | null;
  activo: boolean;
}

function render(ui: ReactNode) {
  return renderBase(<MemoryRouter>{ui}</MemoryRouter>);
}

const proveedoresIniciales: ProveedorPrueba[] = [
  { id: 1, razonSocial: 'Andina Suministros', ruc: '20111111111', pais: 'Perú', email: 'ventas@andina.pe', telefono: '+51 999 111 111', activo: true },
  { id: 2, razonSocial: 'Insumos del Norte', ruc: '20222222222', pais: 'Perú', email: 'contacto@norte.pe', telefono: '+51 999 222 222', activo: true },
  { id: 3, razonSocial: 'Maquinaria Sur', ruc: '20333333333', pais: 'Perú', email: null, telefono: '+51 999 333 333', activo: true },
  { id: 4, razonSocial: 'Chile Industrial', ruc: '20444444444', pais: 'Chile', email: 'ventas@chile.cl', telefono: '+56 9 4444 4444', activo: true },
  { id: 5, razonSocial: 'Andes Exportaciones', ruc: '20555555555', pais: 'Chile', email: 'contacto@andes.cl', telefono: '+56 9 5555 5555', activo: true },
  { id: 6, razonSocial: 'Asia Components', ruc: '20666666666', pais: 'China', email: null, telefono: null, activo: false },
];

function detalleProveedor(proveedor: ProveedorPrueba) {
  return {
    id: proveedor.id,
    razonSocial: proveedor.razonSocial,
    ruc: proveedor.ruc,
    pais: proveedor.pais,
    email: proveedor.email,
    telefono: proveedor.telefono,
    activo: proveedor.activo,
  };
}

function prepararApi(opciones: OpcionesApi = {}) {
  const proveedores = [...proveedoresIniciales];
  const llamadas: Array<{ url: string; init: RequestInit }> = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });
    if (url.endsWith('/proveedores/paises')) {
      return Response.json(['Chile', 'China', 'Perú']);
    }
    if (url.endsWith('/proveedores/resumen')) {
      return Response.json({
        totalProveedores: proveedoresIniciales.length,
        activos: 5,
        inactivos: 1,
        sinEmail: 2,
        porPais: [
          { pais: 'Perú', totalProveedores: 3, activos: 3 },
          { pais: 'Chile', totalProveedores: 2, activos: 2 },
          { pais: 'China', totalProveedores: 1, activos: 0 },
        ],
      });
    }
    if (url.includes('/proveedores?') && metodo === 'GET') {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'No se pudo cargar la lista de proveedores.' }, { status: 500 });
      }
      if (opciones.vacio) {
        return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
      }
      const consulta = new URL(url, 'http://localhost').searchParams;
      const pagina = Number(consulta.get('pagina') ?? '1');
      const items = pagina > 1 ? proveedores.slice(0, 2) : proveedores;
      const total = opciones.paginado ? 12 : proveedores.length;
      return Response.json({ items, total, pagina, tamanoPagina: 10 });
    }
    const id = Number(url.slice(url.lastIndexOf('/') + 1));
    if (metodo === 'GET' && Number.isInteger(id)) {
      const proveedor = proveedores.find((item) => item.id === id);
      if (proveedor === undefined) {
        return Response.json({ StatusCode: 404, Message: `El proveedor ${id} no existe.` }, { status: 404 });
      }
      return Response.json(detalleProveedor(proveedor));
    }
    if (metodo === 'POST' && opciones.errorGuardar) {
      return Response.json({ StatusCode: 409, Message: 'Ya existe un proveedor con el RUC 20111111111.' }, { status: 409 });
    }
    if (metodo === 'POST') {
      return Response.json({ id: 7 });
    }
    if (metodo === 'PUT') {
      return Response.json({ id });
    }
    if (metodo === 'DELETE') {
      const indice = proveedores.findIndex((item) => item.id === id);
      if (indice >= 0) {
        proveedores.splice(indice, 1);
      }
      return new Response(null, { status: 204 });
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

async function esperarCargaInicialProveedores(fetchMock: ReturnType<typeof vi.fn>) {
  vi.useFakeTimers();
  render(<ProveedoresPage />);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();
  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'Andina Suministros' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Andina Suministros' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'China' })).toBeInTheDocument();
    expect(screen.getByText('Total de proveedores')).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/proveedores?'))).toHaveLength(1);
    expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/proveedores/resumen'))).toBe(true);
    expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/proveedores/paises'))).toBe(true);
  });
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('pantalla de Proveedores', () => {
  it('A2 presenta resumen, tabla, estado y detalle del proveedor inicial', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialProveedores(fetchMock);
    expect(screen.getAllByText('Andina Suministros')).toHaveLength(2);
    expect(screen.getByText('Total de proveedores')).toBeInTheDocument();
    expect(screen.getAllByText('Perú').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Activo').length).toBeGreaterThan(0);
    expect(await screen.findByRole('heading', { name: 'Andina Suministros' })).toBeInTheDocument();
    expect(screen.getByText('+51 999 111 111')).toBeInTheDocument();
  });

  it('A3 aplica la búsqueda tras 300 ms y solo consulta el último texto', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialProveedores(fetchMock);
    await screen.findByRole('button', { name: 'Andina Suministros' }, { timeout: 10000 });
    await screen.findByRole('heading', { name: 'Andina Suministros' }, { timeout: 10000 });
    await screen.findByText('6');
    await screen.findByRole('option', { name: 'China' });
    vi.useFakeTimers();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    vi.useRealTimers();
    vi.useFakeTimers();
    fetchMock.mockClear();
    const busqueda = screen.getByRole('searchbox', { name: 'Buscar por razón social o RUC' });
    fireEvent.change(busqueda, { target: { value: 'A' } });
    fireEvent.change(busqueda, { target: { value: 'An' } });
    fireEvent.change(busqueda, { target: { value: 'Andina' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(fetchMock).not.toHaveBeenCalled();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/proveedores?'))).toHaveLength(1);
    expect(String(fetchMock.mock.calls.find((call) => String(call[0]).includes('/proveedores?'))?.[0])).toContain('busqueda=Andina');
  });

  it('A3 consulta país y estado, y devuelve la paginación a página 1', async () => {
    const { fetchMock } = prepararApi({ paginado: true });
    await esperarCargaInicialProveedores(fetchMock);
    await screen.findByRole('button', { name: 'Andina Suministros' });
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(screen.getByText('Página 2 de 2')).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('País', { selector: 'select' }), { target: { value: 'Chile' } });
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'false' } });
    await waitFor(() => expect(screen.getByText('Página 1 de 2')).toBeInTheDocument());
    const query = String(fetchMock.mock.calls.find((call) => String(call[0]).includes('activo=false'))?.[0]);
    expect(query).toContain('pais=Chile');
    expect(query).toContain('activo=false');
    expect(query).toContain('pagina=1');
  });

  it('A3 presenta estados vacío y error de carga', async () => {
    prepararApi({ vacio: true });
    const { unmount } = render(<ProveedoresPage />);
    expect(await screen.findByText('No hay proveedores que coincidan')).toBeInTheDocument();
    unmount();
    prepararApi({ errorLista: true });
    render(<ProveedoresPage />);
    expect(await screen.findByText('No se pudo cargar la lista de proveedores.')).toBeInTheDocument();
  });

  it('A4 muestra «Sin email» y «Sin teléfono» cuando el detalle carece de esos datos', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialProveedores(fetchMock);
    await screen.findByRole('button', { name: 'Maquinaria Sur' });
    fireEvent.click(screen.getByRole('button', { name: 'Asia Components' }));
    const tituloDetalle = await screen.findByRole('heading', { name: 'Asia Components' });
    const panelDetalle = tituloDetalle.closest('section');
    expect(panelDetalle).not.toBeNull();
    expect(within(panelDetalle as HTMLElement).getByText('Sin email')).toBeInTheDocument();
    expect(within(panelDetalle as HTMLElement).getByText('Sin teléfono')).toBeInTheDocument();
    expect(within(panelDetalle as HTMLElement).getByText('Inactivo')).toBeInTheDocument();
  });

  it('A4 valida todos los campos localmente y no llama a POST', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialProveedores(fetchMock);
    fireEvent.click(await screen.findByRole('button', { name: 'Nuevo proveedor' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar proveedor' }));
    expect(screen.getByText('La razón social es obligatoria.')).toBeInTheDocument();
    expect(screen.getByText('El RUC debe tener 11 dígitos numéricos.')).toBeInTheDocument();
    expect(screen.getByText('El país es obligatorio.')).toBeInTheDocument();
    expect(screen.getByLabelText('Razón social')).toHaveAttribute('aria-describedby', 'proveedor-razon-social-error');
    expect(screen.getByLabelText('RUC')).toHaveAttribute('aria-describedby', 'proveedor-ruc-error');
    expect(screen.getByLabelText('País', { selector: 'input' })).toHaveAttribute('aria-describedby', 'proveedor-pais-error');
    fireEvent.change(screen.getByLabelText('Razón social'), { target: { value: 'R'.repeat(201) } });
    fireEvent.change(screen.getByLabelText('RUC'), { target: { value: '123' } });
    fireEvent.change(screen.getByLabelText('País', { selector: 'input' }), { target: { value: 'P'.repeat(101) } });
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'no-valido' } });
    fireEvent.change(screen.getByLabelText('Teléfono'), { target: { value: 'abc' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar proveedor' }));
    expect(screen.getByText('La razón social no puede superar 200 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('El país no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('El email no tiene un formato válido.')).toBeInTheDocument();
    expect(screen.getByText('El teléfono no tiene un formato válido.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => (call[1] as RequestInit | undefined)?.method === 'POST')).toBe(false);
  });

  it('A4 muestra el 409 de RUC sin perder los datos del formulario', async () => {
    const { fetchMock } = prepararApi({ errorGuardar: true });
    await esperarCargaInicialProveedores(fetchMock);
    fireEvent.click(await screen.findByRole('button', { name: 'Nuevo proveedor' }));
    fireEvent.change(screen.getByLabelText('Razón social'), { target: { value: 'Proveedor nuevo' } });
    fireEvent.change(screen.getByLabelText('RUC'), { target: { value: '20777777777' } });
    fireEvent.change(screen.getByLabelText('País', { selector: 'input' }), { target: { value: 'Perú' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar proveedor' }));
    expect(await screen.findByText('Ya existe un proveedor con el RUC 20111111111.')).toHaveAttribute('role', 'alert');
    expect(screen.getByLabelText('Razón social')).toHaveValue('Proveedor nuevo');
    expect(screen.getByLabelText('RUC')).toHaveValue('20777777777');
  });

  it('A4 precarga la edición y envía PUT al id del proveedor', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialProveedores(fetchMock);
    await screen.findByRole('heading', { name: 'Andina Suministros' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    expect(screen.getByRole('heading', { name: 'Editar proveedor' })).toBeInTheDocument();
    expect(screen.getByLabelText('RUC')).toHaveValue('20111111111');
    fireEvent.change(screen.getByLabelText('Razón social'), { target: { value: 'Andina Actualizada' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar proveedor' }));
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/proveedores/1' && (call[1] as RequestInit).method === 'PUT')).toBe(true));
  });

  it('A4 confirma la baja y refresca la lista', async () => {
    const { fetchMock, llamadas } = prepararApi();
    await esperarCargaInicialProveedores(fetchMock);
    await screen.findByRole('heading', { name: 'Andina Suministros' });
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar proveedor' }));
    await waitFor(() => expect(llamadas.filter((call) => call.url.includes('/proveedores?')).length).toBeGreaterThan(1));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('A4 Escape cancela la baja sin petición y devuelve el foco', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialProveedores(fetchMock);
    await screen.findByRole('heading', { name: 'Andina Suministros' });
    const abrirBaja = screen.getByRole('button', { name: 'Eliminar' });
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

  it('A6 registra el módulo de Proveedores desde el catálogo', async () => {
    prepararApi();
    render(<ProveedoresPage />);
    expect(screen.getByRole('heading', { name: '11 · Proveedores' })).toBeInTheDocument();
    const migas = screen.getByRole('navigation', { name: 'Miga de pan' });
    expect(within(migas).getByRole('link', { name: 'Inicio' })).toHaveAttribute('href', '/');
  });
});
