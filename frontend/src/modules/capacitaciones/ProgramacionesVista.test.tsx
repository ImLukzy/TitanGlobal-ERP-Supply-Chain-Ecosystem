import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import {
  ANIO_PRUEBAS,
  limpiarCapacitaciones,
  montarProgramacionesCargadas,
  prepararApiCapacitaciones,
  renderCapacitaciones,
  urlsQueContienen,
} from './capacitaciones.testUtils';

afterEach(() => {
  limpiarCapacitaciones();
});

async function seleccionar(nombre: string, indice: number, titulo: string): Promise<void> {
  fireEvent.click(screen.getAllByRole('button', { name: nombre })[indice] as HTMLElement);
  await waitFor(() => {
    expect(screen.getByRole('heading', { name: nombre, level: 2 })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Inscritos' })).toBeInTheDocument();
    expect(screen.getByText(titulo)).toBeInTheDocument();
  });
}

function dialogo(): HTMLElement {
  return screen.getByRole('dialog');
}

function llamadasPost(llamadas: { url: string; metodo: string; cuerpo: unknown }[], sufijo: string) {
  return llamadas.filter((llamada) => llamada.metodo === 'POST' && llamada.url.endsWith(sufijo));
}

describe('vista de Programaciones: listado y filtros', () => {
  it('lista curso, fechas, lugar, inscritos / cupo y estado con texto', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    expect(urlsQueContienen(fetchMock, '/programaciones-curso?')).toEqual([`/api/v1/programaciones-curso?anio=${ANIO_PRUEBAS}&pagina=1&tamanoPagina=10`]);
    const tabla = screen.getByRole('table', { name: 'Listado de programaciones de cursos' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual(['Curso', 'Fechas', 'Lugar', 'Inscritos / Cupo', 'Estado']);
    const filas = within(tabla).getAllByRole('row');
    expect(filas).toHaveLength(5);
    expect(filas[1]).toHaveTextContent('Seguridad industrial');
    expect(filas[1]).toHaveTextContent('02/03/2026 - 03/03/2026');
    expect(filas[1]).toHaveTextContent('3 / 3');
    expect(filas[1]).toHaveTextContent('Realizada');
    expect(filas[2]).toHaveTextContent('2 / 3');
    expect(filas[2]).toHaveTextContent('Programada');
    expect(filas[3]).toHaveTextContent('Sin lugar');
    expect(filas[3]).toHaveTextContent('Cancelada');
  });

  it('envía curso y estado a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiCapacitaciones({ totalListado: 25 });
    await montarProgramacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/programaciones-curso?').at(-1)).toContain('pagina=2'));
    fireEvent.change(screen.getByLabelText('Curso'), { target: { value: '2' } });
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/programaciones-curso?').at(-1)).toBe(`/api/v1/programaciones-curso?anio=${ANIO_PRUEBAS}&cursoId=2&pagina=1&tamanoPagina=10`));
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Programada' } });
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/programaciones-curso?').at(-1)).toBe(`/api/v1/programaciones-curso?anio=${ANIO_PRUEBAS}&cursoId=2&estado=Programada&pagina=1&tamanoPagina=10`));
  });

  it('muestra los estados vacío y de error', async () => {
    prepararApiCapacitaciones({ listaVacia: true });
    renderCapacitaciones();
    expect(await screen.findByText('No hay programaciones con esos filtros.')).toHaveAttribute('role', 'status');
    limpiarCapacitaciones();
    prepararApiCapacitaciones({ errorLista: true });
    renderCapacitaciones();
    expect(await screen.findByText('No se pudo cargar la lista de programaciones.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Programaciones: inscripción, retiro y cupo', () => {
  it('muestra el detalle con inscritos y cupo libre', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Liderazgo', 0, '2 de 3 (libres: 1)');
    const tabla = screen.getByRole('table', { name: 'Inscritos de la programación' });
    expect(within(tabla).getAllByRole('row')).toHaveLength(3);
    expect(tabla).toHaveTextContent('EMP-000001 · Ana Torres Vega');
    expect(tabla).toHaveTextContent('Sin certificación');
  });

  it('inscribe con cupo: ofrece solo empleados sin inscripción vigente y agota el cupo', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Liderazgo', 0, '2 de 3 (libres: 1)');
    fireEvent.click(screen.getByRole('button', { name: 'Inscribir' }));
    expect(dialogo()).toHaveAccessibleName('Inscribir empleado');
    expect(within(dialogo()).queryByRole('option', { name: /Torres Vega/ })).not.toBeInTheDocument();
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Inscribir empleado' }));
    expect(await within(dialogo()).findByText('Selecciona un empleado.')).toBeInTheDocument();
    expect(llamadasPost(llamadas, '/inscripciones')).toHaveLength(0);
    fireEvent.change(within(dialogo()).getByLabelText('Empleado'), { target: { value: '3' } });
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Inscribir empleado' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(llamadasPost(llamadas, '/programaciones-curso/11/inscripciones')[0]?.cuerpo).toEqual({ empleadoId: 3 });
    expect(await screen.findByText('3 de 3 (libres: 0)')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Inscribir' })).toBeDisabled();
    expect(screen.getByText('La programación 11 no tiene cupo libre.')).toBeInTheDocument();
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/capacitaciones/resumen')).toHaveLength(2));
  });

  it('una programación sin cupo deshabilita «Inscribir» y dice el motivo', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Liderazgo', 1, '1 de 1 (libres: 0)');
    const boton = screen.getByRole('button', { name: 'Inscribir' });
    expect(boton).toBeDisabled();
    expect(boton).toHaveAccessibleDescription('La programación 13 no tiene cupo libre.');
  });

  it('un 409 al inscribir muestra el mensaje del servidor y deja el diálogo abierto', async () => {
    const { fetchMock } = prepararApiCapacitaciones({ errorEscritura: { metodo: 'POST', sufijo: '/inscripciones', status: 409, mensaje: 'El empleado 3 ya está inscrito en esta programación.' } });
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Liderazgo', 0, '2 de 3 (libres: 1)');
    fireEvent.click(screen.getByRole('button', { name: 'Inscribir' }));
    fireEvent.change(within(dialogo()).getByLabelText('Empleado'), { target: { value: '3' } });
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Inscribir empleado' }));
    expect(await within(dialogo()).findByText('El empleado 3 ya está inscrito en esta programación.')).toHaveAttribute('role', 'alert');
  });

  it('retira a un inscrito sin cuerpo y libera el cupo', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Liderazgo', 0, '2 de 3 (libres: 1)');
    fireEvent.click(screen.getByRole('button', { name: 'Retirar a Ana Torres Vega' }));
    expect(await screen.findByText('1 de 3 (libres: 2)')).toBeInTheDocument();
    expect(llamadasPost(llamadas, '/inscripciones/110/retiro')).toHaveLength(1);
    expect(llamadasPost(llamadas, '/inscripciones/110/retiro')[0]?.cuerpo).toBeUndefined();
    expect(screen.queryByRole('button', { name: 'Retirar a Ana Torres Vega' })).not.toBeInTheDocument();
  });

  it('un 409 al retirar se muestra en el panel', async () => {
    const { fetchMock } = prepararApiCapacitaciones({ errorEscritura: { metodo: 'POST', sufijo: '/retiro', status: 409, mensaje: 'Solo se puede retirar una inscripción antes del inicio del curso.' } });
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Liderazgo', 0, '2 de 3 (libres: 1)');
    fireEvent.click(screen.getByRole('button', { name: 'Retirar a Ana Torres Vega' }));
    expect(await screen.findByText('Solo se puede retirar una inscripción antes del inicio del curso.')).toHaveAttribute('role', 'alert');
  });
});

describe('vista de Programaciones: realización y cancelación', () => {
  it('marca como realizada y pasa a mostrar la tabla de resultados', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Liderazgo', 0, '2 de 3 (libres: 1)');
    fireEvent.click(screen.getByRole('button', { name: 'Marcar realizada' }));
    expect(await screen.findByRole('table', { name: 'Resultados de los inscritos' })).toBeInTheDocument();
    expect(llamadasPost(llamadas, '/programaciones-curso/11/realizacion')).toHaveLength(1);
    expect(screen.queryByRole('button', { name: 'Marcar realizada' })).not.toBeInTheDocument();
  });

  it('un 409 al marcar realizada muestra el mensaje literal', async () => {
    const mensaje = 'La programación 11 termina el 2026-12-11 y aún no se puede marcar como realizada.';
    const { fetchMock } = prepararApiCapacitaciones({ errorEscritura: { metodo: 'POST', sufijo: '/realizacion', status: 409, mensaje } });
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Liderazgo', 0, '2 de 3 (libres: 1)');
    fireEvent.click(screen.getByRole('button', { name: 'Marcar realizada' }));
    expect(await screen.findByText(mensaje)).toHaveAttribute('role', 'alert');
  });

  it('cancelar pide confirmación; «Volver» no envía nada y confirmar retira a todos', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Liderazgo', 0, '2 de 3 (libres: 1)');
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar programación' }));
    expect(dialogo()).toHaveAccessibleName('Cancelar programación');
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Volver' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(llamadasPost(llamadas, '/cancelacion')).toHaveLength(0);
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar programación' }));
    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar programación' }));
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Confirmar cancelación' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(llamadasPost(llamadas, '/programaciones-curso/11/cancelacion')).toHaveLength(1);
    expect(await screen.findByText('0 de 3 (libres: 3)')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Cancelar programación' })).not.toBeInTheDocument();
  });
});

describe('vista de Programaciones: resultados y certificación', () => {
  it('muestra la certificación vencida con texto y bloquea a quien ya la tiene', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Seguridad industrial', 0, '3 de 3 (libres: 0)');
    const tabla = screen.getByRole('table', { name: 'Resultados de los inscritos' });
    expect(tabla).toHaveTextContent('CERT-001');
    expect(tabla).toHaveTextContent('Vence 01/05/2026');
    expect(tabla).toHaveTextContent('Vencida');
    expect(screen.getByLabelText('Resultado de Ana Torres Vega')).toBeDisabled();
    expect(screen.getByLabelText('Nota de Ana Torres Vega')).toBeDisabled();
    expect(screen.getByLabelText('Resultado de Luis Rojas Mendoza')).toHaveValue('Aprobado');
    expect(screen.getByLabelText('Resultado de Maria Salazar Quispe')).toHaveValue('');
  });

  it('guarda los resultados con un único botón y excluye a los certificados', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Seguridad industrial', 0, '3 de 3 (libres: 0)');
    fireEvent.change(screen.getByLabelText('Resultado de Maria Salazar Quispe'), { target: { value: 'Desaprobado' } });
    fireEvent.change(screen.getByLabelText('Nota de Maria Salazar Quispe'), { target: { value: '9,5' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar resultados' }));
    // Tras guardar, la lista se recarga y la tabla se vuelve a montar: se espera el mensaje de forma estable.
    await waitFor(() => expect(screen.getByText('Resultados guardados.')).toBeInTheDocument());
    expect(llamadasPost(llamadas, '/programaciones-curso/10/resultados')[0]?.cuerpo).toEqual({
      resultados: [{ inscripcionId: 101, aprobado: true, nota: 15 }, { inscripcionId: 102, aprobado: false, nota: 9.5 }],
    });
    await waitFor(() => expect(screen.getByLabelText('Resultado de Maria Salazar Quispe')).toHaveValue('Desaprobado'));
  });

  it('una nota fuera de rango muestra el mensaje 24 y no envía nada', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Seguridad industrial', 0, '3 de 3 (libres: 0)');
    fireEvent.change(screen.getByLabelText('Nota de Luis Rojas Mendoza'), { target: { value: '21' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar resultados' }));
    expect(await screen.findByText('La nota debe estar entre 0 y 20, con un decimal.')).toBeInTheDocument();
    expect(screen.getByLabelText('Nota de Luis Rojas Mendoza')).toHaveAttribute('aria-invalid', 'true');
    expect(llamadasPost(llamadas, '/resultados')).toHaveLength(0);
  });

  it('un error del servidor al guardar resultados se muestra', async () => {
    const { fetchMock } = prepararApiCapacitaciones({ errorEscritura: { metodo: 'POST', sufijo: '/resultados', status: 409, mensaje: 'La programación 10 debe estar realizada para registrar resultados.' } });
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Seguridad industrial', 0, '3 de 3 (libres: 0)');
    fireEvent.change(screen.getByLabelText('Resultado de Maria Salazar Quispe'), { target: { value: 'Aprobado' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar resultados' }));
    expect(await screen.findByText('La programación 10 debe estar realizada para registrar resultados.')).toHaveAttribute('role', 'alert');
  });

  it('emite una certificación de un aprobado con validación local y cuerpo exacto', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Seguridad industrial', 0, '3 de 3 (libres: 0)');
    fireEvent.click(screen.getByRole('button', { name: 'Emitir certificado a Luis Rojas Mendoza' }));
    expect(dialogo()).toHaveAccessibleName('Emitir certificado');
    expect(within(dialogo()).getByLabelText('Fecha de emisión')).toHaveValue('2026-03-03');
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Emitir certificación' }));
    expect(await within(dialogo()).findByText('El código de certificación es obligatorio y no puede superar 40 caracteres.')).toBeInTheDocument();
    fireEvent.change(within(dialogo()).getByLabelText('Código de certificación'), { target: { value: 'cert-luis' } });
    fireEvent.change(within(dialogo()).getByLabelText('Fecha de emisión'), { target: { value: '2026-03-02' } });
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Emitir certificación' }));
    expect(await within(dialogo()).findByText('La fecha de emisión es obligatoria y no puede ser anterior al fin del curso.')).toBeInTheDocument();
    fireEvent.change(within(dialogo()).getByLabelText('Fecha de emisión'), { target: { value: '2026-03-04' } });
    fireEvent.change(within(dialogo()).getByLabelText('Fecha de vencimiento (opcional)'), { target: { value: '2026-03-03' } });
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Emitir certificación' }));
    expect(await within(dialogo()).findByText('La fecha de vencimiento no puede ser anterior a la de emisión.')).toBeInTheDocument();
    expect(llamadasPost(llamadas, '/certificacion')).toHaveLength(0);
    fireEvent.change(within(dialogo()).getByLabelText('Fecha de vencimiento (opcional)'), { target: { value: '2027-03-04' } });
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Emitir certificación' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(llamadasPost(llamadas, '/programaciones-curso/10/inscripciones/101/certificacion')[0]?.cuerpo).toEqual({ codigo: 'cert-luis', fechaEmision: '2026-03-04', fechaVencimiento: '2027-03-04' });
    expect(await screen.findByText('CERT-LUIS')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Emitir certificado a Luis Rojas Mendoza' })).not.toBeInTheDocument();
  });

  it('un 409 al emitir la certificación se muestra en el diálogo', async () => {
    const mensaje = 'Ya existe una certificación con el código CERT-001.';
    const { fetchMock } = prepararApiCapacitaciones({ errorEscritura: { metodo: 'POST', sufijo: '/certificacion', status: 409, mensaje } });
    await montarProgramacionesCargadas(fetchMock);
    await seleccionar('Seguridad industrial', 0, '3 de 3 (libres: 0)');
    fireEvent.click(screen.getByRole('button', { name: 'Emitir certificado a Luis Rojas Mendoza' }));
    fireEvent.change(within(dialogo()).getByLabelText('Código de certificación'), { target: { value: 'CERT-001' } });
    fireEvent.click(within(dialogo()).getByRole('button', { name: 'Emitir certificación' }));
    expect(await within(dialogo()).findByText(mensaje)).toHaveAttribute('role', 'alert');
  });

  it('sin RRHH ni ADMIN los resultados se ven en solo lectura', async () => {
    const { fetchMock } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock, ['VENTAS']);
    await seleccionar('Seguridad industrial', 0, '3 de 3 (libres: 0)');
    expect(screen.queryByRole('button', { name: 'Guardar resultados' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Emitir certificado a Luis Rojas Mendoza' })).toBeDisabled();
    expect(screen.getByRole('table', { name: 'Inscritos de la programación' })).toHaveTextContent('Vencida');
  });
});

describe('vista de Programaciones: nueva programación', () => {
  it('valida localmente, solo ofrece cursos activos y crea con el cuerpo exacto', async () => {
    const { fetchMock, llamadas } = prepararApiCapacitaciones();
    await montarProgramacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva programación' }));
    const formulario = screen.getByRole('heading', { name: 'Nueva programación' }).closest('section') as HTMLElement;
    expect(within(formulario).queryByRole('option', { name: /EXC-301/ })).not.toBeInTheDocument();
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar programación' }));
    expect(await within(formulario).findByText('Selecciona un curso.')).toBeInTheDocument();
    expect(within(formulario).getAllByText('Las fechas de inicio y fin son obligatorias.')).toHaveLength(2);
    expect(within(formulario).getByText('El cupo debe estar entre 1 y 500.')).toBeInTheDocument();
    expect(llamadasPost(llamadas, '/programaciones-curso')).toHaveLength(0);
    fireEvent.change(within(formulario).getByLabelText('Curso'), { target: { value: '1' } });
    fireEvent.change(within(formulario).getByLabelText('Fecha de inicio'), { target: { value: '2026-12-20' } });
    fireEvent.change(within(formulario).getByLabelText('Fecha de fin'), { target: { value: '2026-12-19' } });
    fireEvent.change(within(formulario).getByLabelText('Cupo'), { target: { value: '12' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar programación' }));
    expect(await within(formulario).findByText('La fecha de fin no puede ser anterior a la de inicio.')).toBeInTheDocument();
    fireEvent.change(within(formulario).getByLabelText('Fecha de fin'), { target: { value: '2026-12-21' } });
    fireEvent.change(within(formulario).getByLabelText('Lugar (opcional)'), { target: { value: ' Sala B ' } });
    fireEvent.click(within(formulario).getByRole('button', { name: 'Guardar programación' }));
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nueva programación' })).not.toBeInTheDocument());
    expect(llamadasPost(llamadas, '/api/v1/programaciones-curso')[0]?.cuerpo).toEqual({ cursoId: 1, fechaInicio: '2026-12-20', fechaFin: '2026-12-21', cupo: 12, lugar: 'Sala B' });
    expect(await screen.findByText('0 de 12 (libres: 12)')).toBeInTheDocument();
  });

  it('un 409 al crear muestra el mensaje y conserva lo escrito', async () => {
    const mensaje = 'El curso SEG-101 está inactivo y no admite programaciones.';
    const { fetchMock } = prepararApiCapacitaciones({ errorEscritura: { metodo: 'POST', sufijo: '/programaciones-curso', status: 409, mensaje } });
    await montarProgramacionesCargadas(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nueva programación' }));
    fireEvent.change(screen.getByLabelText('Curso', { selector: '#programacion-cursoId' }), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText('Fecha de inicio'), { target: { value: '2026-12-20' } });
    fireEvent.change(screen.getByLabelText('Fecha de fin'), { target: { value: '2026-12-21' } });
    fireEvent.change(screen.getByLabelText('Cupo'), { target: { value: '12' } });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar programación' }));
    expect(await screen.findByText(mensaje)).toHaveAttribute('role', 'alert');
    expect(screen.getByLabelText('Cupo')).toHaveValue('12');
  });
});
