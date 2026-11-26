import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  limpiarContratos,
  montarContratosCargados,
  montarSinFilas,
  prepararApiContratos,
  urlsDeConteo,
  urlsDeListado,
} from './contratosLaborales.testUtils';

afterEach(() => {
  limpiarContratos();
});

async function seleccionarContrato(numero: string, nombre: string): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: numero }));
  await screen.findByRole('heading', { name: nombre });
}

const ETIQUETA_BUSQUEDA = 'Buscar por número, cargo o empleado';
const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar contratos.';

describe('pantalla de Contratos laborales: listado, filtros y resumen', () => {
  it('carga una sola vez el listado en página 1 y muestra número, estado y fin sin fecha', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    expect(screen.getByRole('heading', { name: '62 · Contratos laborales' })).toBeInTheDocument();
    expect(urlsDeListado(fetchMock)).toEqual(['/api/v1/contratos-laborales?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table');
    expect(within(tabla).getByRole('button', { name: 'CTR-000007' })).toBeInTheDocument();
    expect(within(tabla).getByText('EMP-000005 · Rosa Vega Paz')).toBeInTheDocument();
    expect(within(tabla).getAllByText('Sin fecha de fin')).toHaveLength(2);
  });

  it('muestra cada estado con texto en pastillas', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    const tabla = screen.getByRole('table');
    expect(within(tabla).getAllByText('Vigente')).toHaveLength(3);
    expect(within(tabla).getAllByText('Renovado')).toHaveLength(2);
    expect(within(tabla).getByText('Vencido')).toBeInTheDocument();
    expect(within(tabla).getByText('Cerrado')).toBeInTheDocument();
    expect(within(tabla).getByText('Anulado')).toBeInTheDocument();
  });

  it('calcula el resumen con tres conteos de una fila: vigentes, vencidos y total', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    const urls = urlsDeConteo(fetchMock);
    expect(urls).toContain('/api/v1/contratos-laborales?estado=Vigente&pagina=1&tamanoPagina=1');
    expect(urls).toContain('/api/v1/contratos-laborales?estado=Vencido&pagina=1&tamanoPagina=1');
    expect(urls).toContain('/api/v1/contratos-laborales?pagina=1&tamanoPagina=1');
    const resumen = screen.getByRole('region', { name: 'Resumen de contratos' });
    expect(within(resumen).getByText('Vigentes').nextElementSibling).toHaveTextContent('3');
    expect(within(resumen).getByText('Vencidos').nextElementSibling).toHaveTextContent('1');
    expect(within(resumen).getByText('Total').nextElementSibling).toHaveTextContent('8');
  });

  it('envía estado y modalidad a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Vencido' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('estado=Vencido'));
    fireEvent.change(screen.getByLabelText('Modalidad'), { target: { value: 'Formativo' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('modalidad=Formativo'));
    expect(urlsDeListado(fetchMock).at(-1)).toBe('/api/v1/contratos-laborales?estado=Vencido&modalidad=Formativo&pagina=1&tamanoPagina=10');
  });

  it('vuelve a la página 1 al filtrar estando en la página 2', async () => {
    const { fetchMock } = prepararApiContratos({ totalListado: 25 });
    await montarContratosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Modalidad'), { target: { value: 'PlazoFijo' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toBe('/api/v1/contratos-laborales?modalidad=PlazoFijo&pagina=1&tamanoPagina=10'));
  });

  it('aplica la búsqueda 300 ms después del último cambio con una sola consulta del último texto', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    const antes = urlsDeListado(fetchMock).length;
    vi.useFakeTimers();
    const buscador = screen.getByLabelText(ETIQUETA_BUSQUEDA);
    fireEvent.change(buscador, { target: { value: 'C' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(200);
    });
    fireEvent.change(buscador, { target: { value: 'CT' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(200);
    });
    fireEvent.change(buscador, { target: { value: 'CTR-000005' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsDeListado(fetchMock)).toHaveLength(antes);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsDeListado(fetchMock)).toHaveLength(antes + 1));
    expect(urlsDeListado(fetchMock).at(-1)).toBe('/api/v1/contratos-laborales?busqueda=CTR-000005&pagina=1&tamanoPagina=10');
  });

  it('muestra el estado vacío cuando no hay contratos', async () => {
    prepararApiContratos({ listaVacia: true });
    await montarSinFilas();
    expect(await screen.findByText('No hay contratos con esos filtros.')).toHaveAttribute('role', 'status');
  });

  it('muestra el error de carga del listado', async () => {
    prepararApiContratos({ errorLista: true });
    await montarSinFilas();
    expect(await screen.findByText('No se pudo cargar la lista de contratos.')).toHaveAttribute('role', 'alert');
  });

  it('muestra el error del resumen sin ocultar el listado', async () => {
    prepararApiContratos({ errorResumen: true });
    await montarSinFilas();
    expect(await screen.findByText('No se pudo cargar el resumen de contratos.')).toHaveAttribute('role', 'alert');
    expect(await screen.findByRole('button', { name: 'CTR-000001' })).toBeInTheDocument();
  });
});

describe('pantalla de Contratos laborales: detalle', () => {
  it('muestra cargo y sueldo vigentes distintos de los originales y la adenda', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    await seleccionarContrato('CTR-000001', 'Ana Torres Vega');
    const datos = screen.getByRole('region', { name: 'Datos del contrato' });
    expect(within(datos).getByText('Gerente de Ventas')).toBeInTheDocument();
    expect(within(datos).getByText('Cargo vigente hoy').nextElementSibling).toHaveTextContent('Gerente Comercial');
    expect(within(datos).getByText('Sueldo básico vigente hoy').nextElementSibling).toHaveTextContent(/9,500\.00/);
    const adendas = screen.getByRole('table', { name: 'Adendas del contrato' });
    expect(within(adendas).getByText('Gerente Comercial')).toBeInTheDocument();
  });

  it('muestra «Sin adendas» y no repite cargo y sueldo cuando no difieren', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    await seleccionarContrato('CTR-000008', 'Rosa Vega Paz');
    expect(screen.getByText('Sin adendas')).toBeInTheDocument();
    expect(screen.queryByText('Cargo vigente hoy')).not.toBeInTheDocument();
  });

  it('muestra el contrato anterior de una renovación', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    await seleccionarContrato('CTR-000003', 'Luis Rojas Mendoza');
    expect(screen.getByText('Contrato anterior').nextElementSibling).toHaveTextContent('CTR-000002');
  });

  it('muestra fecha y motivo de cierre de un contrato anulado', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    await seleccionarContrato('CTR-000007', 'Ana Torres Vega');
    const cierre = screen.getByRole('region', { name: 'Cierre' });
    expect(within(cierre).getByText('Registro duplicado por error')).toBeInTheDocument();
  });

  it('muestra el error de detalle sin ocultar el listado', async () => {
    const { fetchMock } = prepararApiContratos({ errorDetalle: true });
    await montarContratosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'CTR-000001' }));
    expect(await screen.findByText('No se pudo cargar el detalle del contrato.')).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('button', { name: 'CTR-000002' })).toBeInTheDocument();
  });
});

describe('pantalla de Contratos laborales: botones según estado, modalidad y rol', () => {
  it('habilita adenda y anulación en un indeterminado vigente y deshabilita Renovar con su texto', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    await seleccionarContrato('CTR-000001', 'Ana Torres Vega');
    expect(screen.getByRole('button', { name: 'Renovar' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Renovar' })).toHaveAccessibleDescription('Un contrato indeterminado no se renueva.');
    expect(screen.getByRole('button', { name: 'Agregar adenda' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Anular' })).toBeEnabled();
  });

  it('habilita las tres acciones en un contrato de plazo fijo vigente', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    await seleccionarContrato('CTR-000008', 'Rosa Vega Paz');
    expect(screen.getByRole('button', { name: 'Renovar' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Agregar adenda' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Anular' })).toBeEnabled();
  });

  it('en un vencido solo permite Renovar (renovación tardía)', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    await seleccionarContrato('CTR-000005', 'Maria Salazar Quispe');
    expect(screen.getByRole('button', { name: 'Renovar' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Agregar adenda' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Anular' })).toBeDisabled();
  });

  it('deshabilita todas las acciones en un contrato anulado', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock);
    await seleccionarContrato('CTR-000007', 'Ana Torres Vega');
    expect(screen.getByRole('button', { name: 'Renovar' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Agregar adenda' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Anular' })).toBeDisabled();
  });

  it('con el rol CONSULTA deshabilita los cuatro botones de escritura y muestra el texto', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock, ['CONSULTA']);
    await seleccionarContrato('CTR-000008', 'Rosa Vega Paz');
    expect(screen.getByRole('button', { name: 'Nuevo contrato' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Renovar' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Agregar adenda' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Anular' })).toBeDisabled();
    expect(screen.getAllByText(TEXTO_SOLO_RRHH).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByRole('button', { name: 'Nuevo contrato' })).toHaveAccessibleDescription(TEXTO_SOLO_RRHH);
    expect(screen.getByRole('button', { name: 'Anular' })).toHaveAccessibleDescription(TEXTO_SOLO_RRHH);
  });

  it.each(['RRHH', 'ADMIN'])('con el rol %s habilita los botones de escritura', async (rol) => {
    const { fetchMock } = prepararApiContratos();
    await montarContratosCargados(fetchMock, [rol]);
    await seleccionarContrato('CTR-000008', 'Rosa Vega Paz');
    expect(screen.getByRole('button', { name: 'Nuevo contrato' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Renovar' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Agregar adenda' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Anular' })).toBeEnabled();
    expect(screen.queryByText(TEXTO_SOLO_RRHH)).not.toBeInTheDocument();
  });
});
