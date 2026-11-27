import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { EvaluacionProveedoresPage } from './EvaluacionProveedoresPage';
import { cambiarValor, esperarCargaInicial, llamadasLista, prepararApi, renderEnRouter } from './evaluaciones.testUtils';

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date('2026-11-15T12:00:00'));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

const nombreFila2 = 'Ver evaluación de Andes Suministros S.A.C. del periodo 2026-10';

async function abrirFormularioNuevo(): Promise<void> {
  await esperarCargaInicial();
  fireEvent.click(screen.getByRole('button', { name: 'Nueva evaluación' }));
  await screen.findByRole('heading', { name: 'Nueva evaluación' });
}

function cambiarPeriodoFormulario(valor: string): void {
  fireEvent.change(screen.getByLabelText('Periodo', { selector: '#evaluacion-periodo' }), { target: { value: valor } });
}

function llenarPuntajes(cumplimiento: string, calidad: string, plazos: string): void {
  cambiarValor('Puntaje de cumplimiento', cumplimiento);
  cambiarValor('Puntaje de calidad', calidad);
  cambiarValor('Puntaje de plazos', plazos);
}

describe('pantalla de evaluación de proveedores: detalle', () => {
  it('A4 muestra el detalle completo y «Sin observaciones» cuando no hay', async () => {
    prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await esperarCargaInicial();
    fireEvent.click(screen.getByRole('button', { name: nombreFila2 }));
    expect(await screen.findByRole('heading', { name: 'Andes Suministros S.A.C. · 2026-10' })).toBeInTheDocument();
    expect(screen.getByText('Sin observaciones')).toBeInTheDocument();
    const detalle = screen.getByRole('region', { name: 'Andes Suministros S.A.C. · 2026-10' });
    expect(within(detalle).getByText('Cumplimiento').nextElementSibling).toHaveTextContent('95');
    expect(within(detalle).getByText('Puntaje final').nextElementSibling).toHaveTextContent('92,60');
    fireEvent.click(screen.getByRole('button', { name: 'Ver evaluación de Andes Suministros S.A.C. del periodo 2026-09' }));
    expect(await screen.findByText('Entregas completas, un retraso menor.')).toBeInTheDocument();
  });
});

describe('pantalla de evaluación de proveedores: formulario', () => {
  it('A4 valida cada campo con su literal y no envía nada', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await abrirFormularioNuevo();
    fireEvent.click(screen.getByRole('button', { name: 'Guardar evaluación' }));
    expect(await screen.findByText('El proveedor es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El periodo debe tener el formato AAAA-MM.')).toBeInTheDocument();
    expect(screen.getByText('El puntaje de cumplimiento debe estar entre 0 y 100.')).toBeInTheDocument();
    expect(screen.getByText('El puntaje de calidad debe estar entre 0 y 100.')).toBeInTheDocument();
    expect(screen.getByText('El puntaje de plazos debe estar entre 0 y 100.')).toBeInTheDocument();
    const proveedor = screen.getByLabelText('Proveedor', { selector: '#evaluacion-proveedor' });
    expect(proveedor).toHaveAttribute('aria-invalid', 'true');
    expect(proveedor).toHaveAttribute('aria-describedby', 'evaluacion-proveedor-error');
    expect(fetchMock.mock.calls.filter((call) => call[1]?.method === 'POST')).toHaveLength(0);

    cambiarPeriodoFormulario('2027-01');
    fireEvent.change(screen.getByLabelText('Observaciones'), { target: { value: 'x'.repeat(501) } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar evaluación' }));
    expect(await screen.findByText('El periodo no puede ser futuro.')).toBeInTheDocument();
    expect(screen.getByText('Las observaciones no pueden superar 500 caracteres.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => call[1]?.method === 'POST')).toHaveLength(0);
  });

  it('A4 rechaza puntajes fuera de rango o decimales', async () => {
    prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await abrirFormularioNuevo();
    llenarPuntajes('101', '-1', '7.5');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar evaluación' }));
    expect(await screen.findByText('El puntaje de cumplimiento debe estar entre 0 y 100.')).toBeInTheDocument();
    expect(screen.getByText('El puntaje de calidad debe estar entre 0 y 100.')).toBeInTheDocument();
    expect(screen.getByText('El puntaje de plazos debe estar entre 0 y 100.')).toBeInTheDocument();
  });

  it('A4 muestra la vista previa y respeta los límites 90, 75 y 60', async () => {
    prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await abrirFormularioNuevo();
    expect(screen.getByText('Vista previa: completa los tres puntajes entre 0 y 100.')).toBeInTheDocument();
    llenarPuntajes('90', '85', '80');
    expect(screen.getByText('Vista previa: 85,50 · Bueno')).toBeInTheDocument();
    llenarPuntajes('90', '90', '90');
    expect(screen.getByText('Vista previa: 90,00 · Excelente')).toBeInTheDocument();
    llenarPuntajes('75', '75', '75');
    expect(screen.getByText('Vista previa: 75,00 · Bueno')).toBeInTheDocument();
    llenarPuntajes('60', '60', '60');
    expect(screen.getByText('Vista previa: 60,00 · Regular')).toBeInTheDocument();
    llenarPuntajes('59', '59', '59');
    expect(screen.getByText('Vista previa: 59,00 · Deficiente')).toBeInTheDocument();
    llenarPuntajes('100', '100', '100');
    expect(screen.getByText('Vista previa: 100,00 · Excelente')).toBeInTheDocument();
  });

  it('A4 crea la evaluación, la selecciona y refresca lista y resumen', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await abrirFormularioNuevo();
    fireEvent.change(screen.getByLabelText('Proveedor', { selector: '#evaluacion-proveedor' }), { target: { value: '2' } });
    cambiarPeriodoFormulario('2026-11');
    llenarPuntajes('90', '85', '80');
    fireEvent.change(screen.getByLabelText('Observaciones'), { target: { value: '  Buen mes  ' } });
    const listasAntes = llamadasLista(fetchMock).length;
    fireEvent.click(screen.getByRole('button', { name: 'Guardar evaluación' }));
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nueva evaluación' })).not.toBeInTheDocument());
    const post = fetchMock.mock.calls.find((call) => call[1]?.method === 'POST');
    expect(String(post?.[0])).toBe('/api/v1/evaluaciones-proveedores');
    expect(JSON.parse(String(post?.[1]?.body))).toEqual({
      proveedorId: 2,
      periodo: '2026-11',
      puntajeCumplimiento: 90,
      puntajeCalidad: 85,
      puntajePlazos: 80,
      observaciones: 'Buen mes',
    });
    await waitFor(() => expect(llamadasLista(fetchMock).length).toBeGreaterThan(listasAntes));
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/evaluaciones-proveedores/5'))).toBe(true));
  });

  it('A4 muestra el 409 de periodo repetido sin perder lo escrito', async () => {
    prepararApi({ errorCrear: true });
    renderEnRouter(<EvaluacionProveedoresPage />);
    await abrirFormularioNuevo();
    fireEvent.change(screen.getByLabelText('Proveedor', { selector: '#evaluacion-proveedor' }), { target: { value: '1' } });
    cambiarPeriodoFormulario('2026-10');
    llenarPuntajes('90', '85', '80');
    fireEvent.click(screen.getByRole('button', { name: 'Guardar evaluación' }));
    const alerta = await screen.findByText('El proveedor 1 ya tiene una evaluación del periodo 2026-10.');
    expect(alerta).toHaveAttribute('role', 'alert');
    expect(screen.getByRole('heading', { name: 'Nueva evaluación' })).toBeInTheDocument();
    expect(screen.getByLabelText('Puntaje de cumplimiento')).toHaveValue(90);
    expect(screen.getByLabelText('Puntaje de calidad')).toHaveValue(85);
    expect(screen.getByLabelText('Puntaje de plazos')).toHaveValue(80);
    expect(screen.getByLabelText('Periodo', { selector: '#evaluacion-periodo' })).toHaveValue('2026-10');
  });

  it('A4 edita solo puntajes y observaciones, con proveedor y periodo de solo lectura', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await esperarCargaInicial();
    fireEvent.click(screen.getByRole('button', { name: nombreFila2 }));
    await screen.findByRole('heading', { name: 'Andes Suministros S.A.C. · 2026-10' });
    fireEvent.click(screen.getByRole('button', { name: 'Editar' }));
    const formulario = await screen.findByRole('region', { name: 'Editar evaluación de Andes Suministros S.A.C.' });
    expect(within(formulario).queryByLabelText('Proveedor')).not.toBeInTheDocument();
    expect(within(formulario).queryByLabelText('Periodo')).not.toBeInTheDocument();
    expect(within(formulario).getByText('2026-10')).toBeInTheDocument();
    expect(within(formulario).getByLabelText('Puntaje de calidad')).toHaveValue(92);
    fireEvent.change(within(formulario).getByLabelText('Puntaje de cumplimiento'), { target: { value: '100' } });
    expect(within(formulario).getByText('Vista previa: 94,60 · Excelente')).toBeInTheDocument();
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar evaluación' }));
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'PUT')).toBe(true));
    const put = fetchMock.mock.calls.find((call) => call[1]?.method === 'PUT');
    expect(String(put?.[0])).toBe('/api/v1/evaluaciones-proveedores/2');
    expect(JSON.parse(String(put?.[1]?.body))).toEqual({ puntajeCumplimiento: 100, puntajeCalidad: 92, puntajePlazos: 90 });
  });
});

describe('pantalla de evaluación de proveedores: baja', () => {
  it('A4 Escape cancela la baja y devuelve el foco al botón Eliminar', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await esperarCargaInicial();
    fireEvent.click(screen.getByRole('button', { name: nombreFila2 }));
    await screen.findByRole('heading', { name: 'Andes Suministros S.A.C. · 2026-10' });
    const eliminar = screen.getByRole('button', { name: 'Eliminar' });
    eliminar.focus();
    fireEvent.click(eliminar);
    const dialogo = await screen.findByRole('dialog', { name: 'Eliminar evaluación' });
    expect(within(dialogo).getByText('Andes Suministros S.A.C. · 2026-10')).toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await waitFor(() => expect(eliminar).toHaveFocus());
    expect(fetchMock.mock.calls.some((call) => call[1]?.method === 'DELETE')).toBe(false);
  });

  it('A4 confirma la baja, la elimina y refresca lista y resumen', async () => {
    const { fetchMock } = prepararApi();
    renderEnRouter(<EvaluacionProveedoresPage />);
    await esperarCargaInicial();
    fireEvent.click(screen.getByRole('button', { name: nombreFila2 }));
    await screen.findByRole('heading', { name: 'Andes Suministros S.A.C. · 2026-10' });
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    const listasAntes = llamadasLista(fetchMock).length;
    const resumenesAntes = fetchMock.mock.calls.filter((call) => String(call[0]).endsWith('/resumen')).length;
    fireEvent.click(await screen.findByRole('button', { name: 'Eliminar evaluación' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    const baja = fetchMock.mock.calls.find((call) => call[1]?.method === 'DELETE');
    expect(String(baja?.[0])).toBe('/api/v1/evaluaciones-proveedores/2');
    await waitFor(() => expect(llamadasLista(fetchMock).length).toBeGreaterThan(listasAntes));
    await waitFor(() => expect(fetchMock.mock.calls.filter((call) => String(call[0]).endsWith('/resumen')).length).toBeGreaterThan(resumenesAntes));
    expect(screen.getByText('Selecciona una evaluación para ver el detalle.')).toBeInTheDocument();
  });

  it('A4 muestra el error de la API en el diálogo y lo deja abierto', async () => {
    prepararApi({ errorBaja: true });
    renderEnRouter(<EvaluacionProveedoresPage />);
    await esperarCargaInicial();
    fireEvent.click(screen.getByRole('button', { name: nombreFila2 }));
    await screen.findByRole('heading', { name: 'Andes Suministros S.A.C. · 2026-10' });
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Eliminar evaluación' }));
    expect(await screen.findByText('La evaluación 2 no existe.')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });
});
