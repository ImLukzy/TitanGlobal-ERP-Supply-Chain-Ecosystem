import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { montar, prepararFetch } from './cotizaciones.testUtils';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

async function montarConCargaInicial(fetchMock: ReturnType<typeof prepararFetch>) {
  vi.useFakeTimers();
  montar();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();
  await waitFor(() => {
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/cotizaciones?'))).toHaveLength(1);
  });
}

describe('pantalla de cotizaciones', () => {
  it('A3 carga el listado una sola vez y muestra resumen y vacío', async () => {
    const fetchMock = prepararFetch();
    await montarConCargaInicial(fetchMock);
    expect(
      await screen.findByRole('button', { name: 'COT-DEV-001' }),
    ).toBeInTheDocument();
    expect(
      await screen.findAllByRole('cell', { name: 'Acme Industrial' }),
    ).toHaveLength(2);
    expect(
      fetchMock.mock.calls.filter((call) =>
        String(call[0]).includes('/cotizaciones?'),
      ),
    ).toHaveLength(1);
  });

  it('A3 informa cuando el filtro no tiene resultados', async () => {
    const fetchMock = prepararFetch({ listaVacia: true });
    await montarConCargaInicial(fetchMock);
    expect(
      await screen.findByText(
        'No hay cotizaciones que coincidan con los filtros.',
      ),
    ).toHaveAttribute('role', 'status');
  });

  it('A3 muestra el error del listado como alerta', async () => {
    const fetchMock = prepararFetch({ errorLista: true });
    await montarConCargaInicial(fetchMock);
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Servicio temporalmente no disponible.',
    );
  });

  it('A3 espera 300 ms para la búsqueda y vuelve a página 1', async () => {
    const fetchMock = prepararFetch();
    await montarConCargaInicial(fetchMock);
    vi.useFakeTimers();
    const busqueda = screen.getByLabelText('Buscar por número o cliente');
    fireEvent.change(busqueda, { target: { value: 'Acme' } });
    await act(async () => vi.advanceTimersByTimeAsync(299));
    expect(
      fetchMock.mock.calls.filter((call) =>
        String(call[0]).includes('/cotizaciones?'),
      ),
    ).toHaveLength(1);
    await act(async () => vi.advanceTimersByTimeAsync(1));
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(
      fetchMock.mock.calls.filter((call) =>
        String(call[0]).includes('/cotizaciones?'),
      ),
    ).toHaveLength(2);
    expect(fetchMock.mock.calls.at(-1)?.[0]).toContain('busqueda=Acme');
    expect(fetchMock.mock.calls.at(-1)?.[0]).toContain('pagina=1');
  });

  it('A3 valida el rango de fechas localmente sin pedir el listado', async () => {
    const fetchMock = prepararFetch();
    await montarConCargaInicial(fetchMock);
    await screen.findByRole('button', { name: 'COT-DEV-001' });
    fireEvent.change(screen.getByLabelText('Desde'), {
      target: { value: '2026-02-01' },
    });
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    const llamadasAntesDelRangoInvalido = fetchMock.mock.calls.filter((call) =>
      String(call[0]).includes('/cotizaciones?'),
    ).length;
    fireEvent.change(screen.getByLabelText('Hasta'), {
      target: { value: '2026-01-01' },
    });
    expect(screen.getByRole('alert')).toHaveTextContent(
      'La fecha desde no puede ser posterior a la fecha hasta.',
    );
    expect(
      fetchMock.mock.calls.filter((call) =>
        String(call[0]).includes('/cotizaciones?'),
      ),
    ).toHaveLength(llamadasAntesDelRangoInvalido);
  });

  it('A3 envía solo vencidas y excluye la acción Aceptar del detalle vencido', async () => {
    const fetchMock = prepararFetch();
    await montarConCargaInicial(fetchMock);
    fireEvent.click(await screen.findByLabelText('Solo vencidas'));
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(
      fetchMock.mock.calls.some((call) =>
        String(call[0]).includes('vencidas=true'),
      ),
    ).toBe(true);
    expect(screen.getByText('Vencida')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'COT-DEV-002' }));
    expect(
      await screen.findByRole('heading', { name: 'COT-DEV-002' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Aceptar' }),
    ).not.toBeInTheDocument();
  });

  it('A4 precarga precio y calcula descuento redondeado por línea', async () => {
    const fetchMock = prepararFetch();
    await montarConCargaInicial(fetchMock);
    fireEvent.click(
      await screen.findByRole('button', { name: 'Nueva cotización' }),
    );
    fireEvent.change(screen.getByLabelText('Cliente', { selector: 'select' }), {
      target: { value: '1' },
    });
    fireEvent.change(screen.getByLabelText('Válida hasta'), {
      target: { value: '2099-01-01' },
    });
    fireEvent.change(screen.getByLabelText('Producto'), {
      target: { value: '1' },
    });
    fireEvent.change(screen.getByLabelText('Descuento %'), {
      target: { value: '7.5' },
    });
    expect(screen.getByLabelText('Precio unitario')).toHaveValue(2500);
    fireEvent.change(screen.getByLabelText('Cantidad'), {
      target: { value: '3' },
    });
    fireEvent.change(screen.getByLabelText('Precio unitario'), {
      target: { value: '0.35' },
    });
    fireEvent.change(screen.getByLabelText('Descuento %'), {
      target: { value: '10' },
    });
    expect(screen.getAllByText(/S\/ 0\.94/)).toHaveLength(2);
    expect(screen.getByText(/Total:/).parentElement).toHaveTextContent(
      'S/ 0.94',
    );
  });

  it('A4 valida localmente sin petición y conserva los valores tras un 400', async () => {
    const fetchMock = prepararFetch();
    await montarConCargaInicial(fetchMock);
    fireEvent.click(
      await screen.findByRole('button', { name: 'Nueva cotización' }),
    );
    fireEvent.click(screen.getByRole('button', { name: 'Guardar cotización' }));
    expect(screen.getByText('El cliente es obligatorio.')).toBeInTheDocument();
    expect(
      screen.getByText('La fecha de validez es obligatoria.'),
    ).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.some(
        (call) =>
          call[0] === '/api/v1/cotizaciones' && call[1]?.method === 'POST',
      ),
    ).toBe(false);
    fireEvent.change(screen.getByLabelText('Cliente', { selector: 'select' }), {
      target: { value: '1' },
    });
    fireEvent.change(screen.getByLabelText('Válida hasta'), {
      target: { value: '2099-01-01' },
    });
    fireEvent.change(screen.getByLabelText('Producto'), {
      target: { value: '1' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar cotización' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'La fecha de validez no puede ser anterior a hoy.',
    );
    expect(
      screen.getByLabelText('Cliente', { selector: 'select' }),
    ).toHaveValue('1');
    expect(screen.getByLabelText('Producto')).toHaveValue('1');
  });
});
