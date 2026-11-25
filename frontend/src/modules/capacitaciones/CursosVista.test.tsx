import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  limpiarCapacitaciones,
  montarCursosCargados,
  prepararApiCapacitaciones,
  renderCapacitaciones,
  urlsQueContienen,
} from './capacitaciones.testUtils';

afterEach(() => {
  limpiarCapacitaciones();
});

function urlsLista(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueContienen(fetchMock, '/cursos?').filter((url) => url.endsWith('tamanoPagina=10'));
}

async function seleccionarCurso(codigo: string, nombre: string): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: codigo }));
  await waitFor(() => expect(screen.getByRole('heading', { name: nombre })).toBeInTheDocument());
}

function llamadasCurso(llamadas: { url: string; metodo: string; cuerpo: unknown }[], metodo: string, sufijo: string) {
  return llamadas.filter((llamada) => llamada.metodo === metodo && llamada.url.endsWith(sufijo));
}

describe('vista de Cursos: listado y filtros', () => {
  it('lista código, nombre, horas, tipo y estado con texto', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarCursosCargados(fetchMock);
    expect(urlsLista(fetchMock)).toEqual(['/api/v1/cursos?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table', { name: 'Listado de cursos' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual(['Código', 'Nombre', 'Horas', 'Tipo', 'Estado']);
    const filas = within(tabla).getAllByRole('row');
    expect(filas).toHaveLength(4);
    expect(filas[1]).toHaveTextContent('SEG-101');
    expect(filas[1]).toHaveTextContent('Activo');
    expect(filas[3]).toHaveTextContent('12,5');
    expect(filas[3]).toHaveTextContent('Virtual');
    expect(filas[3]).toHaveTextContent('Inactivo');
  });

  it('envía tipo y estado a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiCapacitaciones({ totalListado: 25 });
    await montarCursosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsLista(fetchMock).at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Externo' } });
    await waitFor(() => expect(urlsLista(fetchMock).at(-1)).toBe('/api/v1/cursos?tipo=Externo&pagina=1&tamanoPagina=10'));
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Activos' } });
    await waitFor(() => expect(urlsLista(fetchMock).at(-1)).toBe('/api/v1/cursos?tipo=Externo&activo=true&pagina=1&tamanoPagina=10'));
  });

  it('aplica la búsqueda 300 ms después del último cambio', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarCursosCargados(fetchMock);
    const antes = urlsLista(fetchMock).length;
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText('Buscar por código o nombre'), { target: { value: 'excel' } });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(urlsLista(fetchMock)).toHaveLength(antes);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() => expect(urlsLista(fetchMock).at(-1)).toBe('/api/v1/cursos?busqueda=excel&pagina=1&tamanoPagina=10'));
  });

  it('muestra los estados vacío y de error', async () => {
    prepararApiCapacitaciones({ listaVacia: true });
    renderCapacitaciones(['RRHH'], '/modulos/69-capacitaciones?vista=cursos');
    expect(await screen.findByText('No hay cursos con esos filtros.')).toHaveAttribute('role', 'status');
    limpiarCapacitaciones();
    prepararApiCapacitaciones({ errorLista: true });
    renderCapacitaciones(['RRHH'], '/modulos/69-capacitaciones?vista=cursos');
    expect(await screen.findByText('No se pudo cargar la lista de cursos.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Cursos: detalle, activación y desactivación', () => {
  it('muestra el detalle con tipo y proveedor', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarCursosCargados(fetchMock);
    await seleccionarCurso('LID-201', 'Liderazgo');
    expect(screen.getByText('Instituto Andino')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Desactivar curso' })).toBeEnabled();
    await seleccionarCurso('SEG-101', 'Seguridad industrial');
    expect(screen.getByText('Sin proveedor')).toBeInTheDocument();
  });

  it('desactiva y vuelve a activar un curso con POST sin cuerpo y actualiza el resumen', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarCursosCargados(fetchMock);
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/capacitaciones/resumen')).toHaveLength(1));
    await seleccionarCurso('LID-201', 'Liderazgo');
    fireEvent.click(screen.getByRole('button', { name: 'Desactivar curso' }));
    expect(await screen.findByRole('button', { name: 'Activar curso' })).toBeInTheDocument();
    expect(llamadasCurso(llamadas, 'POST', '/cursos/2/desactivacion')).toHaveLength(1);
    expect(llamadasCurso(llamadas, 'POST', '/cursos/2/desactivacion')[0]?.cuerpo).toBeUndefined();
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/capacitaciones/resumen')).toHaveLength(2));
    fireEvent.click(screen.getByRole('button', { name: 'Activar curso' }));
    expect(await screen.findByRole('button', { name: 'Desactivar curso' })).toBeInTheDocument();
    expect(llamadasCurso(llamadas, 'POST', '/cursos/2/activacion')).toHaveLength(1);
  });

  it('un 409 al cambiar el estado se muestra en el panel', async () => {
    const mensaje = 'El curso LID-201 ya está inactivo.';
    const { fetchMock } = prepararApiCapacitaciones({ errorEscritura: { metodo: 'POST', sufijo: '/desactivacion', status: 409, mensaje } });
    await montarCursosCargados(fetchMock);
    await seleccionarCurso('LID-201', 'Liderazgo');
    fireEvent.click(screen.getByRole('button', { name: 'Desactivar curso' }));
    expect(await screen.findByText(mensaje)).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Cursos: alta y edición', () => {
  it('valida localmente y crea con el cuerpo exacto', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarCursosCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo curso' }));
    const formulario = screen.getByRole('heading', { name: 'Nuevo curso' }).closest('section') as HTMLElement;
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar curso' }));
    expect(await within(formulario).findByText('El código es obligatorio y no puede superar 20 caracteres.')).toBeInTheDocument();
    expect(within(formulario).getByText('El nombre es obligatorio y no puede superar 150 caracteres.')).toBeInTheDocument();
    expect(within(formulario).getByText('Las horas deben ser mayores que 0 y como máximo 500, con un decimal.')).toBeInTheDocument();
    expect(llamadasCurso(llamadas, 'POST', '/api/v1/cursos')).toHaveLength(0);
    fireEvent.change(within(formulario).getByLabelText('Código'), { target: { value: ' nue-001 ' } });
    fireEvent.change(within(formulario).getByLabelText('Nombre'), { target: { value: 'Primeros auxilios' } });
    fireEvent.change(within(formulario).getByLabelText('Horas'), { target: { value: '12,5' } });
    fireEvent.change(within(formulario).getByLabelText('Tipo'), { target: { value: 'Virtual' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar curso' }));
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nuevo curso' })).not.toBeInTheDocument());
    expect(llamadasCurso(llamadas, 'POST', '/api/v1/cursos')[0]?.cuerpo).toEqual({ codigo: 'nue-001', nombre: 'Primeros auxilios', horas: 12.5, tipo: 'Virtual', proveedor: null });
    expect(await screen.findByRole('heading', { name: 'Primeros auxilios' })).toBeInTheDocument();
  });

  it('edita un curso con los datos cargados y conserva lo escrito ante un 409', async () => {
    const mensaje = 'Ya existe un curso con el código SEG-101.';
    const { fetchMock, llamadas } = prepararApiCapacitaciones({ errorEscritura: { metodo: 'PUT', sufijo: '/cursos/2', status: 409, mensaje } });
    await montarCursosCargados(fetchMock);
    await seleccionarCurso('LID-201', 'Liderazgo');
    fireEvent.click(screen.getByRole('button', { name: 'Editar curso' }));
    expect(screen.getByLabelText('Código')).toHaveValue('LID-201');
    expect(screen.getByLabelText('Horas')).toHaveValue('16');
    expect(screen.getByLabelText('Proveedor (opcional)')).toHaveValue('Instituto Andino');
    fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'SEG-101' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar curso' }));
    expect(await screen.findByText(mensaje)).toHaveAttribute('role', 'alert');
    expect(screen.getByLabelText('Código')).toHaveValue('SEG-101');
    expect(llamadasCurso(llamadas, 'PUT', '/cursos/2')).toHaveLength(1);
  });

  it('guarda la edición con PUT y el cuerpo exacto', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarCursosCargados(fetchMock);
    await seleccionarCurso('LID-201', 'Liderazgo');
    fireEvent.click(screen.getByRole('button', { name: 'Editar curso' }));
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Liderazgo avanzado' } });
    fireEvent.change(screen.getByLabelText('Proveedor (opcional)'), { target: { value: '' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar curso' }));
    expect(await screen.findByRole('heading', { name: 'Liderazgo avanzado' })).toBeInTheDocument();
    expect(llamadasCurso(llamadas, 'PUT', '/cursos/2')[0]?.cuerpo).toEqual({ codigo: 'LID-201', nombre: 'Liderazgo avanzado', horas: 16, tipo: 'Externo', proveedor: null });
  });
});
