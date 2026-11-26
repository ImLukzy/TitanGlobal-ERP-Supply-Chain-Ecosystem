import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { pantallas } from '../pantallas';
import type { EstadoRuta } from './tipos';

const ruta = '/api/v1/rutas-produccion';
const producto = 'Laptop Pro';
const conflictoVersion = 'Ya existe la versión 2 para el producto 1.';
const centroInactivo = 'El centro de trabajo 3 está inactivo.';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

interface Opciones {
  vacio?: boolean;
  error?: boolean;
  conflictoAlta?: boolean;
  conflictoActivar?: boolean;
}

function montar({ vacio = false, error = false, conflictoAlta = false, conflictoActivar = false }: Opciones = {}) {
  let estado: EstadoRuta = 'Borrador';
  const fila = { id: 7, productoId: 1, productoNombre: producto, version: 2, totalOperaciones: 2 };
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit) => {
    const url = new URL(String(input), 'http://localhost');
    if (url.pathname === '/api/v1/productos') {
      return Response.json({ items: [{ id: 1, nombre: producto }], total: 1, pagina: 1, tamanoPagina: 10 });
    }
    if (url.pathname === '/api/v1/centros-trabajo') {
      return Response.json({
        items: [{ id: 1, codigo: 'CT-A', nombre: 'Ensamblaje', activo: true }, { id: 3, codigo: 'CT-C', nombre: 'Corte', activo: false }],
        total: 2,
        pagina: 1,
        tamanoPagina: 10,
      });
    }
    if (url.pathname === `${ruta}/7/activar`) {
      if (conflictoActivar) return Response.json({ StatusCode: 409, Message: centroInactivo }, { status: 409 });
      estado = 'Activa';
    }
    if (url.pathname === `${ruta}/7/retirar`) estado = 'Retirada';
    if (init.method === 'POST' || init.method === 'PUT') {
      if (url.pathname === ruta && conflictoAlta) return Response.json({ StatusCode: 409, Message: conflictoVersion }, { status: 409 });
      return Response.json({ id: 7 });
    }
    if (url.pathname === `${ruta}/7`) {
      return Response.json({
        ...fila,
        estado,
        operaciones: [
          { secuencia: 2, nombre: 'Ensamble final', centroTrabajoId: 1, centroTrabajoNombre: 'Ensamblaje', minutosPreparacion: 0, minutosPorUnidad: 1.5 },
          { secuencia: 1, nombre: 'Corte de piezas', centroTrabajoId: 3, centroTrabajoNombre: 'Corte', minutosPreparacion: 15, minutosPorUnidad: 0.25 },
        ],
      });
    }
    if (error) return Response.json({ StatusCode: 500, Message: 'Error de listado.' }, { status: 500 });
    const items = vacio || (url.searchParams.has('estado') && url.searchParams.get('estado') !== estado) ? [] : [{ ...fila, estado }];
    return Response.json({ items, total: items.length === 0 ? 0 : 21, pagina: Number(url.searchParams.get('pagina')), tamanoPagina: 20 });
  });
  vi.stubGlobal('fetch', fetchMock);
  const Pagina = pantallas['32-rutas-produccion'];
  if (Pagina === undefined) throw new Error('La pantalla 32 no está registrada.');
  render(<MemoryRouter initialEntries={['/modulos/32-rutas-produccion']}><Pagina /></MemoryRouter>);
  return fetchMock;
}

type FetchMock = ReturnType<typeof montar>;

function llamadas(fetchMock: FetchMock, prefijo: string, metodo?: string): unknown[][] {
  return fetchMock.mock.calls.filter((call) => String(call[0]).startsWith(prefijo) && (metodo === undefined || call[1]?.method === metodo));
}

// Espera a que termine la carga inicial: la tabla muestra la versión y el listado se pidió una sola vez.
async function esperarCargaInicialRutas(fetchMock: FetchMock): Promise<void> {
  await waitFor(() => {
    expect(screen.getByRole('button', { name: `Ver ${producto}, versión 2` })).toBeInTheDocument();
    expect(llamadas(fetchMock, `${ruta}?`)).toHaveLength(1);
  });
}

async function elegir(etiqueta: string, opcion: string): Promise<void> {
  fireEvent.click(screen.getByLabelText(etiqueta));
  fireEvent.click(await screen.findByRole('button', { name: opcion }));
}

async function abrirDetalle(): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: `Ver ${producto}, versión 2` }));
  await screen.findByText('Corte de piezas');
}

async function completarOperacion(numero: number, nombre: string, centro: string, preparacion: string, porUnidad: string): Promise<void> {
  fireEvent.change(screen.getByLabelText(`Nombre de la operación ${numero}`), { target: { value: nombre } });
  await elegir(`Centro de trabajo ${numero}`, centro);
  fireEvent.change(screen.getByLabelText(`Minutos de preparación ${numero}`), { target: { value: preparacion } });
  fireEvent.change(screen.getByLabelText(`Minutos por unidad ${numero}`), { target: { value: porUnidad } });
}

describe('pantalla de rutas de producción', () => {
  it('carga la ruta del catálogo, muestra el detalle ordenado y combina filtros', async () => {
    const fetchMock = montar();
    expect(screen.getByRole('heading', { name: '32 · Rutas de producción' })).toBeInTheDocument();
    expect(screen.getByText('Cargando rutas de producción…')).toBeInTheDocument();
    await esperarCargaInicialRutas(fetchMock);

    await abrirDetalle();
    const filas = within(screen.getByRole('table', { name: 'Operaciones en orden de secuencia' })).getAllByRole('row');
    expect(filas[1]).toHaveTextContent('1Corte de piezasCorte15.000.25');
    expect(filas[2]).toHaveTextContent('2Ensamble finalEnsamblaje0.001.50');

    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await screen.findByText('Página 2 de 2');
    await elegir('Filtrar por producto', `${producto} · #1`);
    fireEvent.change(screen.getByLabelText('Filtrar por estado'), { target: { value: 'Borrador' } });
    await screen.findByText('Página 1 de 2');
    expect(fetchMock).toHaveBeenCalledWith(`${ruta}?productoId=1&estado=Borrador&pagina=1&tamanoPagina=20`, expect.anything());
  });

  it('muestra el estado vacío', async () => {
    montar({ vacio: true });
    expect(await screen.findByText('No se encontraron rutas de producción.')).toBeInTheDocument();
  });

  it('muestra el error del listado y permite reintentar', async () => {
    const fetchMock = montar({ error: true });
    expect(await screen.findByRole('alert')).toHaveTextContent('Error de listado.');
    fireEvent.click(screen.getByRole('button', { name: 'Reintentar listado' }));
    await waitFor(() => expect(llamadas(fetchMock, `${ruta}?`)).toHaveLength(2));
  });

  it('valida localmente sin llamar a la API y conserva lo escrito ante un 409', async () => {
    const fetchMock = montar({ conflictoAlta: true });
    await esperarCargaInicialRutas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva ruta' }));
    fireEvent.change(screen.getByLabelText('Minutos de preparación 1'), { target: { value: '1.234' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));

    expect(screen.getByText('El identificador del producto debe ser mayor que 0.')).toBeInTheDocument();
    expect(screen.getByText('La versión debe ser mayor que 0.')).toBeInTheDocument();
    expect(screen.getByText('El nombre de la operación es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El identificador del centro de trabajo debe ser mayor que 0.')).toBeInTheDocument();
    expect(screen.getByText('Los minutos de preparación admiten como máximo 2 decimales.')).toBeInTheDocument();
    expect(screen.getByText('Los minutos por unidad deben ser mayores que 0 y no superar 99999999.99.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre de la operación 1')).toHaveAttribute('aria-invalid', 'true');
    expect(llamadas(fetchMock, ruta, 'POST')).toHaveLength(0);

    await elegir('Producto fabricado', `${producto} · #1`);
    fireEvent.change(screen.getByLabelText('Versión'), { target: { value: '2' } });
    await completarOperacion(1, 'Corte', 'Ensamblaje · #1', '10', '1,5');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(conflictoVersion);
    expect(screen.getByLabelText('Versión')).toHaveValue('2');
    expect(screen.getByLabelText('Nombre de la operación 1')).toHaveValue('Corte');
    expect(screen.getByLabelText('Minutos por unidad 1')).toHaveValue('1,5');
    expect(screen.getByLabelText('Producto fabricado')).toHaveTextContent(producto);
  });

  it('asigna la secuencia por posición al mover operaciones y marca los centros inactivos', async () => {
    const fetchMock = montar();
    await esperarCargaInicialRutas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva ruta' }));
    await elegir('Producto fabricado', `${producto} · #1`);
    fireEvent.change(screen.getByLabelText('Versión'), { target: { value: '3' } });
    await completarOperacion(1, 'Ensamble', 'Ensamblaje · #1', '0', '2');
    fireEvent.click(screen.getByRole('button', { name: 'Agregar operación' }));
    await completarOperacion(2, 'Corte', 'Corte (inactivo) · #3', '5', '0.5');
    expect(screen.getByLabelText('Centro de trabajo 2')).toHaveTextContent('Corte (inactivo)');

    fireEvent.click(screen.getByRole('button', { name: 'Subir operación 2' }));
    expect(screen.getByLabelText('Nombre de la operación 1')).toHaveValue('Corte');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));

    expect(await screen.findByText('Borrador creado.')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith(ruta, expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({
        productoId: 1,
        version: 3,
        operaciones: [
          { secuencia: 1, nombre: 'Corte', centroTrabajoId: 3, minutosPreparacion: 5, minutosPorUnidad: 0.5 },
          { secuencia: 2, nombre: 'Ensamble', centroTrabajoId: 1, minutosPreparacion: 0, minutosPorUnidad: 2 },
        ],
      }),
    }));
  });

  it('edita un borrador y envía todas las operaciones sin producto ni versión', async () => {
    const fetchMock = montar();
    await esperarCargaInicialRutas(fetchMock);
    await abrirDetalle();
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    expect(screen.queryByLabelText('Versión')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Nombre de la operación 1')).toHaveValue('Corte de piezas');
    fireEvent.change(screen.getByLabelText('Minutos por unidad 2'), { target: { value: '3' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));

    expect(await screen.findByText('Borrador actualizado.')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith(`${ruta}/7`, expect.objectContaining({
      method: 'PUT',
      body: JSON.stringify({
        operaciones: [
          { secuencia: 1, nombre: 'Corte de piezas', centroTrabajoId: 3, minutosPreparacion: 15, minutosPorUnidad: 0.25 },
          { secuencia: 2, nombre: 'Ensamble final', centroTrabajoId: 1, minutosPreparacion: 0, minutosPorUnidad: 3 },
        ],
      }),
    }));
  });

  it('cancela con Escape sin enviar la transición y devuelve el foco al botón', async () => {
    const fetchMock = montar();
    await esperarCargaInicialRutas(fetchMock);
    await abrirDetalle();
    const activar = screen.getByRole('button', { name: 'Activar' });
    fireEvent.click(activar);
    const dialogo = screen.getByRole('dialog', { name: 'Activar versión 2' });
    expect(dialogo).toHaveAttribute('aria-modal', 'true');
    expect(within(dialogo).getByRole('button', { name: 'Cancelar' })).toHaveFocus();

    fireEvent.keyDown(dialogo, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    await waitFor(() => expect(activar).toHaveFocus());
    expect(llamadas(fetchMock, `${ruta}/7/activar`)).toHaveLength(0);
  });

  it('muestra el 409 de un centro inactivo al activar y permite consultar el estado', async () => {
    const fetchMock = montar({ conflictoActivar: true });
    await esperarCargaInicialRutas(fetchMock);
    await abrirDetalle();
    fireEvent.click(screen.getByRole('button', { name: 'Activar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar activación' }));

    expect(await within(screen.getByRole('dialog')).findByRole('alert')).toHaveTextContent(centroInactivo);
    fireEvent.click(screen.getByRole('button', { name: 'Consultar estado actual' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(await screen.findByText('Versión 2 · Borrador')).toBeInTheDocument();
  });

  it('activa y retira conservando el historial', async () => {
    const fetchMock = montar();
    await esperarCargaInicialRutas(fetchMock);
    await abrirDetalle();
    fireEvent.click(screen.getByRole('button', { name: 'Activar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar activación' }));
    expect(await screen.findByText('Versión activada.')).toBeInTheDocument();

    const retirar = await screen.findByRole('button', { name: 'Retirar' });
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Detalle de la ruta' })).toHaveFocus());
    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument();
    fireEvent.click(retirar);
    expect(screen.getByRole('dialog')).toHaveTextContent('La versión quedará disponible en el historial.');
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar retiro' }));

    expect(await screen.findByText('Versión 2 · Retirada')).toBeInTheDocument();
    expect(screen.getByText('Corte de piezas')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Retirar' })).not.toBeInTheDocument();
  });
});
