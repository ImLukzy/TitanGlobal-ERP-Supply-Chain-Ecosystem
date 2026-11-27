import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { EvaluacionProveedoresPage } from './EvaluacionProveedoresPage';
import { cambiarValor, esperarCargaInicial, llamadasLista, prepararApi, renderEnRouter } from './evaluaciones.testUtils';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('pantalla de evaluación de proveedores: resumen, filtros y tabla', () => {
  it('A3 muestra el resumen con las 4 calificaciones en orden fijo y el ranking', async () => {
    prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await esperarCargaInicial();
    const resumen = screen.getByRole('region', { name: 'Resumen de evaluaciones de proveedores' });
    const tarjetas = within(resumen).getAllByRole('article').map((tarjeta) => tarjeta.textContent);
    expect(tarjetas).toEqual(['Evaluaciones6', 'Proveedores evaluados4', 'Promedio general78,72']);
    const conteos = within(screen.getByRole('list', { name: 'Evaluaciones por calificación' })).getAllByRole('listitem');
    expect(conteos.map((item) => item.textContent)).toEqual(['Excelente2', 'Bueno2', 'Regular1', 'Deficiente1']);
    const ranking = screen.getByRole('table', { name: 'Proveedores ordenados por promedio' });
    const filas = within(ranking).getAllByRole('row').slice(1);
    expect(filas).toHaveLength(3);
    expect(filas[0]).toHaveTextContent('1Pacífico Logística195,50Excelente');
    expect(filas[1]).toHaveTextContent('2Andes Suministros S.A.C.289,05Excelente');
  });

  it('A3 la tabla muestra periodo, proveedor, puntaje final y calificación como texto', async () => {
    prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await esperarCargaInicial();
    const tabla = screen.getByRole('table', { name: 'Evaluaciones de proveedores' });
    const filas = within(tabla).getAllByRole('row').slice(1);
    expect(filas).toHaveLength(3);
    expect(filas[1]).toHaveTextContent('2026-10Andes Suministros S.A.C.92,60Excelente');
    expect(filas[2]).toHaveTextContent('2026-10Cobre Austral SpA65,50Regular');
  });

  it('A3 envía proveedor, periodo y calificación en la query y permite quitar el periodo', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await esperarCargaInicial();
    expect(screen.queryByRole('button', { name: 'Quitar' })).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Proveedor'), { target: { value: '1' } });
    await waitFor(() => expect(llamadasLista(fetchMock)).toHaveLength(2));
    cambiarValor('Periodo', '2026-10');
    await waitFor(() => expect(llamadasLista(fetchMock)).toHaveLength(3));
    fireEvent.change(screen.getByLabelText('Calificación'), { target: { value: 'Excelente' } });
    await waitFor(() => expect(llamadasLista(fetchMock)).toHaveLength(4));
    const consulta = llamadasLista(fetchMock)[3] ?? '';
    expect(consulta).toContain('proveedorId=1');
    expect(consulta).toContain('periodo=2026-10');
    expect(consulta).toContain('calificacion=Excelente');
    expect(consulta).toContain('pagina=1');
    fireEvent.click(screen.getByRole('button', { name: 'Quitar' }));
    await waitFor(() => expect(llamadasLista(fetchMock)).toHaveLength(5));
    expect(llamadasLista(fetchMock)[4]).not.toContain('periodo=');
    expect(screen.queryByRole('button', { name: 'Quitar' })).not.toBeInTheDocument();
  });

  it('A3 vuelve a la página 1 cuando cambia un filtro', async () => {
    const { fetchMock } = prepararApi({ totalFilas: 21 });
    renderEnRouter(<EvaluacionProveedoresPage />);
    await esperarCargaInicial();
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    expect(await screen.findByText('Página 2 de 3')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Calificación'), { target: { value: 'Bueno' } });
    expect(await screen.findByText('Página 1 de 3')).toBeInTheDocument();
    const consulta = llamadasLista(fetchMock).at(-1) ?? '';
    expect(consulta).toContain('pagina=1');
    expect(consulta).toContain('calificacion=Bueno');
  });

  it('A3 presenta los estados vacío y error del listado', async () => {
    prepararApi({ vacio: true });
    renderEnRouter(<EvaluacionProveedoresPage />);
    expect(await screen.findByText('No hay evaluaciones para mostrar.')).toBeInTheDocument();
    cleanup();
    prepararApi({ errorLista: true });
    renderEnRouter(<EvaluacionProveedoresPage />);
    expect(await screen.findByText('No se pudieron cargar las evaluaciones.')).toHaveAttribute('role', 'alert');
  });
});
