import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { formatearFecha } from '../../lib/formato';
import {
  limpiarContratos,
  montarContratosCargados,
  prepararApiContratos,
  type AccionEscritura,
  type LlamadaContratos,
  type OpcionesApiContratos,
} from './contratosLaborales.testUtils';

afterEach(() => {
  limpiarContratos();
});

// CTR-000008 es un plazo fijo vigente que termina el 2027-05-01.
async function abrirDialogo(
  boton: 'Renovar' | 'Agregar adenda' | 'Anular',
  nombreDialogo: string,
  opciones: OpcionesApiContratos = {},
) {
  const api = prepararApiContratos(opciones);
  await montarContratosCargados(api.fetchMock);
  fireEvent.click(screen.getByRole('button', { name: 'CTR-000008' }));
  await screen.findByRole('heading', { name: 'Rosa Vega Paz' });
  fireEvent.click(screen.getByRole('button', { name: boton }));
  const dialogo = within(await screen.findByRole('dialog', { name: nombreDialogo }));
  return { ...api, dialogo };
}

function errorDe(accion: AccionEscritura, status: number, mensaje: string): OpcionesApiContratos {
  return { errorEscritura: { accion, status, mensaje } };
}

function escrituras(llamadas: LlamadaContratos[]): LlamadaContratos[] {
  return llamadas.filter((llamada) => llamada.metodo === 'POST');
}

describe('diálogo de renovación', () => {
  it('muestra la fecha de inicio derivada, prellena los vigentes y enfoca el primer campo', async () => {
    const { dialogo } = await abrirDialogo('Renovar', 'Renovar contrato');
    expect(dialogo.getByText(`Empieza el ${formatearFecha('2027-05-02')}`)).toBeInTheDocument();
    expect(dialogo.getByLabelText('Modalidad')).toHaveValue('PlazoFijo');
    expect(dialogo.getByLabelText('Modalidad')).toHaveFocus();
    expect(dialogo.getByLabelText('Sueldo básico (PEN)')).toHaveValue('2600');
    expect(dialogo.getByLabelText('Cargo')).toHaveValue('Auxiliar de Logistica');
  });

  it('valida en local sin enviar petición', async () => {
    const { dialogo, llamadas } = await abrirDialogo('Renovar', 'Renovar contrato');
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar renovación' }));
    expect(await dialogo.findByText('La fecha de fin es obligatoria para esta modalidad.')).toBeInTheDocument();
    fireEvent.change(dialogo.getByLabelText('Fecha de fin'), { target: { value: '2027-05-02' } });
    fireEvent.change(dialogo.getByLabelText('Sueldo básico (PEN)'), { target: { value: '0.001' } });
    fireEvent.change(dialogo.getByLabelText('Cargo'), { target: { value: '  ' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar renovación' }));
    expect(await dialogo.findByText('La fecha de fin debe ser posterior a la fecha de inicio.')).toBeInTheDocument();
    expect(dialogo.getByText('El sueldo básico admite como máximo 2 decimales.')).toBeInTheDocument();
    expect(dialogo.getByText('El cargo es obligatorio.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('deshabilita la fecha de fin con Indeterminado', async () => {
    const { dialogo } = await abrirDialogo('Renovar', 'Renovar contrato');
    fireEvent.change(dialogo.getByLabelText('Fecha de fin'), { target: { value: '2028-05-01' } });
    fireEvent.change(dialogo.getByLabelText('Modalidad'), { target: { value: 'Indeterminado' } });
    expect(dialogo.getByLabelText('Fecha de fin')).toBeDisabled();
    expect(dialogo.getByLabelText('Fecha de fin')).toHaveValue('');
  });

  it('renueva con el cuerpo exacto, selecciona el contrato nuevo y cierra el diálogo', async () => {
    const { dialogo, llamadas } = await abrirDialogo('Renovar', 'Renovar contrato');
    fireEvent.change(dialogo.getByLabelText('Fecha de fin'), { target: { value: '2028-05-01' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar renovación' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]).toMatchObject({
      url: '/api/v1/contratos-laborales/8/renovacion',
      cuerpo: { modalidad: 'PlazoFijo', fechaFin: '2028-05-01', sueldoBasico: 2600, cargo: 'Auxiliar de Logistica' },
    });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => {
      expect(llamadas.some((llamada) => llamada.url === '/api/v1/contratos-laborales/9')).toBe(true);
    });
  });

  it('muestra el 409 del servidor dentro del diálogo', async () => {
    const mensaje = 'El contrato CTR-000008 no está vigente y no se puede renovar.';
    const { dialogo } = await abrirDialogo('Renovar', 'Renovar contrato', errorDe('renovacion', 409, mensaje));
    fireEvent.change(dialogo.getByLabelText('Fecha de fin'), { target: { value: '2028-05-01' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar renovación' }));
    expect(await dialogo.findByText(mensaje)).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('dialog', { name: 'Renovar contrato' })).toBeInTheDocument();
  });

  it('Escape cierra el diálogo y devuelve el foco al botón Renovar', async () => {
    await abrirDialogo('Renovar', 'Renovar contrato');
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(screen.getByRole('button', { name: 'Renovar' })).toHaveFocus();
  });
});

describe('diálogo de adenda', () => {
  it('muestra la ayuda, enfoca la fecha y valida sin enviar petición', async () => {
    const { dialogo, llamadas } = await abrirDialogo('Agregar adenda', 'Agregar adenda');
    expect(dialogo.getByText('Deja vacío lo que no cambia.')).toBeInTheDocument();
    expect(dialogo.getByLabelText('Fecha de vigencia')).toHaveFocus();
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar adenda' }));
    expect(await dialogo.findByText('La fecha de vigencia es obligatoria.')).toBeInTheDocument();
    expect(dialogo.getByText('La adenda debe cambiar el cargo o el sueldo básico.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('valida el sueldo y el cargo que lleguen', async () => {
    const { dialogo, llamadas } = await abrirDialogo('Agregar adenda', 'Agregar adenda');
    fireEvent.change(dialogo.getByLabelText('Fecha de vigencia'), { target: { value: '2026-08-01' } });
    fireEvent.change(dialogo.getByLabelText('Sueldo básico (PEN)'), { target: { value: '0' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar adenda' }));
    expect(await dialogo.findByText('El sueldo básico debe ser mayor que cero.')).toBeInTheDocument();
    fireEvent.change(dialogo.getByLabelText('Sueldo básico (PEN)'), { target: { value: '' } });
    fireEvent.change(dialogo.getByLabelText('Cargo'), { target: { value: 'x'.repeat(101) } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar adenda' }));
    expect(await dialogo.findByText('El cargo no puede superar 100 caracteres.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('envía solo los campos con valor', async () => {
    const { dialogo, llamadas } = await abrirDialogo('Agregar adenda', 'Agregar adenda');
    fireEvent.change(dialogo.getByLabelText('Fecha de vigencia'), { target: { value: '2026-08-01' } });
    fireEvent.change(dialogo.getByLabelText('Sueldo básico (PEN)'), { target: { value: '2800' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar adenda' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]?.url).toBe('/api/v1/contratos-laborales/8/adendas');
    expect(escrituras(llamadas)[0]?.cuerpo).toEqual({ fechaVigencia: '2026-08-01', sueldoBasico: 2800 });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it.each([
    [409, 'Ya existe una adenda con fecha de vigencia igual o posterior.'],
    [409, 'El contrato CTR-000008 no está vigente y no admite adendas.'],
    [400, 'La fecha de vigencia debe estar dentro del periodo del contrato.'],
    [400, 'La adenda no cambia el cargo ni el sueldo vigentes en esa fecha.'],
  ])('muestra el error %i del servidor dentro del diálogo: %s', async (status, mensaje) => {
    const { dialogo } = await abrirDialogo('Agregar adenda', 'Agregar adenda', errorDe('adendas', status, mensaje));
    fireEvent.change(dialogo.getByLabelText('Fecha de vigencia'), { target: { value: '2026-08-01' } });
    fireEvent.change(dialogo.getByLabelText('Cargo'), { target: { value: 'Supervisor' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar adenda' }));
    expect(await dialogo.findByText(mensaje)).toHaveAttribute('role', 'alert');
    expect(dialogo.getByLabelText('Cargo')).toHaveValue('Supervisor');
  });

  it('Escape cierra el diálogo y devuelve el foco al botón Agregar adenda', async () => {
    await abrirDialogo('Agregar adenda', 'Agregar adenda');
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(screen.getByRole('button', { name: 'Agregar adenda' })).toHaveFocus();
  });
});

describe('diálogo de anulación', () => {
  it('muestra el aviso, enfoca el motivo y valida el motivo vacío o demasiado largo', async () => {
    const { dialogo, llamadas } = await abrirDialogo('Anular', 'Anular contrato');
    expect(dialogo.getByText('La anulación no se puede deshacer.')).toBeInTheDocument();
    expect(dialogo.getByLabelText('Motivo de anulación')).toHaveFocus();
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar anulación' }));
    expect(await dialogo.findByText('El motivo de anulación es obligatorio.')).toBeInTheDocument();
    fireEvent.change(dialogo.getByLabelText('Motivo de anulación'), { target: { value: 'x'.repeat(301) } });
    expect(dialogo.getByText('301/300')).toBeInTheDocument();
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar anulación' }));
    expect(await dialogo.findByText('El motivo de anulación no puede superar 300 caracteres.')).toBeInTheDocument();
    expect(escrituras(llamadas)).toHaveLength(0);
  });

  it('anula con el motivo recortado y cierra el diálogo', async () => {
    const { dialogo, llamadas } = await abrirDialogo('Anular', 'Anular contrato');
    fireEvent.change(dialogo.getByLabelText('Motivo de anulación'), { target: { value: '  Registro duplicado  ' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar anulación' }));
    await waitFor(() => expect(escrituras(llamadas)).toHaveLength(1));
    expect(escrituras(llamadas)[0]).toMatchObject({
      url: '/api/v1/contratos-laborales/8/anulacion',
      cuerpo: { motivo: 'Registro duplicado' },
    });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('muestra el 409 del servidor dentro del diálogo', async () => {
    const mensaje = 'El contrato CTR-000008 no está vigente y no se puede anular.';
    const { dialogo } = await abrirDialogo('Anular', 'Anular contrato', errorDe('anulacion', 409, mensaje));
    fireEvent.change(dialogo.getByLabelText('Motivo de anulación'), { target: { value: 'Duplicado' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Confirmar anulación' }));
    expect(await dialogo.findByText(mensaje)).toHaveAttribute('role', 'alert');
  });

  it('Escape cierra el diálogo y devuelve el foco al botón Anular', async () => {
    await abrirDialogo('Anular', 'Anular contrato');
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(screen.getByRole('button', { name: 'Anular' })).toHaveFocus();
  });
});
