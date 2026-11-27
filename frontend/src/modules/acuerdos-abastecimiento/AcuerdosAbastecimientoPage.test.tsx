import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AcuerdosAbastecimientoPage } from './AcuerdosAbastecimientoPage';
import { esperarCargaInicial, llamadasLista, prepararApi, renderEnRouter, seleccionar } from './acuerdos.testUtils';

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date('2026-11-15T12:00:00'));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('pantalla de acuerdos: resumen, filtros y tabla', () => {
  it('A3 muestra el resumen y el conteo por estado en orden fijo', async () => {
    prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    const resumen = screen.getByRole('region', { name: 'Resumen de acuerdos' });
    expect(within(resumen).getByText('Acuerdos').nextElementSibling).toHaveTextContent('4');
    expect(within(resumen).getByText('Monto consumido').nextElementSibling).toHaveTextContent('61,750.00');
    const items = within(within(resumen).getByRole('list', { name: 'Acuerdos por estado' })).getAllByRole('listitem');
    expect(items.map((item) => item.textContent)).toEqual(['Vigente2', 'Suspendido1', 'Cancelado1']);
  });

  it('A3 la tabla muestra el avance con porcentaje y las etiquetas de vigencia', async () => {
    prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    const fila1 = screen.getByRole('button', { name: 'AM-2026-001' }).closest('tr') as HTMLElement;
    expect(within(fila1).getByText('22%')).toBeInTheDocument();
    const vencida = screen.getByRole('button', { name: 'AM-2026-005' }).closest('tr') as HTMLElement;
    expect(within(vencida).getByText('Vencido')).toBeInTheDocument();
    const futura = screen.getByRole('button', { name: 'AM-2026-006' }).closest('tr') as HTMLElement;
    expect(within(futura).getByText('Aún no inicia')).toBeInTheDocument();
    expect(within(fila1).queryByText('Vencido')).not.toBeInTheDocument();
    const suspendido = screen.getByRole('button', { name: 'AM-2026-003' }).closest('tr') as HTMLElement;
    expect(within(suspendido).queryByText('Vencido')).not.toBeInTheDocument();
  });

  it('A3 cambiar estado y proveedor filtra de inmediato y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Suspendido' } });
    fireEvent.change(screen.getByLabelText('Proveedor'), { target: { value: '2' } });
    await waitFor(() => expect(llamadasLista(fetchMock).at(-1)).toBe('/api/v1/acuerdos-abastecimiento?pagina=1&tamanoPagina=10&estado=Suspendido&proveedorId=2'));
  });

  it('A3 la búsqueda espera 300 ms antes de consultar', async () => {
    vi.useRealTimers();
    vi.useFakeTimers({ toFake: ['Date', 'setTimeout', 'clearTimeout'] });
    const { fetchMock } = prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await vi.waitFor(() => expect(llamadasLista(fetchMock)).toHaveLength(1));
    fireEvent.change(screen.getByLabelText('Buscar'), { target: { value: 'andes' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(299); });
    expect(llamadasLista(fetchMock)).toHaveLength(1);
    await act(async () => { await vi.advanceTimersByTimeAsync(1); });
    await vi.waitFor(() => expect(llamadasLista(fetchMock).at(-1)).toContain('busqueda=andes'));
  });

  it('A3 muestra estado vacío y error de lista', async () => {
    prepararApi({ vacio: true });
    const primera = renderEnRouter(<AcuerdosAbastecimientoPage />);
    expect(await screen.findByText('No hay acuerdos para mostrar.')).toBeInTheDocument();
    primera.unmount();
    prepararApi({ errorLista: true });
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    expect(await screen.findByText('No se pudieron cargar los acuerdos.')).toBeInTheDocument();
  });
});

describe('pantalla de acuerdos: detalle y acciones por estado', () => {
  it('A4 vigente con consumo: líneas con saldo, sin Eliminar, Quitar línea solo sin consumo', async () => {
    prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-001');
    expect(screen.getByText('Marco anual de cobre.')).toBeInTheDocument();
    for (const nombre of ['Editar', 'Agregar línea', 'Suspender', 'Cancelar']) {
      expect(screen.getByRole('button', { name: nombre })).toBeInTheDocument();
    }
    expect(screen.queryByRole('button', { name: 'Eliminar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Reactivar' })).not.toBeInTheDocument();
    const fila = screen.getByRole('rowheader', { name: 'Cobre refinado' }).closest('tr') as HTMLElement;
    expect(within(fila).getByText('600')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Quitar línea de Cobre refinado' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Quitar línea de Lingote de zinc' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Registrar consumo de Cobre refinado' })).toBeEnabled();
  });

  it('A4 una línea sin saldo no permite registrar consumo', async () => {
    prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-002');
    expect(screen.getByRole('button', { name: 'Registrar consumo de Resina epóxica' })).toBeDisabled();
  });

  it('A4 suspendido ofrece Reactivar y no Registrar consumo ni Suspender', async () => {
    prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-003');
    expect(screen.getByRole('button', { name: 'Reactivar' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Suspender' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Registrar consumo de/ })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Agregar línea' })).toBeInTheDocument();
  });

  it('A4 cancelado es de solo lectura', async () => {
    prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-004');
    for (const nombre of ['Editar', 'Agregar línea', 'Suspender', 'Reactivar', 'Cancelar', 'Eliminar']) {
      expect(screen.queryByRole('button', { name: nombre })).not.toBeInTheDocument();
    }
    expect(screen.queryByRole('button', { name: /Quitar línea de/ })).not.toBeInTheDocument();
  });

  it('A4 vencido y no iniciado deshabilitan el consumo con su motivo escrito', async () => {
    prepararApi();
    renderEnRouter(<AcuerdosAbastecimientoPage />);
    await esperarCargaInicial();
    await seleccionar('AM-2026-005');
    expect(screen.getByRole('button', { name: 'Registrar consumo de Lingote de zinc' })).toBeDisabled();
    expect(screen.getByText('Registrar consumo no disponible: El acuerdo está vencido.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Eliminar' })).toBeInTheDocument();
    await seleccionar('AM-2026-006');
    expect(screen.getByText('Aún no hay líneas.')).toBeInTheDocument();
    expect(screen.getAllByText('Aún no inicia').length).toBeGreaterThan(0);
  });
});
