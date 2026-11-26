import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  abrirDetalle,
  avanzar,
  elegirCentro,
  esperarCargaInicialMantenimientos,
  llamadas,
  montar,
  rutaApi,
} from './mantenimientoPreventivo.testUtils';
import { hoyPeru } from './validacion';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

describe('pantalla de mantenimiento preventivo: listado', () => {
  it('carga una vez, muestra el resumen en el orden de la API y la fila', async () => {
    const fetchMock = montar();
    expect(screen.getByRole('heading', { name: '39 · Mantenimiento preventivo' })).toBeInTheDocument();
    expect(screen.getByText('Cargando mantenimientos…')).toBeInTheDocument();
    await esperarCargaInicialMantenimientos(fetchMock);
    expect(llamadas(fetchMock, `${rutaApi}?`)).toEqual(['/api/v1/mantenimientos-preventivos?pagina=1&tamanoPagina=20']);

    const metricas = [...document.querySelectorAll('.mantenimiento-preventivo-metricas span')].map((nodo) => nodo.textContent);
    expect(metricas).toEqual(['Mantenimientos', 'Programado', 'En ejecución', 'Completado', 'Vencidos', 'Próximos 7 días', 'Horas pendientes']);
    expect(screen.getByText('Horas pendientes').nextSibling).toHaveTextContent('12.5 h');

    const fila = screen.getByRole('button', { name: 'Ver mantenimiento MP-000007' }).closest('tr');
    expect(fila).toHaveTextContent('MP-000007CT-LIM-L01 Línea de ensambleCambio de rodamientos20/11/20262.5 hProgramado');
    expect(fila).not.toHaveTextContent('Vencido');
  });

  it('marca con texto las paradas vencidas', async () => {
    const fetchMock = montar({ vencido: true });
    await esperarCargaInicialMantenimientos(fetchMock);
    const fila = screen.getByRole('button', { name: 'Ver mantenimiento MP-000007' }).closest('tr');
    expect(fila).toHaveTextContent('ProgramadoVencido');
  });

  it('aplica la búsqueda a los 300 ms y solo consulta el último texto', async () => {
    const fetchMock = montar();
    await esperarCargaInicialMantenimientos(fetchMock);
    vi.useFakeTimers();
    fetchMock.mockClear();
    const busqueda = screen.getByRole('searchbox', { name: 'Buscar por número o descripción' });
    fireEvent.change(busqueda, { target: { value: 'M' } });
    fireEvent.change(busqueda, { target: { value: 'MP-' } });
    fireEvent.change(busqueda, { target: { value: 'MP-0000' } });

    await avanzar(299);
    expect(llamadas(fetchMock, `${rutaApi}?`)).toHaveLength(0);
    await avanzar(1);
    expect(llamadas(fetchMock, `${rutaApi}?`)).toEqual(['/api/v1/mantenimientos-preventivos?busqueda=MP-0000&pagina=1&tamanoPagina=20']);
  });

  it('combina filtros, incluye centros inactivos y vuelve a la página 1', async () => {
    const fetchMock = montar();
    await esperarCargaInicialMantenimientos(fetchMock);

    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(llamadas(fetchMock, `${rutaApi}?pagina=2`)).toHaveLength(1));
    await elegirCentro('Filtrar por centro');
    fireEvent.change(screen.getByLabelText('Filtrar por estado'), { target: { value: 'EnEjecucion' } });
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-12-01' } });
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-12-31' } });

    await waitFor(() => expect(llamadas(fetchMock, rutaApi)).toContain(
      '/api/v1/mantenimientos-preventivos?centroTrabajoId=4&estado=EnEjecucion&desde=2026-12-01&hasta=2026-12-31&pagina=1&tamanoPagina=20',
    ));
    expect(llamadas(fetchMock, '/api/v1/centros-trabajo')).toEqual(['/api/v1/centros-trabajo?pagina=1&tamanoPagina=20']);
    expect(screen.getByRole('option', { name: 'En ejecución' })).toBeInTheDocument();
  });

  it('no consulta con un rango de fechas invertido', async () => {
    const fetchMock = montar();
    await esperarCargaInicialMantenimientos(fetchMock);
    fetchMock.mockClear();

    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-12-10' } });
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-12-01' } });

    expect(await screen.findByText('La fecha desde no puede ser posterior a la fecha hasta.')).toBeInTheDocument();
    expect(screen.getByLabelText('Hasta')).toHaveAttribute('aria-invalid', 'true');
    expect(llamadas(fetchMock, `${rutaApi}?`).filter((url) => url.includes('hasta=2026-12-01'))).toEqual([]);
  });

  it('muestra el estado vacío', async () => {
    const fetchMock = montar({ vacio: true });
    await esperarCargaInicialMantenimientos(fetchMock, false);
    expect(await screen.findByText('No se encontraron mantenimientos.')).toBeInTheDocument();
  });

  it('muestra el error del listado y reintenta', async () => {
    const fetchMock = montar({ errorListado: true });
    expect(await screen.findByText('Error de listado.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Reintentar listado' }));
    await waitFor(() => expect(llamadas(fetchMock, `${rutaApi}?`)).toHaveLength(2));
  });

  it('muestra los textos por defecto del detalle y las acciones de una programada', async () => {
    const fetchMock = montar();
    await esperarCargaInicialMantenimientos(fetchMock);
    await abrirDetalle();

    expect(screen.getByText('Sin frecuencia')).toBeInTheDocument();
    expect(screen.getByText('Sin responsable')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Editar' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Iniciar' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Cancelar' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Completar' })).not.toBeInTheDocument();
  });

  it('muestra una completada en hora de Perú, con la siguiente parada y sin acciones', async () => {
    const fetchMock = montar({ estadoInicial: 'Completado' });
    await esperarCargaInicialMantenimientos(fetchMock);
    await abrirDetalle();

    expect(screen.getByText('20/11/2026 08:00')).toBeInTheDocument();
    expect(screen.getByText('20/11/2026 19:30')).toBeInTheDocument();
    expect(screen.getByText('3.25 h')).toBeInTheDocument();
    expect(screen.getByText('Cada 30 días')).toBeInTheDocument();
    expect(screen.getByText('Siguiente parada sugerida').nextSibling).toHaveTextContent('20/12/2026');
    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Completar' })).not.toBeInTheDocument();
  });

  it('muestra el motivo de una cancelada', async () => {
    const fetchMock = montar({ estadoInicial: 'Cancelado' });
    await esperarCargaInicialMantenimientos(fetchMock);
    await abrirDetalle();
    expect(screen.getByText('Motivo de cancelación').nextSibling).toHaveTextContent('Línea detenida');
    expect(screen.queryByRole('button', { name: 'Iniciar' })).not.toBeInTheDocument();
  });

  it('calcula «hoy» en hora de Perú: a las 19:00 todavía es el mismo día', () => {
    expect(hoyPeru(Date.parse('2026-11-26T00:00:00Z'))).toBe('2026-11-25');
    expect(hoyPeru(Date.parse('2026-11-26T04:59:59Z'))).toBe('2026-11-25');
    expect(hoyPeru(Date.parse('2026-11-26T05:00:00Z'))).toBe('2026-11-26');
  });
});
