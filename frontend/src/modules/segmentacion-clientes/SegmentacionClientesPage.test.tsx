import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { useLocation } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { SegmentacionClientesPage } from './SegmentacionClientesPage';
import {
  clasificacionSeed,
  limpiarSegmentacion,
  prepararApiSegmentacion,
  renderSegmentacion,
  esperarCargaSegmentos,
} from './segmentacion.testUtils';

afterEach(() => {
  limpiarSegmentacion();
});

function UbicacionActual() {
  const ubicacion = useLocation();
  return <output aria-label="Query actual">{ubicacion.search}</output>;
}

describe('SegmentacionClientesPage', () => {
  it('muestra resumen, criterios y hace una sola petición inicial incluso tras 300 ms', async () => {
    const { fetchMock } = prepararApiSegmentacion();
    vi.useFakeTimers();
    renderSegmentacion();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(301);
    });
    vi.useRealTimers();
    expect(await screen.findByText('≥ S/ 5,000.00 y ≥ 3 órdenes')).toBeInTheDocument();
    expect(screen.getByText('Total de clientes')).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/segmentos-clientes?'))).toHaveLength(1);
  });

  it('cambia a clasificación con teclado y refleja la vista en la URL', async () => {
    const { fetchMock } = prepararApiSegmentacion();
    renderSegmentacion(<><SegmentacionClientesPage /><UbicacionActual /></>);
    await esperarCargaSegmentos(fetchMock);
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowRight' });
    expect(await screen.findByRole('heading', { name: 'Clasificación calculada' })).toBeInTheDocument();
    expect(screen.getByLabelText('Query actual')).toHaveTextContent('?vista=clasificacion');
  });

  it('abre la clasificación indicada por query y muestra clientes con y sin segmento', async () => {
    prepararApiSegmentacion();
    renderSegmentacion(undefined, ['/modulos/02-segmentacion-clientes?vista=clasificacion']);
    expect(await screen.findByText('Acme Industrial')).toBeInTheDocument();
    expect(screen.getAllByText('Sin segmento').length).toBeGreaterThan(0);
    expect(screen.getAllByText('VIP · Clientes VIP').length).toBeGreaterThan(0);
  });

  it('filtra por segmento y «Sin segmento» con la query exacta, en la página 1', async () => {
    const { llamadas } = prepararApiSegmentacion();
    renderSegmentacion(undefined, ['/modulos/02-segmentacion-clientes?vista=clasificacion']);
    await screen.findByText('Acme Industrial');
    const selector = screen.getByLabelText('Segmento');
    fireEvent.change(selector, { target: { value: 'sin-segmento' } });
    await waitFor(() => expect(llamadas.some((llamada) => new URL(llamada.url, 'http://localhost').searchParams.get('sinSegmento') === 'true')).toBe(true));
    expect(await screen.findByText('Cliente sin órdenes')).toBeInTheDocument();
    expect(new URL(llamadas.at(-1)?.url ?? '', 'http://localhost').searchParams.get('pagina')).toBe('1');
  });

  it('filtra la clasificación por segmento activo', async () => {
    const { llamadas } = prepararApiSegmentacion();
    renderSegmentacion(undefined, ['/modulos/02-segmentacion-clientes?vista=clasificacion']);
    await screen.findByText('Acme Industrial');
    await waitFor(() => expect(screen.getByRole('option', { name: /VIP/ })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Segmento'), { target: { value: '1' } });
    await waitFor(() => expect(llamadas.some((llamada) => new URL(llamada.url, 'http://localhost').searchParams.get('segmentoId') === '1')).toBe(true));
  });

  it('espera 300 ms para aplicar búsqueda sin peticiones duplicadas al montar', async () => {
    const { fetchMock, llamadas } = prepararApiSegmentacion();
    vi.useFakeTimers();
    renderSegmentacion();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(301);
    });
    vi.useRealTimers();
    await esperarCargaSegmentos(fetchMock);
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText('Buscar código o nombre'), { target: { value: 'FRE' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(llamadas.filter((llamada) => llamada.url.includes('/segmentos-clientes?'))).toHaveLength(1);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    expect(await screen.findByRole('button', { name: 'FRECUENTE' })).toBeInTheDocument();
    expect(llamadas.filter((llamada) => llamada.url.includes('/segmentos-clientes?'))).toHaveLength(2);
  });

  it('muestra estados vacío y error del listado', async () => {
    prepararApiSegmentacion({ vacio: true });
    renderSegmentacion();
    expect(await screen.findByText('No hay segmentos que coincidan con los filtros.')).toBeInTheDocument();
    limpiarSegmentacion();
    prepararApiSegmentacion({ errorLista: true });
    renderSegmentacion();
    expect(await screen.findByText('No se pudo cargar la lista de segmentos.')).toBeInTheDocument();
  });

  it('muestra estado vacío y error en clasificación', async () => {
    prepararApiSegmentacion();
    renderSegmentacion(undefined, ['/modulos/02-segmentacion-clientes?vista=clasificacion']);
    expect(await screen.findByText('Acme Industrial')).toBeInTheDocument();
    limpiarSegmentacion();
    prepararApiSegmentacion({ errorClasificacion: true });
    renderSegmentacion(undefined, ['/modulos/02-segmentacion-clientes?vista=clasificacion']);
    expect(await screen.findByText('No se pudo cargar la clasificación de clientes.')).toBeInTheDocument();
  });

  it('conserva el formulario y muestra el 409 literal del código duplicado', async () => {
    const { fetchMock } = prepararApiSegmentacion({ errorCodigo: true });
    renderSegmentacion();
    await esperarCargaSegmentos(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo segmento' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'NUEVO' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Segmento nuevo' } });
    fireEvent.change(screen.getByLabelText('Prioridad'), { target: { value: '5' } });
    fireEvent.change(screen.getByLabelText('Órdenes mínimas'), { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar segmento' }));
    expect(await screen.findByText('Ya existe un segmento con el código NUEVO.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre')).toHaveValue('Segmento nuevo');
  });

  it('conserva el formulario y muestra el 409 literal de prioridad duplicada', async () => {
    const { fetchMock } = prepararApiSegmentacion({ errorPrioridad: true });
    renderSegmentacion();
    await esperarCargaSegmentos(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo segmento' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'NUEVO' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Segmento nuevo' } });
    fireEvent.change(screen.getByLabelText('Prioridad'), { target: { value: '5' } });
    fireEvent.change(screen.getByLabelText('Órdenes mínimas'), { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar segmento' }));
    expect(await screen.findByText('Ya existe un segmento con la prioridad 5.')).toBeInTheDocument();
    expect(screen.getByLabelText('Código')).toHaveValue('NUEVO');
  });

  it('valida localmente los campos sin enviar escrituras', async () => {
    const { fetchMock } = prepararApiSegmentacion();
    renderSegmentacion();
    await esperarCargaSegmentos(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo segmento' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar segmento' }));
    expect(await screen.findByText('El código es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El nombre del segmento es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('Define al menos un criterio: monto mínimo u órdenes mínimas.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'POST')).toBe(false);
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'A' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'X'.repeat(101) } });
    fireEvent.change(screen.getByLabelText('Descripción (opcional)'), { target: { value: 'D'.repeat(301) } });
    fireEvent.change(screen.getByLabelText('Prioridad'), { target: { value: '1000' } });
    fireEvent.change(screen.getByLabelText('Monto mínimo (S/)'), { target: { value: '-1' } });
    fireEvent.change(screen.getByLabelText('Órdenes mínimas'), { target: { value: '-1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar segmento' }));
    expect(await screen.findByText('El código solo admite letras, números y guiones (2 a 20 caracteres).')).toBeInTheDocument();
    expect(screen.getByText('El nombre del segmento no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('La descripción no puede superar 300 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('La prioridad debe estar entre 1 y 999.')).toBeInTheDocument();
    expect(screen.getByText('El monto mínimo no puede ser negativo.')).toBeInTheDocument();
    expect(screen.getByText('Las órdenes mínimas no pueden ser negativas.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'POST')).toBe(false);
  });

  it('edita con PUT, elimina y refresca el resumen y devuelve el foco al cancelar con Escape', async () => {
    const { fetchMock } = prepararApiSegmentacion();
    renderSegmentacion();
    await esperarCargaSegmentos(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Editar segmento' }));
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'VIP editado' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar segmento' }));
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'PUT')).toBe(true));
    const botonBaja = await screen.findByRole('button', { name: 'Eliminar segmento' });
    fireEvent.click(botonBaja);
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
    await waitFor(() => expect(botonBaja).toHaveFocus());
    expect(botonBaja).toHaveFocus();
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'DELETE')).toBe(false);
    fireEvent.click(botonBaja);
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar segmento' }));
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'DELETE')).toBe(true));
    await waitFor(() => expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/segmentos-clientes/resumen'))).toHaveLength(2));
  });

  it('incluye el monto y los datos seed esperados de clasificación', () => {
    expect(clasificacionSeed).toHaveLength(5);
    expect(clasificacionSeed[0]?.montoTotal).toBe(10000);
  });
});
