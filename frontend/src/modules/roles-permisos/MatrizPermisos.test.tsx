import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { DOMINIOS_PRUEBA } from './permisos.testUtils';
import { esperarCargaInicialMatriz, esperarCargaInicialRoles, prepararApi, render } from './roles.testUtils';
import { RolesPermisosPage } from './RolesPermisosPage';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

function celdasDeModulo(numero: number): string[] {
  const cabecera = screen.getByRole('rowheader', { name: new RegExp(`^${String(numero).padStart(2, '0')}\\s`) });
  const fila = cabecera.closest('tr') as HTMLElement;
  return within(fila).getAllByRole('cell').map((celda) => celda.textContent ?? '');
}

describe('pestañas y matriz de permisos (módulo 72)', () => {
  it('A5 abre la Matriz con ?vista=matriz y la pide una sola vez', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialMatriz(fetchMock);
    expect(screen.getByRole('tab', { name: 'Matriz' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Roles' })).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByRole('heading', { name: 'Matriz de permisos' })).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]) === '/api/v1/roles/matriz')).toHaveLength(1);
    expect(screen.queryByRole('button', { name: 'Nuevo rol' })).not.toBeInTheDocument();
  });

  it('A5 usa los roles como cabeceras de columna y los módulos como cabeceras de fila', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialMatriz(fetchMock);
    const columnas = screen.getAllByRole('columnheader').map((cabecera) => cabecera.textContent);
    expect(columnas).toEqual(['Módulo', 'ADMIN', 'ALMACEN', 'COMPRAS', 'CONSULTA', 'VENTAS']);
    expect(screen.getAllByRole('rowheader').filter((cabecera) => cabecera.getAttribute('scope') === 'row')).toHaveLength(80);
    expect(screen.getByRole('rowheader', { name: '21 Módulo 21' })).toBeInTheDocument();
    expect(screen.getByRole('table')).toHaveAccessibleName('Nivel de acceso de cada rol a cada módulo');
  });

  it('A5 muestra en cada celda Escritura, Lectura o «sin acceso» con texto', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialMatriz(fetchMock);
    expect(celdasDeModulo(21)).toEqual(['Escritura', 'Escritura', 'Lectura', 'Lectura', 'Lectura']);
    expect(celdasDeModulo(5)).toEqual(['Escritura', '—Sin acceso', '—Sin acceso', 'Lectura', 'Escritura']);
    expect(celdasDeModulo(80)).toEqual(['Escritura', '—Sin acceso', '—Sin acceso', 'Lectura', '—Sin acceso']);
  });

  it('A5 agrupa los módulos por dominio y filtra por dominio', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialMatriz(fetchMock);
    const grupos = Array.from(document.querySelectorAll('.matriz-dominio th')).map((celda) => celda.textContent);
    expect(grupos).toEqual(DOMINIOS_PRUEBA);
    expect(screen.getByText('80 módulos · 5 roles')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Filtrar por dominio'), { target: { value: 'Compras y Abastecimiento Global' } });
    expect(screen.getAllByRole('rowheader').filter((cabecera) => cabecera.getAttribute('scope') === 'row')).toHaveLength(10);
    expect(document.querySelectorAll('.matriz-dominio th')).toHaveLength(1);
    expect(screen.getByText('10 módulos · 5 roles')).toBeInTheDocument();
    expect(celdasDeModulo(15)).toEqual(['Escritura', 'Lectura', 'Escritura', 'Lectura', '—Sin acceso']);
    fireEvent.change(screen.getByLabelText('Filtrar por dominio'), { target: { value: '' } });
    expect(screen.getByText('80 módulos · 5 roles')).toBeInTheDocument();
  });

  it('A5 la tabla tiene su propia región con scroll interno, enfocable y con nombre', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialMatriz(fetchMock);
    const region = screen.getByRole('region', { name: 'Matriz de permisos por rol' });
    expect(region).toHaveAttribute('tabindex', '0');
    expect(region).toContainElement(screen.getByRole('table'));
  });

  it('A5 muestra el error de la API al fallar la matriz', async () => {
    prepararApi({ errorMatriz: true });
    vi.useFakeTimers();
    render(<RolesPermisosPage />, '/?vista=matriz');
    vi.useRealTimers();
    const alerta = await screen.findByText('No se pudo cargar la matriz de permisos.');
    expect(alerta).toHaveAttribute('role', 'alert');
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('A5 las pestañas cambian de vista y reflejan ?vista= en la URL', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    expect(screen.getByTestId('ruta-actual')).toHaveTextContent(/^\/$/);
    expect(screen.getByRole('tab', { name: 'Roles' })).toHaveAttribute('aria-selected', 'true');
    fireEvent.click(screen.getByRole('tab', { name: 'Matriz' }));
    expect(screen.getByTestId('ruta-actual')).toHaveTextContent('/?vista=matriz');
    expect(await screen.findByRole('table')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Nuevo rol' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('tab', { name: 'Roles' }));
    expect(screen.getByTestId('ruta-actual')).toHaveTextContent(/^\/$/);
    expect(screen.getByRole('button', { name: 'Nuevo rol' })).toBeInTheDocument();
    expect(screen.queryByRole('table', { name: 'Nivel de acceso de cada rol a cada módulo' })).not.toBeInTheDocument();
  });

  it('A5 las flechas del teclado cambian de pestaña', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    const pestanaRoles = screen.getByRole('tab', { name: 'Roles' });
    pestanaRoles.focus();
    fireEvent.keyDown(pestanaRoles, { key: 'ArrowRight' });
    expect(screen.getByTestId('ruta-actual')).toHaveTextContent('/?vista=matriz');
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Matriz' })).toHaveFocus());
    fireEvent.keyDown(screen.getByRole('tab', { name: 'Matriz' }), { key: 'ArrowLeft' });
    expect(screen.getByTestId('ruta-actual')).toHaveTextContent(/^\/$/);
  });

  it('A5 al volver a Roles se conserva el rol seleccionado', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'VENTAS' }));
    await screen.findByRole('heading', { name: 'Ventas' });
    fireEvent.click(screen.getByRole('tab', { name: 'Matriz' }));
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('tab', { name: 'Roles' }));
    expect(screen.getByRole('heading', { name: 'Ventas' })).toBeInTheDocument();
  });
});
