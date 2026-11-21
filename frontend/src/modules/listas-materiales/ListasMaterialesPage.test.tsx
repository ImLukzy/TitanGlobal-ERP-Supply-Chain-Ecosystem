import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { pantallas } from '../pantallas';
import type { EstadoLista } from './tipos';

const ruta = '/api/v1/listas-materiales';
const producto = 'Kit de ensamblaje';
const conflicto = 'Ya existe la versión 2 para el producto 31.';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function montar({ vacio = false, error = false, conflictoAlta = false } = {}) {
  let estado: EstadoLista = 'Borrador';
  const fila = { id: 7, productoId: 31, productoNombre: producto, version: 2, totalComponentes: 1 };
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit) => {
    const url = new URL(String(input), 'http://localhost');
    if (url.pathname === '/api/v1/productos') {
      return Response.json({ items: [{ id: 31, nombre: producto }, { id: 32, nombre: 'Tornillo M8' }], total: 2, tamanoPagina: 10 });
    }
    if (url.pathname === `${ruta}/7/activar`) estado = 'Activa';
    if (url.pathname === `${ruta}/7/retirar`) estado = 'Retirada';
    if (init.method === 'POST' || init.method === 'PUT') {
      if (url.pathname === ruta && conflictoAlta) return Response.json({ Message: conflicto }, { status: 409 });
      return Response.json({ id: 7 });
    }
    if (url.pathname === `${ruta}/7`) {
      return Response.json({ ...fila, estado, componentes: [{ componenteProductoId: 32, componenteProductoNombre: 'Tornillo M8', cantidad: 0.125, unidadMedida: 'un' }] });
    }
    if (error) return Response.json({ Message: 'Error de listado.' }, { status: 500 });
    const items = vacio || (url.searchParams.has('estado') && url.searchParams.get('estado') !== estado) ? [] : [{ ...fila, estado }];
    return Response.json({ items, total: items.length === 0 ? 0 : 21, pagina: Number(url.searchParams.get('pagina')), tamanoPagina: 20 });
  });
  vi.stubGlobal('fetch', fetchMock);
  const Pagina = pantallas['31-listas-materiales'];
  render(<MemoryRouter initialEntries={['/modulos/31-listas-materiales']}><Pagina /></MemoryRouter>);
  return fetchMock;
}

async function elegirProducto(etiqueta: string, nombre: string): Promise<void> {
  fireEvent.click(screen.getByLabelText(etiqueta));
  fireEvent.click(await screen.findByRole('button', { name: nombre }));
}

async function abrirDetalle(): Promise<void> {
  fireEvent.click(await screen.findByRole('button', { name: `Ver ${producto}, versión 2` }));
  await screen.findByText('0.125 un');
}

describe('pantalla de listas de materiales', () => {
  it('carga la ruta, combina filtros y vuelve a la primera página', async () => {
    const fetchMock = montar();
    expect(screen.getByRole('heading', { name: '31 · Listas de materiales' })).toBeInTheDocument();
    expect(screen.getByText('Cargando listas de materiales…')).toBeInTheDocument();
    await abrirDetalle();
    expect(screen.getByText('Tornillo M8')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await screen.findByText('Página 2 de 2');
    await elegirProducto('Filtrar por producto', `${producto} · #31`);
    fireEvent.change(screen.getByLabelText('Filtrar por estado'), { target: { value: 'Borrador' } });
    await screen.findByText('Página 1 de 2');
    expect(fetchMock).toHaveBeenCalledWith(`${ruta}?productoId=31&estado=Borrador&pagina=1&tamanoPagina=20`, expect.anything());
  });

  it('muestra el estado vacío', async () => {
    montar({ vacio: true });
    expect(await screen.findByText('No se encontraron listas de materiales.')).toBeInTheDocument();
  });

  it('muestra el error de listado y permite reintentar', async () => {
    const fetchMock = montar({ error: true });
    expect(await screen.findByRole('alert')).toHaveTextContent('Error de listado.');
    fireEvent.click(screen.getByRole('button', { name: 'Reintentar listado' }));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
  });

  it('valida el alta y conserva los datos ante un 409', async () => {
    const fetchMock = montar({ conflictoAlta: true });
    fireEvent.click(screen.getByRole('button', { name: 'Nueva lista' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
    expect(screen.getByText('El identificador del producto debe ser mayor que 0.')).toBeInTheDocument();
    expect(screen.getByText('La versión debe ser mayor que 0.')).toBeInTheDocument();
    await elegirProducto('Producto fabricado', `${producto} · #31`);
    await elegirProducto('Componente 1', 'Tornillo M8 · #32');
    fireEvent.change(screen.getByLabelText('Versión'), { target: { value: '2' } });
    fireEvent.change(screen.getByLabelText('Cantidad'), { target: { value: '1,25' } });
    fireEvent.change(screen.getByLabelText('Unidad de medida'), { target: { value: 'un' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(conflicto);
    expect(screen.getByLabelText('Cantidad')).toHaveValue('1,25');
    expect(screen.getByLabelText('Producto fabricado')).toHaveTextContent(producto);
    expect(fetchMock).toHaveBeenCalledWith(ruta, expect.objectContaining({ method: 'POST' }));
  });

  it('edita un borrador y envía todas las líneas sin producto ni versión', async () => {
    const fetchMock = montar();
    await abrirDetalle();
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    expect(screen.queryByLabelText('Versión')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Cantidad')).toHaveValue('0.125');
    fireEvent.change(screen.getByLabelText('Cantidad'), { target: { value: '2' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));
    expect(await screen.findByText('Borrador actualizado.')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith(`${ruta}/7`, expect.objectContaining({ method: 'PUT', body: '{"componentes":[{"componenteProductoId":32,"cantidad":2,"unidadMedida":"un"}]}' }));
  });

  it('devuelve el foco al cancelar y permite activar y retirar conservando el historial', async () => {
    montar();
    await abrirDetalle();
    const activar = screen.getByRole('button', { name: 'Activar' });
    fireEvent.click(activar);
    const dialogo = screen.getByRole('dialog', { name: 'Activar versión 2' });
    expect(within(dialogo).getByRole('button', { name: 'Cancelar' })).toHaveFocus();
    fireEvent.keyDown(dialogo, { key: 'Escape' });
    expect(activar).toHaveFocus();

    fireEvent.click(activar);
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar activación' }));
    expect(await screen.findByText('Versión activada.')).toBeInTheDocument();
    const retirar = await screen.findByRole('button', { name: 'Retirar' });
    expect(screen.getByRole('heading', { name: 'Detalle de la lista' })).toHaveFocus();
    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument();
    fireEvent.click(retirar);
    expect(screen.getByRole('dialog')).toHaveTextContent('La versión quedará disponible en el historial.');
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar retiro' }));
    expect(await screen.findByText('Versión 2 · Retirada')).toBeInTheDocument();
    expect(screen.getByText('Tornillo M8')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Retirar' })).not.toBeInTheDocument();
  });
});
