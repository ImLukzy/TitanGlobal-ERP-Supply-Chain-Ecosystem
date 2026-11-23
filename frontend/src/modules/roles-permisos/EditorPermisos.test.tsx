import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { DOMINIOS_PRUEBA, permisosVentasPrueba } from './permisos.testUtils';
import { esperarCargaInicialRoles, prepararApi } from './roles.testUtils';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

async function abrirEditor(fetchMock: ReturnType<typeof vi.fn>, codigo: string, nombre: string) {
  await esperarCargaInicialRoles(fetchMock);
  if (codigo !== 'ADMIN') {
    fireEvent.click(screen.getByRole('button', { name: codigo }));
    await screen.findByRole('heading', { name: nombre });
  }
  fireEvent.click(screen.getByRole('button', { name: 'Editar permisos' }));
  await screen.findByRole('heading', { name: `Permisos de ${nombre}` });
  await screen.findByRole('checkbox', { name: 'Leer Módulo 1' });
}

function abrirDominio(dominio: string): void {
  const detalles = screen.getByText(dominio).closest('details');
  if (detalles === null) {
    throw new Error(`No se encontró el dominio ${dominio}`);
  }
  detalles.open = true;
}

function casilla(nombre: string): HTMLInputElement {
  return screen.getByRole('checkbox', { name: nombre }) as HTMLInputElement;
}

describe('editor de permisos del rol (módulo 72)', () => {
  it('A4 abre el editor con los 80 módulos en 8 dominios y solo el primero desplegado', async () => {
    const { fetchMock } = prepararApi();
    await abrirEditor(fetchMock, 'VENTAS', 'Ventas');
    const llamadas = fetchMock.mock.calls.filter((llamada) => String(llamada[0]).endsWith('/roles/2/permisos'));
    expect(llamadas).toHaveLength(1);
    const dominios = document.querySelectorAll('details.permisos-dominio');
    expect(dominios).toHaveLength(8);
    expect(Array.from(dominios).map((elemento) => elemento.querySelector('.permisos-dominio-nombre')?.textContent)).toEqual(DOMINIOS_PRUEBA);
    expect(Array.from(dominios).filter((elemento) => (elemento as HTMLDetailsElement).open)).toHaveLength(1);
    expect(document.querySelectorAll('.permiso-fila')).toHaveLength(80);
    expect(screen.getByText('10 de 10 con lectura · 10 con escritura')).toBeInTheDocument();
    expect(screen.getByText('Sin cambios')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Guardar permisos' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Descartar' })).toBeDisabled();
  });

  it('A4 mueve el foco al título al abrir y lo devuelve al botón al cerrar', async () => {
    const { fetchMock } = prepararApi();
    await abrirEditor(fetchMock, 'VENTAS', 'Ventas');
    expect(screen.getByRole('heading', { name: 'Permisos de Ventas' })).toHaveFocus();
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Editar permisos' })).toHaveFocus());
    expect(screen.queryByRole('heading', { name: 'Permisos de Ventas' })).not.toBeInTheDocument();
  });

  it('A4 marcar «Escribir» marca «Leer» y desmarcar «Leer» desmarca «Escribir»', async () => {
    const { fetchMock } = prepararApi();
    await abrirEditor(fetchMock, 'VENTAS', 'Ventas');
    abrirDominio('Inventarios y Almacenes (WMS)');
    fireEvent.click(casilla('Escribir Módulo 22'));
    expect(casilla('Leer Módulo 22').checked).toBe(true);
    expect(casilla('Escribir Módulo 22').checked).toBe(true);
    fireEvent.click(casilla('Leer Módulo 22'));
    expect(casilla('Leer Módulo 22').checked).toBe(false);
    expect(casilla('Escribir Módulo 22').checked).toBe(false);
    fireEvent.click(casilla('Leer Módulo 1'));
    expect(casilla('Escribir Módulo 1').checked).toBe(false);
  });

  it('A4 los atajos por dominio marcan todo lectura, todo escritura o ninguno', async () => {
    const { fetchMock } = prepararApi();
    await abrirEditor(fetchMock, 'VENTAS', 'Ventas');
    fireEvent.click(screen.getByRole('button', { name: 'Todo lectura en Comercial, Ventas y CRM' }));
    expect(casilla('Leer Módulo 5').checked).toBe(true);
    expect(casilla('Escribir Módulo 5').checked).toBe(false);
    fireEvent.click(screen.getByRole('button', { name: 'Todo escritura en Comercial, Ventas y CRM' }));
    expect(casilla('Escribir Módulo 5').checked).toBe(true);
    fireEvent.click(screen.getByRole('button', { name: 'Ninguno en Comercial, Ventas y CRM' }));
    expect(casilla('Leer Módulo 5').checked).toBe(false);
    expect(casilla('Escribir Módulo 5').checked).toBe(false);
    expect(screen.getByText('10 cambios sin guardar')).toBeInTheDocument();
  });

  it('A4 cuenta los cambios sin guardar y «Descartar» vuelve al estado guardado', async () => {
    const { fetchMock } = prepararApi();
    await abrirEditor(fetchMock, 'VENTAS', 'Ventas');
    abrirDominio('Inventarios y Almacenes (WMS)');
    fireEvent.click(casilla('Escribir Módulo 22'));
    expect(screen.getByText('1 cambio sin guardar')).toBeInTheDocument();
    fireEvent.click(casilla('Leer Módulo 23'));
    expect(screen.getByText('2 cambios sin guardar')).toBeInTheDocument();
    expect(screen.getByText('Si cierras o cambias de rol sin guardar, se pierden estos cambios.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Descartar' }));
    expect(screen.getByText('Sin cambios')).toBeInTheDocument();
    expect(casilla('Escribir Módulo 22').checked).toBe(false);
    expect(casilla('Leer Módulo 23').checked).toBe(false);
  });

  it('A4 guarda con PUT los 80 módulos con el cuerpo exacto y refresca los totales', async () => {
    const { fetchMock } = prepararApi();
    await abrirEditor(fetchMock, 'VENTAS', 'Ventas');
    abrirDominio('Inventarios y Almacenes (WMS)');
    fireEvent.click(casilla('Escribir Módulo 22'));
    const listadosAntes = fetchMock.mock.calls.filter((llamada) => String(llamada[0]).includes('/roles?')).length;
    fireEvent.click(screen.getByRole('button', { name: 'Guardar permisos' }));
    await waitFor(() => expect(screen.getByText('Sin cambios')).toBeInTheDocument());
    const put = fetchMock.mock.calls.find((llamada) => (llamada[1] as RequestInit | undefined)?.method === 'PUT');
    expect(String(put?.[0])).toBe('/api/v1/roles/2/permisos');
    const esperado = permisosVentasPrueba().map((permiso) => ({
      moduloNumero: permiso.moduloNumero,
      puedeLeer: permiso.moduloNumero === 22 ? true : permiso.puedeLeer,
      puedeEscribir: permiso.moduloNumero === 22 ? true : permiso.puedeEscribir,
    }));
    expect((put?.[1] as RequestInit).body).toBe(JSON.stringify({ permisos: esperado }));
    await waitFor(() => {
      const listadosDespues = fetchMock.mock.calls.filter((llamada) => String(llamada[0]).includes('/roles?')).length;
      expect(listadosDespues).toBe(listadosAntes + 1);
    });
    expect(casilla('Escribir Módulo 22').checked).toBe(true);
  });

  it('A4 muestra el error de la API sin perder los cambios', async () => {
    const { fetchMock } = prepararApi({ errorGuardarPermisos: true });
    await abrirEditor(fetchMock, 'VENTAS', 'Ventas');
    abrirDominio('Inventarios y Almacenes (WMS)');
    fireEvent.click(casilla('Escribir Módulo 22'));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar permisos' }));
    const alerta = await screen.findByRole('alert');
    expect(alerta).toHaveTextContent('El rol ADMIN es de sistema y no se puede modificar de esa forma.');
    expect(screen.getByText('1 cambio sin guardar')).toBeInTheDocument();
    expect(casilla('Escribir Módulo 22').checked).toBe(true);
    expect(screen.getByRole('button', { name: 'Guardar permisos' })).toBeEnabled();
  });

  it('A4 en un rol de sistema deshabilita las casillas que quitarían permisos y explica por qué', async () => {
    const { fetchMock } = prepararApi();
    await abrirEditor(fetchMock, 'ADMIN', 'Administrador');
    expect(screen.getByText(/Este rol es de sistema: no se le pueden quitar permisos/)).toBeInTheDocument();
    expect(casilla('Leer Módulo 1')).toBeDisabled();
    expect(casilla('Escribir Módulo 1')).toBeDisabled();
    expect(casilla('Leer Módulo 1')).toHaveAccessibleDescription(/no se le pueden quitar permisos/);
    expect(screen.getByRole('button', { name: 'Ninguno en Comercial, Ventas y CRM' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Todo lectura en Comercial, Ventas y CRM' })).toBeEnabled();
    expect(screen.getByText('Sin cambios')).toBeInTheDocument();
  });

  it('A4 en un rol personalizado ninguna casilla está bloqueada', async () => {
    const { fetchMock } = prepararApi();
    await abrirEditor(fetchMock, 'VENTAS', 'Ventas');
    const casillas = within(document.querySelector('.permisos-dominios') as HTMLElement).getAllByRole('checkbox');
    expect(casillas.length).toBeGreaterThan(0);
    expect(casillas.every((elemento) => !(elemento as HTMLInputElement).disabled)).toBe(true);
    expect(screen.queryByText(/Este rol es de sistema/)).not.toBeInTheDocument();
  });

  it('A4 muestra el error si no se pueden cargar los permisos', async () => {
    const { fetchMock } = prepararApi({ errorPermisos: true });
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Editar permisos' }));
    const alerta = await screen.findByText('No se pudieron cargar los permisos del rol.');
    expect(alerta).toHaveAttribute('role', 'alert');
    expect(screen.queryByRole('button', { name: 'Guardar permisos' })).not.toBeInTheDocument();
  });

  it('A4 cambiar de rol cierra el editor', async () => {
    const { fetchMock } = prepararApi();
    await abrirEditor(fetchMock, 'VENTAS', 'Ventas');
    fireEvent.click(screen.getByRole('button', { name: 'COMPRAS' }));
    await screen.findByRole('heading', { name: 'Compras' });
    expect(screen.queryByRole('heading', { name: 'Permisos de Ventas' })).not.toBeInTheDocument();
  });
});
