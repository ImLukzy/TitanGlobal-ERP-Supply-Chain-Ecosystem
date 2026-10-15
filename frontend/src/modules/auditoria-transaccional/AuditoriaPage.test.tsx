import { afterEach, describe, expect, it } from 'vitest';
import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { cambiosMock, detalleMock, limpiarAuditoria, montarAuditoriaCargada, prepararApiAuditoria, renderAuditoria, registrosMock } from './auditoria.testUtils';

afterEach(limpiarAuditoria);

describe('visor de auditoría transaccional', () => {
  it('A3 carga página 1 de 20, entidades, resumen y un solo listado', async () => {
    const fetchMock = prepararApiAuditoria();
    await montarAuditoriaCargada(fetchMock);
    expect(fetchMock.mock.calls.find((call) => String(call[0]).includes('/auditoria?'))?.[0]).toBe('/api/v1/auditoria?pagina=1&tamanoPagina=20');
    expect(screen.getByRole('navigation', { name: 'Paginación: Tabla de auditoría' })).toHaveTextContent('Página 1 de 2');
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/auditoria?'))).toHaveLength(1);
  });

  it('A3 aplica los filtros a la consulta y vuelve a página 1', async () => {
    const fetchMock = prepararApiAuditoria();
    await montarAuditoriaCargada(fetchMock);
    fireEvent.change(screen.getByLabelText('Entidad'), { target: { value: 'Cliente' } });
    fireEvent.change(screen.getByLabelText('Acción'), { target: { value: 'Modificacion' } });
    fireEvent.change(screen.getByLabelText('Usuario'), { target: { value: 'demo' } });
    fireEvent.change(screen.getByLabelText('Id del registro'), { target: { value: '3' } });
    fireEvent.change(screen.getByLabelText('Fecha desde'), { target: { value: '2026-10-01' } });
    fireEvent.change(screen.getByLabelText('Fecha hasta'), { target: { value: '2026-10-15' } });
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]).includes('entidad=Cliente&entidadId=3&accion=Modificacion&usuario=demo&fechaDesde=2026-10-01&fechaHasta=2026-10-15&pagina=1&tamanoPagina=20'))).toBe(true));
  });

  it('A3 inicializa el filtro entidadId desde la query y abre su primer detalle', async () => {
    const fetchMock = prepararApiAuditoria();
    renderAuditoria('/modulos/77-auditoria-transaccional?entidad=Cliente&entidadId=3');
    expect(await screen.findByRole('heading', { name: 'Modificación · Cliente #3' })).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => String(call[0]).includes('/auditoria?entidad=Cliente&entidadId=3&pagina=1&tamanoPagina=20'))).toBe(true);
    expect(fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/auditoria/11')).toBe(true);
  });

  it('A3 impide consultar un rango de fechas inválido', async () => {
    const fetchMock = prepararApiAuditoria();
    await montarAuditoriaCargada(fetchMock);
    fireEvent.change(screen.getByLabelText('Fecha hasta'), { target: { value: '2026-10-10' } });
    await waitFor(() => expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/auditoria?'))).toHaveLength(2));
    fireEvent.change(screen.getByLabelText('Fecha desde'), { target: { value: '2026-10-11' } });
    expect(await screen.findByRole('alert')).toHaveTextContent('La fecha desde no puede ser posterior a la fecha hasta.');
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/auditoria?'))).toHaveLength(2);
  });

  it('A3 muestra la acción con texto y selecciona el detalle', async () => {
    const fetchMock = prepararApiAuditoria();
    await montarAuditoriaCargada(fetchMock);
    expect(within(screen.getByRole('table', { name: 'Registros de auditoría' })).getByText('Modificación')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Ver registro 11' }));
    expect(await screen.findByRole('heading', { name: 'Modificación · Cliente #3' })).toBeInTheDocument();
    expect(screen.getByRole('table', { name: 'Cambios del registro de auditoría' })).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: '—' })).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/auditoria/11')).toBe(true);
  });

  it('A3 enlaza al historial filtrado de la misma entidad e Id', async () => {
    const fetchMock = prepararApiAuditoria();
    await montarAuditoriaCargada(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Ver registro 11' }));
    fireEvent.click(await screen.findByRole('link', { name: 'Ver historial de este registro' }));
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]).includes('/auditoria?entidad=Cliente&entidadId=3&pagina=1'))).toBe(true));
  });

  it('A3 muestra estados vacío y error del listado', async () => {
    const fetchVacio = prepararApiAuditoria({ listaVacia: true });
    renderAuditoria();
    expect(await screen.findByText('No hay registros de auditoría que coincidan con los filtros.')).toBeInTheDocument();
    expect(fetchVacio).toHaveBeenCalled();
    limpiarAuditoria();
    const fetchError = prepararApiAuditoria({ errorLista: true });
    renderAuditoria();
    expect(await screen.findByText('Falló la consulta de auditoría.')).toBeInTheDocument();
    expect(fetchError).toHaveBeenCalled();
  });

  it('A3 pagina a la segunda página conservando el tamaño 20', async () => {
    const fetchMock = prepararApiAuditoria();
    await montarAuditoriaCargada(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]) === '/api/v1/auditoria?pagina=2&tamanoPagina=20')).toBe(true));
    expect(screen.getByRole('navigation', { name: 'Paginación: Tabla de auditoría' })).toHaveTextContent('Página 2 de 2');
  });

  it('A4 representa los siete días con cantidad y fecha visibles, incluidos ceros', async () => {
    const fetchMock = prepararApiAuditoria();
    await montarAuditoriaCargada(fetchMock);
    const actividad = screen.getByRole('region', { name: 'Actividad de los últimos 7 días' });
    const barras = within(actividad).getAllByRole('meter');
    expect(barras).toHaveLength(7);
    expect(within(actividad).getAllByText('0')).toHaveLength(2);
    expect(within(actividad).getByText('15/10')).toBeInTheDocument();
    expect(within(actividad).getByLabelText('15/10/2026')).toHaveAttribute('title', '15/10/2026');
    expect(barras[1]).toHaveAccessibleName('0 registros el 10/10/2026');
    expect(barras[1]).toHaveAttribute('aria-valuenow', '0');
  });

  it('A3 muestra guion para valores nulos y cifras de cambios', async () => {
    const fetchMock = prepararApiAuditoria();
    await montarAuditoriaCargada(fetchMock);
    fireEvent.click(screen.getByRole('button', { name: 'Ver registro 11' }));
    const tablaCambios = await screen.findByRole('table', { name: 'Cambios del registro de auditoría' });
    expect(within(tablaCambios).getByRole('cell', { name: 'Acme Industrial' })).toBeInTheDocument();
    expect(within(tablaCambios).getByRole('cell', { name: '—' })).toBeInTheDocument();
    expect(detalleMock.cambios).toEqual(cambiosMock);
    expect(registrosMock[0].camposCambiados).toBe(2);
    expect(await screen.findByRole('link', { name: 'Ver historial de este registro' })).toBeInTheDocument();
  });
});
