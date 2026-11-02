import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  actualizarEmpleado,
  crearEmpleado,
  listarEmpleados,
  obtenerEmpleado,
  registrarCeseEmpleado,
} from './api';
import type { GuardarEmpleadoRequest } from './tipos';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

const cuerpoEmpleado: GuardarEmpleadoRequest = {
  tipoDocumento: 'DNI',
  numeroDocumento: '10000009',
  nombres: 'Pedro',
  apellidos: 'Quispe Lima',
  fechaNacimiento: '1992-04-10',
  fechaIngreso: '2024-02-01',
  cargo: 'Operario',
  area: 'Almacen',
  email: null,
  telefono: null,
};

describe('API de empleados', () => {
  it('lista empleados con los filtros exactos en la query', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarEmpleados({ busqueda: ' ana ', estado: 'Cesado', pagina: 2, tamanoPagina: 10 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/empleados?busqueda=ana&estado=Cesado&pagina=2&tamanoPagina=10');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('omite busqueda vacía y estado «Todos» al listar', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarEmpleados({ busqueda: '   ', estado: '', pagina: 1, tamanoPagina: 1 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/empleados?pagina=1&tamanoPagina=1');
  });

  it('obtiene el empleado por id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 4 }));
    vi.stubGlobal('fetch', fetchMock);
    await obtenerEmpleado(4);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/empleados/4');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'GET' });
  });

  it('crea empleados con POST y el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 5 }));
    vi.stubGlobal('fetch', fetchMock);
    await crearEmpleado(cuerpoEmpleado);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/empleados');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cuerpoEmpleado) });
  });

  it('actualiza el empleado con PUT, su id y el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 1 }));
    vi.stubGlobal('fetch', fetchMock);
    await actualizarEmpleado(1, cuerpoEmpleado);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/empleados/1');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify(cuerpoEmpleado) });
  });

  it('registra el cese con POST en /cese y el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 1 }));
    vi.stubGlobal('fetch', fetchMock);
    const cese = { fechaCese: '2026-10-31', motivo: 'Renuncia voluntaria' };
    await registrarCeseEmpleado(1, cese);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/empleados/1/cese');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(cese) });
  });
});
