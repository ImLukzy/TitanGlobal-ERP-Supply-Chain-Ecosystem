import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { limpiarSst, llamadasSst, montarIncidentesCargados, prepararApiSst, urlsQueContienen } from './sst.testUtils';

afterEach(() => {
  limpiarSst();
});

function urlsLista(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return urlsQueContienen(fetchMock, '/incidentes-sst?');
}

async function seleccionar(numero: string): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: numero }));
  await waitFor(() => expect(screen.getByRole('heading', { name: `Incidente ${numero}` })).toBeInTheDocument());
}

describe('vista de Incidentes: listado y filtros', () => {
  it('lista número, fecha, empleado, tipo, severidad, días y estado con texto', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    expect(urlsLista(fetchMock)).toEqual(['/api/v1/incidentes-sst?pagina=1&tamanoPagina=10']);
    const tabla = screen.getByRole('table', { name: 'Listado de incidentes' });
    expect(within(tabla).getAllByRole('columnheader').map((celda) => celda.textContent)).toEqual(['Número', 'Fecha', 'Empleado', 'Tipo', 'Severidad', 'Días de baja', 'Estado']);
    const filas = within(tabla).getAllByRole('row');
    expect(filas).toHaveLength(4);
    expect(filas[1]).toHaveTextContent('10/05/2026');
    expect(filas[1]).toHaveTextContent('Accidente con baja');
    expect(filas[1]).toHaveTextContent('Grave');
    expect(filas[1]).toHaveTextContent('Abierto');
    expect(filas[2]).toHaveTextContent('Cerrado');
  });

  it('envía tipo, severidad, estado y rango a la query y vuelve a la página 1', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'AccidenteConBaja' } });
    await waitFor(() => expect(urlsLista(fetchMock).at(-1)).toBe('/api/v1/incidentes-sst?tipo=AccidenteConBaja&pagina=1&tamanoPagina=10'));
    fireEvent.change(screen.getByLabelText('Severidad'), { target: { value: 'Grave' } });
    fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'Abierto' } });
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-01-01' } });
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-12-31' } });
    await waitFor(() => expect(urlsLista(fetchMock).at(-1)).toBe('/api/v1/incidentes-sst?tipo=AccidenteConBaja&severidad=Grave&estado=Abierto&desde=2026-01-01&hasta=2026-12-31&pagina=1&tamanoPagina=10'));
  });

  it('aplica la búsqueda 300 ms después del último cambio', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    const antes = urlsLista(fetchMock).length;
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText('Buscar por número o empleado'), { target: { value: 'torres' } });
    await vi.advanceTimersByTimeAsync(299);
    expect(urlsLista(fetchMock)).toHaveLength(antes);
    await vi.advanceTimersByTimeAsync(2);
    vi.useRealTimers();
    await waitFor(() => expect(urlsLista(fetchMock).at(-1)).toBe('/api/v1/incidentes-sst?busqueda=torres&pagina=1&tamanoPagina=10'));
  });
});

describe('vista de Incidentes: alta', () => {
  it('muestra los mensajes locales y no envía nada con datos inválidos', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo incidente' }));
    fireEvent.click(screen.getByRole('button', { name: 'Registrar incidente' }));
    expect(screen.getByText('El empleado es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El tipo de incidente debe ser AccidenteSinBaja, AccidenteConBaja, IncidenteSinLesion o EnfermedadOcupacional.')).toBeInTheDocument();
    expect(screen.getByText('La severidad debe ser Leve, Moderada, Grave o Mortal.')).toBeInTheDocument();
    expect(screen.getByText('La fecha es obligatoria y no puede ser posterior a hoy.')).toBeInTheDocument();
    expect(screen.getByText('La descripción es obligatoria y no puede superar 1000 caracteres.')).toBeInTheDocument();
    expect(llamadasSst(llamadas, 'POST', '/incidentes-sst')).toHaveLength(0);
  });

  it('rechaza una fecha futura y los días de baja solo aparecen con AccidenteConBaja', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo incidente' }));
    expect(screen.queryByLabelText('Días de baja')).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Tipo', { selector: '#incidente-tipo' }), { target: { value: 'AccidenteConBaja' } });
    fireEvent.change(screen.getByLabelText('Fecha', { selector: '#incidente-fecha' }), { target: { value: '2026-11-23' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar incidente' }));
    expect(screen.getByText('Un accidente con baja exige al menos 1 día de baja.')).toBeInTheDocument();
    expect(screen.getAllByText('La fecha es obligatoria y no puede ser posterior a hoy.')).toHaveLength(1);
    fireEvent.change(screen.getByLabelText('Tipo', { selector: '#incidente-tipo' }), { target: { value: 'IncidenteSinLesion' } });
    expect(screen.queryByLabelText('Días de baja')).not.toBeInTheDocument();
    expect(llamadasSst(llamadas, 'POST', '/incidentes-sst')).toHaveLength(0);
  });

  it('registra un accidente con baja y abre su detalle', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo incidente' }));
    fireEvent.change(screen.getByLabelText('Empleado', { selector: '#incidente-empleado' }), { target: { value: '2' } });
    fireEvent.change(screen.getByLabelText('Tipo', { selector: '#incidente-tipo' }), { target: { value: 'AccidenteConBaja' } });
    fireEvent.change(screen.getByLabelText('Severidad', { selector: '#incidente-severidad' }), { target: { value: 'Moderada' } });
    fireEvent.change(screen.getByLabelText('Fecha', { selector: '#incidente-fecha' }), { target: { value: '2026-11-20' } });
    fireEvent.change(screen.getByLabelText('Días de baja'), { target: { value: '4' } });
    fireEvent.change(screen.getByLabelText('Lugar (opcional)'), { target: { value: ' Almacén ' } });
    fireEvent.change(screen.getByLabelText('Descripción', { selector: '#incidente-descripcion' }), { target: { value: ' Caída de carga ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar incidente' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'POST', '/incidentes-sst')).toHaveLength(1));
    expect(llamadasSst(llamadas, 'POST', '/incidentes-sst')[0]?.cuerpo).toEqual({ empleadoId: 2, tipo: 'AccidenteConBaja', severidad: 'Moderada', fecha: '2026-11-20', lugar: 'Almacén', descripcion: 'Caída de carga', diasBaja: 4 });
    expect(await screen.findByRole('heading', { name: 'Incidente INC-000009' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Nuevo incidente' })).not.toBeInTheDocument();
  });

  it('muestra el mensaje literal del backend y conserva lo escrito', async () => {
    const { fetchMock } = prepararApiSst({ errorEscritura: { metodo: 'POST', sufijo: '/incidentes-sst', status: 404, mensaje: 'El empleado 2 no existe.' } });
    await montarIncidentesCargados(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Nuevo incidente' }));
    fireEvent.change(screen.getByLabelText('Empleado', { selector: '#incidente-empleado' }), { target: { value: '2' } });
    fireEvent.change(screen.getByLabelText('Tipo', { selector: '#incidente-tipo' }), { target: { value: 'IncidenteSinLesion' } });
    fireEvent.change(screen.getByLabelText('Severidad', { selector: '#incidente-severidad' }), { target: { value: 'Leve' } });
    fireEvent.change(screen.getByLabelText('Fecha', { selector: '#incidente-fecha' }), { target: { value: '2026-11-20' } });
    fireEvent.change(screen.getByLabelText('Descripción', { selector: '#incidente-descripcion' }), { target: { value: 'Resbalón' } });
    fireEvent.click(screen.getByRole('button', { name: 'Registrar incidente' }));
    expect(await screen.findByText('El empleado 2 no existe.')).toBeInTheDocument();
    expect(screen.getByLabelText('Descripción', { selector: '#incidente-descripcion' })).toHaveValue('Resbalón');
  });
});

describe('vista de Incidentes: acciones y cierre', () => {
  it('muestra el detalle con sus acciones y bloquea el cierre con el motivo visible', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    await seleccionar('INC-000001');
    expect(screen.getByText('Descripción del incidente 1')).toBeInTheDocument();
    expect(screen.getByRole('table', { name: 'Acciones correctivas' })).toHaveTextContent('Instalar guarda');
    expect(screen.getByRole('button', { name: 'Cerrar incidente' })).toBeDisabled();
    expect(screen.getByText('Hay 1 acción correctiva sin completar.')).toBeInTheDocument();
    await seleccionar('INC-000003');
    expect(screen.getByRole('button', { name: 'Cerrar incidente' })).toBeDisabled();
    expect(screen.getByText('Registra al menos una acción correctiva para poder cerrar el incidente.')).toBeInTheDocument();
  });

  it('un incidente cerrado no ofrece acciones ni cierre', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    await seleccionar('INC-000002');
    expect(screen.queryByRole('button', { name: 'Agregar acción' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Cerrar incidente' })).not.toBeInTheDocument();
    expect(screen.getByText('Completada 09/02/2026')).toBeInTheDocument();
  });

  it('agrega una acción: valida el mensaje 28 y luego envía el cuerpo', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    await seleccionar('INC-000003');
    fireEvent.click(screen.getByRole('button', { name: 'Agregar acción' }));
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Agregar acción' }));
    expect(screen.getAllByText('La acción correctiva necesita descripción (máximo 500), responsable (máximo 150) y fecha de compromiso.')).toHaveLength(3);
    expect(llamadasSst(llamadas, 'POST', '/acciones')).toHaveLength(0);
    fireEvent.change(screen.getByLabelText('Descripción'), { target: { value: ' Capacitar al personal ' } });
    fireEvent.change(screen.getByLabelText('Responsable'), { target: { value: 'RRHH' } });
    fireEvent.change(screen.getByLabelText('Fecha de compromiso'), { target: { value: '2026-12-15' } });
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Agregar acción' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'POST', '/incidentes-sst/3/acciones')).toHaveLength(1));
    expect(llamadasSst(llamadas, 'POST', '/acciones')[0]?.cuerpo).toEqual({ descripcion: 'Capacitar al personal', responsable: 'RRHH', fechaCompromiso: '2026-12-15' });
    expect(await screen.findByText('Capacitar al personal')).toBeInTheDocument();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('completa la acción pendiente con la fecha de hoy y habilita el cierre', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    await seleccionar('INC-000001');
    fireEvent.click(screen.getByRole('button', { name: 'Completar acción Instalar guarda' }));
    expect(screen.getByLabelText('Fecha de completado')).toHaveValue('2026-11-22');
    fireEvent.change(screen.getByLabelText('Fecha de completado'), { target: { value: '2026-11-23' } });
    fireEvent.click(screen.getByRole('button', { name: 'Completar acción' }));
    expect(screen.getByText('La fecha es obligatoria y no puede ser posterior a hoy.')).toBeInTheDocument();
    expect(llamadasSst(llamadas, 'POST', '/completar')).toHaveLength(0);
    fireEvent.change(screen.getByLabelText('Fecha de completado'), { target: { value: '2026-11-22' } });
    fireEvent.click(screen.getByRole('button', { name: 'Completar acción' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'POST', '/incidentes-sst/1/acciones/11/completar')).toHaveLength(1));
    expect(llamadasSst(llamadas, 'POST', '/completar')[0]?.cuerpo).toEqual({ fechaCompletada: '2026-11-22' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Cerrar incidente' })).toBeEnabled());
  });

  it('cierra el incidente cuando todas las acciones están completas', async () => {
    const { fetchMock, llamadas } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    await seleccionar('INC-000001');
    fireEvent.click(screen.getByRole('button', { name: 'Completar acción Instalar guarda' }));
    fireEvent.click(screen.getByRole('button', { name: 'Completar acción' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Cerrar incidente' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar incidente' }));
    await waitFor(() => expect(llamadasSst(llamadas, 'POST', '/incidentes-sst/1/cierre')).toHaveLength(1));
    expect(llamadasSst(llamadas, 'POST', '/cierre')[0]?.cuerpo).toBeUndefined();
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Cerrar incidente' })).not.toBeInTheDocument());
    expect(screen.getByText('Completada 22/11/2026')).toBeInTheDocument();
  });

  it('muestra el mensaje del backend si el cierre falla por un conflicto', async () => {
    const { fetchMock } = prepararApiSst({ errorEscritura: { metodo: 'POST', sufijo: '/incidentes-sst/1/cierre', status: 409, mensaje: 'El incidente INC-000001 tiene 1 acciones correctivas sin completar.' } });
    await montarIncidentesCargados(fetchMock);
    await seleccionar('INC-000001');
    fireEvent.click(screen.getByRole('button', { name: 'Completar acción Instalar guarda' }));
    fireEvent.click(screen.getByRole('button', { name: 'Completar acción' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Cerrar incidente' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar incidente' }));
    expect(await screen.findByText('El incidente INC-000001 tiene 1 acciones correctivas sin completar.')).toBeInTheDocument();
  });
});
