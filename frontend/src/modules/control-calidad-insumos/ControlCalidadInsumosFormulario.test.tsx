import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  abrirDetalle,
  elegir,
  esperarCargaInicialInspecciones,
  llamadas,
  loteRepetido,
  montar,
  rutaApi,
  type FetchMock,
} from './controlCalidadInsumos.testUtils';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

function cuerpo(fetchMock: FetchMock, ruta: string, metodo: string): unknown {
  const llamada = fetchMock.mock.calls.find((call) => String(call[0]) === ruta && call[1]?.method === metodo);
  return llamada === undefined ? undefined : JSON.parse(String(llamada[1]?.body));
}

async function abrirNueva(fetchMock: FetchMock): Promise<void> {
  await esperarCargaInicialInspecciones(fetchMock);
  fireEvent.click(screen.getByRole('button', { name: 'Nueva inspección' }));
  await screen.findByRole('heading', { name: 'Nueva inspección' });
}

async function completarAlta(): Promise<void> {
  await elegir('Insumo', 'Mouse Óptico · #3');
  await elegir('Proveedor', 'Andes Suministros S.A.C. · #1');
  fireEvent.change(screen.getByLabelText('Lote'), { target: { value: 'mo-2026-001' } });
  fireEvent.change(screen.getByLabelText('Fecha de inspección'), { target: { value: '2026-11-02' } });
  fireEvent.change(screen.getByLabelText('Cantidad recibida'), { target: { value: '500,125' } });
  fireEvent.change(screen.getByLabelText('Cantidad de la muestra'), { target: { value: '50' } });
  fireEvent.change(screen.getByLabelText('Unidad de medida'), { target: { value: ' unidad ' } });
  fireEvent.change(screen.getByLabelText('Tolerancia de defectos (%)'), { target: { value: '5' } });
  fireEvent.change(screen.getByLabelText('Inspector'), { target: { value: ' Rosa Díaz ' } });
}

async function abrirDialogo(fetchMock: FetchMock, accion: 'Dictaminar' | 'Anular'): Promise<void> {
  await esperarCargaInicialInspecciones(fetchMock);
  await abrirDetalle();
  fireEvent.click(screen.getByRole('button', { name: accion }));
  await screen.findByRole('dialog');
}

describe('pantalla de control de calidad de insumos: formulario y diálogos', () => {
  it('valida en local con los mensajes literales y no llama a la API', async () => {
    const fetchMock = montar();
    await abrirNueva(fetchMock);
    fireEvent.change(screen.getByLabelText('Fecha de inspección'), { target: { value: '2999-01-01' } });
    fireEvent.change(screen.getByLabelText('Cantidad recibida'), { target: { value: '10' } });
    fireEvent.change(screen.getByLabelText('Cantidad de la muestra'), { target: { value: '10,0001' } });
    fireEvent.change(screen.getByLabelText('Tolerancia de defectos (%)'), { target: { value: '100.01' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar inspección' }));

    for (const mensaje of [
      'El identificador del producto debe ser mayor que 0.',
      'El identificador del proveedor debe ser mayor que 0.',
      'El lote es obligatorio.',
      'La fecha de inspección no puede ser posterior a hoy.',
      'La cantidad de la muestra admite como máximo 3 decimales.',
      'La unidad de medida es obligatoria.',
      'La tolerancia de defectos debe estar entre 0 y 100.',
      'El inspector es obligatorio.',
    ]) {
      expect(await screen.findByText(mensaje)).toBeInTheDocument();
    }
    expect(screen.getByLabelText('Lote')).toHaveAttribute('aria-invalid', 'true');
    expect(llamadas(fetchMock, rutaApi, 'POST')).toHaveLength(0);
  });

  it('propone hoy en hora de Perú aunque en UTC ya sea mañana', async () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-11-25T00:30:00Z'));
    const fetchMock = montar();
    await abrirNueva(fetchMock);

    expect(screen.getByLabelText('Fecha de inspección')).toHaveValue('2026-11-24');
    expect(screen.getByLabelText('Fecha de inspección')).toHaveAttribute('max', '2026-11-24');
  });

  it('registra con el cuerpo exacto, el lote en mayúsculas y refresca el detalle', async () => {
    const fetchMock = montar();
    await abrirNueva(fetchMock);
    await completarAlta();
    expect(screen.getByLabelText('Lote')).toHaveValue('MO-2026-001');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar inspección' }));

    expect(await screen.findByText('Inspección registrada.')).toBeInTheDocument();
    expect(cuerpo(fetchMock, rutaApi, 'POST')).toEqual({
      productoId: 3,
      proveedorId: 1,
      lote: 'MO-2026-001',
      fechaInspeccion: '2026-11-02',
      cantidadRecibida: 500.125,
      cantidadMuestra: 50,
      unidadMedida: 'unidad',
      toleranciaDefectosPorcentaje: 5,
      inspector: 'Rosa Díaz',
    });
    expect(await screen.findByRole('heading', { name: 'Inspección CCI-000007' })).toBeInTheDocument();
  });

  it('conserva lo escrito y muestra el 409 de la API', async () => {
    const fetchMock = montar({ conflictoAlta: true });
    await abrirNueva(fetchMock);
    await completarAlta();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar inspección' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(loteRepetido);
    expect(screen.getByLabelText('Lote')).toHaveValue('MO-2026-001');
    expect(screen.getByLabelText('Inspector')).toHaveValue(' Rosa Díaz ');
    expect(screen.getByLabelText('Insumo')).toHaveTextContent('Mouse Óptico');
  });

  it('edita una pendiente con sus datos cargados', async () => {
    const fetchMock = montar();
    await esperarCargaInicialInspecciones(fetchMock);
    await abrirDetalle();
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    await screen.findByRole('heading', { name: 'Editar inspección CCI-000007' });
    expect(screen.getByLabelText('Cantidad recibida')).toHaveValue('500');
    fireEvent.change(screen.getByLabelText('Inspector'), { target: { value: 'Luis Paredes' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar inspección' }));

    expect(await screen.findByText('Inspección actualizada.')).toBeInTheDocument();
    expect(cuerpo(fetchMock, `${rutaApi}/7`, 'PUT')).toMatchObject({ productoId: 3, proveedorId: 1, inspector: 'Luis Paredes', cantidadMuestra: 50 });
  });

  it('calcula el porcentaje y solo deja rechazar sobre la tolerancia', async () => {
    const fetchMock = montar();
    await abrirDialogo(fetchMock, 'Dictaminar');
    const aprobada = screen.getByRole('radio', { name: 'Aprobada' });
    fireEvent.click(aprobada);
    fireEvent.change(screen.getByLabelText('Cantidad defectuosa'), { target: { value: '2,5' } });
    expect(screen.getByText('Defectos: 5.00 %. Dentro de la tolerancia.')).toBeInTheDocument();
    expect(aprobada).toBeEnabled();

    fireEvent.change(screen.getByLabelText('Cantidad defectuosa'), { target: { value: '6.25' } });
    expect(screen.getByText('Defectos: 12.50 %. Supera la tolerancia: solo puede rechazarse.')).toBeInTheDocument();
    expect(aprobada).toBeDisabled();
    expect(aprobada).not.toBeChecked();
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar dictamen' }));
    expect(await screen.findByText('El resultado debe ser Aprobada o Rechazada.')).toBeInTheDocument();
    expect(llamadas(fetchMock, `${rutaApi}/7/dictaminar`, 'POST')).toHaveLength(0);

    fireEvent.click(screen.getByRole('radio', { name: 'Rechazada' }));
    fireEvent.change(screen.getByLabelText('Observaciones (opcional)'), { target: { value: '  Carcasas rotas  ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar dictamen' }));
    expect(await screen.findByText('Inspección dictaminada.')).toBeInTheDocument();
    expect(cuerpo(fetchMock, `${rutaApi}/7/dictaminar`, 'POST')).toEqual({
      resultado: 'Rechazada',
      cantidadDefectuosa: 6.25,
      observaciones: 'Carcasas rotas',
    });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('anula con el motivo y lo exige en local', async () => {
    const fetchMock = montar();
    await abrirDialogo(fetchMock, 'Anular');
    expect(screen.getByLabelText('Motivo de anulación')).toHaveFocus();
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar anulación' }));
    expect(await screen.findByText('El motivo de anulación es obligatorio.')).toBeInTheDocument();
    expect(llamadas(fetchMock, `${rutaApi}/7/anular`, 'POST')).toHaveLength(0);

    fireEvent.change(screen.getByLabelText('Motivo de anulación'), { target: { value: ' Registro duplicado ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar anulación' }));
    expect(await screen.findByText('Inspección anulada.')).toBeInTheDocument();
    expect(cuerpo(fetchMock, `${rutaApi}/7/anular`, 'POST')).toEqual({ motivo: 'Registro duplicado' });
  });

  it('cierra con Escape sin enviar nada y devuelve el foco al botón de origen', async () => {
    const fetchMock = montar();
    await abrirDialogo(fetchMock, 'Dictaminar');
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(screen.getByRole('button', { name: 'Dictaminar' })).toHaveFocus());
    expect(llamadas(fetchMock, `${rutaApi}/7/dictaminar`, 'POST')).toHaveLength(0);
  });

  it('mantiene el foco dentro del diálogo con Tab y Shift+Tab', async () => {
    const fetchMock = montar();
    await abrirDialogo(fetchMock, 'Anular');
    const confirmar = screen.getByRole('button', { name: 'Confirmar anulación' });
    confirmar.focus();
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Tab' });
    expect(screen.getByLabelText('Motivo de anulación')).toHaveFocus();
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Tab', shiftKey: true });
    expect(confirmar).toHaveFocus();
  });
});
