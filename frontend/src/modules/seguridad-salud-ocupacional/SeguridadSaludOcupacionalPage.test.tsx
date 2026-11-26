import { fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import {
  ANIO_PRUEBAS,
  esperarCargaIncidentes,
  limpiarSst,
  montarEppCargado,
  montarExamenesCargados,
  montarIncidentesCargados,
  prepararApiSst,
  renderSst,
  urlsQueContienen,
} from './sst.testUtils';

afterEach(() => {
  limpiarSst();
});

describe('página de Seguridad y salud: pestañas', () => {
  it('abre Incidentes por defecto y con un valor desconocido de ?vista=', async () => {
    const { fetchMock } = prepararApiSst();
    renderSst(['RRHH'], '/modulos/70-seguridad-salud-ocupacional?vista=otra');
    await esperarCargaIncidentes(fetchMock);
    expect(screen.getByRole('tab', { name: 'Incidentes' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-sst-incidentes');
  });

  it('abre EPP con ?vista=epp y Exámenes médicos con ?vista=examenes', async () => {
    const { fetchMock } = prepararApiSst();
    await montarEppCargado(fetchMock);
    expect(screen.getByRole('tab', { name: 'EPP' })).toHaveAttribute('aria-selected', 'true');
    limpiarSst();
    const otra = prepararApiSst();
    await montarExamenesCargados(otra.fetchMock);
    expect(screen.getByRole('tab', { name: 'Exámenes médicos' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'tab-sst-examenes');
  });

  it('cambia de pestaña con clic y con las flechas, y solo la activa tiene tabIndex 0', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    expect(screen.getByRole('tab', { name: 'EPP' })).toHaveAttribute('tabindex', '-1');
    fireEvent.click(screen.getByRole('tab', { name: 'EPP' }));
    expect(await screen.findByRole('table', { name: 'Listado de entregas de EPP' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Incidentes' })).toHaveAttribute('tabindex', '-1');
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowRight' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Exámenes médicos' })).toHaveAttribute('aria-selected', 'true'));
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowRight' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Incidentes' })).toHaveAttribute('aria-selected', 'true'));
    fireEvent.keyDown(screen.getByRole('tablist'), { key: 'ArrowLeft' });
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Exámenes médicos' })).toHaveAttribute('aria-selected', 'true'));
  });
});

describe('página de Seguridad y salud: resumen', () => {
  it('muestra el resumen del año actual en UTC con cifras solas y avisos con texto', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    expect(urlsQueContienen(fetchMock, '/incidentes-sst/resumen')).toEqual([`/api/v1/incidentes-sst/resumen?anio=${ANIO_PRUEBAS}`]);
    const resumen = screen.getByRole('region', { name: `Resumen de seguridad y salud ${ANIO_PRUEBAS}` });
    expect(resumen).toHaveTextContent('Incidentes abiertos2');
    expect(resumen).toHaveTextContent('Días de baja5');
    expect(resumen).toHaveTextContent('EPP vencidos1Requiere atención');
    expect(resumen).toHaveTextContent('Exámenes vencidos1Requiere atención');
    expect(resumen).toHaveTextContent('Empleados no aptos0');
    expect(resumen).not.toHaveTextContent('Empleados no aptos0Requiere atención');
  });

  it('el selector de año vuelve a pedir el resumen con el año elegido', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    fireEvent.change(screen.getByLabelText('Año'), { target: { value: String(ANIO_PRUEBAS + 1) } });
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/incidentes-sst/resumen').at(-1)).toBe(`/api/v1/incidentes-sst/resumen?anio=${ANIO_PRUEBAS + 1}`));
  });

  it('cada número enlaza a su pestaña con el filtro aplicado', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    expect(screen.getByRole('link', { name: /Accidentes con baja/ })).toHaveAttribute('href', `/modulos/70-seguridad-salud-ocupacional?vista=incidentes&desde=${ANIO_PRUEBAS}-01-01&hasta=${ANIO_PRUEBAS}-12-31&tipo=AccidenteConBaja`);
    expect(screen.queryByRole('link', { name: /Días de baja/ })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('link', { name: /Incidentes abiertos/ }));
    await waitFor(() => expect(screen.getByLabelText('Estado')).toHaveValue('Abierto'));
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/incidentes-sst?').at(-1)).toBe('/api/v1/incidentes-sst?estado=Abierto&pagina=1&tamanoPagina=10'));
  });

  it('EPP vencidos abre EPP con «Solo vencidas» y Empleados no aptos abre Exámenes con NoApto', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    fireEvent.click(screen.getByRole('link', { name: /EPP vencidos/ }));
    await waitFor(() => expect(screen.getByLabelText('Solo vencidas')).toBeChecked());
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/entregas-epp?').at(-1)).toBe('/api/v1/entregas-epp?vencidas=true&pagina=1&tamanoPagina=10'));
    expect(await screen.findByRole('tab', { name: 'EPP' })).toHaveAttribute('aria-selected', 'true');
    fireEvent.click(screen.getByRole('link', { name: /Empleados no aptos/ }));
    await waitFor(() => expect(screen.getByLabelText('Resultado')).toHaveValue('NoApto'));
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/examenes-medicos?').at(-1)).toBe('/api/v1/examenes-medicos?resultado=NoApto&pagina=1&tamanoPagina=10'));
  });

  it('Exámenes vencidos abre Exámenes con «Solo vencidos»', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock);
    fireEvent.click(screen.getByRole('link', { name: /Exámenes vencidos/ }));
    await waitFor(() => expect(screen.getByLabelText('Solo vencidos')).toBeChecked());
    await waitFor(() => expect(urlsQueContienen(fetchMock, '/examenes-medicos?').at(-1)).toBe('/api/v1/examenes-medicos?vencidos=true&pagina=1&tamanoPagina=10'));
  });
});

describe('página de Seguridad y salud: roles', () => {
  it('RRHH y ADMIN pueden escribir; otros roles no y ven el aviso', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock, ['ADMIN']);
    expect(screen.getByRole('button', { name: 'Nuevo incidente' })).toBeEnabled();
    expect(screen.queryByText('Solo RRHH o ADMIN pueden modificar seguridad y salud.')).not.toBeInTheDocument();
    limpiarSst();
    const otra = prepararApiSst();
    await montarIncidentesCargados(otra.fetchMock, ['VENTAS']);
    expect(screen.getByRole('button', { name: 'Nuevo incidente' })).toBeDisabled();
    expect(screen.getByText('Solo RRHH o ADMIN pueden modificar seguridad y salud.')).toBeInTheDocument();
  });

  it('sin RRHH ni ADMIN quedan deshabilitadas las acciones de EPP y Exámenes', async () => {
    const { fetchMock } = prepararApiSst();
    await montarEppCargado(fetchMock, ['VENTAS']);
    expect(screen.getByRole('button', { name: 'Nueva entrega' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Nuevo equipo' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Editar equipo CASCO' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Desactivar equipo CASCO' })).toBeDisabled();
    expect(screen.getAllByText('Solo RRHH o ADMIN pueden modificar seguridad y salud.').length).toBeGreaterThan(0);
    limpiarSst();
    const otra = prepararApiSst();
    await montarExamenesCargados(otra.fetchMock, ['VENTAS']);
    expect(screen.getByRole('button', { name: 'Nuevo examen' })).toBeDisabled();
  });

  it('sin RRHH ni ADMIN el detalle del incidente no permite agregar, completar ni cerrar', async () => {
    const { fetchMock } = prepararApiSst();
    await montarIncidentesCargados(fetchMock, ['VENTAS']);
    fireEvent.click(screen.getByRole('button', { name: 'INC-000001' }));
    expect(await screen.findByRole('button', { name: 'Agregar acción' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Cerrar incidente' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Completar acción Instalar guarda' })).toBeDisabled();
  });
});
