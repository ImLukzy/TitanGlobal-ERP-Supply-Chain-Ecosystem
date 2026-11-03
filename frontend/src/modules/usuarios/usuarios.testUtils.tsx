import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { modulos } from '../catalogo';
import { UsuariosPage } from './UsuariosPage';
import type { NivelPermiso, PermisoEfectivo } from './tipos';

interface UsuarioMock {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  email: string;
  activo: boolean;
  roles: string[];
  rolIds: number[];
}

export interface LlamadaUsuarios {
  url: string;
  init: RequestInit;
}

export interface OpcionesApiUsuarios {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorRoles?: boolean;
  errorDetalle?: boolean;
  errorPermisos?: boolean;
  paginado?: boolean;
  errorGuardar?: 'duplicado' | 'ultimoAdmin' | 'rolInexistente';
  errorEliminar?: boolean;
  totalTrasBaja?: number;
  totalListado?: number;
}

export const rolesMock = [
  { id: 1, codigo: 'ADMIN', nombre: 'Administrador', esSistema: true, modulosConLectura: 80, modulosConEscritura: 80 },
  { id: 2, codigo: 'VENTAS', nombre: 'Ventas', esSistema: false, modulosConLectura: 11, modulosConEscritura: 10 },
  { id: 3, codigo: 'COMPRAS', nombre: 'Compras', esSistema: false, modulosConLectura: 14, modulosConEscritura: 10 },
  { id: 4, codigo: 'ALMACEN', nombre: 'Almacén', esSistema: false, modulosConLectura: 20, modulosConEscritura: 10 },
  { id: 5, codigo: 'CONSULTA', nombre: 'Consulta', esSistema: false, modulosConLectura: 80, modulosConEscritura: 0 },
];

// Los usuarios replican el seed de la spec 43 §4 (dominio ficticio @titanglobal.example).
const usuariosMock: UsuarioMock[] = [
  { id: 1, nombreUsuario: 'admin', nombreCompleto: 'Administrador General', email: 'admin@titanglobal.example', activo: true, roles: ['ADMIN'], rolIds: [1] },
  { id: 2, nombreUsuario: 'ana.ventas', nombreCompleto: 'Ana Quispe Ventas', email: 'ana.ventas@titanglobal.example', activo: true, roles: ['VENTAS'], rolIds: [2] },
  { id: 3, nombreUsuario: 'invitado', nombreCompleto: 'Invitado Demo', email: 'invitado@titanglobal.example', activo: true, roles: [], rolIds: [] },
  { id: 4, nombreUsuario: 'luis.compras', nombreCompleto: 'Luis Castillo Compras', email: 'luis.compras@titanglobal.example', activo: true, roles: ['COMPRAS', 'CONSULTA'], rolIds: [3, 5] },
  { id: 5, nombreUsuario: 'maria.almacen', nombreCompleto: 'María Ruiz Almacén', email: 'maria.almacen@titanglobal.example', activo: false, roles: ['ALMACEN'], rolIds: [4] },
];

// Matriz de la spec 41 §4 (seed de roles): escritura y lectura por rango de módulos.
const permisosPorRol: Record<string, { escritura: [number, number][]; lectura: [number, number][] }> = {
  ADMIN: { escritura: [[1, 80]], lectura: [] },
  VENTAS: { escritura: [[1, 10]], lectura: [[21, 21]] },
  COMPRAS: { escritura: [[11, 20]], lectura: [[21, 24]] },
  ALMACEN: { escritura: [[21, 30]], lectura: [[11, 20]] },
  CONSULTA: { escritura: [], lectura: [[1, 80]] },
};

function enRangos(numero: number, rangos: [number, number][]): boolean {
  return rangos.some(([desde, hasta]) => {
    return numero >= desde && numero <= hasta;
  });
}

function calcularPermisos(usuario: UsuarioMock): PermisoEfectivo[] {
  return modulos.map((modulo) => {
    const activos = usuario.activo ? usuario.roles : [];
    const conEscritura = activos.filter((codigo) => enRangos(modulo.numero, permisosPorRol[codigo].escritura));
    const conLectura = activos.filter((codigo) => enRangos(modulo.numero, permisosPorRol[codigo].lectura));
    let permiso: NivelPermiso = 'Ninguno';
    let otorgadoPor: string[] = [];
    if (conEscritura.length > 0) {
      permiso = 'Escritura';
      otorgadoPor = conEscritura;
    } else if (conLectura.length > 0) {
      permiso = 'Lectura';
      otorgadoPor = conLectura;
    }
    return {
      moduloNumero: modulo.numero,
      moduloNombre: modulo.nombre,
      dominio: modulo.dominio,
      permiso,
      otorgadoPor,
    };
  });
}

function listado(usuario: UsuarioMock) {
  const { id, nombreUsuario, nombreCompleto, email, activo, roles } = usuario;
  return { id, nombreUsuario, nombreCompleto, email, activo, roles };
}

function detalle(usuario: UsuarioMock) {
  const permisos = calcularPermisos(usuario);
  return {
    ...listado(usuario),
    fechaAlta: '2026-10-15T17:33:21Z',
    rolIds: usuario.rolIds,
    modulosConLectura: permisos.filter((permiso) => permiso.permiso !== 'Ninguno').length,
    modulosConEscritura: permisos.filter((permiso) => permiso.permiso === 'Escritura').length,
  };
}

const resumenMock = {
  total: 5,
  activos: 4,
  inactivos: 1,
  sinRol: 1,
  porRol: rolesMock.map((rol) => {
    return {
      rolId: rol.id,
      codigo: rol.codigo,
      usuarios: usuariosMock.filter((usuario) => usuario.roles.includes(rol.codigo)).length,
    };
  }),
};

function respuestaLista(parsed: URL, opciones: OpcionesApiUsuarios): Response {
  if (opciones.errorLista) {
    return Response.json({ StatusCode: 500, Message: 'No se pudo cargar la lista de usuarios.' }, { status: 500 });
  }
  if (opciones.listaVacia) {
    return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
  }
  const busqueda = parsed.searchParams.get('busqueda')?.toLowerCase() ?? '';
  const rolId = parsed.searchParams.get('rolId');
  const activo = parsed.searchParams.get('activo');
  const filtrados = usuariosMock.filter((usuario) => {
    const coincide = busqueda === ''
      || usuario.nombreUsuario.includes(busqueda)
      || usuario.nombreCompleto.toLowerCase().includes(busqueda)
      || usuario.email.includes(busqueda);
    const coincideRol = rolId === null || usuario.rolIds.includes(Number(rolId));
    const coincideActivo = activo === null || usuario.activo === (activo === 'true');
    return coincide && coincideRol && coincideActivo;
  });
  const total = opciones.totalListado ?? (opciones.paginado ? 12 : filtrados.length);
  return Response.json({
    items: filtrados.map(listado),
    total,
    pagina: Number(parsed.searchParams.get('pagina') ?? '1'),
    tamanoPagina: 10,
  });
}

function respuestaUsuario(id: number, metodo: string, opciones: OpcionesApiUsuarios): Response {
  const usuario = usuariosMock.find((item) => item.id === id);
  if (metodo === 'DELETE') {
    if (opciones.errorEliminar) {
      return Response.json({ StatusCode: 409, Message: 'Debe quedar al menos un usuario activo con el rol ADMIN.' }, { status: 409 });
    }
    return new Response(null, { status: 204 });
  }
  if (metodo === 'PUT') {
    return respuestaGuardar(id, opciones);
  }
  if (opciones.errorDetalle) {
    return Response.json({ StatusCode: 500, Message: 'No se pudo cargar el detalle del usuario.' }, { status: 500 });
  }
  if (usuario === undefined) {
    return Response.json({ StatusCode: 404, Message: `El usuario ${id} no existe.` }, { status: 404 });
  }
  return Response.json(detalle(usuario));
}

function respuestaGuardar(id: number, opciones: OpcionesApiUsuarios): Response {
  if (opciones.errorGuardar === 'duplicado') {
    return Response.json({ StatusCode: 409, Message: 'Ya existe el usuario ana.ventas.' }, { status: 409 });
  }
  if (opciones.errorGuardar === 'ultimoAdmin') {
    return Response.json({ StatusCode: 409, Message: 'Debe quedar al menos un usuario activo con el rol ADMIN.' }, { status: 409 });
  }
  if (opciones.errorGuardar === 'rolInexistente') {
    return Response.json({ StatusCode: 400, Message: 'El rol 99 no existe.' }, { status: 400 });
  }
  return Response.json({ id });
}

function respuestaMock(parsed: URL, metodo: string, opciones: OpcionesApiUsuarios): Response {
  const ruta = parsed.pathname;
  if (ruta.endsWith('/roles')) {
    if (opciones.errorRoles) {
      return Response.json({ StatusCode: 500, Message: 'No se pudieron cargar los roles.' }, { status: 500 });
    }
    return Response.json({ items: rolesMock, total: rolesMock.length, pagina: 1, tamanoPagina: 100 });
  }
  if (ruta.endsWith('/usuarios/resumen')) {
    return Response.json(resumenMock);
  }
  const permisos = /\/usuarios\/(\d+)\/permisos$/.exec(ruta);
  if (permisos) {
    const usuario = usuariosMock.find((item) => item.id === Number(permisos[1]));
    if (opciones.errorPermisos) {
      return Response.json({ StatusCode: 500, Message: 'No se pudieron cargar los permisos efectivos.' }, { status: 500 });
    }
    return Response.json(usuario === undefined ? [] : calcularPermisos(usuario));
  }
  const individual = /\/usuarios\/(\d+)$/.exec(ruta);
  if (individual) {
    return respuestaUsuario(Number(individual[1]), metodo, opciones);
  }
  if (ruta.endsWith('/usuarios') && metodo === 'POST') {
    return respuestaGuardar(9, opciones);
  }
  if (ruta.endsWith('/usuarios')) {
    return respuestaLista(parsed, opciones);
  }
  return Response.json({});
}

export function prepararApiUsuarios(opciones: OpcionesApiUsuarios = {}) {
  const llamadas: LlamadaUsuarios[] = [];
  // Copia mutable: tras una baja correcta el total del listado pasa a totalTrasBaja.
  const vigentes: OpcionesApiUsuarios = { ...opciones };
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });
    const respuesta = respuestaMock(parsed, metodo, vigentes);
    if (metodo === 'DELETE' && respuesta.ok && opciones.totalTrasBaja !== undefined) {
      vigentes.totalListado = opciones.totalTrasBaja;
    }
    return respuesta;
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

export function renderUsuarios(path = '/modulos/71-usuarios') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <UsuariosPage />
    </MemoryRouter>,
  );
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando usuarios…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de usuarios…')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'ana.ventas' })).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((llamada) => String(llamada[0]).includes('/usuarios?'))).toHaveLength(1);
    expect(fetchMock.mock.calls.filter((llamada) => String(llamada[0]).endsWith('/usuarios/resumen'))).toHaveLength(1);
    expect(fetchMock.mock.calls.filter((llamada) => String(llamada[0]).endsWith('/roles?tamanoPagina=100'))).toHaveLength(1);
  });
}

// Monta la pantalla con temporizadores falsos para que el temporizador de búsqueda (300 ms) se resuelva sin esperas reales.
export async function montarUsuariosCargados(fetchMock: ReturnType<typeof vi.fn>, path?: string): Promise<void> {
  vi.useFakeTimers();
  renderUsuarios(path);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock);
}

export function limpiarUsuarios(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
