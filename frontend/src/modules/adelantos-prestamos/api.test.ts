import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  anularPrestamo,
  listarPrestamos,
  obtenerCapacidadPrestamo,
  obtenerPrestamo,
  obtenerResumenPrestamos,
  registrarPrestamo,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de préstamos', () => {
  it('lista préstamos con todos los filtros exactos en la query', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarPrestamos({ busqueda: ' ana ', tipo: 'Prestamo', estado: 'Activo', empleadoId: 7, pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/prestamos?busqueda=ana&empleadoId=7&tipo=Prestamo&estado=Activo&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite los filtros vacíos', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarPrestamos({ busqueda: '  ', tipo: '', estado: '', empleadoId: null, pagina: 1, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/prestamos?pagina=1&tamanoPagina=10');
  });

  it('pide el resumen', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ activos: 1 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerResumenPrestamos();
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/prestamos/resumen');
  });

  it('pide el detalle por id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 4 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerPrestamo(4);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/prestamos/4');
  });

  it('pide la capacidad con y sin fecha', async () => {
    const fetchMock = vi.fn().mockImplementation(async () => respuestaJson({ empleadoId: 1 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerCapacidadPrestamo(1, '2026-11-22');
    await obtenerCapacidadPrestamo(1, '');
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/prestamos/capacidad/1?fecha=2026-11-22');
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/v1/prestamos/capacidad/1');
  });

  it('registra un préstamo con el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 5, numero: 'PRE-000005' }));
    vi.stubGlobal('fetch', fetchMock);
    const request = {
      empleadoId: 1,
      tipo: 'Prestamo' as const,
      monto: 1000,
      numeroCuotas: 3,
      fechaOtorgamiento: '2026-11-22',
      primerAnio: 2026,
      primerMes: 12,
      motivo: 'Gastos médicos',
    };
    await expect(registrarPrestamo(request)).resolves.toEqual({ id: 5, numero: 'PRE-000005' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/prestamos');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(request) });
  });

  it('anula un préstamo con el motivo', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 4 }));
    vi.stubGlobal('fetch', fetchMock);
    await anularPrestamo(4, { motivo: 'Error de carga' });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/prestamos/4/anulacion');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify({ motivo: 'Error de carga' }) });
  });

  it('propaga el mensaje literal del servidor en un 409', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ StatusCode: 409, Message: 'El préstamo PRE-000001 ya tiene descuentos y no se puede anular.' }, 409));
    vi.stubGlobal('fetch', fetchMock);
    await expect(anularPrestamo(1, { motivo: 'x' })).rejects.toThrow('El préstamo PRE-000001 ya tiene descuentos y no se puede anular.');
  });
});
