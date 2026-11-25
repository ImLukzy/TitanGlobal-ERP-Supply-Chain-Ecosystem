import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import {
  esperarCargaAfiliaciones,
  limpiarPlanillas,
  prepararApiPlanillas,
  renderPlanillas,
  type LlamadaPlanillas,
  type OpcionesApiPlanillas,
} from './planillas.testUtils';

afterEach(() => {
  limpiarPlanillas();
});

const RUTA = '/modulos/66-planillas?vista=afiliaciones';

function escrituras(llamadas: LlamadaPlanillas[]): LlamadaPlanillas[] {
  return llamadas.filter((llamada) => llamada.metodo !== 'GET');
}

async function montar(roles: string[] = ['RRHH'], opciones: OpcionesApiPlanillas = {}) {
  const api = prepararApiPlanillas(opciones);
  renderPlanillas(roles, RUTA);
  await esperarCargaAfiliaciones();
  return api;
}

describe('vista de Afiliaciones: listado', () => {
  it('cruza empleados y afiliaciones y destaca «Sin afiliación» con texto', async () => {
    await montar();
    expect(screen.getByRole('tab', { name: 'Afiliaciones' })).toHaveAttribute('aria-selected', 'true');
    const tabla = screen.getByRole('table', { name: 'Afiliaciones a sistemas de pensiones' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual([
      'Legajo', 'Empleado', 'Sistema de pensiones', 'CUSPP', 'Afiliación', 'Acciones',
    ]);
    const filas = within(tabla).getAllByRole('row');
    expect(filas).toHaveLength(4);
    expect(filas[1]).toHaveTextContent('INTEGRA (AFP)');
    expect(filas[1]).toHaveTextContent('123456ABCDEF');
    expect(filas[2]).toHaveTextContent('ONP (ONP)');
    expect(filas[3]).toHaveTextContent('Sin afiliación');
    expect(screen.getByText('1 empleado no tiene sistema de pensiones y se excluyen del cálculo de planilla.')).toBeInTheDocument();
  });

  it('filtra por empleado y muestra el error de la lista', async () => {
    await montar();
    fireEvent.change(screen.getByLabelText('Empleado'), { target: { value: '3' } });
    await waitFor(() => expect(screen.queryByText('Torres Vega, Ana')).not.toBeInTheDocument());
    expect(screen.getByText('Salazar Quispe, Maria')).toBeInTheDocument();
    limpiarPlanillas();
    prepararApiPlanillas({ errorAfiliaciones: true });
    renderPlanillas(['RRHH'], RUTA);
    expect(await screen.findByText('No se pudo cargar la lista de afiliaciones.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Afiliaciones: alta y cambio', () => {
  it('valida el sistema y la fecha con los mensajes literales', async () => {
    const { llamadas } = await montar();
    fireEvent.click(screen.getByRole('button', { name: 'Nueva afiliación' }));
    const formulario = await screen.findByRole('region', { name: 'Nueva afiliación' });
    fireEvent.change(within(formulario).getByLabelText('Fecha de afiliación'), { target: { value: '' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar afiliación' }));
    expect(await within(formulario).findByText('El empleado es obligatorio.')).toBeInTheDocument();
    expect(within(formulario).getByText('El sistema de pensiones es obligatorio.')).toBeInTheDocument();
    expect(within(formulario).getByText('La fecha de afiliación es obligatoria.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('el CUSPP solo aparece, se exige y se envía con una AFP', async () => {
    const { llamadas } = await montar();
    fireEvent.click(screen.getByRole('button', { name: 'Afiliar a Salazar Quispe, Maria' }));
    const formulario = await screen.findByRole('region', { name: 'Nueva afiliación' });
    expect(within(formulario).getByLabelText('Empleado')).toHaveValue('3');
    expect(within(formulario).queryByLabelText('CUSPP')).not.toBeInTheDocument();
    fireEvent.change(within(formulario).getByLabelText('Sistema de pensiones'), { target: { value: 'INTEGRA' } });
    expect(within(formulario).getByLabelText('CUSPP')).toBeInTheDocument();
    fireEvent.change(within(formulario).getByLabelText('Fecha de afiliación'), { target: { value: '2026-01-15' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar afiliación' }));
    expect(await within(formulario).findByText('El CUSPP es obligatorio en una AFP y tiene 12 caracteres.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
    fireEvent.change(within(formulario).getByLabelText('CUSPP'), { target: { value: ' 123456ABCDEF ' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar afiliación' }));
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Nueva afiliación' })).not.toBeInTheDocument());
    expect(escrituras(llamadas)).toEqual([{
      url: '/api/v1/afiliaciones-pension/3',
      metodo: 'PUT',
      cuerpo: { codigoPension: 'INTEGRA', cuspp: '123456ABCDEF', fechaAfiliacion: '2026-01-15' },
    }]);
  });

  it('un cambio a ONP no envía el CUSPP', async () => {
    const { llamadas } = await montar();
    fireEvent.click(screen.getByRole('button', { name: 'Cambiar afiliación de Torres Vega, Ana' }));
    const formulario = await screen.findByRole('region', { name: 'Cambiar afiliación' });
    expect(within(formulario).getByLabelText('CUSPP')).toHaveValue('123456ABCDEF');
    fireEvent.change(within(formulario).getByLabelText('Sistema de pensiones'), { target: { value: 'ONP' } });
    expect(within(formulario).queryByLabelText('CUSPP')).not.toBeInTheDocument();
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar afiliación' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]).toEqual({
      url: '/api/v1/afiliaciones-pension/1',
      metodo: 'PUT',
      cuerpo: { codigoPension: 'ONP', fechaAfiliacion: '2021-03-01' },
    });
  });

  it('muestra el mensaje del servidor y conserva lo escrito', async () => {
    await montar(['RRHH'], { errorEscritura: { accion: 'guardar-afiliacion', status: 404, mensaje: 'El sistema de pensiones ONP no existe.' } });
    fireEvent.click(screen.getByRole('button', { name: 'Afiliar a Salazar Quispe, Maria' }));
    const formulario = await screen.findByRole('region', { name: 'Nueva afiliación' });
    fireEvent.change(within(formulario).getByLabelText('Sistema de pensiones'), { target: { value: 'ONP' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar afiliación' }));
    expect(await within(formulario).findByText('El sistema de pensiones ONP no existe.')).toHaveAttribute('role', 'alert');
    expect(within(formulario).getByLabelText('Sistema de pensiones')).toHaveValue('ONP');
  });

  it('sin RRHH ni ADMIN las acciones quedan deshabilitadas', async () => {
    await montar([]);
    expect(screen.getByRole('button', { name: 'Nueva afiliación' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Afiliar a Salazar Quispe, Maria' })).toBeDisabled();
    expect(screen.getByText('Solo RRHH o ADMIN pueden modificar planillas.')).toBeInTheDocument();
  });
});
