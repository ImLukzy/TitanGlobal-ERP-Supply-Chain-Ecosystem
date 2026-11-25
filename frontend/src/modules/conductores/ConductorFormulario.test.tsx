import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { conductoresBase, esperarCargaConductores, fechaEnDias, prepararApiConductores, renderConductores } from './conductores.testUtils';

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

async function abrirFormulario(errorGuardar?: 400 | 409) {
  const api = prepararApiConductores({ errorGuardar });
  renderConductores();
  await esperarCargaConductores();
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo conductor' }));
  const formulario = within(screen.getByRole('region', { name: 'Nuevo conductor' }));
  fireEvent.change(formulario.getByLabelText('Nombre'), { target: { value: 'Conductor QA' } });
  fireEvent.change(formulario.getByLabelText('Documento de licencia'), { target: { value: 'PLT-QA-01' } });
  fireEvent.change(formulario.getByLabelText('Vencimiento de licencia'), { target: { value: fechaEnDias(365).slice(0, 10) } });
  return { ...api, formulario };
}

describe('Formulario de conductores', () => {
  it.each([
    ['Nombre', '', 'El nombre es obligatorio.'],
    ['Nombre', 'N'.repeat(101), 'El nombre no puede superar 100 caracteres.'],
    ['Documento de licencia', ' ', 'El documento de licencia es obligatorio.'],
    ['Documento de licencia', 'L'.repeat(21), 'El documento de licencia no puede superar 20 caracteres.'],
    ['Vencimiento de licencia', '', 'La fecha de vencimiento de la licencia no es válida.'],
    ['Vencimiento de licencia', '2020-01-01', 'La fecha de vencimiento de la licencia no es válida.'],
    ['Observaciones (opcional)', 'O'.repeat(501), 'Las observaciones no pueden superar 500 caracteres.'],
  ])('valida %s sin enviar datos', async (campo, valor, mensaje) => {
    const { formulario, fetchMock } = await abrirFormulario();
    const input = formulario.getByLabelText(campo);
    fireEvent.change(input, { target: { value: valor } });
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar conductor' }));
    expect(formulario.getByText(mensaje)).toBeInTheDocument();
    expect(input).toHaveAttribute('aria-invalid', 'true');
    expect(input).toHaveAccessibleDescription(mensaje);
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'POST')).toBe(false);
  });

  it('crea recortando textos, conservando mayúsculas y enviando fecha UTC', async () => {
    const { formulario, fetchMock, filas } = await abrirFormulario();
    expect(formulario.getByLabelText('Habilitado')).toBeChecked();
    expect(formulario.getByLabelText('Activo')).toBeChecked();
    fireEvent.change(formulario.getByLabelText('Nombre'), { target: { value: ' Conductor QA ' } });
    fireEvent.change(formulario.getByLabelText('Documento de licencia'), { target: { value: ' plt-QA-01 ' } });
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar conductor' }));
    await screen.findByRole('heading', { name: 'Conductor QA' });
    const llamada = fetchMock.mock.calls.find((call) => call[1]?.method === 'POST');
    expect(JSON.parse(String(llamada?.[1]?.body))).toEqual({ nombre: 'Conductor QA', documentoLicencia: 'plt-QA-01', vencimientoLicencia: `${fechaEnDias(365).slice(0, 10)}T00:00:00Z`, habilitado: true, activo: true });
    expect(filas).toHaveLength(4);
  });

  it.each([400, 409] as const)('mantiene valores y el mensaje literal ante %s', async (codigo) => {
    const { formulario } = await abrirFormulario(codigo);
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar conductor' }));
    await formulario.findByRole('alert');
    expect(formulario.getByLabelText('Documento de licencia')).toHaveValue('PLT-QA-01');
    expect(formulario.getByLabelText('Nombre')).toHaveValue('Conductor QA');
    expect(formulario.getByRole('alert')).toHaveTextContent(codigo === 409 ? 'Ya existe un conductor con el documento de licencia PLT-QA-01.' : 'La fecha de vencimiento de la licencia no es válida.');
  });

  it('desactiva una licencia vencida sin perder el instante guardado ni el permiso manual', async () => {
    const { fetchMock } = prepararApiConductores();
    renderConductores();
    await esperarCargaConductores();
    fireEvent.click(screen.getByRole('button', { name: 'PLT-0002' }));
    await screen.findByRole('heading', { name: 'Maria Quispe' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar conductor' }));
    const formulario = within(screen.getByRole('region', { name: 'Editar conductor' }));
    expect(formulario.getByLabelText('Habilitado')).toBeChecked();
    expect(formulario.getByLabelText('Vencimiento de licencia')).toHaveValue(conductoresBase[1]!.vencimientoLicencia.slice(0, 10));
    fireEvent.click(formulario.getByLabelText('Activo'));
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar conductor' }));
    await screen.findByText('Inactivo');
    const llamada = fetchMock.mock.calls.find((call) => call[1]?.method === 'PUT');
    expect(String(llamada?.[0])).toBe('/api/v1/conductores/2');
    expect(JSON.parse(String(llamada?.[1]?.body))).toMatchObject({ vencimientoLicencia: conductoresBase[1]!.vencimientoLicencia, habilitado: true, activo: false });
  });

  it('rechaza cambiar una fecha vencida por otra pasada aunque se desactive', async () => {
    const { fetchMock } = prepararApiConductores();
    renderConductores();
    await esperarCargaConductores();
    fireEvent.click(screen.getByRole('button', { name: 'PLT-0002' }));
    await screen.findByRole('heading', { name: 'Maria Quispe' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar conductor' }));
    const formulario = within(screen.getByRole('region', { name: 'Editar conductor' }));
    fireEvent.change(formulario.getByLabelText('Vencimiento de licencia'), { target: { value: '2020-01-01' } });
    fireEvent.click(formulario.getByLabelText('Activo'));
    fireEvent.click(formulario.getByRole('button', { name: 'Guardar conductor' }));
    expect(formulario.getByText('La fecha de vencimiento de la licencia no es válida.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'PUT')).toBe(false);
  });

  it('no duplica el envío y deshabilita los controles durante la petición', async () => {
    const { formulario, fetchMock } = await abrirFormulario();
    const responder = fetchMock.getMockImplementation()!;
    let completar: () => void = () => { throw new Error('Petición no iniciada'); };
    fetchMock.mockImplementation(async (input, init) => {
      if (init?.method === 'POST') { await new Promise<void>((resolve) => { completar = resolve; }); }
      return responder(input, init);
    });
    const boton = formulario.getByRole('button', { name: 'Guardar conductor' });
    fireEvent.click(boton);
    fireEvent.submit(boton.closest('form')!);
    expect(formulario.getByRole('button', { name: 'Guardando…' })).toBeDisabled();
    expect(formulario.getByLabelText('Nombre')).toBeDisabled();
    expect(fetchMock.mock.calls.filter((call) => call[1]?.method === 'POST')).toHaveLength(1);
    await act(async () => { completar(); });
    await waitFor(() => expect(screen.queryByRole('region', { name: 'Nuevo conductor' })).not.toBeInTheDocument());
  });

  it('devuelve el foco al botón de edición al cancelar', async () => {
    prepararApiConductores();
    renderConductores();
    await esperarCargaConductores();
    const boton = screen.getByRole('button', { name: 'Editar conductor' });
    fireEvent.click(boton);
    fireEvent.click(within(screen.getByRole('region', { name: 'Editar conductor' })).getByRole('button', { name: 'Cancelar' }));
    await waitFor(() => expect(boton).toHaveFocus());
  });
});
