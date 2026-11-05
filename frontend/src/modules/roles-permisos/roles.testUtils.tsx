import { act, render as renderBase, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import type { ReactNode } from 'react';
import { expect, vi } from 'vitest';
import { crearPermisosPrueba, permisosAdminPrueba, permisosVentasPrueba } from './permisos.testUtils';
import { RolesPermisosPage } from './RolesPermisosPage';
import type { PermisoModulo } from './tipos';

export interface OpcionesApiRoles {
  /** El GET de los permisos de un rol responde 500. */
  errorPermisos?: boolean;
  /** El PUT de los permisos responde 409. */
  errorGuardarPermisos?: boolean;
  vacio?: boolean;
  errorLista?: boolean;
  errorGuardar?: boolean;
  paginado?: boolean;
  /** El PUT responde 409 de código repetido. */
  errorEditar?: boolean;
  /** El DELETE de un rol personalizado responde 409 porque tiene usuarios. */
  errorBajaConUsuarios?: boolean;
  /** El GET del detalle de este rol responde 500. */
  errorDetalleId?: number;
  /** El GET del detalle de este rol queda en espera hasta llamar a `liberarDetalle`. */
  retrasarDetalleId?: number;
}

interface RolPrueba {
  id: number;
  codigo: string;
  nombre: string;
  descripcion: string | null;
  esSistema: boolean;
  modulosConLectura: number;
  modulosConEscritura: number;
}

export function render(ui: ReactNode) {
  return renderBase(<MemoryRouter>{ui}</MemoryRouter>);
}

const rolesIniciales: RolPrueba[] = [
  { id: 1, codigo: 'ADMIN', nombre: 'Administrador', descripcion: 'Acceso total al sistema.', esSistema: true, modulosConLectura: 80, modulosConEscritura: 80 },
  { id: 2, codigo: 'VENTAS', nombre: 'Ventas', descripcion: 'Gestión comercial.', esSistema: false, modulosConLectura: 11, modulosConEscritura: 10 },
  { id: 3, codigo: 'COMPRAS', nombre: 'Compras', descripcion: 'Gestión de abastecimiento.', esSistema: false, modulosConLectura: 14, modulosConEscritura: 10 },
  { id: 4, codigo: 'ALMACEN', nombre: 'Almacén', descripcion: 'Operación de inventarios.', esSistema: false, modulosConLectura: 20, modulosConEscritura: 10 },
  { id: 5, codigo: 'CONSULTA', nombre: 'Consulta', descripcion: 'Solo lectura.', esSistema: false, modulosConLectura: 80, modulosConEscritura: 0 },
  { id: 6, codigo: 'AUDITOR', nombre: 'Auditor', descripcion: null, esSistema: false, modulosConLectura: 1, modulosConEscritura: 1 },
];

function listadoRol(rol: RolPrueba) {
  return {
    id: rol.id,
    codigo: rol.codigo,
    nombre: rol.nombre,
    esSistema: rol.esSistema,
    modulosConLectura: rol.modulosConLectura,
    modulosConEscritura: rol.modulosConEscritura,
  };
}

function permisosDeRol(rolId: number): PermisoModulo[] {
  if (rolId === 1) {
    return permisosAdminPrueba();
  }
  if (rolId === 2) {
    return permisosVentasPrueba();
  }
  return crearPermisosPrueba(() => 'ninguno');
}

export function prepararApi(opciones: OpcionesApiRoles = {}) {
  const roles = [...rolesIniciales];
  let liberarDetalle: () => void = () => undefined;
  const compuertaDetalle = new Promise<void>((resolver) => {
    liberarDetalle = resolver;
  });
  const llamadas: Array<{ url: string; init: RequestInit }> = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });
    const rutaPermisos = /\/roles\/(\d+)\/permisos$/.exec(url);
    if (rutaPermisos !== null) {
      const rolId = Number(rutaPermisos[1]);
      if (metodo === 'PUT') {
        if (opciones.errorGuardarPermisos) {
          return Response.json(
            { StatusCode: 409, Message: 'El rol ADMIN es de sistema y no se puede modificar de esa forma.' },
            { status: 409 },
          );
        }
        const enviados = (JSON.parse(String(init.body)) as { permisos: Array<Pick<PermisoModulo, 'moduloNumero' | 'puedeLeer' | 'puedeEscribir'>> }).permisos;
        const base = permisosDeRol(rolId);
        return Response.json(base.map((permiso) => ({ ...permiso, ...enviados.find((item) => item.moduloNumero === permiso.moduloNumero) })));
      }
      if (opciones.errorPermisos) {
        return Response.json({ StatusCode: 500, Message: 'No se pudieron cargar los permisos del rol.' }, { status: 500 });
      }
      return Response.json(permisosDeRol(rolId));
    }
    if (url.includes('/roles?') && metodo === 'GET') {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'No se pudo cargar la lista de roles.' }, { status: 500 });
      }
      if (opciones.vacio) {
        return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
      }
      const consulta = new URL(url, 'http://localhost').searchParams;
      const pagina = Number(consulta.get('pagina') ?? '1');
      const items = (pagina > 1 ? roles.slice(0, 2) : roles).map(listadoRol);
      const total = opciones.paginado ? 12 : roles.length;
      return Response.json({ items, total, pagina, tamanoPagina: 10 });
    }
    if (metodo === 'POST') {
      if (opciones.errorGuardar) {
        return Response.json({ StatusCode: 409, Message: 'Ya existe un rol con el código VENTAS.' }, { status: 409 });
      }
      return Response.json({ id: 7 });
    }
    const id = Number(url.slice(url.lastIndexOf('/') + 1));
    if (metodo === 'GET' && Number.isInteger(id)) {
      if (id === opciones.retrasarDetalleId) {
        await compuertaDetalle;
      }
      if (id === opciones.errorDetalleId) {
        return Response.json({ StatusCode: 500, Message: 'No se pudo cargar el detalle del rol.' }, { status: 500 });
      }
      const rol = roles.find((item) => item.id === id);
      if (rol === undefined) {
        return Response.json({ StatusCode: 404, Message: `El rol ${id} no existe.` }, { status: 404 });
      }
      return Response.json({ ...listadoRol(rol), descripcion: rol.descripcion });
    }
    if (metodo === 'PUT') {
      if (opciones.errorEditar) {
        return Response.json({ StatusCode: 409, Message: 'Ya existe un rol con el código VENTAS.' }, { status: 409 });
      }
      return Response.json({ id });
    }
    if (metodo === 'DELETE') {
      const rol = roles.find((item) => item.id === id);
      if (rol?.esSistema === true) {
        return Response.json(
          { StatusCode: 409, Message: `El rol ${rol.codigo} es de sistema y no se puede modificar de esa forma.` },
          { status: 409 },
        );
      }
      if (opciones.errorBajaConUsuarios && rol !== undefined) {
        return Response.json(
          { StatusCode: 409, Message: `No se puede eliminar el rol ${rol.codigo} porque tiene usuarios.` },
          { status: 409 },
        );
      }
      const indice = roles.findIndex((item) => item.id === id);
      if (indice >= 0) {
        roles.splice(indice, 1);
      }
      return new Response(null, { status: 204 });
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas, liberarDetalle };
}

export async function esperarCargaInicialRoles(fetchMock: ReturnType<typeof vi.fn>) {
  vi.useFakeTimers();
  render(<RolesPermisosPage />);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();
  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'ADMIN' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Administrador' })).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/roles?'))).toHaveLength(1);
  });
}
