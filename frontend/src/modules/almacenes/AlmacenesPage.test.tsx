import { afterEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import {
  esperarCargaAlmacenes,
  prepararApiAlmacenes,
  renderAlmacenes,
} from './almacenes.testUtils';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

describe('AlmacenesPage', () => {
  it('muestra capacidades del seed y estados como pastillas', async () => {
    const { fetchMock } = prepararApiAlmacenes();
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    expect(screen.getByText('1,025.00')).toBeInTheDocument();
    expect(screen.getByText('925.00')).toBeInTheDocument();
    expect(document.querySelectorAll('.proveedor-status.is-active').length).toBeGreaterThan(0);
    expect(document.querySelectorAll('.proveedor-status.is-inactive').length).toBeGreaterThan(0);
  });

  it('pide el listado una sola vez al montar, incluso después de 300 ms', async () => {
    vi.useFakeTimers();
    const { fetchMock } = prepararApiAlmacenes();
    renderAlmacenes();
    await vi.advanceTimersByTimeAsync(301);
    await vi.waitFor(() => {
      expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/almacenes?'))).toHaveLength(1);
    });
    vi.useRealTimers();
    await esperarCargaAlmacenes(fetchMock);
  });

  it('envía los filtros de ciudad, tipo y estado y reinicia la página', async () => {
    const { fetchMock } = prepararApiAlmacenes();
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    fireEvent.change(screen.getByLabelText('Ciudad', { selector: 'select' }), { target: { value: 'Lima' } });
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Regional' } });
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'false' } });
    await waitFor(() => {
      const solicitudes = fetchMock.mock.calls.filter((call) => String(call[0]).includes('/almacenes?'));
      expect(solicitudes.at(-1)?.[0]).toContain('ciudad=Lima');
      expect(solicitudes.at(-1)?.[0]).toContain('tipo=Regional');
      expect(solicitudes.at(-1)?.[0]).toContain('activo=false');
      expect(solicitudes.at(-1)?.[0]).toContain('pagina=1');
    });
  });

  it('filtra por búsqueda con temporizadores falsos', async () => {
    vi.useFakeTimers();
    const { fetchMock } = prepararApiAlmacenes();
    renderAlmacenes();
    await vi.advanceTimersByTimeAsync(301);
    await vi.waitFor(() => expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/almacenes?'))).toHaveLength(1));
    vi.useRealTimers();
    await esperarCargaAlmacenes(fetchMock);
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText('Buscar por código o nombre'), { target: { value: 'Arequipa' } });
    await vi.advanceTimersByTimeAsync(301);
    await vi.waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]).includes('busqueda=Arequipa'))).toBe(true));
  });

  it('muestra el estado vacío', async () => {
    const { fetchMock } = prepararApiAlmacenes({ vacio: true });
    renderAlmacenes();
    expect(await screen.findByText('No hay almacenes que coincidan con los filtros.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/almacenes?'))).toHaveLength(1);
  });

  it('muestra el error del listado', async () => {
    prepararApiAlmacenes({ errorLista: true });
    renderAlmacenes();
    expect(await screen.findByText('No se pudo cargar el listado de almacenes.')).toBeInTheDocument();
  });

  it('muestra Sin responsable en el detalle correspondiente', async () => {
    const { fetchMock } = prepararApiAlmacenes();
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'ALM-CAL-TR' }));
    expect(await screen.findByText('Sin responsable')).toBeInTheDocument();
  });

  it('valida los campos obligatorios localmente sin enviar una petición', async () => {
    const { fetchMock } = prepararApiAlmacenes();
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo almacén' }));
    fetchMock.mockClear();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar almacén' }));
    expect(await screen.findByText('El código es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El nombre del almacén es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('La dirección es obligatoria.')).toBeInTheDocument();
    expect(screen.getByText('La ciudad es obligatoria.')).toBeInTheDocument();
    expect(screen.getByText('La capacidad debe ser mayor que 0.')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('valida formato y longitudes localmente sin enviar una petición', async () => {
    const { fetchMock } = prepararApiAlmacenes();
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo almacén' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'alm con espacios' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'N'.repeat(201) } });
    fireEvent.change(screen.getByLabelText('Dirección'), { target: { value: 'D'.repeat(301) } });
    fireEvent.change(screen.getByLabelText('Ciudad', { selector: 'input' }), { target: { value: 'C'.repeat(101) } });
    fireEvent.change(screen.getByLabelText('Responsable (opcional)'), { target: { value: 'R'.repeat(101) } });
    fireEvent.change(screen.getByLabelText('Capacidad (m³)'), { target: { value: '100' } });
    fetchMock.mockClear();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar almacén' }));
    expect(await screen.findByText('El código solo admite letras, números y guiones (2 a 20 caracteres).')).toBeInTheDocument();
    expect(screen.getByText('El nombre del almacén no puede superar 200 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('La dirección no puede superar 300 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('La ciudad no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('El responsable no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('muestra el 409 conservando los valores del formulario', async () => {
    const { fetchMock } = prepararApiAlmacenes({ errorGuardarCodigo: true });
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo almacén' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'ALM-NEW-01' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Nuevo almacén' } });
    fireEvent.change(screen.getByLabelText('Dirección'), { target: { value: 'Av. Nueva 1' } });
    fireEvent.change(screen.getByLabelText('Ciudad', { selector: 'input' }), { target: { value: 'Lima' } });
    fireEvent.change(screen.getByLabelText('Capacidad (m³)'), { target: { value: '500' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar almacén' }));
    expect(await screen.findByText('Ya existe un almacén con el código ALM-NEW-01.')).toBeInTheDocument();
    expect(screen.getByLabelText('Código')).toHaveValue('ALM-NEW-01');
    expect(screen.getByLabelText('Nombre')).toHaveValue('Nuevo almacén');
  });

  it('muestra el error 400 de la API conservando los valores', async () => {
    const { fetchMock } = prepararApiAlmacenes({ errorGuardar400: true });
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo almacén' }));
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'ALM-NEW-02' } });
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Nuevo almacén' } });
    fireEvent.change(screen.getByLabelText('Dirección'), { target: { value: 'Av. Nueva 2' } });
    fireEvent.change(screen.getByLabelText('Ciudad', { selector: 'input' }), { target: { value: 'Lima' } });
    fireEvent.change(screen.getByLabelText('Capacidad (m³)'), { target: { value: '500' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar almacén' }));
    expect(await screen.findByText('El nombre del almacén no puede superar 200 caracteres.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre')).toHaveValue('Nuevo almacén');
  });

  it('actualiza el almacén seleccionado con PUT', async () => {
    const { fetchMock } = prepararApiAlmacenes();
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Editar almacén' }));
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Central Lima actualizada' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar almacén' }));
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'PUT' && String(call[0]).endsWith('/almacenes/3'))).toBe(true));
  });

  it('elimina con confirmación y refresca el listado', async () => {
    const { fetchMock } = prepararApiAlmacenes();
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar almacén' }));
    const confirmar = screen.getAllByRole('button', { name: 'Eliminar almacén' }).at(-1);
    if (confirmar !== undefined) {
      fireEvent.click(confirmar);
    }
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'DELETE')).toBe(true));
    await waitFor(() => expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/almacenes?'))).toHaveLength(2));
  });

  it('muestra el 409 de la API si el almacén tiene zonas', async () => {
    const { fetchMock } = prepararApiAlmacenes({ errorEliminarConZonas: true });
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar almacén' }));
    const confirmar = screen.getAllByRole('button', { name: 'Eliminar almacén' }).at(-1);
    if (confirmar !== undefined) {
      fireEvent.click(confirmar);
    }
    expect(await screen.findByText('No se puede eliminar el almacén 3 porque tiene zonas.')).toBeInTheDocument();
  });

  it('Escape cancela la baja sin petición y devuelve el foco', async () => {
    const { fetchMock } = prepararApiAlmacenes();
    renderAlmacenes();
    await esperarCargaAlmacenes(fetchMock);
    const botonBaja = screen.getByRole('button', { name: 'Eliminar almacén' });
    fireEvent.click(botonBaja);
    fetchMock.mockClear();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
    await waitFor(() => expect(botonBaja).toHaveFocus());
  });
});
