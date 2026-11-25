import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  agregarAdendaContrato,
  anularContratoLaboral,
  crearContratoLaboral,
  listarContratosLaborales,
  obtenerContratoLaboral,
  renovarContratoLaboral,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de contratos laborales', () => {
  it('lista contratos con todos los filtros exactos en la query', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarContratosLaborales({
      busqueda: ' ana ',
      estado: 'Vencido',
      modalidad: 'PlazoFijo',
      empleadoId: 7,
      pagina: 2,
      tamanoPagina: 10,
    });
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      '/api/v1/contratos-laborales?busqueda=ana&estado=Vencido&modalidad=PlazoFijo&empleadoId=7&pagina=2&tamanoPagina=10',
    );
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite busqueda, estado, modalidad y empleadoId vacíos', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarContratosLaborales({ busqueda: '  ', estado: '', modalidad: '', empleadoId: null, pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/contratos-laborales?pagina=1&tamanoPagina=10');
  });

  it('obtiene el detalle por id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 5 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerContratoLaboral(5);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/contratos-laborales/5');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea un contrato con POST y el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 9, numero: 'CTR-000009' }));
    vi.stubGlobal('fetch', fetchMock);
    const cuerpo = {
      empleadoId: 3,
      modalidad: 'PlazoFijo' as const,
      fechaInicio: '2026-12-01',
      fechaFin: '2027-11-30',
      sueldoBasico: 2500.5,
      cargo: 'Operario',
    };
    const resultado = await crearContratoLaboral(cuerpo);
    expect(resultado).toEqual({ id: 9, numero: 'CTR-000009' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/contratos-laborales');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpo) });
  });

  it('renueva con POST a /renovacion', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 10, numero: 'CTR-000010' }));
    vi.stubGlobal('fetch', fetchMock);
    const cuerpo = { modalidad: 'Indeterminado' as const, fechaFin: null, sueldoBasico: 3000, cargo: 'Analista' };
    await renovarContratoLaboral(8, cuerpo);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/contratos-laborales/8/renovacion');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpo) });
  });

  it('agrega una adenda con POST a /adendas', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 8 }));
    vi.stubGlobal('fetch', fetchMock);
    const cuerpo = { fechaVigencia: '2026-08-01', sueldoBasico: 2800 };
    await agregarAdendaContrato(8, cuerpo);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/contratos-laborales/8/adendas');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpo) });
  });

  it('anula con POST a /anulacion', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 8 }));
    vi.stubGlobal('fetch', fetchMock);
    await anularContratoLaboral(8, { motivo: 'Registro duplicado' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/contratos-laborales/8/anulacion');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ motivo: 'Registro duplicado' }) });
  });
});
