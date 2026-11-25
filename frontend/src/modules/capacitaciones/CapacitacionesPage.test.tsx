import { fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import {
  ANIO_PRUEBAS,
  esperarCargaCursos,
  esperarCargaHoras,
  esperarCargaProgramaciones,
  limpiarCapacitaciones,
  montarCursosCargados,
  montarProgramacionesCargadas,
  prepararApiCapacitaciones,
  renderCapacitaciones,
  urlsQueContienen,
} from './capacitaciones.testUtils';

afterEach(() => {
  limpiarCapacitaciones();
});

describe('página de Capacitaciones: pestañas', () => {
  it('abre Programaciones por defecto y con un valor desconocido de ?vista=', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    renderCapacitaciones(['RRHH'], '/modulos/69-capacitaciones?vista=otra');
    await esperarCargaProgramaciones(fetchMock);
    expect(screen.getByRole('tab', { name: 'Programaciones' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-capacitaciones-programaciones');
  });

  it('abre Cursos con ?vista=cursos y Horas por empleado con ?vista=horas', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarCursosCargados(fetchMock);
    expect(screen.getByRole('tab', { name: 'Cursos' })).toHaveAttribute('aria-selected', 'true');
    limpiarCapacitaciones();
    const otra = prepararApiCapacitaciones();
    renderCapacitaciones(['RRHH'], '/modulos/69-capacitaciones?vista=horas');
    await esperarCargaHoras(otra.fetchMock);
    expect(screen.getByRole('tab', { name: 'Horas por empleado' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-capacitaciones-horas');
  });

  it('cambia de pestaña con clic y con las flechas, y solo la activa tiene tabIndex 0', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    expect(screen.getByRole('tab', { name: 'Cursos' })).toHaveAttribute('tabindex', '-1');
    fireEvent.click(screen.getByRole('tab', { name: 'Cursos' }));
    await esperarCargaCursos(fetchMock);
    expect(screen.getByRole('tab', { name: 'Programaciones' })).toHaveAttribute('tabindex', '-1');
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowRight' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Horas por empleado' })).toHaveAttribute('aria-selected', 'true'));
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowRight' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Programaciones' })).toHaveAttribute('aria-selected', 'true'));
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowLeft' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Horas por empleado' })).toHaveAttribute('aria-selected', 'true'));
  });
});

describe('página de Capacitaciones: resumen y año', () => {
  it('muestra el resumen del año actual en UTC con cifras solas', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    expect(urlsQueContienen(fetchMock, '/capacitaciones/resumen')).toEqual([`/api/v1/capacitaciones/resumen?anio=${ANIO_PRUEBAS}`]);
    expect(screen.getByLabelText('Año')).toHaveValue(String(ANIO_PRUEBAS));
    const resumen = screen.getByRole('region', { name: `Resumen de capacitaciones ${ANIO_PRUEBAS}` });
    expect(resumen).toHaveTextContent('Cursos activos2');
    expect(resumen).toHaveTextContent('Realizadas1');
    expect(resumen).toHaveTextContent('Horas totales16');
    expect(resumen).toHaveTextContent('Certificaciones vencidas1');
  });

  it('el selector de año vuelve a pedir resumen y programaciones con el año elegido', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    fireEvent.change(screen.getByLabelText('Año'), { target: { value: String(ANIO_PRUEBAS + 1) } });
    await waitFor(() => {
      expect(urlsQueContienen(fetchMock, '/capacitaciones/resumen').at(-1)).toBe(`/api/v1/capacitaciones/resumen?anio=${ANIO_PRUEBAS + 1}`);
      expect(urlsQueContienen(fetchMock, '/programaciones-curso?').at(-1)).toBe(`/api/v1/programaciones-curso?anio=${ANIO_PRUEBAS + 1}&pagina=1&tamanoPagina=10`);
    });
    expect(await screen.findByText('No hay programaciones con esos filtros.')).toBeInTheDocument();
  });
});

describe('página de Capacitaciones: roles', () => {
  it('RRHH y ADMIN pueden escribir; otros roles no y ven el aviso', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock, ['ADMIN']);
    expect(screen.getByRole('button', { name: 'Nueva programación' })).toBeEnabled();
    expect(screen.queryByText('Solo RRHH o ADMIN pueden modificar capacitaciones.')).not.toBeInTheDocument();
    limpiarCapacitaciones();
    const otra = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(otra.fetchMock, ['VENTAS']);
    expect(screen.getByRole('button', { name: 'Nueva programación' })).toBeDisabled();
    expect(screen.getByText('Solo RRHH o ADMIN pueden modificar capacitaciones.')).toBeInTheDocument();
    fireEvent.click(screen.getAllByRole('button', { name: 'Liderazgo' })[0] as HTMLElement);
    expect(await screen.findByRole('button', { name: 'Inscribir' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Marcar realizada' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Cancelar programación' })).toBeDisabled();
    for (const retirar of screen.getAllByRole('button', { name: /^Retirar a/ })) {
      expect(retirar).toBeDisabled();
    }
  });

  it('sin RRHH ni ADMIN las acciones de Cursos quedan deshabilitadas', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarCursosCargados(fetchMock, ['VENTAS']);
    expect(screen.getByRole('button', { name: 'Nuevo curso' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'LID-201' }));
    expect(await screen.findByRole('button', { name: 'Editar curso' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Desactivar curso' })).toBeDisabled();
    expect(screen.getAllByText('Solo RRHH o ADMIN pueden modificar capacitaciones.').length).toBeGreaterThan(0);
  });
});
