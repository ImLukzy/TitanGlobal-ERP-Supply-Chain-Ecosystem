import { fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import {
  esperarCargaCiclos,
  esperarCargaEvaluaciones,
  limpiarEvaluacion,
  montarCiclosCargados,
  montarEvaluacionesCargadas,
  prepararApiEvaluacion,
  renderEvaluacion,
} from './evaluacion.testUtils';

afterEach(() => {
  limpiarEvaluacion();
});

describe('página de Evaluación de desempeño: pestañas', () => {
  it('abre Ciclos por defecto y con un valor desconocido de ?vista=', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    renderEvaluacion(['RRHH'], '/modulos/68-evaluacion-desempeno?vista=otra');
    await esperarCargaCiclos(fetchMock);
    expect(screen.getByRole('tab', { name: 'Ciclos' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Evaluaciones' })).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-evaluacion-desempeno-ciclos');
  });

  it('abre Evaluaciones con ?vista=evaluaciones', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock);
    expect(screen.getByRole('tab', { name: 'Evaluaciones' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-evaluacion-desempeno-evaluaciones');
  });

  it('cambia de pestaña con clic y con las flechas, y solo la activa tiene tabIndex 0', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock);
    expect(screen.getByRole('tab', { name: 'Evaluaciones' })).toHaveAttribute('tabindex', '-1');
    fireEvent.click(screen.getByRole('tab', { name: 'Evaluaciones' }));
    await esperarCargaEvaluaciones(fetchMock);
    expect(screen.getByRole('tab', { name: 'Evaluaciones' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Ciclos' })).toHaveAttribute('tabindex', '-1');
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowRight' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Ciclos' })).toHaveAttribute('aria-selected', 'true'));
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowLeft' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Evaluaciones' })).toHaveAttribute('aria-selected', 'true'));
  });
});

describe('página de Evaluación de desempeño: roles', () => {
  it('RRHH y ADMIN pueden escribir en Ciclos; otros roles no y ven el aviso', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarCiclosCargados(fetchMock, ['ADMIN']);
    expect(screen.getByRole('button', { name: 'Nuevo ciclo' })).toBeEnabled();
    expect(screen.queryByText('Solo RRHH o ADMIN pueden modificar evaluaciones.')).not.toBeInTheDocument();
    limpiarEvaluacion();
    const otra = prepararApiEvaluacion();
    await montarCiclosCargados(otra.fetchMock, ['VENTAS']);
    expect(screen.getByRole('button', { name: 'Nuevo ciclo' })).toBeDisabled();
    expect(screen.getByText('Solo RRHH o ADMIN pueden modificar evaluaciones.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: '2027-S2' }));
    expect(await screen.findByRole('button', { name: 'Abrir ciclo' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Editar ciclo' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Agregar competencia' })).toBeDisabled();
  });

  it('sin RRHH ni ADMIN las acciones de Evaluaciones quedan deshabilitadas', async () => {
    const { fetchMock } = prepararApiEvaluacion();
    await montarEvaluacionesCargadas(fetchMock, ['VENTAS']);
    expect(screen.getByRole('button', { name: 'Nueva evaluación' })).toBeDisabled();
    expect(screen.getAllByText('Solo RRHH o ADMIN pueden modificar evaluaciones.').length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole('button', { name: 'Ana Torres Vega' }));
    expect(await screen.findAllByRole('radio')).toHaveLength(10);
    for (const radio of screen.getAllByRole('radio')) {
      expect(radio).toBeDisabled();
    }
    expect(screen.getByRole('button', { name: 'Guardar' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Enviar' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Maria Salazar Quispe' }));
    expect(await screen.findByRole('button', { name: 'Cerrar evaluación' })).toBeDisabled();
  });
});
