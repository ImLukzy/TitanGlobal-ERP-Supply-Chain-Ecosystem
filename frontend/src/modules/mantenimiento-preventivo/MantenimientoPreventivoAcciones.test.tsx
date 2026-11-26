import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  abrirDetalle,
  cuerpo,
  elegirCentro,
  esperarCargaInicialMantenimientos,
  fechaOcupada,
  llamadas,
  montar,
  rutaApi,
  type FetchMock,
} from './mantenimientoPreventivo.testUtils';

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

async function abrirNuevo(fetchMock: FetchMock): Promise<void> {
  await esperarCargaInicialMantenimientos(fetchMock);
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo mantenimiento' }));
  await screen.findByRole('heading', { name: 'Nuevo mantenimiento' });
}

async function completarAlta(): Promise<void> {
  await elegirCentro('Centro de trabajo');
  fireEvent.change(screen.getByLabelText('Descripción'), { target: { value: '  Cambio de rodamientos  ' } });
  fireEvent.change(screen.getByLabelText('Fecha programada'), { target: { value: '2999-03-01' } });
  fireEvent.change(screen.getByLabelText('Duración planificada (horas)'), { target: { value: '2,5' } });
  fireEvent.change(screen.getByLabelText('Frecuencia en días (opcional)'), { target: { value: '30' } });
  fireEvent.change(screen.getByLabelText('Responsable (opcional)'), { target: { value: ' Luis Rojas ' } });
}

async function abrirDialogo(fetchMock: FetchMock, accion: 'Completar' | 'Cancelar'): Promise<void> {
  await esperarCargaInicialMantenimientos(fetchMock);
  await abrirDetalle();
  fireEvent.click(screen.getByRole('button', { name: accion }));
  await screen.findByRole('dialog');
}

describe('pantalla de mantenimiento preventivo: formulario y transiciones', () => {
  it('valida en local con los mensajes literales y no llama a la API', async () => {
    const fetchMock = montar();
    await abrirNuevo(fetchMock);
    fireEvent.change(screen.getByLabelText('Fecha programada'), { target: { value: '2020-01-01' } });
    fireEvent.change(screen.getByLabelText('Duración planificada (horas)'), { target: { value: '168.01' } });
    fireEvent.change(screen.getByLabelText('Frecuencia en días (opcional)'), { target: { value: '3651' } });
    fireEvent.change(screen.getByLabelText('Responsable (opcional)'), { target: { value: 'x'.repeat(101) } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar mantenimiento' }));

    for (const mensaje of [
      'El identificador del centro de trabajo debe ser mayor que 0.',
      'La descripción del mantenimiento es obligatoria.',
      'La fecha programada no puede ser anterior a hoy.',
      'La duración debe ser mayor que 0 y como máximo 168 horas.',
      'La frecuencia debe estar entre 1 y 3650 días.',
      'El responsable no puede superar 100 caracteres.',
    ]) {
      expect(await screen.findByText(mensaje)).toBeInTheDocument();
    }
    expect(screen.getByLabelText('Descripción')).toHaveAttribute('aria-invalid', 'true');

    fireEvent.change(screen.getByLabelText('Descripción'), { target: { value: 'x'.repeat(251) } });
    fireEvent.change(screen.getByLabelText('Fecha programada'), { target: { value: '' } });
    fireEvent.change(screen.getByLabelText('Duración planificada (horas)'), { target: { value: '1.234' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar mantenimiento' }));
    for (const mensaje of [
      'La descripción del mantenimiento no puede superar 250 caracteres.',
      'La fecha programada es obligatoria.',
      'La duración admite como máximo 2 decimales.',
    ]) {
      expect(await screen.findByText(mensaje)).toBeInTheDocument();
    }
    expect(llamadas(fetchMock, rutaApi, 'POST')).toHaveLength(0);
  });

  it('propone hoy en hora de Perú aunque en UTC ya sea mañana', async () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-11-26T00:30:00Z'));
    const fetchMock = montar();
    await abrirNuevo(fetchMock);

    expect(screen.getByLabelText('Fecha programada')).toHaveValue('2026-11-25');
    expect(screen.getByLabelText('Fecha programada')).toHaveAttribute('min', '2026-11-25');
  });

  it('programa con el cuerpo exacto, pide solo centros activos y abre el detalle', async () => {
    const fetchMock = montar();
    await abrirNuevo(fetchMock);
    await completarAlta();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar mantenimiento' }));

    expect(await screen.findByText('Mantenimiento programado.')).toBeInTheDocument();
    expect(cuerpo(fetchMock, rutaApi, 'POST')).toEqual({
      centroTrabajoId: 4,
      descripcion: 'Cambio de rodamientos',
      fechaProgramada: '2999-03-01',
      duracionHorasPlanificada: 2.5,
      frecuenciaDias: 30,
      responsable: 'Luis Rojas',
    });
    expect(llamadas(fetchMock, '/api/v1/centros-trabajo')).toEqual(['/api/v1/centros-trabajo?activo=true&pagina=1&tamanoPagina=20']);
    expect(await screen.findByRole('heading', { name: 'Mantenimiento MP-000007' })).toBeInTheDocument();
  });

  it('conserva lo escrito y muestra el 409 de fecha repetida', async () => {
    const fetchMock = montar({ conflictoAlta: true });
    await abrirNuevo(fetchMock);
    await completarAlta();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar mantenimiento' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(fechaOcupada);
    expect(screen.getByLabelText('Descripción')).toHaveValue('  Cambio de rodamientos  ');
    expect(screen.getByLabelText('Fecha programada')).toHaveValue('2999-03-01');
    expect(screen.getByLabelText('Centro de trabajo')).toHaveTextContent('CT-LIM-L01 · Línea de ensamble');
  });

  it('edita una programada sin el centro y sin enviar los opcionales vacíos', async () => {
    const fetchMock = montar();
    await esperarCargaInicialMantenimientos(fetchMock);
    await abrirDetalle();
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    await screen.findByRole('heading', { name: 'Editar mantenimiento MP-000007' });

    expect(screen.queryByRole('button', { name: 'Seleccionar centro' })).not.toBeInTheDocument();
    expect(screen.getByText('CT-LIM-L01 · Línea de ensamble')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Fecha programada'), { target: { value: '2999-01-10' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar mantenimiento' }));

    expect(await screen.findByText('Mantenimiento actualizado.')).toBeInTheDocument();
    expect(cuerpo(fetchMock, `${rutaApi}/7`, 'PUT')).toEqual({
      descripcion: 'Cambio de rodamientos',
      fechaProgramada: '2999-01-10',
      duracionHorasPlanificada: 2.5,
    });
  });

  it('inicia sin diálogo ni cuerpo y luego ofrece completar', async () => {
    const fetchMock = montar();
    await esperarCargaInicialMantenimientos(fetchMock);
    await abrirDetalle();
    fireEvent.click(screen.getByRole('button', { name: 'Iniciar' }));

    expect(await screen.findByText('Mantenimiento iniciado.')).toBeInTheDocument();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(llamadas(fetchMock, `${rutaApi}/7/iniciar`, 'POST')).toHaveLength(1);
    expect(cuerpo(fetchMock, `${rutaApi}/7/iniciar`, 'POST')).toBeUndefined();
    expect(await screen.findByRole('button', { name: 'Completar' })).toBeInTheDocument();
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Mantenimiento MP-000007' })).toHaveFocus());
  });

  it('completa con las horas reales y valida el cierre en local', async () => {
    const fetchMock = montar({ estadoInicial: 'EnEjecucion' });
    await abrirDialogo(fetchMock, 'Completar');
    expect(screen.getByLabelText('Horas reales')).toHaveFocus();
    fireEvent.change(screen.getByLabelText('Horas reales'), { target: { value: '0' } });
    fireEvent.change(screen.getByLabelText('Observaciones (opcional)'), { target: { value: 'x'.repeat(501) } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cierre' }));
    expect(await screen.findByText('Las horas reales deben ser mayores que 0 y como máximo 168.')).toBeInTheDocument();
    expect(screen.getByText('Las observaciones no pueden superar 500 caracteres.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Horas reales'), { target: { value: '3.255' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cierre' }));
    expect(await screen.findByText('Las horas reales admiten como máximo 2 decimales.')).toBeInTheDocument();
    expect(llamadas(fetchMock, `${rutaApi}/7/completar`, 'POST')).toHaveLength(0);

    fireEvent.change(screen.getByLabelText('Horas reales'), { target: { value: '3,25' } });
    fireEvent.change(screen.getByLabelText('Observaciones (opcional)'), { target: { value: '   ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cierre' }));
    expect(await screen.findByText('Mantenimiento completado.')).toBeInTheDocument();
    expect(cuerpo(fetchMock, `${rutaApi}/7/completar`, 'POST')).toEqual({ horasReales: 3.25 });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('cancela con el motivo y lo exige en local', async () => {
    const fetchMock = montar();
    await abrirDialogo(fetchMock, 'Cancelar');
    expect(screen.getByLabelText('Motivo de cancelación')).toHaveFocus();
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cancelación' }));
    expect(await screen.findByText('El motivo de cancelación es obligatorio.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Motivo de cancelación'), { target: { value: 'x'.repeat(251) } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cancelación' }));
    expect(await screen.findByText('El motivo de cancelación no puede superar 250 caracteres.')).toBeInTheDocument();
    expect(llamadas(fetchMock, `${rutaApi}/7/cancelar`, 'POST')).toHaveLength(0);

    fireEvent.change(screen.getByLabelText('Motivo de cancelación'), { target: { value: ' Línea detenida ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar cancelación' }));
    expect(await screen.findByText('Mantenimiento cancelado.')).toBeInTheDocument();
    expect(cuerpo(fetchMock, `${rutaApi}/7/cancelar`, 'POST')).toEqual({ motivo: 'Línea detenida' });
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Mantenimiento MP-000007' })).toHaveFocus());
  });

  it('cierra con Escape sin enviar nada y devuelve el foco al botón de origen', async () => {
    const fetchMock = montar();
    await abrirDialogo(fetchMock, 'Cancelar');
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(screen.getByRole('button', { name: 'Cancelar' })).toHaveFocus());
    expect(llamadas(fetchMock, `${rutaApi}/7/cancelar`, 'POST')).toHaveLength(0);
  });

  it('mantiene el foco dentro del diálogo con Tab y Shift+Tab', async () => {
    const fetchMock = montar({ estadoInicial: 'EnEjecucion' });
    await abrirDialogo(fetchMock, 'Completar');
    const confirmar = screen.getByRole('button', { name: 'Confirmar cierre' });
    confirmar.focus();
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Tab' });
    expect(screen.getByLabelText('Horas reales')).toHaveFocus();
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Tab', shiftKey: true });
    expect(confirmar).toHaveFocus();
  });
});
