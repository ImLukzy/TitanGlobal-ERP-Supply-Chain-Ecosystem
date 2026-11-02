import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { RolesPermisosPage } from './RolesPermisosPage';
import { esperarCargaInicialRoles, prepararApi, render } from './roles.testUtils';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('pantalla de Roles (módulo 72)', () => {
  it('A3 presenta la tabla de roles, la pastilla de tipo y el detalle del rol inicial', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    const tabla = screen.getByRole('region', { name: 'Tabla de roles' });
    expect(within(tabla).getAllByRole('row')).toHaveLength(7);
    expect(within(tabla).getByText('De sistema')).toBeInTheDocument();
    expect(within(tabla).getAllByText('Personalizado')).toHaveLength(5);
    expect(within(tabla).getAllByText('80 módulos').length).toBeGreaterThan(0);
    expect(within(tabla).getAllByText('1 módulo')).toHaveLength(2);
    const panel = screen.getByRole('heading', { name: 'Administrador' }).closest('section') as HTMLElement;
    expect(within(panel).getByText('Acceso total al sistema.')).toBeInTheDocument();
    expect(within(panel).getByText('Módulos con escritura')).toBeInTheDocument();
  });

  it('A3 hace una sola petición de listado al cargar', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/roles?'))).toHaveLength(1);
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe('/api/v1/roles?pagina=1&tamanoPagina=10');
  });

  it('A3 aplica la búsqueda tras 300 ms y solo consulta el último texto', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    vi.useFakeTimers();
    fetchMock.mockClear();
    const busqueda = screen.getByRole('searchbox', { name: 'Buscar por código o nombre' });
    fireEvent.change(busqueda, { target: { value: 'V' } });
    fireEvent.change(busqueda, { target: { value: 'Ve' } });
    fireEvent.change(busqueda, { target: { value: 'Ventas' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(fetchMock).not.toHaveBeenCalled();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    const consultas = fetchMock.mock.calls.filter((call) => String(call[0]).includes('/roles?'));
    expect(consultas).toHaveLength(1);
    expect(String(consultas[0]?.[0])).toContain('busqueda=Ventas');
  });

  it('A3 pagina la lista y vuelve a la página 1 al buscar', async () => {
    const { fetchMock } = prepararApi({ paginado: true });
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(screen.getByText('Página 2 de 2')).toBeInTheDocument());
    expect(fetchMock.mock.calls.some((call) => String(call[0]).includes('pagina=2'))).toBe(true);
    fireEvent.change(screen.getByRole('searchbox', { name: 'Buscar por código o nombre' }), { target: { value: 'A' } });
    await waitFor(() => expect(screen.getByText('Página 1 de 2')).toBeInTheDocument());
  });

  it('A3 presenta estados vacío y error de carga', async () => {
    prepararApi({ vacio: true });
    const { unmount } = render(<RolesPermisosPage />);
    expect(await screen.findByText('No hay roles que coincidan')).toBeInTheDocument();
    unmount();
    prepararApi({ errorLista: true });
    render(<RolesPermisosPage />);
    expect(await screen.findByText('No se pudo cargar la lista de roles.')).toBeInTheDocument();
  });

  it('A3 muestra «Sin descripción» cuando el rol no la tiene', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'AUDITOR' }));
    const titulo = await screen.findByRole('heading', { name: 'Auditor' });
    const panel = titulo.closest('section') as HTMLElement;
    expect(within(panel).getByText('Sin descripción')).toBeInTheDocument();
    expect(within(panel).getByText('Personalizado')).toBeInTheDocument();
  });

  it('A3 valida el formulario localmente y no llama a POST', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo rol' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar rol' }));
    expect(screen.getByText('El código es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El nombre del rol es obligatorio.')).toBeInTheDocument();
    expect(screen.getByLabelText('Código')).toHaveAttribute('aria-describedby', 'rol-codigo-error');
    expect(screen.getByLabelText('Nombre')).toHaveAttribute('aria-describedby', 'rol-nombre-error');
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'ab cd' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'N'.repeat(101) } });
    fireEvent.change(screen.getByLabelText('Descripción'), { target: { value: 'D'.repeat(301) } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar rol' }));
    expect(screen.getByText('El código solo admite letras, números, guiones y guion bajo (2 a 30 caracteres).')).toBeInTheDocument();
    expect(screen.getByText('El nombre del rol no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('La descripción no puede superar 300 caracteres.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => (call[1] as RequestInit | undefined)?.method === 'POST')).toBe(false);
  });

  it('A3 crea el rol con POST, código en mayúsculas y cuerpo exacto', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo rol' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'auditoria' } });
    expect(screen.getByLabelText('Código')).toHaveValue('AUDITORIA');
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: '  Auditoría interna ' } });
    fireEvent.change(screen.getByLabelText('Descripción'), { target: { value: 'Revisa registros.' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar rol' }));
    const cuerpo = JSON.stringify({ codigo: 'AUDITORIA', nombre: 'Auditoría interna', descripcion: 'Revisa registros.' });
    await waitFor(() => {
      const envio = fetchMock.mock.calls.find((call) => (call[1] as RequestInit | undefined)?.method === 'POST');
      expect(String(envio?.[0])).toBe('/api/v1/roles');
      expect((envio?.[1] as RequestInit).body).toBe(cuerpo);
    });
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nuevo rol' })).not.toBeInTheDocument());
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/roles?')).length).toBeGreaterThan(1);
  });

  it('A3 muestra el 409 de código repetido sin perder lo escrito', async () => {
    const { fetchMock } = prepararApi({ errorGuardar: true });
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo rol' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'ventas' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Ventas nuevo' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar rol' }));
    expect(await screen.findByText('Ya existe un rol con el código VENTAS.')).toHaveAttribute('role', 'alert');
    expect(screen.getByLabelText('Código')).toHaveValue('VENTAS');
    expect(screen.getByLabelText('Nombre')).toHaveValue('Ventas nuevo');
  });

  it('A3 precarga la edición, bloquea el código de un rol de sistema y envía PUT al id', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    expect(screen.getByRole('heading', { name: 'Editar rol' })).toBeInTheDocument();
    expect(screen.getByLabelText('Código')).toHaveValue('ADMIN');
    expect(screen.getByLabelText('Código')).toBeDisabled();
    expect(screen.getByText('El código de un rol de sistema no se puede cambiar.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Administrador general' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar rol' }));
    await waitFor(() => {
      const envio = fetchMock.mock.calls.find((call) => (call[1] as RequestInit | undefined)?.method === 'PUT');
      expect(String(envio?.[0])).toBe('/api/v1/roles/1');
      expect((envio?.[1] as RequestInit).body).toBe(
        JSON.stringify({ codigo: 'ADMIN', nombre: 'Administrador general', descripcion: 'Acceso total al sistema.' }),
      );
    });
  });

  it('A3 permite cambiar el código de un rol que no es de sistema', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'VENTAS' }));
    await screen.findByRole('heading', { name: 'Ventas' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    expect(screen.getByLabelText('Código')).toBeEnabled();
    expect(screen.queryByText('El código de un rol de sistema no se puede cambiar.')).not.toBeInTheDocument();
  });

  it('A3 confirma la baja de un rol y refresca la lista', async () => {
    const { fetchMock, llamadas } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'VENTAS' }));
    await screen.findByRole('heading', { name: 'Ventas' });
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar rol' }));
    await waitFor(() => expect(llamadas.some((llamada) => llamada.init.method === 'DELETE' && llamada.url === '/api/v1/roles/2')).toBe(true));
    await waitFor(() => expect(llamadas.filter((llamada) => llamada.url.includes('/roles?')).length).toBeGreaterThan(1));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('A3 muestra el 409 de rol de sistema dentro del diálogo de baja', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar rol' }));
    const dialogo = screen.getByRole('dialog');
    expect(await within(dialogo).findByText('El rol ADMIN es de sistema y no se puede modificar de esa forma.')).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('A3 Escape cancela la baja sin petición y devuelve el foco', async () => {
    const { fetchMock } = prepararApi();
    await esperarCargaInicialRoles(fetchMock);
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

  it('A6 registra el módulo 72 desde el catálogo', async () => {
    prepararApi();
    render(<RolesPermisosPage />);
    expect(screen.getByRole('heading', { name: '72 · RolesPermisos' })).toBeInTheDocument();
    const migas = screen.getByRole('navigation', { name: 'Miga de pan' });
    expect(within(migas).getByRole('link', { name: 'Inicio' })).toHaveAttribute('href', '/');
  });
});
