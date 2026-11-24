import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  esperarCargaBonos,
  limpiarPlanillas,
  prepararApiPlanillas,
  renderPlanillas,
  urlsQueTerminanEn,
  type LlamadaPlanillas,
  moneda,
} from './planillas.testUtils';

afterEach(() => {
  limpiarPlanillas();
});

const RUTA_BONOS = '/modulos/66-planillas?vista=bonos';

function urlsDeBonos(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueTerminanEn(fetchMock, '/bonos-planilla?', 'tamanoPagina=10');
}

function escrituras(llamadas: LlamadaPlanillas[]): LlamadaPlanillas[] {
  return llamadas.filter((llamada) => llamada.metodo !== 'GET');
}

async function montarBonos(roles: string[] = ['RRHH'], opciones = {}) {
  const api = prepararApiPlanillas(opciones);
  renderPlanillas(roles, RUTA_BONOS);
  await esperarCargaBonos(api.fetchMock);
  return api;
}

describe('vista de Bonos: listado y filtros', () => {
  it('lista los bonos con periodo, tipo, monto y planilla (o sin planilla)', async () => {
    const { fetchMock } = await montarBonos();
    expect(urlsDeBonos(fetchMock)).toEqual(['/api/v1/bonos-planilla?pagina=1&tamanoPagina=10']);
    expect(screen.getByRole('tab', { name: 'Bonos' })).toHaveAttribute('aria-selected', 'true');
    const tabla = screen.getByRole('table', { name: 'Listado de bonos' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual([
      'Empleado', 'Periodo', 'Tipo', 'Concepto', 'Monto', 'Planilla', 'Acciones',
    ]);
    const filas = within(tabla).getAllByRole('row');
    expect(filas[1]).toHaveTextContent('EMP-000001 · Ana Torres Vega');
    expect(filas[1]).toHaveTextContent('10/2026');
    expect(filas[1]).toHaveTextContent(moneda(300));
    expect(filas[1]).toHaveTextContent('PLA-202610-M');
    expect(filas[2]).toHaveTextContent('Complementaria');
    expect(filas[2]).toHaveTextContent('Sin planilla');
  });

  it('envía año, mes y empleado a la query y vuelve a la página 1', async () => {
    const { fetchMock } = await montarBonos();
    fireEvent.change(screen.getByLabelText('Año'), { target: { value: '2026' } });
    fireEvent.change(screen.getByLabelText('Mes'), { target: { value: '11' } });
    fireEvent.change(screen.getByLabelText('Empleado'), { target: { value: '2' } });
    await waitFor(() => expect(urlsDeBonos(fetchMock).at(-1)).toBe('/api/v1/bonos-planilla?anio=2026&mes=11&empleadoId=2&pagina=1&tamanoPagina=10'));
    await waitFor(() => expect(screen.queryByText('Bono por metas')).not.toBeInTheDocument());
    expect(screen.getByText('Ajuste de campaña')).toBeInTheDocument();
  });

  it('muestra los estados de error y vacío', async () => {
    prepararApiPlanillas({ errorBonos: true });
    renderPlanillas(['RRHH'], RUTA_BONOS);
    expect(await screen.findByText('No se pudo cargar la lista de bonos.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Bonos: alta', () => {
  async function abrirFormulario(): Promise<HTMLElement> {
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo bono' }));
    return screen.findByRole('region', { name: 'Nuevo bono' });
  }

  it('valida localmente con los mensajes literales y no llama a la API', async () => {
    const { llamadas } = await montarBonos();
    const formulario = await abrirFormulario();
    fireEvent.change(within(formulario).getByLabelText('Año'), { target: { value: '' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Registrar bono' }));
    expect(await within(formulario).findByText('El empleado es obligatorio.')).toBeInTheDocument();
    expect(within(formulario).getByText('El año es obligatorio y debe estar entre 2000 y 2100.')).toBeInTheDocument();
    expect(within(formulario).getByText('El concepto es obligatorio y no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(within(formulario).getByText('El monto debe ser mayor que cero, con 2 decimales como máximo y no superar 9999999999.99.')).toBeInTheDocument();
    fireEvent.change(within(formulario).getByLabelText('Monto'), { target: { value: '12.345' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Registrar bono' }));
    expect(await within(formulario).findByText('El monto debe ser mayor que cero, con 2 decimales como máximo y no superar 9999999999.99.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('registra el bono con el cuerpo exacto y recarga la lista', async () => {
    const { fetchMock, llamadas } = await montarBonos();
    const formulario = await abrirFormulario();
    fireEvent.change(within(formulario).getByLabelText('Empleado'), { target: { value: '2' } });
    fireEvent.change(within(formulario).getByLabelText('Tipo de planilla'), { target: { value: 'Complementaria' } });
    fireEvent.change(within(formulario).getByLabelText('Año'), { target: { value: '2026' } });
    fireEvent.change(within(formulario).getByLabelText('Mes'), { target: { value: '11' } });
    fireEvent.change(within(formulario).getByLabelText('Concepto'), { target: { value: '  Bono de campaña ' } });
    fireEvent.change(within(formulario).getByLabelText('Monto'), { target: { value: '250,75' } });
    const antes = urlsDeBonos(fetchMock).length;
    fireEvent.click(within(formulario).getByRole('button', { name: 'Registrar bono' }));
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Nuevo bono' })).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toEqual([{
      url: '/api/v1/bonos-planilla',
      metodo: 'POST',
      cuerpo: { empleadoId: 2, anio: 2026, mes: 11, tipoPlanilla: 'Complementaria', concepto: 'Bono de campaña', monto: 250.75 },
    }]);
    await waitFor(() => expect(urlsDeBonos(fetchMock).length).toBe(antes + 1));
  });

  it('muestra el mensaje del servidor y conserva lo escrito', async () => {
    await montarBonos(['RRHH'], { errorEscritura: { accion: 'crear-bono', status: 404, mensaje: 'El empleado 2 no existe.' } });
    const formulario = await abrirFormulario();
    fireEvent.change(within(formulario).getByLabelText('Empleado'), { target: { value: '2' } });
    fireEvent.change(within(formulario).getByLabelText('Concepto'), { target: { value: 'Bono' } });
    fireEvent.change(within(formulario).getByLabelText('Monto'), { target: { value: '10' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Registrar bono' }));
    expect(await within(formulario).findByText('El empleado 2 no existe.')).toHaveAttribute('role', 'alert');
    expect(within(formulario).getByLabelText('Concepto')).toHaveValue('Bono');
  });
});

describe('vista de Bonos: eliminación y roles', () => {
  it('elimina con confirmación mediante DialogoBaja', async () => {
    const { llamadas } = await montarBonos();
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar bono Bono por metas de Ana Torres Vega' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Eliminar bono' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar' }));
    expect(escrituras(llamadas)).toHaveLength(0);
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar bono Bono por metas de Ana Torres Vega' }));
    fireEvent.click(within(await screen.findByRole('dialog', { name: 'Eliminar bono' })).getByRole('button', { name: 'Eliminar bono' }));
    await waitFor(() => expect(screen.queryByText('Bono por metas')).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toEqual([{ url: '/api/v1/bonos-planilla/1', metodo: 'DELETE', cuerpo: undefined }]);
  });

  it('muestra el mensaje 21 del servidor dentro del diálogo', async () => {
    await montarBonos(['RRHH'], { errorEscritura: { accion: 'eliminar-bono', status: 409, mensaje: 'El bono está en una planilla aprobada y no se puede eliminar.' } });
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar bono Bono por metas de Ana Torres Vega' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Eliminar bono' });
    fireEvent.click(within(dialogo).getByRole('button', { name: 'Eliminar bono' }));
    expect(await within(dialogo).findByText('El bono está en una planilla aprobada y no se puede eliminar.')).toHaveAttribute('role', 'alert');
  });

  it('sin RRHH ni ADMIN no se puede registrar ni eliminar', async () => {
    await montarBonos([]);
    expect(screen.getByRole('button', { name: 'Nuevo bono' })).toBeDisabled();
    expect(screen.getByText('Solo RRHH o ADMIN pueden modificar planillas.')).toBeInTheDocument();
    for (const boton of screen.getAllByRole('button', { name: /^Eliminar bono / })) {
      expect(boton).toBeDisabled();
    }
  });
});
