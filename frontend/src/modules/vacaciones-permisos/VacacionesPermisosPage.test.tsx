import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { formatearFecha } from '../../lib/formato';
import {
  limpiarAusencias,
  montarAusenciasCargadas,
  montarSinFilas,
  prepararApiAusencias,
  urlsDeListado,
  type LlamadaAusencias,
  type OpcionesApiAusencias,
} from './ausencias.testUtils';

afterEach(() => {
  limpiarAusencias();
});

const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden aprobar o rechazar ausencias.';

async function seleccionar(numero: string, nombre: string): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: numero }));
  await screen.findByRole('heading', { name: nombre });
}

// La ficha es la sección etiquetada por el título del detalle.
function ficha(): ReturnType<typeof within> {
  const seccion = document.getElementById('ausencia-detail-title')?.closest('section');
  if (seccion === null || seccion === undefined) {
    throw new Error('La ficha de la ausencia no está en pantalla.');
  }
  return within(seccion);
}

function escrituras(llamadas: LlamadaAusencias[]): LlamadaAusencias[] {
  return llamadas.filter((llamada) => llamada.metodo === 'POST');
}

describe('pantalla de Vacaciones y permisos: listado, filtros y resumen', () => {
  it('carga una sola vez el listado en página 1 con número, tipo legible y días', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    expect(screen.getByRole('heading', { name: '65 · Vacaciones y permisos' })).toBeInTheDocument();
    expect(urlsDeListado(fetchMock)).toEqual(['/api/v1/ausencias?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table');
    expect(within(tabla).getByRole('button', { name: 'AUS-000007' })).toBeInTheDocument();
    expect(within(tabla).getAllByText('EMP-000002 · Luis Rojas Mendoza')).toHaveLength(2);
    expect(within(tabla).getByText('Descanso médico')).toBeInTheDocument();
    expect(within(tabla).getByText('Permiso con goce')).toBeInTheDocument();
    expect(within(tabla).getByText('Permiso sin goce')).toBeInTheDocument();
  });

  it('muestra cada estado con texto en pastillas', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    const tabla = screen.getByRole('table');
    expect(within(tabla).getAllByText('Aprobada')).toHaveLength(3);
    expect(within(tabla).getAllByText('Pendiente')).toHaveLength(2);
    expect(within(tabla).getByText('Rechazada')).toBeInTheDocument();
    expect(within(tabla).getByText('Cancelada')).toBeInTheDocument();
  });

  it('muestra las fechas del periodo sin desfase de zona', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    const fila = screen.getByRole('button', { name: 'AUS-000002' }).closest('tr');
    expect(fila).not.toBeNull();
    expect(within(fila as HTMLElement).getByText(formatearFecha('2099-01-04'))).toBeInTheDocument();
    expect(within(fila as HTMLElement).getByText(formatearFecha('2099-01-15'))).toBeInTheDocument();
    expect(formatearFecha('2099-01-04')).toContain('4');
  });

  it('muestra las cinco tarjetas del resumen con una sola petición', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    const resumen = screen.getByRole('region', { name: 'Resumen de ausencias' });
    expect(within(resumen).getByText('Pendientes').nextElementSibling).toHaveTextContent('2');
    expect(within(resumen).getByText('Ausentes hoy').nextElementSibling).toHaveTextContent('1');
    expect(within(resumen).getByText('De vacaciones hoy').nextElementSibling).toHaveTextContent('0');
    expect(within(resumen).getByText('Descanso médico hoy').nextElementSibling).toHaveTextContent('1');
    expect(within(resumen).getByText('Aprobadas este mes').nextElementSibling).toHaveTextContent('3');
  });

  it('envía tipo, estado y fechas a la query', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Vacaciones' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('tipo=Vacaciones'));
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Pendiente' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('estado=Pendiente'));
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2099-01-01' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('desde=2099-01-01'));
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2099-12-31' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('hasta=2099-12-31'));
    expect(urlsDeListado(fetchMock).at(-1)).toBe(
      '/api/v1/ausencias?tipo=Vacaciones&estado=Pendiente&desde=2099-01-01&hasta=2099-12-31&pagina=1&tamanoPagina=10',
    );
  });

  it('vuelve a la página 1 al filtrar estando en la página 2', async () => {
    const { fetchMock } = prepararApiAusencias({ totalListado: 25 });
    await montarAusenciasCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'DescansoMedico' } });
    await waitFor(() => expect(urlsDeListado(fetchMock).at(-1)).toBe('/api/v1/ausencias?tipo=DescansoMedico&pagina=1&tamanoPagina=10'));
  });

  it('aplica la búsqueda 300 ms después del último cambio con una sola consulta del último texto', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    const antes = urlsDeListado(fetchMock).length;
    vi.useFakeTimers();
    const buscador = screen.getByLabelText('Buscar por número o empleado');
    fireEvent.change(buscador, { target: { value: 'A' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(200);
    });
    fireEvent.change(buscador, { target: { value: 'AU' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(200);
    });
    fireEvent.change(buscador, { target: { value: 'AUS-000005' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsDeListado(fetchMock)).toHaveLength(antes);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsDeListado(fetchMock)).toHaveLength(antes + 1));
    expect(urlsDeListado(fetchMock).at(-1)).toBe('/api/v1/ausencias?busqueda=AUS-000005&pagina=1&tamanoPagina=10');
  });

  it('muestra el estado vacío cuando no hay ausencias', async () => {
    prepararApiAusencias({ listaVacia: true });
    await montarSinFilas();
    expect(await screen.findByText('No hay ausencias con esos filtros.')).toHaveAttribute('role', 'status');
  });

  it('muestra el error de carga del listado', async () => {
    prepararApiAusencias({ errorLista: true });
    await montarSinFilas();
    expect(await screen.findByText('No se pudo cargar la lista de ausencias.')).toHaveAttribute('role', 'alert');
  });

  it('muestra el error del resumen sin ocultar el listado', async () => {
    prepararApiAusencias({ errorResumen: true });
    await montarSinFilas();
    expect(await screen.findByText('No se pudo cargar el resumen de ausencias.')).toHaveAttribute('role', 'alert');
    expect(await screen.findByRole('button', { name: 'AUS-000001' })).toBeInTheDocument();
  });
});

describe('pantalla de Vacaciones y permisos: detalle', () => {
  it('invita a seleccionar una ausencia antes de elegir', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    expect(screen.getByText('Selecciona una ausencia para ver su ficha.')).toBeInTheDocument();
  });

  it('muestra los campos de un permiso pendiente sin saldo vacacional', async () => {
    const { fetchMock, llamadas } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    await seleccionar('AUS-000003', 'Ana Torres Vega');
    const detalle = ficha();
    expect(detalle.getByText('Permiso con goce')).toBeInTheDocument();
    expect(detalle.getByText('Trámite personal')).toBeInTheDocument();
    expect(detalle.getByText(formatearFecha('2099-03-10'))).toBeInTheDocument();
    expect(detalle.getByText(formatearFecha('2026-01-10'))).toBeInTheDocument();
    expect(detalle.queryByText('Saldo vacacional del empleado')).not.toBeInTheDocument();
    expect(llamadas.some((llamada) => llamada.url.includes('saldo-vacacional'))).toBe(false);
  });

  it('muestra el saldo vacacional del empleado en una ausencia de vacaciones', async () => {
    const { fetchMock, llamadas } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    await seleccionar('AUS-000002', 'Ana Torres Vega');
    const saldo = within(await screen.findByRole('group', { name: 'Saldo vacacional' }));
    expect(await saldo.findByText('Días devengados')).toBeInTheDocument();
    expect(saldo.getByText('Días devengados').nextElementSibling).toHaveTextContent('30.0');
    expect(saldo.getByText('Días gozados').nextElementSibling).toHaveTextContent('12.0');
    expect(saldo.getByText('Días programados').nextElementSibling).toHaveTextContent('12.0');
    expect(saldo.getByText('Saldo').nextElementSibling).toHaveTextContent('6.5');
    expect(llamadas.map((llamada) => llamada.url)).toContain('/api/v1/ausencias/saldo-vacacional/1?fecha=2099-01-04');
  });

  it('muestra decisión y motivo del rechazo en una ausencia rechazada, sin acciones', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    await seleccionar('AUS-000005', 'Maria Salazar Quispe');
    const detalle = ficha();
    expect(detalle.getByText('Periodo de cierre contable')).toBeInTheDocument();
    expect(detalle.getByText('rrhh')).toBeInTheDocument();
    expect(detalle.queryByRole('button', { name: 'Aprobar' })).not.toBeInTheDocument();
    expect(detalle.queryByRole('button', { name: 'Cancelar solicitud' })).not.toBeInTheDocument();
  });

  it('muestra la fecha de cancelación en una ausencia cancelada, sin acciones', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    await seleccionar('AUS-000006', 'Maria Salazar Quispe');
    const detalle = ficha();
    expect(detalle.getByText(formatearFecha('2026-10-20'))).toBeInTheDocument();
    expect(detalle.queryByRole('button', { name: 'Cancelar solicitud' })).not.toBeInTheDocument();
  });

  it('muestra el error del detalle', async () => {
    const { fetchMock } = prepararApiAusencias({ errorDetalle: true });
    await montarAusenciasCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'AUS-000003' }));
    expect(await screen.findByText('No se pudo cargar el detalle de la ausencia.')).toHaveAttribute('role', 'alert');
  });
});

describe('pantalla de Vacaciones y permisos: acciones según estado, fecha y rol', () => {
  it('una pendiente futura ofrece Aprobar, Rechazar y Cancelar solicitud', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    await seleccionar('AUS-000003', 'Ana Torres Vega');
    const detalle = ficha();
    expect(detalle.getByRole('button', { name: 'Aprobar' })).toBeEnabled();
    expect(detalle.getByRole('button', { name: 'Rechazar' })).toBeEnabled();
    expect(detalle.getByRole('button', { name: 'Cancelar solicitud' })).toBeEnabled();
  });

  it('una aprobada futura solo ofrece Cancelar solicitud', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    await seleccionar('AUS-000002', 'Ana Torres Vega');
    const detalle = ficha();
    expect(detalle.queryByRole('button', { name: 'Aprobar' })).not.toBeInTheDocument();
    expect(detalle.queryByRole('button', { name: 'Rechazar' })).not.toBeInTheDocument();
    expect(detalle.getByRole('button', { name: 'Cancelar solicitud' })).toBeEnabled();
  });

  it('una aprobada que ya empezó no ofrece ninguna acción', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock);
    await seleccionar('AUS-000001', 'Ana Torres Vega');
    const detalle = ficha();
    expect(detalle.queryByRole('button', { name: 'Cancelar solicitud' })).not.toBeInTheDocument();
    expect(detalle.queryByRole('button', { name: 'Aprobar' })).not.toBeInTheDocument();
  });

  it('sin RRHH ni ADMIN deshabilita Aprobar y Rechazar con el aviso y deja Cancelar habilitado', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock, ['VENTAS']);
    await seleccionar('AUS-000003', 'Ana Torres Vega');
    const detalle = ficha();
    expect(detalle.getByRole('button', { name: 'Aprobar' })).toBeDisabled();
    expect(detalle.getByRole('button', { name: 'Rechazar' })).toBeDisabled();
    expect(detalle.getByRole('button', { name: 'Cancelar solicitud' })).toBeEnabled();
    expect(detalle.getByText(TEXTO_SOLO_RRHH)).toBeInTheDocument();
    expect(detalle.getByRole('button', { name: 'Aprobar' })).toHaveAccessibleDescription(TEXTO_SOLO_RRHH);
  });

  it('con ADMIN habilita Aprobar y Rechazar y no muestra el aviso de rol', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock, ['ADMIN']);
    await seleccionar('AUS-000003', 'Ana Torres Vega');
    expect(screen.getByRole('button', { name: 'Aprobar' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Rechazar' })).toBeEnabled();
    expect(screen.queryByText(TEXTO_SOLO_RRHH)).not.toBeInTheDocument();
  });

  it('cualquier sesión puede abrir el formulario de registro', async () => {
    const { fetchMock } = prepararApiAusencias();
    await montarAusenciasCargadas(fetchMock, []);
    expect(screen.getByRole('button', { name: 'Nueva ausencia' })).toBeEnabled();
  });
});

describe('pantalla de Vacaciones y permisos: aprobar, rechazar y cancelar', () => {
  async function abrirPendiente(opciones: OpcionesApiAusencias = {}) {
    const api = prepararApiAusencias(opciones);
    await montarAusenciasCargadas(api.fetchMock);
    await seleccionar('AUS-000003', 'Ana Torres Vega');
    return api;
  }

  it('aprueba sin cuerpo y vuelve a pedir listado, resumen y detalle', async () => {
    const { fetchMock, llamadas } = await abrirPendiente();
    const antes = urlsDeListado(fetchMock).length;
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar' }));
    await waitFor(() => expect(urlsDeListado(fetchMock)).toHaveLength(antes + 1));
    const posts = escrituras(llamadas);
    expect(posts).toHaveLength(1);
    expect(posts[0]).toMatchObject({ url: '/api/v1/ausencias/3/aprobacion', metodo: 'POST', cuerpo: undefined });
    expect(llamadas.filter((llamada) => llamada.url === '/api/v1/ausencias/resumen')).toHaveLength(2);
  });

  it('muestra el 409 de la aprobación junto a la ficha', async () => {
    await abrirPendiente({
      errorEscritura: { accion: 'aprobacion', status: 409, mensaje: 'El saldo vacacional es de 2.5 días y la solicitud pide 10.' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar' }));
    expect(await screen.findByText('El saldo vacacional es de 2.5 días y la solicitud pide 10.')).toHaveAttribute('role', 'alert');
  });

  it('rechaza con motivo: valida en local, envía el cuerpo y cierra el diálogo', async () => {
    const { llamadas } = await abrirPendiente();
    fireEvent.click(screen.getByRole('button', { name: 'Rechazar' }));
    const dialogo = within(await screen.findByRole('dialog', { name: 'Rechazar ausencia' }));
    expect(dialogo.getByLabelText('Motivo del rechazo')).toHaveFocus();
    fireEvent.click(dialogo.getByRole('button', { name: 'Rechazar ausencia' }));
    expect(await dialogo.findByText('El motivo del rechazo es obligatorio.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
    fireEvent.change(dialogo.getByLabelText('Motivo del rechazo'), { target: { value: 'a'.repeat(301) } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Rechazar ausencia' }));
    expect(await dialogo.findByText('El motivo del rechazo no puede superar 300 caracteres.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
    fireEvent.change(dialogo.getByLabelText('Motivo del rechazo'), { target: { value: ' Cierre contable ' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Rechazar ausencia' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toEqual([
      { url: '/api/v1/ausencias/3/rechazo', metodo: 'POST', cuerpo: { motivo: 'Cierre contable' } },
    ]);
  });

  it('muestra el 409 del rechazo dentro del diálogo y lo mantiene abierto con el motivo', async () => {
    await abrirPendiente({ errorEscritura: { accion: 'rechazo', status: 409, mensaje: 'La ausencia AUS-000003 no está pendiente.' } });
    fireEvent.click(screen.getByRole('button', { name: 'Rechazar' }));
    const dialogo = within(await screen.findByRole('dialog', { name: 'Rechazar ausencia' }));
    fireEvent.change(dialogo.getByLabelText('Motivo del rechazo'), { target: { value: 'Cierre contable' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Rechazar ausencia' }));
    expect(await dialogo.findByText('La ausencia AUS-000003 no está pendiente.')).toHaveAttribute('role', 'alert');
    expect(dialogo.getByLabelText('Motivo del rechazo')).toHaveValue('Cierre contable');
  });

  it('cierra el diálogo de rechazo con Escape y devuelve el foco al botón', async () => {
    await abrirPendiente();
    const boton = screen.getByRole('button', { name: 'Rechazar' });
    fireEvent.click(boton);
    await screen.findByRole('dialog', { name: 'Rechazar ausencia' });
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(boton).toHaveFocus();
  });

  it('cancela con confirmación y sin cuerpo', async () => {
    const { llamadas } = await abrirPendiente();
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar solicitud' }));
    const dialogo = within(await screen.findByRole('dialog', { name: 'Cancelar solicitud' }));
    expect(dialogo.getByText('AUS-000003')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar cancelación' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toEqual([
      { url: '/api/v1/ausencias/3/cancelacion', metodo: 'POST', cuerpo: undefined },
    ]);
  });

  it('muestra el 409 de la cancelación dentro del diálogo', async () => {
    await abrirPendiente({
      errorEscritura: { accion: 'cancelacion', status: 409, mensaje: 'La ausencia AUS-000003 ya empezó y no se puede cancelar.' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar solicitud' }));
    const dialogo = within(await screen.findByRole('dialog', { name: 'Cancelar solicitud' }));
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar cancelación' }));
    expect(await dialogo.findByText('La ausencia AUS-000003 ya empezó y no se puede cancelar.')).toHaveAttribute('role', 'alert');
  });

  it('«Volver» cierra la confirmación sin enviar nada', async () => {
    const { llamadas } = await abrirPendiente();
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar solicitud' }));
    const dialogo = within(await screen.findByRole('dialog', { name: 'Cancelar solicitud' }));
    fireEvent.click(dialogo.getByRole('button', { name: 'Volver' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toHaveLength(0);
  });
});
