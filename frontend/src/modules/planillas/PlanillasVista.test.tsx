import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import {
  esperarCargaInicialPlanillas,
  limpiarPlanillas,
  montarPlanillasCargadas,
  prepararApiPlanillas,
  renderPlanillas,
  seleccionarPlanilla,
  urlsDeListadoPlanillas,
  moneda,
} from './planillas.testUtils';

afterEach(() => {
  limpiarPlanillas();
});

describe('vista de Planillas: resumen y listado', () => {
  it('muestra el resumen con el periodo como mes/año y las cifras', async () => {
    const { fetchMock } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    const resumen = await screen.findByRole('region', { name: 'Resumen de planillas' });
    expect(within(resumen).getByText('Último periodo').nextSibling).toHaveTextContent('11/2026');
    expect(within(resumen).getByText('Estado').nextSibling).toHaveTextContent('Borrador');
    expect(within(resumen).getByText('Neto del último periodo').nextSibling).toHaveTextContent(moneda(9000));
    expect(within(resumen).getByText('Empleados').nextSibling).toHaveTextContent('2');
    expect(within(resumen).getByText('Incidencias').nextSibling).toHaveTextContent('1');
    expect(within(resumen).getByText('Pendientes de aprobar').nextSibling).toHaveTextContent('2');
  });

  it('lista número, periodo, tipo, estado con texto, empleados y neto', async () => {
    const { fetchMock } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    expect(urlsDeListadoPlanillas(fetchMock)).toEqual(['/api/v1/planillas?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table', { name: 'Listado de planillas' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual([
      'Número', 'Periodo', 'Tipo', 'Estado', 'Empleados', 'Neto',
    ]);
    const filas = within(tabla).getAllByRole('row');
    expect(filas).toHaveLength(5);
    expect(filas[1]).toHaveTextContent('PLA-202611-M');
    expect(filas[1]).toHaveTextContent('11/2026');
    expect(filas[1]).toHaveTextContent('Mensual');
    expect(filas[1]).toHaveTextContent('Borrador');
    expect(filas[2]).toHaveTextContent('10/2026');
    expect(filas[2]).toHaveTextContent('Pagada');
    expect(filas[2]).toHaveTextContent(moneda(9000));
  });

  it('envía año, tipo y estado a la query, vuelve a la página 1 e ignora un año incompleto', async () => {
    const { fetchMock } = prepararApiPlanillas({ totalListado: 25 });
    await montarPlanillasCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsDeListadoPlanillas(fetchMock).at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Año'), { target: { value: '20' } });
    const antes = urlsDeListadoPlanillas(fetchMock).length;
    fireEvent.change(screen.getByLabelText('Año'), { target: { value: '2026' } });
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Complementaria' } });
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Aprobada' } });
    await waitFor(() => expect(urlsDeListadoPlanillas(fetchMock).at(-1)).toBe(
      '/api/v1/planillas?anio=2026&tipo=Complementaria&estado=Aprobada&pagina=1&tamanoPagina=10',
    ));
    expect(urlsDeListadoPlanillas(fetchMock).slice(antes).some((url) => url.includes('anio=20&'))).toBe(false);
  });

  it('muestra los estados vacío y de error', async () => {
    prepararApiPlanillas({ listaVacia: true, errorResumen: true });
    renderPlanillas();
    expect(await screen.findByText('No hay planillas con esos filtros.')).toHaveAttribute('role', 'status');
    expect(await screen.findByText('No se pudo cargar el resumen de planillas.')).toHaveAttribute('role', 'alert');
    limpiarPlanillas();
    prepararApiPlanillas({ errorLista: true });
    renderPlanillas();
    expect(await screen.findByText('No se pudo cargar la lista de planillas.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Planillas: nueva planilla', () => {
  async function abrirDialogo(): Promise<HTMLElement> {
    fireEvent.click(screen.getByRole('button', { name: 'Nueva planilla' }));
    return screen.findByRole('dialog', { name: 'Nueva planilla' });
  }

  it('valida año, mes, tipo y periodo futuro sin llamar a la API', async () => {
    const { fetchMock, llamadas } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    const dialogo = await abrirDialogo();
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Crear planilla' }));
    expect(await within(dialogo).findByText('El mes es obligatorio y debe estar entre 1 y 12.')).toBeInTheDocument();
    fireEvent.change(within(dialogo).getByLabelText('Año'), { target: { value: '1999' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Crear planilla' }));
    expect(await within(dialogo).findByText('El año es obligatorio y debe estar entre 2000 y 2100.')).toBeInTheDocument();
    fireEvent.change(within(dialogo).getByLabelText('Año'), { target: { value: '2100' } });
    fireEvent.change(within(dialogo).getByLabelText('Mes'), { target: { value: '12' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Crear planilla' }));
    expect(await within(dialogo).findByText('No se puede crear una planilla de un periodo futuro.')).toBeInTheDocument();
    expect(llamadas.filter((llamada) => llamada.metodo === 'POST')).toHaveLength(0);
  });

  it('crea la planilla con el cuerpo exacto, cierra el diálogo y la selecciona', async () => {
    const { fetchMock, llamadas } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    const dialogo = await abrirDialogo();
    fireEvent.change(within(dialogo).getByLabelText('Año'), { target: { value: '2020' } });
    fireEvent.change(within(dialogo).getByLabelText('Mes'), { target: { value: '1' } });
    fireEvent.change(within(dialogo).getByLabelText('Tipo'), { target: { value: 'Complementaria' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Crear planilla' }));
    expect(await screen.findByRole('heading', { name: 'Planilla 1/2020 · Complementaria' })).toBeInTheDocument();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(llamadas.find((llamada) => llamada.metodo === 'POST')).toMatchObject({
      url: '/api/v1/planillas',
      cuerpo: { anio: 2020, mes: 1, tipo: 'Complementaria' },
    });
  });

  it('muestra el mensaje del servidor dentro del diálogo y conserva lo escrito', async () => {
    const { fetchMock } = prepararApiPlanillas({
      errorEscritura: { accion: 'crear', status: 409, mensaje: 'Ya existe la planilla Mensual del periodo 1/2020.' },
    });
    await montarPlanillasCargadas(fetchMock);
    const dialogo = await abrirDialogo();
    fireEvent.change(within(dialogo).getByLabelText('Año'), { target: { value: '2020' } });
    fireEvent.change(within(dialogo).getByLabelText('Mes'), { target: { value: '1' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Crear planilla' }));
    expect(await within(dialogo).findByText('Ya existe la planilla Mensual del periodo 1/2020.')).toHaveAttribute('role', 'alert');
    expect(within(dialogo).getByLabelText('Año')).toHaveValue('2020');
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });
});

describe('vista de Planillas: parámetros y roles', () => {
  it('abre y cierra el panel de parámetros de solo lectura', async () => {
    const { fetchMock } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Parámetros' }));
    const panel = await screen.findByRole('region', { name: 'Tabla de parámetros generales' });
    expect(within(panel).getByText('UIT')).toBeInTheDocument();
    const pensiones = screen.getByRole('region', { name: 'Tabla de sistemas de pensiones' });
    expect(within(pensiones).getByText('INTEGRA')).toBeInTheDocument();
    expect(within(pensiones).getByText('—')).toBeInTheDocument();
    const tramos = screen.getByRole('region', { name: 'Tabla de tramos de quinta categoría' });
    expect(within(tramos).getByText('En adelante')).toBeInTheDocument();
    expect(screen.queryByRole('textbox', { name: 'UIT' })).not.toBeInTheDocument();
    fireEvent.click(within(screen.getByRole('region', { name: 'Parámetros de planilla' })).getByRole('button', { name: 'Cerrar' }));
    expect(screen.queryByRole('region', { name: 'Tabla de parámetros generales' })).not.toBeInTheDocument();
  });

  it('sin RRHH ni ADMIN las escrituras quedan deshabilitadas con el aviso', async () => {
    const { fetchMock } = prepararApiPlanillas();
    renderPlanillas([]);
    await esperarCargaInicialPlanillas(fetchMock);
    expect(screen.getByRole('button', { name: 'Nueva planilla' })).toBeDisabled();
    expect(screen.getByText('Solo RRHH o ADMIN pueden modificar planillas.')).toBeInTheDocument();
    await seleccionarPlanilla('PLA-202609-M', 'Planilla 9/2026 · Mensual');
    expect(screen.getByRole('button', { name: 'Recalcular' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Aprobar' })).toBeDisabled();
    expect(screen.getAllByText('Solo RRHH o ADMIN pueden modificar planillas.')).toHaveLength(2);
  });

  it('ADMIN también puede escribir', async () => {
    const { fetchMock } = prepararApiPlanillas();
    await montarPlanillasCargadas(fetchMock, ['ADMIN']);
    expect(screen.getByRole('button', { name: 'Nueva planilla' })).toBeEnabled();
  });
});
