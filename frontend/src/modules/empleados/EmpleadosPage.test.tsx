import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { formatearFecha } from '../../lib/formato';
import {
  esperarCargaInicial,
  limpiarEmpleados,
  montarEmpleadosCargados,
  prepararApiEmpleados,
  renderEmpleados,
  urlsDeConteo,
  urlsDeListado,
} from './empleados.testUtils';

afterEach(() => {
  limpiarEmpleados();
});

async function seleccionarEmpleado(legajo: string, nombre: string): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: legajo }));
  await screen.findByRole('heading', { name: nombre });
}

// Para listas vacías o con error no hay filas que esperar: solo se espera el fin de la carga.
async function montarSinFilas(): Promise<void> {
  vi.useFakeTimers();
  renderEmpleados();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(300);
  });
  vi.useRealTimers();
}

describe('pantalla de Empleados: listado y detalle', () => {
  it('carga una sola vez el listado en página 1 y muestra legajo, estado y cargo', async () => {
    const { fetchMock } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    expect(screen.getByRole('heading', { name: '61 · Empleados' })).toBeInTheDocument();
    expect(urlsDeListado(fetchMock)).toEqual(['/api/v1/empleados?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table');
    expect(within(tabla).getByRole('button', { name: 'EMP-000004' })).toBeInTheDocument();
    expect(within(tabla).getByText('Gerente de Ventas')).toBeInTheDocument();
    expect(within(tabla).getByText('Cesado')).toBeInTheDocument();
  });

  it('envía el estado a la query al filtrar y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Cesado' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('estado=Cesado'));
    expect(urlsDeListado(fetchMock).at(-1)).toContain('pagina=1');
  });

  it('muestra el estado vacío cuando no hay empleados', async () => {
    prepararApiEmpleados({ listaVacia: true });
    await montarSinFilas();
    expect(await screen.findByText('No hay empleados con esos filtros.')).toHaveAttribute('role', 'status');
  });

  it('muestra el error de carga del listado', async () => {
    prepararApiEmpleados({ errorLista: true });
    await montarSinFilas();
    expect(await screen.findByText('No se pudo cargar la lista de empleados.')).toHaveAttribute('role', 'alert');
  });

  it('muestra la ficha con datos personales, laborales y de cese al seleccionar un empleado', async () => {
    const { fetchMock } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    await seleccionarEmpleado('EMP-000004', 'Carlos Diaz Soto');
    const cese = screen.getByRole('region', { name: 'Cese' });
    expect(within(cese).getByText('Renuncia voluntaria')).toBeInTheDocument();
  });

  it('muestra el error de detalle sin ocultar el listado', async () => {
    const { fetchMock } = prepararApiEmpleados({ errorDetalle: true });
    await montarEmpleadosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'EMP-000001' }));
    expect(await screen.findByText('No se pudo cargar el detalle del empleado.')).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('button', { name: 'EMP-000002' })).toBeInTheDocument();
  });
});

const ETIQUETA_BUSQUEDA = 'Buscar por legajo, nombre o documento';

// Con total 25 hay 3 páginas; el mock no tiene filas en la página 2, pero lo que se prueba es la query.
async function irAPagina2(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
  await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('pagina=2'));
}

describe('pantalla de Empleados: búsqueda, paginación y resumen', () => {
  it('aplica la búsqueda 300 ms después del último cambio con una sola consulta del último texto', async () => {
    const { fetchMock } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    const antes = urlsDeListado(fetchMock).length;
    vi.useFakeTimers();
    const buscador = screen.getByLabelText(ETIQUETA_BUSQUEDA);
    fireEvent.change(buscador, { target: { value: 'A' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(200);
    });
    fireEvent.change(buscador, { target: { value: 'An' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(200);
    });
    fireEvent.change(buscador, { target: { value: 'Ana' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsDeListado(fetchMock)).toHaveLength(antes);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsDeListado(fetchMock)).toHaveLength(antes + 1));
    expect(urlsDeListado(fetchMock).at(-1)).toBe('/api/v1/empleados?busqueda=Ana&pagina=1&tamanoPagina=10');
  });

  it('vuelve a la página 1 al cambiar el estado estando en la página 2', async () => {
    const { fetchMock } = prepararApiEmpleados({ totalListado: 25 });
    await montarEmpleadosCargados(fetchMock);
    await irAPagina2(fetchMock);
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Activo' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('estado=Activo'));
    expect(urlsDeListado(fetchMock).at(-1)).toContain('pagina=1');
  });

  it('vuelve a la página 1 al cambiar la búsqueda estando en la página 2', async () => {
    const { fetchMock } = prepararApiEmpleados({ totalListado: 25 });
    await montarEmpleadosCargados(fetchMock);
    await irAPagina2(fetchMock);
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText(ETIQUETA_BUSQUEDA), { target: { value: 'Luis' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('busqueda=Luis'));
    expect(urlsDeListado(fetchMock).at(-1)).toContain('pagina=1');
  });

  it('muestra Activos, Cesados y Total con una consulta de conteo por estado', async () => {
    const { fetchMock } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    expect(urlsDeConteo(fetchMock).sort()).toEqual([
      '/api/v1/empleados?estado=Activo&pagina=1&tamanoPagina=1',
      '/api/v1/empleados?estado=Cesado&pagina=1&tamanoPagina=1',
    ]);
    const resumen = screen.getByRole('region', { name: 'Resumen de empleados' });
    expect(within(resumen).getByText('Activos').nextElementSibling).toHaveTextContent('3');
    expect(within(resumen).getByText('Cesados').nextElementSibling).toHaveTextContent('1');
    expect(within(resumen).getByText('Total').nextElementSibling).toHaveTextContent('4');
  });

  it('muestra el error del resumen sin romper el resto de la pantalla', async () => {
    const { fetchMock } = prepararApiEmpleados({ errorResumen: true });
    await montarEmpleadosCargados(fetchMock);
    expect(screen.getByText('No se pudo cargar el resumen de empleados.')).toHaveAttribute('role', 'alert');
    expect(screen.queryByRole('region', { name: 'Resumen de empleados' })).not.toBeInTheDocument();
    await seleccionarEmpleado('EMP-000001', 'Ana Torres Vega');
  });

  it('muestra el estado como texto en la tabla y en la ficha', async () => {
    const { fetchMock } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    const tabla = screen.getByRole('table');
    expect(within(tabla).getAllByText('Activo')).toHaveLength(3);
    expect(within(tabla).getAllByText('Cesado')).toHaveLength(1);
    await seleccionarEmpleado('EMP-000004', 'Carlos Diaz Soto');
    const laborales = screen.getByRole('region', { name: 'Datos laborales' });
    expect(within(laborales).getByText('Cesado')).toBeInTheDocument();
  });
});

describe('pantalla de Empleados: detalle según estado', () => {
  it('muestra fecha y motivo de cese y "Sin dato" en el contacto de un empleado cesado', async () => {
    const { fetchMock } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    await seleccionarEmpleado('EMP-000004', 'Carlos Diaz Soto');
    const cese = screen.getByRole('region', { name: 'Cese' });
    expect(within(cese).getByText(formatearFecha('2026-09-30'))).toBeInTheDocument();
    expect(within(cese).getByText('Renuncia voluntaria')).toBeInTheDocument();
    const contacto = screen.getByRole('region', { name: 'Contacto' });
    expect(within(contacto).getAllByText('Sin dato')).toHaveLength(2);
  });

  it('no muestra la región de cese en un empleado activo', async () => {
    const { fetchMock } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    await seleccionarEmpleado('EMP-000001', 'Ana Torres Vega');
    expect(screen.queryByRole('region', { name: 'Cese' })).not.toBeInTheDocument();
    expect(screen.queryByText('Motivo de cese')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Editar' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Registrar cese' })).toBeEnabled();
  });

  it('deshabilita Editar y Registrar cese en un empleado cesado con el aviso asociado', async () => {
    const { fetchMock } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    await seleccionarEmpleado('EMP-000004', 'Carlos Diaz Soto');
    const aviso = screen.getByText('Empleado cesado: no admite cambios.');
    expect(aviso).toBeVisible();
    for (const nombre of ['Editar', 'Registrar cese']) {
      const boton = screen.getByRole('button', { name: nombre });
      expect(boton).toBeDisabled();
      expect(boton).toHaveAttribute('aria-describedby', aviso.id);
      expect(boton).toHaveAccessibleDescription('Empleado cesado: no admite cambios.');
    }
  });
});

describe('pantalla de Empleados: formulario', () => {
  it('valida en local sin petición de guardado', async () => {
    const { fetchMock, llamadas } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo empleado' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar empleado' }));
    expect(await screen.findByText('Los nombres son obligatorios.')).toBeInTheDocument();
    expect(llamadas.filter((llamada) => llamada.metodo === 'POST')).toHaveLength(0);
  });

  it('muestra el 409 de documento duplicado conservando lo escrito', async () => {
    const { fetchMock } = prepararApiEmpleados({ errorGuardar: 'duplicado' });
    await montarEmpleadosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo empleado' }));
    fireEvent.change(screen.getByLabelText('Número de documento'), { target: { value: '10000001' } });
    fireEvent.change(screen.getByLabelText('Nombres'), { target: { value: 'Ana' } });
    fireEvent.change(screen.getByLabelText('Apellidos'), { target: { value: 'Torres Vega' } });
    fireEvent.change(screen.getByLabelText('Fecha de nacimiento'), { target: { value: '1990-05-14' } });
    fireEvent.change(screen.getByLabelText('Fecha de ingreso'), { target: { value: '2024-02-01' } });
    fireEvent.change(screen.getByLabelText('Cargo'), { target: { value: 'Gerente' } });
    fireEvent.change(screen.getByLabelText('Área'), { target: { value: 'Ventas' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar empleado' }));
    expect(await screen.findByText('Ya existe un empleado con el documento DNI 10000001.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombres')).toHaveValue('Ana');
  });

  it('edita con PUT y el cuerpo con el cargo nuevo', async () => {
    const { fetchMock, llamadas } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    await seleccionarEmpleado('EMP-000001', 'Ana Torres Vega');
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    fireEvent.change(screen.getByLabelText('Cargo'), { target: { value: 'Jefa de Ventas' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar empleado' }));
    await waitFor(() => expect(llamadas.some((llamada) => llamada.metodo === 'PUT')).toBe(true));
    const put = llamadas.find((llamada) => llamada.metodo === 'PUT');
    expect(put?.url).toBe('/api/v1/empleados/1');
    expect(put?.cuerpo).toMatchObject({ cargo: 'Jefa de Ventas' });
  });
});

describe('pantalla de Empleados: cese', () => {
  it('valida fecha y motivo en local, con la fecha anterior al ingreso como error', async () => {
    const { fetchMock, llamadas } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    await seleccionarEmpleado('EMP-000001', 'Ana Torres Vega');
    fireEvent.click(screen.getByRole('button', { name: 'Registrar cese' }));
    const dialogo = screen.getByRole('dialog', { name: 'Registrar cese' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Confirmar cese' }));
    expect(await screen.findByText('La fecha de cese es obligatoria.')).toBeInTheDocument();
    expect(screen.getByText('El motivo de cese es obligatorio.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Fecha de cese'), { target: { value: '2020-01-01' } });
    fireEvent.change(screen.getByLabelText('Motivo de cese'), { target: { value: 'Renuncia' } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Confirmar cese' }));
    expect(await screen.findByText('La fecha de cese no puede ser anterior a la fecha de ingreso.')).toBeInTheDocument();
    expect(llamadas.filter((llamada) => llamada.url.endsWith('/cese'))).toHaveLength(0);
  });

  it('registra el cese con POST y el cuerpo exacto', async () => {
    const { fetchMock, llamadas } = prepararApiEmpleados();
    await montarEmpleadosCargados(fetchMock);
    await seleccionarEmpleado('EMP-000001', 'Ana Torres Vega');
    fireEvent.click(screen.getByRole('button', { name: 'Registrar cese' }));
    fireEvent.change(screen.getByLabelText('Fecha de cese'), { target: { value: '2026-10-31' } });
    fireEvent.change(screen.getByLabelText('Motivo de cese'), { target: { value: 'Renuncia voluntaria' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cese' }));
    await waitFor(() => expect(llamadas.some((llamada) => llamada.url.endsWith('/cese'))).toBe(true));
    const cese = llamadas.find((llamada) => llamada.url.endsWith('/cese'));
    expect(cese?.metodo).toBe('POST');
    expect(cese?.cuerpo).toEqual({ fechaCese: '2026-10-31', motivo: 'Renuncia voluntaria' });
  });

  it('muestra el 409 de empleado ya cesado en el diálogo', async () => {
    const { fetchMock } = prepararApiEmpleados({ errorCese: 'yaCesado' });
    await montarEmpleadosCargados(fetchMock);
    await seleccionarEmpleado('EMP-000001', 'Ana Torres Vega');
    fireEvent.click(screen.getByRole('button', { name: 'Registrar cese' }));
    fireEvent.change(screen.getByLabelText('Fecha de cese'), { target: { value: '2026-10-31' } });
    fireEvent.change(screen.getByLabelText('Motivo de cese'), { target: { value: 'Renuncia' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cese' }));
    expect(await screen.findByText('El empleado 1 ya está cesado.')).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('dialog', { name: 'Registrar cese' })).toBeInTheDocument();
  });
});

describe('pantalla de Empleados: diálogo de cese', () => {
  async function abrirCese() {
    const api = prepararApiEmpleados();
    await montarEmpleadosCargados(api.fetchMock);
    await seleccionarEmpleado('EMP-000001', 'Ana Torres Vega');
    fireEvent.click(screen.getByRole('button', { name: 'Registrar cese' }));
    return api;
  }

  it('enfoca la fecha de cese al abrir y muestra el contador del motivo', async () => {
    await abrirCese();
    expect(screen.getByLabelText('Fecha de cese')).toHaveFocus();
    expect(screen.getByText('0/300')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Motivo de cese'), { target: { value: 'Renuncia' } });
    expect(screen.getByText('8/300')).toBeInTheDocument();
    expect(screen.getByLabelText('Motivo de cese')).toHaveAccessibleDescription('8/300');
  });

  it('cierra con Escape y devuelve el foco a Registrar cese', async () => {
    await abrirCese();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog', { name: 'Registrar cese' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Registrar cese' })).toHaveFocus();
  });

  it('muestra dentro del diálogo el 400 de fecha del servidor y no lo cierra', async () => {
    const { fetchMock } = prepararApiEmpleados({ errorCese: 'fecha' });
    await montarEmpleadosCargados(fetchMock);
    await seleccionarEmpleado('EMP-000001', 'Ana Torres Vega');
    fireEvent.click(screen.getByRole('button', { name: 'Registrar cese' }));
    fireEvent.change(screen.getByLabelText('Fecha de cese'), { target: { value: '2026-10-31' } });
    fireEvent.change(screen.getByLabelText('Motivo de cese'), { target: { value: 'Renuncia' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cese' }));
    const dialogo = screen.getByRole('dialog', { name: 'Registrar cese' });
    const alerta = await within(dialogo).findByText('La fecha de cese no puede ser anterior a la fecha de ingreso.');
    expect(alerta).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('dialog', { name: 'Registrar cese' })).toBeInTheDocument();
    expect(screen.getByLabelText('Motivo de cese')).toHaveValue('Renuncia');
  });

  it('cierra el diálogo tras un cese correcto y recarga listado, resumen y detalle', async () => {
    const { fetchMock, llamadas } = await abrirCese();
    const listadosAntes = urlsDeListado(fetchMock).length;
    const conteosAntes = urlsDeConteo(fetchMock).length;
    const detallesAntes = llamadas.filter((llamada) => llamada.url === '/api/v1/empleados/1').length;
    fireEvent.change(screen.getByLabelText('Fecha de cese'), { target: { value: '2026-10-31' } });
    fireEvent.change(screen.getByLabelText('Motivo de cese'), { target: { value: 'Renuncia voluntaria' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cese' }));
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Registrar cese' })).not.toBeInTheDocument());
    await waitFor(() => {
      expect(urlsDeListado(fetchMock)).toHaveLength(listadosAntes + 1);
      expect(urlsDeConteo(fetchMock)).toHaveLength(conteosAntes + 2);
      expect(llamadas.filter((llamada) => llamada.url === '/api/v1/empleados/1')).toHaveLength(detallesAntes + 1);
    });
  });
});

describe('pantalla de Empleados: carga inicial', () => {
  it('espera la carga inicial del listado y del resumen', async () => {
    const { fetchMock } = prepararApiEmpleados();
    renderEmpleados();
    await esperarCargaInicial(fetchMock);
  });
});
