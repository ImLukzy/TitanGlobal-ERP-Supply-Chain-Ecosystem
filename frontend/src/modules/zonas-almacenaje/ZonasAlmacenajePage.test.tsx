import { afterEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { limpiarZonas, prepararApiZonas, renderZonas, esperarCargaZonas } from './zonas.testUtils';

afterEach(limpiarZonas);

describe('ZonasAlmacenajePage', () => {
  it('carga listado, resumen de ocupación y detalle inicial', async () => {
    vi.useFakeTimers();
    const { fetchMock } = prepararApiZonas();
    renderZonas();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(301);
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();
    });
    vi.useRealTimers();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/zonas-almacenaje?'))).toHaveLength(1);
    expect(screen.getByRole('button', { name: 'REC-01' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Editar zona' })).toBeInTheDocument();
    expect(screen.getByText('Casi lleno')).toBeInTheDocument();
    expect(screen.getByRole('meter', { name: 'Ocupación de ALM-LIM-01' })).toHaveAttribute('aria-valuenow', '90');
    expect(screen.getByText('450.00 de 500.00 m³ · 90.00%')).toBeInTheDocument();
    expect(screen.getByText('Recepción principal')).toBeInTheDocument();
  });

  it('pide el listado una sola vez al montar aun después de 300 ms', async () => {
    vi.useFakeTimers();
    const { fetchMock } = prepararApiZonas();
    renderZonas();
    await vi.advanceTimersByTimeAsync(301);
    await vi.waitFor(() => {
      expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/zonas-almacenaje?'))).toHaveLength(1);
    });
  });

  it('aplica filtros y restablece la página a 1', async () => {
    const { fetchMock } = prepararApiZonas();
    renderZonas();
    await esperarCargaZonas(fetchMock);
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Picking' } });
    await waitFor(() => {
      const llamadas = fetchMock.mock.calls.filter((call) => String(call[0]).includes('/zonas-almacenaje?'));
      expect(String(llamadas.at(-1)?.[0])).toContain('tipo=Picking');
      expect(String(llamadas.at(-1)?.[0])).toContain('pagina=1');
    });
  });

  it('preselecciona el almacén de la URL y filtra lista y resumen', async () => {
    const { fetchMock } = prepararApiZonas();
    renderZonas(undefined, ['/modulos/24-zonas-almacenaje?almacen=3']);
    await screen.findByRole('button', { name: 'FRIO-01' });
    expect(screen.getByLabelText('Almacén')).toHaveValue('3');
    expect(screen.getByText('130.00 de 150.00 m³ · 86.67%')).toBeInTheDocument();
    const urls = fetchMock.mock.calls.map((call) => String(call[0]));
    expect(urls.some((url) => url.includes('/zonas-almacenaje?almacenId=3'))).toBe(true);
    expect(urls.some((url) => url.includes('/zonas-almacenaje/resumen?almacenId=3'))).toBe(true);
  });

  it('envía temperatura en la query del listado', async () => {
    const { fetchMock } = prepararApiZonas();
    renderZonas();
    await esperarCargaZonas(fetchMock);
    fireEvent.change(screen.getByLabelText('Temperatura'), { target: { value: 'Refrigerada' } });
    await waitFor(() => {
      const urls = fetchMock.mock.calls.filter((call) => String(call[0]).includes('/zonas-almacenaje?'));
      expect(String(urls.at(-1)?.[0])).toContain('temperatura=Refrigerada');
    });
  });

  it('muestra estados vacío y error del listado', async () => {
    prepararApiZonas({ vacio: true });
    renderZonas();
    expect(await screen.findByText('No hay zonas que coincidan con los filtros.')).toBeInTheDocument();
    limpiarZonas();
    prepararApiZonas({ errorLista: true });
    renderZonas();
    expect(await screen.findByText('No se pudo cargar el listado de zonas.')).toBeInTheDocument();
  });

  it('valida obligatorios y capacidad localmente sin enviar peticiones', async () => {
    const { fetchMock } = prepararApiZonas();
    renderZonas();
    await esperarCargaZonas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva zona' }));
    fetchMock.mockClear();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar zona' }));
    expect(await screen.findByText('El código de zona es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El nombre de la zona es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('La capacidad debe ser mayor que 0.')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('muestra conflicto de capacidad y conserva lo escrito', async () => {
    const { fetchMock } = prepararApiZonas();
    renderZonas();
    await esperarCargaZonas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva zona' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'EXTRA-01' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Zona adicional' } });
    fireEvent.change(screen.getByLabelText('Capacidad (m³)'), { target: { value: '75' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar zona' }));
    expect(await screen.findByText(/La capacidad de las zonas activas/)).toBeInTheDocument();
    expect(screen.getByLabelText('Código')).toHaveValue('EXTRA-01');
    expect(screen.getByLabelText('Nombre')).toHaveValue('Zona adicional');
  });

  it('muestra 409 por código repetido conservando los valores', async () => {
    const { fetchMock } = prepararApiZonas();
    renderZonas();
    await esperarCargaZonas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva zona' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'ALM-A' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Otra zona de almacenamiento' } });
    fireEvent.change(screen.getByLabelText('Capacidad (m³)'), { target: { value: '10' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar zona' }));
    expect(await screen.findByText('Ya existe la zona ALM-A en el almacén ALM-LIM-01.')).toBeInTheDocument();
    expect(screen.getByLabelText('Código')).toHaveValue('ALM-A');
    expect(screen.getByLabelText('Nombre')).toHaveValue('Otra zona de almacenamiento');
  });

  it('edita con PUT y elimina con refresco', async () => {
    const { fetchMock } = prepararApiZonas();
    renderZonas();
    await esperarCargaZonas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Editar zona' }));
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Recepción actualizada' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar zona' }));
    await waitFor(() => {
      expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'PUT' && /\/zonas-almacenaje\/\d+$/.test(String(call[0])))).toBe(true);
    });
    await screen.findByRole('button', { name: 'REC-01' });
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar zona' }));
    const dialogo = screen.getByRole('dialog');
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Eliminar zona' }));
    await waitFor(() => {
      expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'DELETE' && /\/zonas-almacenaje\/\d+$/.test(String(call[0])))).toBe(true);
    });
    await waitFor(() => {
      expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/zonas-almacenaje?'))).toHaveLength(3);
    });
  });

  it('cancela el diálogo con Escape y devuelve el foco', async () => {
    const { fetchMock } = prepararApiZonas();
    renderZonas();
    await esperarCargaZonas(fetchMock);
    const botonEliminar = screen.getByRole('button', { name: 'Eliminar zona' });
    fireEvent.click(botonEliminar);
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(botonEliminar).toHaveFocus());
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'DELETE')).toBe(false);
  });
});
