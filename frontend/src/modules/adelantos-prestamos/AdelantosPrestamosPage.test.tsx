import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { formatearFecha } from '../../lib/formato';
import {
  limpiarPrestamos,
  moneda,
  montarPrestamosCargados,
  montarSinFilas,
  prepararApiPrestamos,
  urlsDeListado,
  type LlamadaPrestamos,
  type OpcionesApiPrestamos,
} from './prestamos.testUtils';

afterEach(() => {
  limpiarPrestamos();
});

const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden registrar o anular préstamos.';

async function seleccionar(numero: string, nombre: string): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: numero }));
  await screen.findByRole('heading', { name: nombre });
}

// La ficha es la sección etiquetada por el título del detalle.
function ficha(): ReturnType<typeof within> {
  const seccion = document.getElementById('prestamo-detail-title')?.closest('section');
  if (seccion === null || seccion === undefined) {
    throw new Error('La ficha del préstamo no está en pantalla.');
  }
  return within(seccion);
}

function escrituras(llamadas: LlamadaPrestamos[]): LlamadaPrestamos[] {
  return llamadas.filter((llamada) => llamada.metodo === 'POST');
}

describe('pantalla de Adelantos y préstamos: listado, filtros y resumen', () => {
  it('carga una sola vez el listado en página 1 con número, tipo, importes y fechas', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock);
    expect(screen.getByRole('heading', { name: '67 · Adelantos y préstamos' })).toBeInTheDocument();
    expect(urlsDeListado(fetchMock)).toEqual(['/api/v1/prestamos?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table');
    expect(within(tabla).getByRole('button', { name: 'PRE-000004' })).toBeInTheDocument();
    expect(within(tabla).getAllByText('EMP-000001 · Ana Torres Vega')).toHaveLength(2);
    expect(within(tabla).getAllByText('Préstamo')).toHaveLength(2);
    expect(within(tabla).getAllByText('Adelanto')).toHaveLength(2);
    expect(within(tabla).getByText(moneda(6000))).toBeInTheDocument();
    expect(within(tabla).getByText(moneda(4000))).toBeInTheDocument();
    expect(within(tabla).getByText(formatearFecha('2026-09-01'))).toBeInTheDocument();
  });

  it('muestra cada estado con texto en pastillas', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock);
    const tabla = screen.getByRole('table');
    expect(within(tabla).getAllByText('Activo')).toHaveLength(2);
    expect(within(tabla).getByText('Cancelado')).toBeInTheDocument();
    expect(within(tabla).getByText('Anulado')).toBeInTheDocument();
  });

  it('muestra las cuatro tarjetas del resumen en moneda con una sola petición', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock);
    const resumen = screen.getByRole('region', { name: 'Resumen de préstamos' });
    expect(within(resumen).getByText('Préstamos activos').nextElementSibling).toHaveTextContent('2');
    expect(within(resumen).getByText('Saldo pendiente').nextElementSibling).toHaveTextContent(moneda(4500));
    expect(within(resumen).getByText('Otorgado este mes').nextElementSibling).toHaveTextContent(moneda(500));
    expect(within(resumen).getByText('Cuotas del mes').nextElementSibling).toHaveTextContent(moneda(1500));
  });

  it('envía tipo y estado a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiPrestamos({ totalListado: 25 });
    await montarPrestamosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Adelanto' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toBe('/api/v1/prestamos?tipo=Adelanto&pagina=1&tamanoPagina=10'));
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Cancelado' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toBe('/api/v1/prestamos?tipo=Adelanto&estado=Cancelado&pagina=1&tamanoPagina=10'));
  });

  it('aplica la búsqueda 300 ms después del último cambio con una sola consulta del último texto', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock);
    const antes = urlsDeListado(fetchMock).length;
    vi.useFakeTimers();
    const buscador = screen.getByLabelText('Buscar por número o empleado');
    fireEvent.change(buscador, { target: { value: 'P' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(200);
    });
    fireEvent.change(buscador, { target: { value: 'PRE-000003' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsDeListado(fetchMock)).toHaveLength(antes);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsDeListado(fetchMock)).toHaveLength(antes + 1));
    expect(urlsDeListado(fetchMock).at(-1)).toBe('/api/v1/prestamos?busqueda=PRE-000003&pagina=1&tamanoPagina=10');
  });

  it('muestra el estado vacío cuando no hay préstamos', async () => {
    prepararApiPrestamos({ listaVacia: true });
    await montarSinFilas();
    expect(await screen.findByText('No hay préstamos con esos filtros.')).toHaveAttribute('role', 'status');
  });

  it('muestra el error de carga del listado', async () => {
    prepararApiPrestamos({ errorLista: true });
    await montarSinFilas();
    expect(await screen.findByText('No se pudo cargar la lista de préstamos.')).toHaveAttribute('role', 'alert');
  });

  it('muestra el error del resumen sin ocultar el listado', async () => {
    prepararApiPrestamos({ errorResumen: true });
    await montarSinFilas();
    expect(await screen.findByText('No se pudo cargar el resumen de préstamos.')).toHaveAttribute('role', 'alert');
    expect(await screen.findByRole('button', { name: 'PRE-000001' })).toBeInTheDocument();
  });
});

describe('pantalla de Adelantos y préstamos: detalle', () => {
  it('invita a seleccionar un préstamo antes de elegir', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock);
    expect(screen.getByText('Selecciona un préstamo para ver su ficha.')).toBeInTheDocument();
  });

  it('muestra los datos y la tabla de cuotas con mes, descontado y estado', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock);
    await seleccionar('PRE-000001', 'Ana Torres Vega');
    const detalle = ficha();
    expect(detalle.getByText('Gastos médicos')).toBeInTheDocument();
    expect(detalle.getByText('Primer descuento').nextElementSibling).toHaveTextContent('09/2026');
    expect(detalle.getByText(formatearFecha('2026-09-01'))).toBeInTheDocument();
    const cuotas = within(detalle.getByRole('table'));
    expect(cuotas.getAllByRole('row')).toHaveLength(7);
    expect(cuotas.getByText('01/2026')).toBeInTheDocument();
    expect(cuotas.getAllByText('Descontada')).toHaveLength(2);
    expect(cuotas.getAllByText('Pendiente')).toHaveLength(4);
    expect(cuotas.getAllByText(moneda(1000)).length).toBeGreaterThanOrEqual(8);
  });

  it('muestra fecha y motivo de la anulación en un préstamo anulado', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock);
    await seleccionar('PRE-000003', 'Maria Salazar Quispe');
    const detalle = ficha();
    expect(detalle.getByText('Error de carga')).toBeInTheDocument();
    expect(detalle.getByText(formatearFecha('2026-07-05'))).toBeInTheDocument();
  });

  it('muestra el error del detalle', async () => {
    const { fetchMock } = prepararApiPrestamos({ errorDetalle: true });
    await montarPrestamosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'PRE-000004' }));
    expect(await screen.findByText('No se pudo cargar el detalle del préstamo.')).toHaveAttribute('role', 'alert');
  });
});

describe('pantalla de Adelantos y préstamos: «Anular» según estado, descuentos y rol', () => {
  it('un préstamo activo sin descuentos ofrece Anular habilitado y sin aviso', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock);
    await seleccionar('PRE-000004', 'Ana Torres Vega');
    const detalle = ficha();
    expect(detalle.getByRole('button', { name: 'Anular' })).toBeEnabled();
    expect(detalle.queryByText(/no se puede anular/)).not.toBeInTheDocument();
  });

  it('un préstamo con descuentos deshabilita Anular y muestra el motivo', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock);
    await seleccionar('PRE-000001', 'Ana Torres Vega');
    const detalle = ficha();
    const texto = 'El préstamo PRE-000001 ya tiene descuentos y no se puede anular.';
    expect(detalle.getByRole('button', { name: 'Anular' })).toBeDisabled();
    expect(detalle.getByText(texto)).toBeInTheDocument();
    expect(detalle.getByRole('button', { name: 'Anular' })).toHaveAccessibleDescription(texto);
  });

  it('un préstamo cancelado o anulado deshabilita Anular con el motivo del estado', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock);
    await seleccionar('PRE-000002', 'Luis Rojas Mendoza');
    expect(ficha().getByRole('button', { name: 'Anular' })).toBeDisabled();
    expect(ficha().getByText('El préstamo PRE-000002 no está activo y no se puede anular.')).toBeInTheDocument();
    await seleccionar('PRE-000003', 'Maria Salazar Quispe');
    expect(ficha().getByRole('button', { name: 'Anular' })).toBeDisabled();
    expect(ficha().getByText('El préstamo PRE-000003 no está activo y no se puede anular.')).toBeInTheDocument();
  });

  it('sin RRHH ni ADMIN deshabilita «Nuevo préstamo» y Anular con el aviso de rol', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock, ['VENTAS']);
    const nuevo = screen.getByRole('button', { name: 'Nuevo préstamo' });
    expect(nuevo).toBeDisabled();
    expect(nuevo).toHaveAccessibleDescription(TEXTO_SOLO_RRHH);
    await seleccionar('PRE-000004', 'Ana Torres Vega');
    const anular = ficha().getByRole('button', { name: 'Anular' });
    expect(anular).toBeDisabled();
    expect(anular).toHaveAccessibleDescription(TEXTO_SOLO_RRHH);
  });

  it('con ADMIN habilita «Nuevo préstamo» y no muestra el aviso de rol', async () => {
    const { fetchMock } = prepararApiPrestamos();
    await montarPrestamosCargados(fetchMock, ['ADMIN']);
    expect(screen.getByRole('button', { name: 'Nuevo préstamo' })).toBeEnabled();
    expect(screen.queryByText(TEXTO_SOLO_RRHH)).not.toBeInTheDocument();
  });
});

describe('pantalla de Adelantos y préstamos: anulación', () => {
  async function abrirActivo(opciones: OpcionesApiPrestamos = {}) {
    const api = prepararApiPrestamos(opciones);
    await montarPrestamosCargados(api.fetchMock);
    await seleccionar('PRE-000004', 'Ana Torres Vega');
    return api;
  }

  it('anula con motivo: valida en local, envía el cuerpo, cierra y vuelve a pedir listado, resumen y detalle', async () => {
    const { fetchMock, llamadas } = await abrirActivo();
    const antes = urlsDeListado(fetchMock).length;
    fireEvent.click(screen.getByRole('button', { name: 'Anular' }));
    const dialogo = within(await screen.findByRole('dialog', { name: 'Anular préstamo' }));
    expect(dialogo.getByLabelText('Motivo de la anulación')).toHaveFocus();
    fireEvent.click(dialogo.getByRole('button', { name: 'Anular préstamo' }));
    expect(await dialogo.findByText('El motivo de la anulación es obligatorio.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
    fireEvent.change(dialogo.getByLabelText('Motivo de la anulación'), { target: { value: 'a'.repeat(301) } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Anular préstamo' }));
    expect(await dialogo.findByText('El motivo de la anulación no puede superar 300 caracteres.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
    fireEvent.change(dialogo.getByLabelText('Motivo de la anulación'), { target: { value: ' Error de carga ' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Anular préstamo' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toEqual([
      { url: '/api/v1/prestamos/4/anulacion', metodo: 'POST', cuerpo: { motivo: 'Error de carga' } },
    ]);
    await waitFor(() => expect(urlsDeListado(fetchMock)).toHaveLength(antes + 1));
    expect(llamadas.filter((llamada) => llamada.url === '/api/v1/prestamos/resumen')).toHaveLength(2);
  });

  it('muestra el 409 dentro del diálogo, lo mantiene abierto con el motivo y pone al día la pantalla', async () => {
    const { fetchMock } = await abrirActivo({
      errorEscritura: { accion: 'anulacion', status: 409, mensaje: 'El préstamo PRE-000004 ya tiene descuentos y no se puede anular.' },
    });
    const antes = urlsDeListado(fetchMock).length;
    fireEvent.click(screen.getByRole('button', { name: 'Anular' }));
    const dialogo = within(await screen.findByRole('dialog', { name: 'Anular préstamo' }));
    fireEvent.change(dialogo.getByLabelText('Motivo de la anulación'), { target: { value: 'Error de carga' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Anular préstamo' }));
    expect(await dialogo.findByText('El préstamo PRE-000004 ya tiene descuentos y no se puede anular.')).toHaveAttribute('role', 'alert');
    expect(dialogo.getByLabelText('Motivo de la anulación')).toHaveValue('Error de carga');
    await waitFor(() => expect(urlsDeListado(fetchMock)).toHaveLength(antes + 1));
  });

  it('cierra el diálogo con Escape y devuelve el foco al botón', async () => {
    await abrirActivo();
    const boton = screen.getByRole('button', { name: 'Anular' });
    fireEvent.click(boton);
    await screen.findByRole('dialog', { name: 'Anular préstamo' });
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(boton).toHaveFocus();
  });

  it('«Volver» cierra el diálogo sin enviar nada', async () => {
    const { llamadas } = await abrirActivo();
    fireEvent.click(screen.getByRole('button', { name: 'Anular' }));
    const dialogo = within(await screen.findByRole('dialog', { name: 'Anular préstamo' }));
    fireEvent.click(dialogo.getByRole('button', { name: 'Volver' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toHaveLength(0);
  });
});
