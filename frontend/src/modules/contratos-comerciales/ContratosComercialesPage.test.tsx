import { afterEach, describe, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { contratosMock, esperarCargaInicial, limpiarContratos, prepararApiContratos, renderContratos } from './contratos.testUtils';

afterEach(limpiarContratos);

async function montarCargado(fetchMock: ReturnType<typeof prepararApiContratos>['fetchMock']): Promise<void> {
  vi.useFakeTimers();
  renderContratos();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock);
}

describe('ContratosComerciales: listado, consumo y acciones', () => {
  it('A3 carga resumen y listado una sola vez', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarCargado(fetchMock);
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/contratos-comerciales?'))).toHaveLength(1);
    expect(screen.getByText('Contratos por condición de pago')).toBeInTheDocument();
  });

  it('A3 muestra medidor accesible, texto de consumo, excedido, vacío y error', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'CC-DEV-001' }));
    expect(await screen.findByText('Contrato anual')).toBeInTheDocument();
    const consumo = screen.getByRole('meter', { name: 'Consumo de CC-DEV-001' });
    expect(consumo).toHaveAttribute('aria-valuenow', '45');
    expect(consumo).toHaveAttribute('aria-valuetext', expect.stringContaining('9,000.00 de'));
    expect(consumo).toHaveAttribute('aria-valuetext', expect.stringContaining('20,000.00'));
    expect(screen.getByText('45.00 %')).toBeInTheDocument();
    expect(screen.getAllByText('Crédito 30 días')).toHaveLength(2);
    expect(screen.queryByText('Credito30')).not.toBeInTheDocument();
    cleanup();
    vi.unstubAllGlobals();
    const empty = prepararApiContratos({ listaVacia: true });
    renderContratos();
    expect(await screen.findByText('No hay contratos que coincidan con los filtros.')).toBeInTheDocument();
    cleanup();
    vi.unstubAllGlobals();
    const failed = prepararApiContratos({ errorLista: true });
    renderContratos();
    expect(await screen.findByText('Falló el listado.')).toBeInTheDocument();
    expect(empty.fetchMock).toHaveBeenCalled();
    expect(failed.fetchMock).toHaveBeenCalled();
  });

  it('A3 filtra por estado, cliente y vencidos, regresando a página 1', async () => {
    const { fetchMock, llamadas } = prepararApiContratos();
    await montarCargado(fetchMock);
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Vigente' } });
    fireEvent.change(screen.getByLabelText('Cliente'), { target: { value: '1' } });
    fireEvent.click(screen.getByLabelText('Solo vencidos'));
    await waitFor(() => expect(llamadas.some((item) => item.url.includes('estado=Vigente') && item.url.includes('clienteId=1') && item.url.includes('vencidos=true') && item.url.includes('pagina=1'))).toBe(true));
  });

  it('A3 muestra consumo excedido en la tabla y saldo negativo en detalle', async () => {
    const { fetchMock } = prepararApiContratos();
    await montarCargado(fetchMock);
    expect(screen.getByText(/Excedido/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'CC-DEV-004' }));
    expect(await screen.findByText(/S\/ 250\.00/)).toBeInTheDocument();
  });

  it('A4 valida campos localmente sin enviar y crea contratos válidos', async () => {
    const { fetchMock, llamadas } = prepararApiContratos();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo contrato' }));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar contrato' }));
    expect(screen.getByText('Selecciona un cliente.')).toBeInTheDocument();
    expect(screen.getByText('El título es obligatorio.')).toBeInTheDocument();
    expect(llamadas.filter((item) => item.init.method === 'POST' && item.url.endsWith('/contratos-comerciales'))).toHaveLength(0);
    const dialogo = within(screen.getByRole('dialog'));
    fireEvent.change(dialogo.getByLabelText('Cliente'), { target: { value: '1' } });
    fireEvent.change(dialogo.getByLabelText('Título'), { target: { value: 'T'.repeat(201) } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar contrato' }));
    expect(screen.getByText('El título no puede superar 200 caracteres.')).toBeInTheDocument();
    fireEvent.change(dialogo.getByLabelText('Título'), { target: { value: 'Convenio 2026' } });
    fireEvent.change(dialogo.getByLabelText('Monto comprometido'), { target: { value: '5000' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar contrato' }));
    await waitFor(() => expect(llamadas.some((item) => item.init.method === 'POST' && item.url.endsWith('/contratos-comerciales'))).toBe(true));
  });

  it('A4 valida fechas, monto y observaciones por campo y conserva los datos ante 400', async () => {
    const { fetchMock, llamadas } = prepararApiContratos({ errorCrear: true });
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo contrato' }));
    const dialogo = within(screen.getByRole('dialog'));
    fireEvent.change(dialogo.getByLabelText('Cliente'), { target: { value: '1' } });
    fireEvent.change(dialogo.getByLabelText('Título'), { target: { value: 'Acuerdo anual' } });
    fireEvent.change(dialogo.getByLabelText('Fecha de fin'), { target: { value: '2025-01-01' } });
    fireEvent.change(dialogo.getByLabelText('Monto comprometido'), { target: { value: '0' } });
    fireEvent.change(dialogo.getByLabelText('Observaciones'), { target: { value: 'a'.repeat(501) } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar contrato' }));
    expect(screen.getByText('La fecha de fin debe ser posterior a la de inicio.')).toBeInTheDocument();
    expect(screen.getByText('El monto comprometido debe ser mayor que 0.')).toBeInTheDocument();
    expect(screen.getByText('Las observaciones no pueden superar 500 caracteres.')).toBeInTheDocument();
    expect(llamadas.filter((item) => item.init.method === 'POST' && item.url.endsWith('/contratos-comerciales'))).toHaveLength(0);
    fireEvent.change(dialogo.getByLabelText('Fecha de fin'), { target: { value: '2026-12-31' } });
    fireEvent.change(dialogo.getByLabelText('Monto comprometido'), { target: { value: '5000' } });
    fireEvent.change(dialogo.getByLabelText('Observaciones'), { target: { value: 'Se conserva el formulario.' } });
    fireEvent.click(dialogo.getByRole('button', { name: 'Guardar contrato' }));
    expect(await screen.findByText('El cliente 9999 no existe.')).toBeInTheDocument();
    expect(dialogo.getByLabelText('Título')).toHaveValue('Acuerdo anual');
  });

  it('A4 permite editar con PUT y activar sin cuerpo; presenta 409 en el diálogo', async () => {
    const { fetchMock, llamadas } = prepararApiContratos({ errorActivar: true });
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'CC-DEV-002' }));
    expect(await screen.findByRole('button', { name: 'Editar' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    fireEvent.change(screen.getByLabelText('Título'), { target: { value: 'Acuerdo renovado' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar contrato' }));
    await waitFor(() => expect(llamadas.some((item) => item.init.method === 'PUT' && item.url.endsWith('/contratos-comerciales/2'))).toBe(true));
    fireEvent.click(screen.getByRole('button', { name: 'CC-DEV-002' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Activar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar activar' }));
    expect(await screen.findByText('El cliente ya tiene el contrato vigente CC-DEV-001 en ese periodo.')).toBeInTheDocument();
    expect(llamadas.some((item) => item.url.endsWith('/contratos-comerciales/2/activar') && item.init.body === undefined)).toBe(true);
    expect(fetchMock).toHaveBeenCalled();
  });

  it('A4 cancela con motivo y permite Escape del diálogo sin petición', async () => {
    const { fetchMock, llamadas } = prepararApiContratos();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'CC-DEV-001' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Cancelar contrato' }));
    const antes = llamadas.length;
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cancelar' }));
    expect(screen.getByText('El motivo de cancelación es obligatorio.')).toBeInTheDocument();
    expect(llamadas).toHaveLength(antes);
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(llamadas).toHaveLength(antes);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Cancelar contrato' })).toHaveFocus());
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar contrato' }));
    fireEvent.change(screen.getByLabelText('Motivo de cancelación'), { target: { value: 'Cambio de condiciones' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cancelar' }));
    await waitFor(() => expect(llamadas.some((item) => item.url.endsWith('/contratos-comerciales/1/cancelar') && item.init.body === '{"motivo":"Cambio de condiciones"}')).toBe(true));
  });

  it('A4 finaliza vigentes y elimina borradores; estados cerrados son de solo lectura', async () => {
    const { llamadas, fetchMock } = prepararApiContratos();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'CC-DEV-001' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Finalizar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar finalizar' }));
    await waitFor(() => expect(llamadas.some((item) => item.url.endsWith('/contratos-comerciales/1/finalizar'))).toBe(true));
    fireEvent.click(screen.getByRole('button', { name: 'CC-DEV-002' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Eliminar' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar eliminar' }));
    await waitFor(() => expect(llamadas.some((item) => item.init.method === 'DELETE' && item.url.endsWith('/contratos-comerciales/2'))).toBe(true));
    fireEvent.click(screen.getByRole('button', { name: 'CC-DEV-003' }));
    await screen.findByText(/El cliente desistió del acuerdo/);
    expect(screen.queryByRole('button', { name: 'Activar' })).not.toBeInTheDocument();
    expect(contratosMock).toHaveLength(4);
  });
});
