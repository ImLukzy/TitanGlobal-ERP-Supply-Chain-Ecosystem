import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  esperarCargaInicial,
  limpiarUsuarios,
  montarUsuariosCargados,
  prepararApiUsuarios,
  renderUsuarios,
} from './usuarios.testUtils';

afterEach(() => {
  limpiarUsuarios();
});

function urlsDeListado(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map((llamada) => String(llamada[0]))
    .filter((url) => url.includes('/usuarios?'));
}

async function montarSinEsperar(): Promise<void> {
  vi.useFakeTimers();
  renderUsuarios();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
}

describe('pantalla de Usuarios: listado y resumen', () => {
  it('A3 carga una sola vez el listado en página 1 y muestra roles, estado y resumen', async () => {
    const { fetchMock } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    expect(screen.getByRole('heading', { name: '71 · Usuarios' })).toBeInTheDocument();
    expect(urlsDeListado(fetchMock)).toEqual(['/api/v1/usuarios?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('region', { name: 'Tabla de usuarios' });
    expect(within(tabla).getByRole('button', { name: 'ana.ventas' })).toBeInTheDocument();
    expect(within(tabla).getByText('VENTAS')).toBeInTheDocument();
    expect(within(tabla).getByText('Sin rol')).toBeInTheDocument();
    expect(within(tabla).getAllByText('Activo')).toHaveLength(4);
    expect(within(tabla).getByText('Inactivo')).toBeInTheDocument();
    const resumen = screen.getByRole('region', { name: 'Resumen de usuarios' });
    expect(within(resumen).getByText('Total de usuarios').parentElement).toHaveTextContent('5');
    expect(within(resumen).getByText('Sin rol').parentElement).toHaveTextContent('1');
    expect(within(resumen).getByRole('list')).toHaveTextContent('ADMIN');
  });

  it('A3 envía rol y estado a la query y devuelve la paginación a la página 1', async () => {
    const { fetchMock } = prepararApiUsuarios({ paginado: true });
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(screen.getByText('Página 2 de 2')).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Rol'), { target: { value: '2' } });
    await waitFor(() => expect(screen.getByText('Página 1 de 2')).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'false' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('activo=false'));
    const ultima = urlsDeListado(fetchMock).at(-1) ?? '';
    expect(ultima).toContain('rolId=2');
    expect(ultima).toContain('pagina=1');
  });

  it('A3 aplica la búsqueda tras 300 ms y consulta solo el último texto', async () => {
    const { fetchMock } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    fetchMock.mockClear();
    vi.useFakeTimers();
    const busqueda = screen.getByRole('searchbox', { name: 'Buscar por usuario, nombre o email' });
    fireEvent.change(busqueda, { target: { value: 'a' } });
    fireEvent.change(busqueda, { target: { value: 'an' } });
    fireEvent.change(busqueda, { target: { value: 'ana' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsDeListado(fetchMock)).toHaveLength(0);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsDeListado(fetchMock)).toHaveLength(1));
    expect(urlsDeListado(fetchMock)[0]).toContain('busqueda=ana');
    expect(urlsDeListado(fetchMock)[0]).toContain('pagina=1');
  });

  it('A3 presenta el estado vacío de la tabla', async () => {
    prepararApiUsuarios({ listaVacia: true });
    await montarSinEsperar();
    expect(await screen.findByText('No hay usuarios que coincidan con los filtros')).toHaveAttribute('role', 'status');
  });

  it('A3 presenta el error de carga del listado', async () => {
    prepararApiUsuarios({ errorLista: true });
    await montarSinEsperar();
    expect(await screen.findByText('No se pudo cargar la lista de usuarios.')).toHaveAttribute('role', 'alert');
  });
});

describe('pantalla de Usuarios: detalle y permisos efectivos', () => {
  it('A3 agrupa los permisos por dominio con su nivel y los roles que lo otorgan', async () => {
    const { fetchMock } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'luis.compras' }));
    const panel = await screen.findByRole('region', { name: 'Luis Castillo Compras' });
    expect(within(panel).getByText('80 módulos con lectura, 10 con escritura')).toBeInTheDocument();
    const dominio = within(panel).getByText('Compras y Abastecimiento Global').closest('details');
    expect(dominio).not.toBeNull();
    expect(within(dominio as HTMLElement).getAllByRole('listitem')).toHaveLength(10);
    const proveedores = within(panel).getByText('11 · Proveedores').closest('li');
    expect(proveedores).toHaveTextContent('Escritura');
    expect(proveedores).toHaveTextContent('por COMPRAS');
    const productos = within(panel).getByText('21 · Productos').closest('li');
    expect(productos).toHaveTextContent('Lectura');
    expect(productos).toHaveTextContent('por COMPRAS, CONSULTA');
    const clientes = within(panel).getByText('01 · Clientes').closest('li');
    expect(clientes).toHaveTextContent('Lectura');
    expect(clientes).toHaveTextContent('por CONSULTA');
    const llamadasPermisos = fetchMock.mock.calls.filter((llamada) => String(llamada[0]).endsWith('/usuarios/4/permisos'));
    expect(llamadasPermisos).toHaveLength(1);
  });

  it('A3 muestra «Sin acceso» en los 80 módulos cuando el usuario no tiene roles', async () => {
    const { fetchMock } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'invitado' }));
    const panel = await screen.findByRole('region', { name: 'Invitado Demo' });
    await waitFor(() => expect(within(panel).getAllByText('Sin acceso')).toHaveLength(80));
    expect(within(panel).getByText('0 módulos con lectura, 0 con escritura')).toBeInTheDocument();
  });

  it('A3 muestra «Sin acceso» en los 80 módulos cuando el usuario está inactivo', async () => {
    const { fetchMock } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'maria.almacen' }));
    const panel = await screen.findByRole('region', { name: 'María Ruiz Almacén' });
    await waitFor(() => expect(within(panel).getAllByText('Sin acceso')).toHaveLength(80));
  });

  it('A3 muestra el error del detalle sin mostrar permisos', async () => {
    const { fetchMock } = prepararApiUsuarios({ errorDetalle: true });
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'ana.ventas' }));
    expect(await screen.findByText('No se pudo cargar el detalle del usuario.')).toHaveAttribute('role', 'alert');
    expect(screen.queryByText('Permisos efectivos')).not.toBeInTheDocument();
  });

  it('A3 muestra el error de permisos cuando el detalle carga', async () => {
    const { fetchMock } = prepararApiUsuarios({ errorPermisos: true });
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'ana.ventas' }));
    expect(await screen.findByText('No se pudieron cargar los permisos efectivos.')).toHaveAttribute('role', 'alert');
  });

  it('A3 muestra el aviso de selección antes de elegir un usuario', async () => {
    const { fetchMock } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    expect(screen.getByText('Selecciona un usuario para ver sus permisos efectivos.')).toBeInTheDocument();
    await esperarCargaInicial(fetchMock);
  });
});
