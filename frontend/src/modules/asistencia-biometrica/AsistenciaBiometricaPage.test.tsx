import { fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import {
  esperarCargaJornadas,
  esperarCargaMarcaciones,
  limpiarAsistencia,
  prepararApiAsistencia,
  renderAsistencia,
} from './asistencia.testUtils';

afterEach(() => {
  limpiarAsistencia();
});

describe('página de Asistencia biométrica: pestañas', () => {
  it('abre la vista Jornadas por defecto y con un valor desconocido de ?vista=', async () => {
    const { fetchMock } = prepararApiAsistencia();
    renderAsistencia(['RRHH'], '/modulos/63-asistencia-biometrica?vista=otra');
    await esperarCargaJornadas(fetchMock);
    expect(screen.getByRole('tab', { name: 'Jornadas' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Marcaciones' })).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-asistencia-jornadas');
  });

  it('abre Marcaciones con ?vista=marcaciones', async () => {
    const { fetchMock } = prepararApiAsistencia();
    renderAsistencia(['RRHH'], '/modulos/63-asistencia-biometrica?vista=marcaciones');
    await esperarCargaMarcaciones(fetchMock);
    expect(screen.getByRole('tab', { name: 'Marcaciones' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-asistencia-marcaciones');
  });

  it('cambia de pestaña con clic y con las flechas, y solo la activa tiene tabIndex 0', async () => {
    const { fetchMock } = prepararApiAsistencia();
    renderAsistencia(['RRHH']);
    await esperarCargaJornadas(fetchMock);
    expect(screen.getByRole('tab', { name: 'Marcaciones' })).toHaveAttribute('tabindex', '-1');
    fireEvent.click(screen.getByRole('tab', { name: 'Marcaciones' }));
    await esperarCargaMarcaciones(fetchMock);
    expect(screen.getByRole('tab', { name: 'Marcaciones' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Jornadas' })).toHaveAttribute('tabindex', '-1');
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowLeft' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Jornadas' })).toHaveAttribute('aria-selected', 'true'));
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowRight' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Marcaciones' })).toHaveAttribute('aria-selected', 'true'));
  });
});

describe('página de Asistencia biométrica: roles', () => {
  it('RRHH y ADMIN pueden escribir; otros roles y la sesión sin roles no', async () => {
    const { fetchMock } = prepararApiAsistencia();
    renderAsistencia(['RRHH'], '/modulos/63-asistencia-biometrica?vista=marcaciones');
    await esperarCargaMarcaciones(fetchMock);
    expect(screen.getByRole('button', { name: 'Registrar marcación' })).toBeEnabled();
    limpiarAsistencia();
    prepararApiAsistencia();
    renderAsistencia([], '/modulos/63-asistencia-biometrica?vista=marcaciones');
    expect(await screen.findByRole('button', { name: 'Registrar marcación' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Importar archivo del reloj' })).toBeDisabled();
  });
});
