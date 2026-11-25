import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { CABECERA_CSV } from './csvReloj';
import { semanaActual } from './presentacion';
import {
  limpiarAsistencia,
  montarMarcacionesCargadas,
  prepararApiAsistencia,
  renderAsistencia,
  urlsDeListadoMarcaciones,
  type LlamadaAsistencia,
} from './asistencia.testUtils';

afterEach(() => {
  limpiarAsistencia();
});

const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar la asistencia.';

function escrituras(llamadas: LlamadaAsistencia[]): LlamadaAsistencia[] {
  return llamadas.filter((llamada) => llamada.metodo !== 'GET');
}

function archivoCsv(contenido: string): File {
  return new File([contenido], 'reloj.csv', { type: 'text/csv' });
}

function filasCsv(cantidad: number): string {
  return Array.from({ length: cantidad }, (_, indice) => `R-${indice + 1},EMP-000001,Entrada,2026-11-03T13:00:00Z`).join('\n');
}

function elegirArchivo(dialogo: HTMLElement, archivo: File): void {
  fireEvent.change(within(dialogo).getByLabelText('Archivo del reloj (.csv)'), { target: { files: [archivo] } });
}

describe('vista de Marcaciones: listado y filtros', () => {
  it('pide la semana actual y muestra instante local, empleado, tipo, origen y detalle', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    const { desde, hasta } = semanaActual();
    expect(urlsDeListadoMarcaciones(fetchMock)).toEqual([`/api/v1/marcaciones?desde=${desde}&hasta=${hasta}&pagina=1&tamanoPagina=10`]);
    expect(screen.getByRole('tab', { name: 'Marcaciones' })).toHaveAttribute('aria-selected', 'true');
    const tabla = screen.getByRole('table', { name: 'Listado de marcaciones' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual([
      'Instante', 'Empleado', 'Tipo', 'Origen', 'Dispositivo u observación', 'Acciones',
    ]);
    const filas = within(tabla).getAllByRole('row');
    expect(filas[1]).toHaveTextContent('03/11/2026 08:15');
    expect(filas[1]).toHaveTextContent('EMP-000001 · Ana Torres Vega');
    expect(filas[1]).toHaveTextContent('Entrada');
    expect(filas[1]).toHaveTextContent('Reloj');
    expect(filas[1]).toHaveTextContent('RELOJ-01 · R-1001');
    expect(filas[3]).toHaveTextContent('04/11/2026 18:30');
    expect(filas[3]).toHaveTextContent('Manual');
    expect(filas[3]).toHaveTextContent('Olvidó marcar la salida');
  });

  it('solo las marcaciones manuales tienen botón Eliminar', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    const botones = screen.getAllByRole('button', { name: /^Eliminar marcación de/ });
    expect(botones).toHaveLength(1);
    expect(botones[0]).toHaveAccessibleName('Eliminar marcación de Luis Rojas Mendoza, 04/11/2026 18:30');
  });

  it('envía empleado, rango, tipo y origen a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiAsistencia({ totalListado: 25 });
    await montarMarcacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsDeListadoMarcaciones(fetchMock).at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Empleado'), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-11-01' } });
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-11-30' } });
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Salida' } });
    fireEvent.change(screen.getByLabelText('Origen'), { target: { value: 'Manual' } });
    await waitFor(() => expect(urlsDeListadoMarcaciones(fetchMock).at(-1)).toBe(
      '/api/v1/marcaciones?empleadoId=1&desde=2026-11-01&hasta=2026-11-30&tipo=Salida&origen=Manual&pagina=1&tamanoPagina=10',
    ));
  });

  it('muestra el estado vacío y el error del listado', async () => {
    prepararApiAsistencia({ listaVacia: true });
    renderAsistencia(['RRHH'], '/modulos/63-asistencia-biometrica?vista=marcaciones');
    expect(await screen.findByText('No hay marcaciones con esos filtros.')).toHaveAttribute('role', 'status');
    limpiarAsistencia();
    prepararApiAsistencia({ errorLista: true });
    renderAsistencia(['RRHH'], '/modulos/63-asistencia-biometrica?vista=marcaciones');
    expect(await screen.findByText('No se pudo cargar la lista de marcaciones.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Marcaciones: registro manual', () => {
  async function abrirFormulario(): Promise<HTMLElement> {
    fireEvent.click(screen.getByRole('button', { name: 'Registrar marcación' }));
    return screen.findByRole('region', { name: 'Registrar marcación' });
  }

  it('valida los mensajes 1 a 4 sin enviar', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    const formulario = await abrirFormulario();
    fireEvent.click(within(formulario).getByRole('button', { name: 'Registrar marcación' }));
    expect(await within(formulario).findByText('El empleado es obligatorio.')).toBeInTheDocument();
    expect(within(formulario).getByText('El tipo de marcación debe ser Entrada o Salida.')).toBeInTheDocument();
    expect(within(formulario).getAllByText('El instante de la marcación es obligatorio.')).toHaveLength(2);
    expect(within(formulario).getByText('La observación es obligatoria en una marcación manual.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('valida el límite de 200 caracteres de la observación (mensaje 5)', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    const formulario = await abrirFormulario();
    fireEvent.change(within(formulario).getByLabelText('Observación'), { target: { value: 'a'.repeat(201) } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Registrar marcación' }));
    expect(await within(formulario).findByText('La observación no puede superar 200 caracteres.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('convierte la fecha y hora locales a UTC, cierra el formulario y recarga el listado', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    const formulario = await abrirFormulario();
    fireEvent.change(within(formulario).getByLabelText('Empleado'), { target: { value: '1' } });
    fireEvent.change(within(formulario).getByLabelText('Tipo'), { target: { value: 'Entrada' } });
    fireEvent.change(within(formulario).getByLabelText('Fecha (hora de Perú)'), { target: { value: '2026-11-03' } });
    fireEvent.change(within(formulario).getByLabelText('Hora (hora de Perú)'), { target: { value: '08:00' } });
    fireEvent.change(within(formulario).getByLabelText('Observación'), { target: { value: '  Olvidó marcar  ' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Registrar marcación' }));
    await waitFor(() => expect(escrituras(llamadas)).toEqual([
      {
        url: '/api/v1/marcaciones',
        metodo: 'POST',
        cuerpo: { empleadoId: 1, tipo: 'Entrada', instante: '2026-11-03T13:00:00.000Z', observacion: 'Olvidó marcar' },
      },
    ]));
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Registrar marcación' })).not.toBeInTheDocument());
    await waitFor(() => expect(urlsDeListadoMarcaciones(fetchMock)).toHaveLength(2));
  });

  it('muestra el mensaje del servidor (duplicado, 409) y mantiene el formulario abierto', async () => {
    const { fetchMock } = prepararApiAsistencia({ errorEscritura: { accion: 'registrar-marcacion', status: 409, mensaje: 'Ya existe una marcación del mismo tipo en ese minuto.' } });
    await montarMarcacionesCargadas(fetchMock);
    const formulario = await abrirFormulario();
    fireEvent.change(within(formulario).getByLabelText('Empleado'), { target: { value: '1' } });
    fireEvent.change(within(formulario).getByLabelText('Tipo'), { target: { value: 'Entrada' } });
    fireEvent.change(within(formulario).getByLabelText('Fecha (hora de Perú)'), { target: { value: '2026-11-03' } });
    fireEvent.change(within(formulario).getByLabelText('Hora (hora de Perú)'), { target: { value: '08:00' } });
    fireEvent.change(within(formulario).getByLabelText('Observación'), { target: { value: 'Olvidó marcar' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Registrar marcación' }));
    expect(await within(formulario).findByText('Ya existe una marcación del mismo tipo en ese minuto.')).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('region', { name: 'Registrar marcación' })).toBeInTheDocument();
  });

  it('Cancelar cierra el formulario', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    const formulario = await abrirFormulario();
    fireEvent.click(within(formulario).getByRole('button', { name: 'Cancelar' }));
    expect(screen.queryByRole('region', { name: 'Registrar marcación' })).not.toBeInTheDocument();
  });
});

describe('vista de Marcaciones: importación del reloj', () => {
  async function abrirDialogo(): Promise<HTMLElement> {
    fireEvent.click(screen.getByRole('button', { name: 'Importar archivo del reloj' }));
    return screen.findByRole('dialog', { name: 'Importar archivo del reloj' });
  }

  it('pide el dispositivo (mensaje 10) y el archivo antes de enviar', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    const dialogo = await abrirDialogo();
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Importar' }));
    expect(await within(dialogo).findByText('El dispositivo es obligatorio.')).toBeInTheDocument();
    expect(within(dialogo).getByText('Selecciona el archivo CSV del reloj.')).toBeInTheDocument();
    fireEvent.change(within(dialogo).getByLabelText('Dispositivo'), { target: { value: 'a'.repeat(51) } });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Importar' }));
    expect(await within(dialogo).findByText('El dispositivo no puede superar 50 caracteres.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('muestra el error de una cabecera inválida y no envía', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    const dialogo = await abrirDialogo();
    fireEvent.change(within(dialogo).getByLabelText('Dispositivo'), { target: { value: 'RELOJ-01' } });
    elegirArchivo(dialogo, archivoCsv('legajo,tipo\nEMP-000001,Entrada'));
    expect(await within(dialogo).findByText('El archivo debe tener la cabecera numeroRegistro,legajo,tipo,instante.')).toBeInTheDocument();
    expect(within(dialogo).queryByText(/Total de registros/)).not.toBeInTheDocument();
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Importar' }));
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('rechaza más de 1000 registros con el mensaje 11', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    const dialogo = await abrirDialogo();
    elegirArchivo(dialogo, archivoCsv(`${CABECERA_CSV}\n${filasCsv(1001)}`));
    expect(await within(dialogo).findByText('La importación debe tener entre 1 y 1000 registros.')).toBeInTheDocument();
  });

  it('muestra la vista previa de 5 filas y el total, envía el JSON y muestra el resultado con los rechazados', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    const dialogo = await abrirDialogo();
    fireEvent.change(within(dialogo).getByLabelText('Dispositivo'), { target: { value: ' RELOJ-01 ' } });
    elegirArchivo(dialogo, archivoCsv(`${CABECERA_CSV}\n${filasCsv(7)}\n`));
    expect(await within(dialogo).findByText('Total de registros: 7')).toBeInTheDocument();
    const vista = within(dialogo).getByRole('table', { name: 'Primeras 5 filas' });
    expect(within(vista).getAllByRole('row')).toHaveLength(6);
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Importar' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    const envio = escrituras(llamadas)[0];
    expect(envio?.url).toBe('/api/v1/marcaciones/importacion');
    expect(envio?.cuerpo).toMatchObject({ dispositivo: 'RELOJ-01' });
    const registros = (envio?.cuerpo as { registros: unknown[] }).registros;
    expect(registros).toHaveLength(7);
    expect(registros[0]).toEqual({ numeroRegistro: 'R-1', legajo: 'EMP-000001', tipo: 'Entrada', instante: '2026-11-03T13:00:00Z' });
    expect(await within(dialogo).findByText('Recibidos')).toBeInTheDocument();
    expect(within(dialogo).getByText('Recibidos').nextElementSibling).toHaveTextContent('3');
    expect(within(dialogo).getByText('Importados').nextElementSibling).toHaveTextContent('1');
    expect(within(dialogo).getByText('Ya importados').nextElementSibling).toHaveTextContent('1');
    expect(within(dialogo).getByText('Duplicados').nextElementSibling).toHaveTextContent('0');
    const rechazados = within(dialogo).getByRole('table', { name: 'Registros rechazados' });
    expect(within(rechazados).getByText('R-3')).toBeInTheDocument();
    expect(within(rechazados).getByText('No existe un empleado con el legajo EMP-999999.')).toBeInTheDocument();
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Cerrar' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(urlsDeListadoMarcaciones(fetchMock)).toHaveLength(2));
  });

  it('muestra el error del servidor (mensaje 28) en el diálogo', async () => {
    const { fetchMock } = prepararApiAsistencia({ errorEscritura: { accion: 'importar', status: 400, mensaje: 'El dispositivo no puede superar 50 caracteres.' } });
    await montarMarcacionesCargadas(fetchMock);
    const dialogo = await abrirDialogo();
    fireEvent.change(within(dialogo).getByLabelText('Dispositivo'), { target: { value: 'RELOJ-01' } });
    elegirArchivo(dialogo, archivoCsv(`${CABECERA_CSV}\n${filasCsv(1)}`));
    await within(dialogo).findByText('Total de registros: 1');
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Importar' }));
    expect(await within(dialogo).findByText('El dispositivo no puede superar 50 caracteres.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Marcaciones: eliminar', () => {
  it('elimina una marcación manual tras confirmar y recarga el listado', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: /^Eliminar marcación de/ }));
    const dialogo = await screen.findByRole('dialog', { name: 'Eliminar marcación' });
    expect(dialogo).toHaveTextContent('Luis Rojas Mendoza, 04/11/2026 18:30');
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Eliminar marcación' }));
    await waitFor(() => expect(escrituras(llamadas)).toEqual([{ url: '/api/v1/marcaciones/3', metodo: 'DELETE', cuerpo: undefined }]));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(urlsDeListadoMarcaciones(fetchMock)).toHaveLength(2));
  });

  it('muestra el error del servidor al eliminar (409) y mantiene el diálogo', async () => {
    const { fetchMock } = prepararApiAsistencia({ errorEscritura: { accion: 'eliminar-marcacion', status: 409, mensaje: 'La jornada del 2026-11-04 ya tiene las horas extra decididas y no se modifica.' } });
    await montarMarcacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: /^Eliminar marcación de/ }));
    const dialogo = await screen.findByRole('dialog', { name: 'Eliminar marcación' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Eliminar marcación' }));
    expect(await within(dialogo).findByText('La jornada del 2026-11-04 ya tiene las horas extra decididas y no se modifica.')).toBeInTheDocument();
    await waitFor(() => expect(urlsDeListadoMarcaciones(fetchMock)).toHaveLength(2));
  });

  it('Cancelar no elimina nada', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: /^Eliminar marcación de/ }));
    const dialogo = await screen.findByRole('dialog', { name: 'Eliminar marcación' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });
});

describe('vista de Marcaciones: roles', () => {
  it('sin RRHH ni ADMIN deshabilita registrar, importar y eliminar con el aviso', async () => {
    const { fetchMock, llamadas } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock, ['CONSULTA']);
    expect(screen.getByRole('button', { name: 'Registrar marcación' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Importar archivo del reloj' })).toBeDisabled();
    expect(screen.getByRole('button', { name: /^Eliminar marcación de/ })).toBeDisabled();
    expect(screen.getByText(TEXTO_SOLO_RRHH)).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('con ADMIN las acciones están habilitadas', async () => {
    const { fetchMock } = prepararApiAsistencia();
    await montarMarcacionesCargadas(fetchMock, ['ADMIN']);
    expect(screen.getByRole('button', { name: 'Registrar marcación' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Importar archivo del reloj' })).toBeEnabled();
    expect(screen.getByRole('button', { name: /^Eliminar marcación de/ })).toBeEnabled();
    expect(screen.queryByText(TEXTO_SOLO_RRHH)).not.toBeInTheDocument();
  });
});
