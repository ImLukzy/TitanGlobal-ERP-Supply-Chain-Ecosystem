import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  ANIO_PRUEBAS,
  limpiarCapacitaciones,
  montarHorasCargadas,
  prepararApiCapacitaciones,
  renderCapacitaciones,
  urlsQueContienen,
} from './capacitaciones.testUtils';

afterEach(() => {
  limpiarCapacitaciones();
});

describe('vista de Horas por empleado', () => {
  it('lista legajo, empleado, cursos aprobados y horas del año', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarHorasCargadas(fetchMock);
    expect(urlsQueContienen(fetchMock, '/capacitaciones/horas?')).toEqual([`/api/v1/capacitaciones/horas?anio=${ANIO_PRUEBAS}&pagina=1&tamanoPagina=10`]);
    const tabla = screen.getByRole('table', { name: 'Horas de capacitación por empleado' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual(['Legajo', 'Empleado', 'Cursos aprobados', 'Horas del año']);
    const filas = within(tabla).getAllByRole('row');
    expect(filas).toHaveLength(4);
    expect(filas[1]).toHaveTextContent('EMP-000001');
    expect(filas[1]).toHaveTextContent('Ana Torres Vega');
    expect(filas[1]).toHaveTextContent('16');
  });

  it('el selector de año vuelve a pedir las horas con el año elegido', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarHorasCargadas(fetchMock);
    fireEvent.change(screen.getByLabelText('Año'), { target: { value: String(ANIO_PRUEBAS - 1) } });
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/capacitaciones/horas?').at(-1)).toBe(`/api/v1/capacitaciones/horas?anio=${ANIO_PRUEBAS - 1}&pagina=1&tamanoPagina=10`));
  });

  it('aplica la búsqueda 300 ms después del último cambio', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarHorasCargadas(fetchMock);
    const antes = urlsQueContienen(fetchMock, '/capacitaciones/horas?').length;
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText('Buscar por legajo o nombre'), { target: { value: 'luis' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsQueContienen(fetchMock, '/capacitaciones/horas?')).toHaveLength(antes);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/capacitaciones/horas?').at(-1)).toBe(`/api/v1/capacitaciones/horas?anio=${ANIO_PRUEBAS}&busqueda=luis&pagina=1&tamanoPagina=10`));
  });

  it('al elegir un empleado muestra su ficha con cursos y la certificación vencida con texto', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarHorasCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Ana Torres Vega' }));
    expect(await screen.findByText(`Horas del año ${ANIO_PRUEBAS}: 8`)).toBeInTheDocument();
    expect(urlsQueContienen(fetchMock, '/capacitaciones/empleados/1')).toEqual([`/api/v1/capacitaciones/empleados/1?anio=${ANIO_PRUEBAS}`]);
    const cursos = screen.getByRole('table', { name: 'Cursos del empleado' });
    expect(cursos).toHaveTextContent('SEG-101 · Seguridad industrial');
    expect(cursos).toHaveTextContent('02/03/2026 - 03/03/2026');
    expect(cursos).toHaveTextContent('Aprobado');
    const certificaciones = screen.getByRole('table', { name: 'Certificaciones del empleado' });
    expect(certificaciones).toHaveTextContent('CERT-001');
    expect(certificaciones).toHaveTextContent('01/05/2026');
    expect(certificaciones).toHaveTextContent('Vencida');
  });

  it('muestra los estados vacío y de error', async () => {
    prepararApiCapacitaciones({ listaVacia: true });
    renderCapacitaciones(['RRHH'], '/modulos/69-capacitaciones?vista=horas');
    expect(await screen.findByText('No hay empleados con esos filtros.')).toHaveAttribute('role', 'status');
    limpiarCapacitaciones();
    prepararApiCapacitaciones({ errorLista: true });
    renderCapacitaciones(['RRHH'], '/modulos/69-capacitaciones?vista=horas');
    expect(await screen.findByText('No se pudieron cargar las horas por empleado.')).toHaveAttribute('role', 'alert');
  });
});
