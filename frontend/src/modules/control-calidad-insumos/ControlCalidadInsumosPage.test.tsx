import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  abrirDetalle,
  avanzar,
  elegir,
  esperarCargaInicialInspecciones,
  llamadas,
  montar,
  rutaApi,
} from './controlCalidadInsumos.testUtils';
import { hoyPeru } from './validacion';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

describe('pantalla de control de calidad de insumos: listado', () => {
  it('carga una vez, muestra el resumen por proveedor y el detalle', async () => {
    const fetchMock = montar();
    expect(screen.getByRole('heading', { name: '36 · Control de calidad de insumos' })).toBeInTheDocument();
    expect(screen.getByText('Cargando inspecciones…')).toBeInTheDocument();
    await esperarCargaInicialInspecciones(fetchMock);
    expect(llamadas(fetchMock, rutaApi)).toEqual(expect.arrayContaining(['/api/v1/inspecciones-insumos?pagina=1&tamanoPagina=20']));

    const proveedores = within(screen.getByRole('table', { name: 'Tasa de rechazo por proveedor' })).getAllByRole('row');
    expect(proveedores[1]).toHaveTextContent('Cobre Austral SpA1266.67 %');
    expect(screen.getByText('Rechazadas', { selector: 'span' }).nextSibling).toHaveTextContent('2');
    expect(screen.getByText('Aprobadas', { selector: 'span' }).nextSibling).toHaveTextContent('0');

    const fila = screen.getByRole('button', { name: 'Ver inspección CCI-000007' }).closest('tr');
    expect(fila).toHaveTextContent('CCI-000007Mouse ÓpticoAndes Suministros S.A.C.MO-2026-00102/11/2026500 unidadPendiente—');

    await abrirDetalle();
    expect(screen.getByText('Rosa Díaz')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Dictaminar' })).toBeInTheDocument();
  });

  it('aplica la búsqueda a los 300 ms y solo consulta el último texto', async () => {
    const fetchMock = montar();
    await esperarCargaInicialInspecciones(fetchMock);
    vi.useFakeTimers();
    fetchMock.mockClear();
    const busqueda = screen.getByRole('searchbox', { name: 'Buscar por número o lote' });
    fireEvent.change(busqueda, { target: { value: 'M' } });
    fireEvent.change(busqueda, { target: { value: 'MO-' } });
    fireEvent.change(busqueda, { target: { value: 'MO-2026' } });

    await avanzar(299);
    expect(llamadas(fetchMock, `${rutaApi}?`)).toHaveLength(0);
    await avanzar(1);
    expect(llamadas(fetchMock, `${rutaApi}?`)).toEqual(['/api/v1/inspecciones-insumos?busqueda=MO-2026&pagina=1&tamanoPagina=20']);
  });

  it('combina filtros y vuelve a la página 1 al cambiarlos', async () => {
    const fetchMock = montar();
    await esperarCargaInicialInspecciones(fetchMock);

    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(llamadas(fetchMock, `${rutaApi}?pagina=2`)).toHaveLength(1));
    await elegir('Filtrar por insumo', 'Mouse Óptico · #3');
    await elegir('Filtrar por proveedor', 'Andes Suministros S.A.C. · #1');
    fireEvent.change(screen.getByLabelText('Filtrar por estado'), { target: { value: 'Pendiente' } });
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-10-01' } });
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-10-31' } });

    await waitFor(() => expect(llamadas(fetchMock, rutaApi)).toContain(
      '/api/v1/inspecciones-insumos?productoId=3&proveedorId=1&estado=Pendiente&desde=2026-10-01&hasta=2026-10-31&pagina=1&tamanoPagina=20',
    ));
    expect(llamadas(fetchMock, '/api/v1/proveedores')).toEqual(['/api/v1/proveedores?activo=true&pagina=1&tamanoPagina=20']);
  });

  it('no consulta con un rango de fechas invertido', async () => {
    const fetchMock = montar();
    await esperarCargaInicialInspecciones(fetchMock);
    fetchMock.mockClear();

    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-11-10' } });
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-11-01' } });

    expect(await screen.findByText('La fecha desde no puede ser posterior a la fecha hasta.')).toBeInTheDocument();
    expect(screen.getByLabelText('Hasta')).toHaveAttribute('aria-invalid', 'true');
    expect(llamadas(fetchMock, `${rutaApi}?`).filter((url) => url.includes('hasta=2026-11-01'))).toEqual([]);
    expect(llamadas(fetchMock, `${rutaApi}?`)).toEqual(['/api/v1/inspecciones-insumos?desde=2026-11-10&pagina=1&tamanoPagina=20']);
  });

  it('muestra el estado vacío', async () => {
    const fetchMock = montar({ vacio: true });
    await esperarCargaInicialInspecciones(fetchMock, false);
    expect(await screen.findByText('No se encontraron inspecciones.')).toBeInTheDocument();
  });

  it('muestra el error del listado y reintenta', async () => {
    const fetchMock = montar({ errorListado: true });
    expect(await screen.findByText('Error de listado.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Reintentar listado' }));
    await waitFor(() => expect(llamadas(fetchMock, `${rutaApi}?`)).toHaveLength(2));
  });

  it('solo ofrece editar, dictaminar y anular en las pendientes, y muestra el dictamen en hora de Perú', async () => {
    const fetchMock = montar({ estadoInicial: 'Rechazada' });
    await esperarCargaInicialInspecciones(fetchMock);
    await abrirDetalle();

    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Dictaminar' })).not.toBeInTheDocument();
    expect(screen.getByText('Supera la tolerancia')).toBeInTheDocument();
    expect(screen.getByText('12.50 % / 5.00 %')).toBeInTheDocument();
    expect(screen.getByText('24/11/2026 19:30')).toBeInTheDocument();
  });

  it('calcula «hoy» en hora de Perú: a las 19:00 todavía es el mismo día', () => {
    expect(hoyPeru(Date.parse('2026-11-25T00:00:00Z'))).toBe('2026-11-24');
    expect(hoyPeru(Date.parse('2026-11-25T04:59:59Z'))).toBe('2026-11-24');
    expect(hoyPeru(Date.parse('2026-11-25T05:00:00Z'))).toBe('2026-11-25');
  });
});
