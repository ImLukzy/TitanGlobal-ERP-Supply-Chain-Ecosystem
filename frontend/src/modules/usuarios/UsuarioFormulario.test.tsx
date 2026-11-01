import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  limpiarUsuarios,
  montarUsuariosCargados,
  prepararApiUsuarios,
} from './usuarios.testUtils';

afterEach(() => {
  limpiarUsuarios();
});

interface LlamadaRegistrada {
  url: string;
  init: RequestInit;
}

function llamadasCon(llamadas: LlamadaRegistrada[], metodo: string): LlamadaRegistrada[] {
  return llamadas.filter((llamada) => (llamada.init.method ?? 'GET') === metodo);
}

function abrirNuevo(): void {
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo usuario' }));
}

function completarFormulario(usuario: string, nombre: string, email: string): void {
  fireEvent.change(screen.getByLabelText('Nombre de usuario'), { target: { value: usuario } });
  fireEvent.change(screen.getByLabelText('Nombre completo'), { target: { value: nombre } });
  fireEvent.change(screen.getByLabelText('Email'), { target: { value: email } });
}

describe('formulario de usuarios: alta y edición', () => {
  it('A4 valida los campos localmente con los literales de la spec 43 y no llama a POST', async () => {
    const { fetchMock, llamadas } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    abrirNuevo();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar usuario' }));
    expect(screen.getByText('El nombre de usuario es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El nombre completo es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El email es obligatorio.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre de usuario')).toHaveAttribute('aria-describedby', 'usuario-nombre-usuario-error');
    fireEvent.change(screen.getByLabelText('Nombre de usuario'), { target: { value: 'ab' } });
    fireEvent.change(screen.getByLabelText('Nombre completo'), { target: { value: 'N'.repeat(101) } });
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'no-valido' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar usuario' }));
    expect(screen.getByText('El nombre de usuario solo admite letras minúsculas, números, punto, guion y guion bajo (3 a 30 caracteres).')).toBeInTheDocument();
    expect(screen.getByText('El nombre completo no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('El email no tiene un formato válido.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: `${'a'.repeat(145)}@t.example` } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar usuario' }));
    expect(screen.getByText('El email no tiene un formato válido.')).toBeInTheDocument();
    expect(llamadasCon(llamadas, 'POST')).toHaveLength(0);
  });

  it('A4 pasa a minúsculas el usuario y el email, y envía las casillas de roles en rolIds', async () => {
    const { fetchMock, llamadas } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    abrirNuevo();
    fireEvent.change(screen.getByLabelText('Nombre de usuario'), { target: { value: 'Ana.Nueva' } });
    expect(screen.getByLabelText('Nombre de usuario')).toHaveValue('ana.nueva');
    fireEvent.change(screen.getByLabelText('Nombre completo'), { target: { value: '  Ana Nueva  ' } });
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'Ana.Nueva@Titanglobal.example' } });
    fireEvent.click(screen.getByRole('checkbox', { name: 'CONSULTA · Consulta' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'VENTAS · Ventas' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar usuario' }));
    await waitFor(() => expect(llamadasCon(llamadas, 'POST')).toHaveLength(1));
    const [post] = llamadasCon(llamadas, 'POST');
    expect(post?.url).toBe('/api/v1/usuarios');
    expect(JSON.parse(String(post?.init.body))).toEqual({
      nombreUsuario: 'ana.nueva',
      nombreCompleto: 'Ana Nueva',
      email: 'ana.nueva@titanglobal.example',
      activo: true,
      rolIds: [2, 5],
    });
  });

  it('A4 muestra el 409 de usuario duplicado y conserva lo escrito', async () => {
    const { fetchMock } = prepararApiUsuarios({ errorGuardar: 'duplicado' });
    await montarUsuariosCargados(fetchMock);
    abrirNuevo();
    completarFormulario('ana.ventas', 'Ana Duplicada', 'ana.duplicada@titanglobal.example');
    fireEvent.click(screen.getByRole('checkbox', { name: 'VENTAS · Ventas' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar usuario' }));
    expect(await screen.findByText('Ya existe el usuario ana.ventas.')).toHaveAttribute('role', 'alert');
    expect(screen.getByLabelText('Nombre de usuario')).toHaveValue('ana.ventas');
    expect(screen.getByLabelText('Nombre completo')).toHaveValue('Ana Duplicada');
    expect(screen.getByLabelText('Email')).toHaveValue('ana.duplicada@titanglobal.example');
    expect(screen.getByRole('checkbox', { name: 'VENTAS · Ventas' })).toBeChecked();
  });

  it('A4 edita con PUT al id del usuario y envía los roles marcados', async () => {
    const { fetchMock, llamadas } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'luis.compras' }));
    await screen.findByRole('region', { name: 'Luis Castillo Compras' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    expect(screen.getByRole('heading', { name: 'Editar usuario' })).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre de usuario')).toHaveValue('luis.compras');
    expect(screen.getByRole('checkbox', { name: 'COMPRAS · Compras' })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: 'CONSULTA · Consulta' })).toBeChecked();
    fireEvent.click(screen.getByRole('checkbox', { name: 'COMPRAS · Compras' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar usuario' }));
    await waitFor(() => expect(llamadasCon(llamadas, 'PUT')).toHaveLength(1));
    const [put] = llamadasCon(llamadas, 'PUT');
    expect(put?.url).toBe('/api/v1/usuarios/4');
    expect(JSON.parse(String(put?.init.body)).rolIds).toEqual([5]);
  });

  it('A4 muestra el 409 de último ADMIN al quitar el rol y conserva la casilla desmarcada', async () => {
    const { fetchMock } = prepararApiUsuarios({ errorGuardar: 'ultimoAdmin' });
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'admin' }));
    await screen.findByRole('region', { name: 'Administrador General' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'ADMIN · Administrador' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar usuario' }));
    expect(await screen.findByText('Debe quedar al menos un usuario activo con el rol ADMIN.')).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('checkbox', { name: 'ADMIN · Administrador' })).not.toBeChecked();
    expect(screen.getByLabelText('Nombre de usuario')).toHaveValue('admin');
  });

  it('A4 muestra el 400 de rol inexistente sin perder los datos', async () => {
    const { fetchMock } = prepararApiUsuarios({ errorGuardar: 'rolInexistente' });
    await montarUsuariosCargados(fetchMock);
    abrirNuevo();
    completarFormulario('nuevo.rol', 'Nuevo Rol', 'nuevo.rol@titanglobal.example');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar usuario' }));
    expect(await screen.findByText('El rol 99 no existe.')).toHaveAttribute('role', 'alert');
    expect(screen.getByLabelText('Nombre completo')).toHaveValue('Nuevo Rol');
  });
});

describe('baja de usuarios', () => {
  it('A4 confirma la baja con DELETE, cierra el diálogo y vuelve a leer el listado', async () => {
    const { fetchMock, llamadas } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'ana.ventas' }));
    await screen.findByRole('region', { name: 'Ana Quispe Ventas' });
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar usuario' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    const [borrado] = llamadasCon(llamadas, 'DELETE');
    expect(borrado?.url).toBe('/api/v1/usuarios/2');
    expect(fetchMock.mock.calls.filter((llamada) => String(llamada[0]).includes('/usuarios?')).length).toBeGreaterThan(1);
  });

  it('A4 muestra el 409 de último ADMIN dentro del diálogo y no lo cierra', async () => {
    const { fetchMock } = prepararApiUsuarios({ errorEliminar: true });
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'admin' }));
    await screen.findByRole('region', { name: 'Administrador General' });
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar usuario' }));
    const dialogo = await screen.findByRole('dialog');
    expect(within(dialogo).getByRole('alert')).toHaveTextContent('Debe quedar al menos un usuario activo con el rol ADMIN.');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('A4 Escape cancela la baja sin petición y devuelve el foco al botón Eliminar', async () => {
    const { fetchMock } = prepararApiUsuarios();
    await montarUsuariosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'ana.ventas' }));
    await screen.findByRole('region', { name: 'Ana Quispe Ventas' });
    const rafSimulado = vi.spyOn(window, 'requestAnimationFrame').mockImplementation((callback: FrameRequestCallback) => {
      callback(0);
      return 0;
    });
    const abrirBaja = screen.getByRole('button', { name: 'Eliminar' });
    fireEvent.click(abrirBaja);
    fetchMock.mockClear();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(document.activeElement).toBe(abrirBaja);
    expect(fetchMock).not.toHaveBeenCalled();
    rafSimulado.mockRestore();
  });
});
