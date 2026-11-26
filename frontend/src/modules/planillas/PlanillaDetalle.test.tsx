import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  limpiarPlanillas,
  montarPlanillasCargadas,
  prepararApiPlanillas,
  seleccionarPlanilla,
  urlsDeDetalles,
  type LlamadaPlanillas,
  moneda,
} from './planillas.testUtils';

afterEach(() => {
  limpiarPlanillas();
  vi.restoreAllMocks();
});

const ACCIONES = ['Calcular', 'Recalcular', 'Aprobar', 'Registrar pago'];

function accionesVisibles(): string[] {
  return ACCIONES.filter((nombre) => screen.queryByRole('button', { name: nombre }) !== null);
}

function escrituras(llamadas: LlamadaPlanillas[]): LlamadaPlanillas[] {
  return llamadas.filter((llamada) => llamada.metodo !== 'GET');
}

describe('detalle de planilla: acciones según el estado', () => {
  it('ofrece solo las acciones que corresponden a cada estado', async () => {
    const { fetchMock } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202611-M', 'Planilla 11/2026 · Mensual');
    expect(accionesVisibles()).toEqual(['Calcular']);
    await seleccionarPlanilla('PLA-202609-M', 'Planilla 9/2026 · Mensual');
    expect(accionesVisibles()).toEqual(['Recalcular', 'Aprobar']);
    await seleccionarPlanilla('PLA-202608-M', 'Planilla 8/2026 · Mensual');
    expect(accionesVisibles()).toEqual(['Registrar pago']);
    await seleccionarPlanilla('PLA-202610-M', 'Planilla 10/2026 · Mensual');
    expect(accionesVisibles()).toEqual([]);
  });

  it('muestra los totales de la cabecera y los aportes del empleador', async () => {
    const { fetchMock } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202610-M', 'Planilla 10/2026 · Mensual');
    const totales = screen.getByLabelText('Totales de la planilla');
    expect(within(totales).getByText('Total ingresos').nextSibling).toHaveTextContent(moneda(11000));
    expect(within(totales).getByText('Total descuentos').nextSibling).toHaveTextContent(moneda(2000));
    expect(within(totales).getByText('Total neto').nextSibling).toHaveTextContent(moneda(9000));
    expect(within(totales).getByText('Aportes del empleador').nextSibling).toHaveTextContent(moneda(900));
  });

  it('calcula sin cuerpo, muestra el resultado y pasa a Calculada con Recalcular', async () => {
    const { fetchMock, llamadas } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202611-M', 'Planilla 11/2026 · Mensual');
    fireEvent.click(screen.getByRole('button', { name: 'Calcular' }));
    expect(await screen.findByText('Cálculo terminado: 2 empleados y 1 incidencias.')).toHaveAttribute('role', 'status');
    await waitFor(() => expect(screen.getByRole('button', { name: 'Recalcular' })).toBeInTheDocument());
    expect(escrituras(llamadas)).toEqual([{ url: '/api/v1/planillas/1/calculo', metodo: 'POST', cuerpo: undefined }]);
  });

  it('muestra el mensaje del servidor si el cálculo falla (planilla aprobada)', async () => {
    const { fetchMock } = prepararApiPlanillas({
      errorEscritura: { accion: 'calcular', status: 409, mensaje: 'La planilla PLA-202609-M está aprobada y no se modifica.' },
    });
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202609-M', 'Planilla 9/2026 · Mensual');
    fireEvent.click(screen.getByRole('button', { name: 'Recalcular' }));
    expect(await screen.findByText('La planilla PLA-202609-M está aprobada y no se modifica.')).toHaveAttribute('role', 'alert');
  });
});

describe('detalle de planilla: aprobar y pagar', () => {
  it('aprueba solo tras confirmar; cancelar no llama a la API', async () => {
    const { fetchMock, llamadas } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202609-M', 'Planilla 9/2026 · Mensual');
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Aprobar planilla' });
    expect(within(dialogo).getByText('La planilla aprobada no se puede modificar.')).toBeInTheDocument();
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar' }));
    fireEvent.click(within(await screen.findByRole('dialog', { name: 'Aprobar planilla' })).getByRole('button', { name: 'Aprobar planilla' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Registrar pago' })).toBeInTheDocument());
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(escrituras(llamadas)).toEqual([{ url: '/api/v1/planillas/3/aprobacion', metodo: 'POST', cuerpo: undefined }]);
  });

  it('muestra el mensaje del servidor dentro del diálogo de aprobación', async () => {
    const { fetchMock } = prepararApiPlanillas({
      errorEscritura: { accion: 'aprobar', status: 409, mensaje: 'La planilla PLA-202609-M debe estar calculada para aprobarse.' },
    });
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202609-M', 'Planilla 9/2026 · Mensual');
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Aprobar planilla' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Aprobar planilla' }));
    expect(await within(dialogo).findByText('La planilla PLA-202609-M debe estar calculada para aprobarse.')).toHaveAttribute('role', 'alert');
  });

  it('exige la fecha de pago (mensaje 7) y registra el pago con la fecha', async () => {
    const { fetchMock, llamadas } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202608-M', 'Planilla 8/2026 · Mensual');
    fireEvent.click(screen.getByRole('button', { name: 'Registrar pago' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Registrar pago' });
    fireEvent.change(within(dialogo).getByLabelText('Fecha de pago'), { target: { value: '' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Registrar pago' }));
    expect(await within(dialogo).findByText('La fecha de pago es obligatoria.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
    fireEvent.change(within(dialogo).getByLabelText('Fecha de pago'), { target: { value: '2026-11-30' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Registrar pago' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toEqual([{ url: '/api/v1/planillas/4/pago', metodo: 'POST', cuerpo: { fechaPago: '2026-11-30' } }]);
    await waitFor(() => expect(accionesVisibles()).toEqual([]));
  });

  it('muestra el mensaje 12 del servidor al pagar antes de la aprobación', async () => {
    const { fetchMock } = prepararApiPlanillas({
      errorEscritura: { accion: 'pagar', status: 400, mensaje: 'La fecha de pago no puede ser anterior a la aprobación.' },
    });
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202608-M', 'Planilla 8/2026 · Mensual');
    fireEvent.click(screen.getByRole('button', { name: 'Registrar pago' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Registrar pago' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Registrar pago' }));
    expect(await within(dialogo).findByText('La fecha de pago no puede ser anterior a la aprobación.')).toHaveAttribute('role', 'alert');
  });
});

describe('detalle de planilla: empleados e incidencias', () => {
  it('lista los empleados con importes y destaca la incidencia con texto', async () => {
    const { fetchMock } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202610-M', 'Planilla 10/2026 · Mensual');
    const tabla = screen.getByRole('table', { name: 'Detalle por empleado' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual([
      'Legajo', 'Empleado', 'Días', 'Ingresos', 'Pensión', 'Quinta', 'Préstamos', 'Neto', 'Incidencia', 'Boleta',
    ]);
    const filas = within(tabla).getAllByRole('row');
    expect(filas).toHaveLength(4);
    expect(filas[1]).toHaveTextContent('EMP-000001');
    expect(filas[1]).toHaveTextContent(moneda(4250));
    expect(filas[3]).toHaveTextContent('Sin afiliación a un sistema de pensiones.');
    expect(within(tabla).getAllByRole('button', { name: /^Ver boleta de/ })).toHaveLength(2);
    expect(within(tabla).queryByRole('button', { name: 'Ver boleta de Maria Salazar Quispe' })).not.toBeInTheDocument();
  });

  it('filtra con «Solo incidencias» y busca con 300 ms de espera', async () => {
    const { fetchMock } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202610-M', 'Planilla 10/2026 · Mensual');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Solo incidencias' }));
    await waitFor(() => expect(urlsDeDetalles(fetchMock).at(-1)).toBe('/api/v1/planillas/2/detalles?soloIncidencias=true&pagina=1&tamanoPagina=10'));
    await waitFor(() => expect(screen.queryByText('Ana Torres Vega')).not.toBeInTheDocument());
    expect(screen.getByText('Maria Salazar Quispe')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Solo incidencias' }));
    await waitFor(() => expect(screen.getByText('Ana Torres Vega')).toBeInTheDocument());
    const antes = urlsDeDetalles(fetchMock).length;
    vi.useFakeTimers();
    const buscador = screen.getByLabelText('Buscar por legajo o empleado');
    fireEvent.change(buscador, { target: { value: 'Luis' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsDeDetalles(fetchMock)).toHaveLength(antes);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsDeDetalles(fetchMock).at(-1)).toBe('/api/v1/planillas/2/detalles?busqueda=Luis&pagina=1&tamanoPagina=10'));
  });

  it('muestra el error del detalle por empleado', async () => {
    const { fetchMock } = prepararApiPlanillas({ errorDetalles: true });
    await montarPlanillasCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'PLA-202610-M' }));
    expect(await screen.findByText('No se pudo cargar el detalle por empleado.')).toHaveAttribute('role', 'alert');
  });
});

describe('boleta de pago', () => {
  async function abrirBoleta(): Promise<HTMLElement> {
    const { fetchMock } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202610-M', 'Planilla 10/2026 · Mensual');
    fireEvent.click(screen.getByRole('button', { name: 'Ver boleta de Ana Torres Vega' }));
    return screen.findByRole('dialog', { name: 'Boleta de pago' });
  }

  it('muestra la boleta, activa la impresión solo mientras está abierta e imprime', async () => {
    const imprimir = vi.spyOn(window, 'print').mockImplementation(() => undefined);
    const dialogo = await abrirBoleta();
    const boleta = await within(dialogo).findByRole('article', { name: 'Boleta de pago BOL-202610-M-000001' });
    expect(document.body).toHaveClass('planillas-imprimiendo');
    expect(within(boleta).getByText('TitanGlobal')).toBeInTheDocument();
    expect(within(boleta).getByText('EMP-000001 · Ana Torres Vega')).toBeInTheDocument();
    expect(within(boleta).getByText('Gerente de Ventas')).toBeInTheDocument();
    expect(within(boleta).getByText('Horas extra (90 min)')).toBeInTheDocument();
    expect(within(boleta).getByText(moneda(4615.83))).toBeInTheDocument();
    expect(within(boleta).getByText(moneda(487.85))).toBeInTheDocument();
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Imprimir' }));
    expect(imprimir).toHaveBeenCalledTimes(1);
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Cerrar' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(document.body).not.toHaveClass('planillas-imprimiendo');
  });

  it('Escape cierra la boleta y retira la clase de impresión', async () => {
    await abrirBoleta();
    expect(document.body).toHaveClass('planillas-imprimiendo');
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(document.body).not.toHaveClass('planillas-imprimiendo');
  });

  it('muestra el error del servidor y no permite imprimir', async () => {
    const { fetchMock } = prepararApiPlanillas({ errorBoleta: true });
    await montarPlanillasCargadas(fetchMock);
    await seleccionarPlanilla('PLA-202610-M', 'Planilla 10/2026 · Mensual');
    fireEvent.click(screen.getByRole('button', { name: 'Ver boleta de Ana Torres Vega' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Boleta de pago' });
    expect(await within(dialogo).findByText('La boleta se genera al aprobar la planilla.')).toHaveAttribute('role', 'alert');
    expect(within(dialogo).getByRole('button', { name: 'Imprimir' })).toBeDisabled();
  });
});
