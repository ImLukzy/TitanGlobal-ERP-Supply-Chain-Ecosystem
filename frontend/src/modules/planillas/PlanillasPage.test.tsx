import { fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import {
  esperarCargaAfiliaciones,
  esperarCargaBonos,
  esperarCargaInicialPlanillas,
  limpiarPlanillas,
  prepararApiPlanillas,
  renderPlanillas,
} from './planillas.testUtils';

afterEach(() => {
  limpiarPlanillas();
});

describe('página de Planillas: pestañas', () => {
  it('abre Planillas por defecto y con un valor desconocido de ?vista=', async () => {
    const { fetchMock } = prepararApiPlanillas();
    renderPlanillas(['RRHH'], '/modulos/66-planillas?vista=otra');
    await esperarCargaInicialPlanillas(fetchMock);
    expect(screen.getByRole('tab', { name: 'Planillas' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Bonos' })).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByRole('tab', { name: 'Afiliaciones' })).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-planillas-planillas');
  });

  it('abre Bonos y Afiliaciones con ?vista=', async () => {
    const { fetchMock } = prepararApiPlanillas();
    renderPlanillas(['RRHH'], '/modulos/66-planillas?vista=bonos');
    await esperarCargaBonos(fetchMock);
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-planillas-bonos');
    limpiarPlanillas();
    prepararApiPlanillas();
    renderPlanillas(['RRHH'], '/modulos/66-planillas?vista=afiliaciones');
    await esperarCargaAfiliaciones();
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-planillas-afiliaciones');
  });

  it('cambia de pestaña con clic y con las flechas, y solo la activa tiene tabIndex 0', async () => {
    const { fetchMock } = prepararApiPlanillas();
    renderPlanillas(['RRHH']);
    await esperarCargaInicialPlanillas(fetchMock);
    expect(screen.getByRole('tab', { name: 'Bonos' })).toHaveAttribute('tabindex', '-1');
    fireEvent.click(screen.getByRole('tab', { name: 'Bonos' }));
    await esperarCargaBonos(fetchMock);
    expect(screen.getByRole('tab', { name: 'Bonos' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Planillas' })).toHaveAttribute('tabindex', '-1');
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowRight' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Afiliaciones' })).toHaveAttribute('aria-selected', 'true'));
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowRight' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Planillas' })).toHaveAttribute('aria-selected', 'true'));
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowLeft' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Afiliaciones' })).toHaveAttribute('aria-selected', 'true'));
  });
});

describe('página de Planillas: roles', () => {
  it('RRHH y ADMIN pueden escribir; otros roles y la sesión sin roles no', async () => {
    const { fetchMock } = prepararApiPlanillas();
    renderPlanillas(['RRHH']);
    await esperarCargaInicialPlanillas(fetchMock);
    expect(screen.getByRole('button', { name: 'Nueva planilla' })).toBeEnabled();
    limpiarPlanillas();
    prepararApiPlanillas();
    renderPlanillas(['VENTAS']);
    expect(await screen.findByRole('button', { name: 'Nueva planilla' })).toBeDisabled();
  });
});
