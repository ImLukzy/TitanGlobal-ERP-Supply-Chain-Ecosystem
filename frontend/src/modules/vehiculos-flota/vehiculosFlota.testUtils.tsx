import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { VehiculosFlotaPage } from './VehiculosFlotaPage';
import type { GuardarVehiculoFlotaRequest, ResumenVehiculosFlota, VehiculoFlotaDetalle } from './tipos';

export const vehiculosBase: VehiculoFlotaDetalle[] = [
  { id: 1, placa: 'ABC-123', marca: 'Volvo', modelo: 'FH', anio: 2025, tipo: 'Camion', capacidadKg: 15000, activo: true, observaciones: null },
  { id: 2, placa: 'XYZ-789', marca: 'Toyota', modelo: 'Hiace', anio: 2024, tipo: 'Furgon', capacidadKg: 1200.5, activo: false, observaciones: 'En mantenimiento' },
];

export function prepararApiVehiculos(opciones: { filas?: VehiculoFlotaDetalle[]; errorGuardar?: 400 | 409; errorLista?: boolean; errorEliminar?: boolean } = {}) {
  const filas = structuredClone(opciones.filas ?? vehiculosBase);
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const url = new URL(String(input), 'http://localhost');
    const metodo = init?.method ?? 'GET';
    const ruta = url.pathname;
    if (ruta.endsWith('/tipos')) { return Response.json([...new Set(filas.map((item) => item.tipo))].sort()); }
    if (ruta.endsWith('/resumen')) {
      const resumen: ResumenVehiculosFlota = {
        totalVehiculos: filas.length, activos: filas.filter((item) => item.activo).length,
        inactivos: filas.filter((item) => !item.activo).length,
        capacidadTotalKg: filas.reduce((total, item) => total + item.capacidadKg, 0),
        porTipo: (['Camion', 'Furgon', 'Moto', 'Otro'] as const).flatMap((tipo) => {
          const grupo = filas.filter((item) => item.tipo === tipo);
          return grupo.length === 0 ? [] : [{ tipo, totalVehiculos: grupo.length, activos: grupo.filter((item) => item.activo).length }];
        }),
      };
      return Response.json(resumen);
    }
    if (ruta === '/api/v1/vehiculos-flota' && metodo === 'GET') {
      if (opciones.errorLista) { return Response.json({ StatusCode: 500, Message: 'Lista no disponible.' }, { status: 500 }); }
      const consulta = url.searchParams;
      const busqueda = (consulta.get('busqueda') ?? '').toLowerCase();
      const tipo = consulta.get('tipo');
      const activo = consulta.get('activo');
      const resultados = filas.filter((item) => `${item.placa} ${item.marca} ${item.modelo}`.toLowerCase().includes(busqueda)
        && (!tipo || item.tipo === tipo) && (!activo || item.activo === (activo === 'true')))
        .sort((a, b) => a.placa.localeCompare(b.placa));
      const pagina = Number(consulta.get('pagina') ?? 1);
      const tamanoPagina = Number(consulta.get('tamanoPagina') ?? 10);
      return Response.json({ items: resultados.slice((pagina - 1) * tamanoPagina, pagina * tamanoPagina), total: resultados.length, pagina, tamanoPagina });
    }
    const id = Number(ruta.split('/').at(-1));
    if (metodo === 'POST' || metodo === 'PUT') {
      if (opciones.errorGuardar) {
        const Message = opciones.errorGuardar === 409 ? 'Ya existe un vehículo con la placa NUE-001.' : 'La capacidad debe ser mayor que cero.';
        return Response.json({ StatusCode: opciones.errorGuardar, Message }, { status: opciones.errorGuardar });
      }
      const solicitud = JSON.parse(String(init?.body)) as GuardarVehiculoFlotaRequest;
      const nuevoId = metodo === 'POST' ? Math.max(0, ...filas.map((item) => item.id)) + 1 : id;
      const vehiculo = { ...solicitud, id: nuevoId, observaciones: solicitud.observaciones ?? null };
      if (metodo === 'POST') { filas.push(vehiculo); }
      else { filas.splice(filas.findIndex((item) => item.id === id), 1, vehiculo); }
      return Response.json({ id: nuevoId });
    }
    if (metodo === 'DELETE') {
      if (opciones.errorEliminar) { return Response.json({ StatusCode: 409, Message: 'No se puede eliminar el vehículo.' }, { status: 409 }); }
      filas.splice(filas.findIndex((item) => item.id === id), 1);
      return new Response(null, { status: 204 });
    }
    const vehiculo = filas.find((item) => item.id === id);
    return vehiculo ? Response.json(vehiculo) : Response.json({ StatusCode: 404, Message: `El vehículo ${id} no existe.` }, { status: 404 });
  });
  vi.stubGlobal('fetch', fetchMock);
  return { filas, fetchMock };
}

export function renderVehiculos() {
  return render(<MemoryRouter><VehiculosFlotaPage /></MemoryRouter>);
}

export async function esperarCargaVehiculos(fetchMock: ReturnType<typeof prepararApiVehiculos>['fetchMock']) {
  await screen.findByRole('table', { name: 'Vehículos de flota registrados' });
  await screen.findByRole('button', { name: 'Editar vehículo' });
  await screen.findByRole('region', { name: 'Resumen de vehículos de flota' });
  await waitFor(() => expect(fetchMock.mock.calls.some((call) => String(call[0]).endsWith('/tipos'))).toBe(true));
}
