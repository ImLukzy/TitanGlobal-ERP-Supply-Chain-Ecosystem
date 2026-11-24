import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { vi } from 'vitest';
import { ConductoresPage } from './ConductoresPage';
import type { ConductorDetalle, GuardarConductorRequest } from './tipos';

export function fechaEnDias(dias: number, hora = 0): string {
  const fecha = new Date();
  fecha.setUTCDate(fecha.getUTCDate() + dias);
  fecha.setUTCHours(hora, 0, 0, 0);
  return fecha.toISOString();
}

export const conductoresBase: ConductorDetalle[] = [
  { id: 1, nombre: 'Carlos Ruiz', documentoLicencia: 'PLT-0001', vencimientoLicencia: fechaEnDias(365), habilitado: true, activo: true, observaciones: null },
  { id: 2, nombre: 'Maria Quispe', documentoLicencia: 'PLT-0002', vencimientoLicencia: fechaEnDias(-1, 13), habilitado: true, activo: true, observaciones: 'Licencia pendiente de renovación' },
  { id: 3, nombre: 'Jose Flores', documentoLicencia: 'PLT-0003', vencimientoLicencia: fechaEnDias(200), habilitado: false, activo: false, observaciones: null },
];

interface Opciones {
  filas?: ConductorDetalle[];
  errorGuardar?: 400 | 409;
  errorLista?: boolean;
  errorEliminar?: boolean;
}

export function prepararApiConductores(opciones: Opciones = {}) {
  const filas = structuredClone(opciones.filas ?? conductoresBase);
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const url = new URL(String(input), 'http://localhost');
    const metodo = init?.method ?? 'GET';
    if (url.pathname === '/api/v1/conductores' && metodo === 'GET') {
      if (opciones.errorLista) { return Response.json({ StatusCode: 500, Message: 'Lista no disponible.' }, { status: 500 }); }
      const consulta = url.searchParams;
      const busqueda = (consulta.get('busqueda') ?? '').toLowerCase();
      const habilitado = consulta.get('habilitado');
      const resultados = filas.filter((item) => `${item.nombre} ${item.documentoLicencia}`.toLowerCase().includes(busqueda)
        && (habilitado === null || item.habilitado === (habilitado === 'true')))
        .sort((a, b) => a.documentoLicencia.localeCompare(b.documentoLicencia) || a.id - b.id);
      const pagina = Number(consulta.get('pagina') ?? 1);
      const tamanoPagina = Number(consulta.get('tamanoPagina') ?? 10);
      return Response.json({ items: resultados.slice((pagina - 1) * tamanoPagina, pagina * tamanoPagina), total: resultados.length, pagina, tamanoPagina });
    }
    const id = Number(url.pathname.split('/').at(-1));
    if (metodo === 'POST' || metodo === 'PUT') {
      if (opciones.errorGuardar) {
        const Message = opciones.errorGuardar === 409 ? 'Ya existe un conductor con el documento de licencia PLT-QA-01.' : 'La fecha de vencimiento de la licencia no es válida.';
        return Response.json({ StatusCode: opciones.errorGuardar, Message }, { status: opciones.errorGuardar });
      }
      const solicitud = JSON.parse(String(init?.body)) as GuardarConductorRequest;
      const nuevoId = metodo === 'POST' ? Math.max(0, ...filas.map((item) => item.id)) + 1 : id;
      const conductor = { ...solicitud, id: nuevoId, observaciones: solicitud.observaciones ?? null };
      if (metodo === 'POST') { filas.push(conductor); }
      else { filas.splice(filas.findIndex((item) => item.id === id), 1, conductor); }
      return Response.json({ id: nuevoId });
    }
    if (metodo === 'DELETE') {
      if (opciones.errorEliminar) { return Response.json({ StatusCode: 404, Message: `El conductor ${id} no existe.` }, { status: 404 }); }
      filas.splice(filas.findIndex((item) => item.id === id), 1);
      return new Response(null, { status: 204 });
    }
    const conductor = filas.find((item) => item.id === id);
    return conductor ? Response.json(conductor) : Response.json({ StatusCode: 404, Message: `El conductor ${id} no existe.` }, { status: 404 });
  });
  vi.stubGlobal('fetch', fetchMock);
  return { filas, fetchMock };
}

export function renderConductores() {
  return render(<MemoryRouter initialEntries={['/modulos/42-conductores']}><ConductoresPage /></MemoryRouter>);
}

export async function esperarCargaConductores() {
  await screen.findByRole('table', { name: 'Conductores registrados' });
  await screen.findByRole('button', { name: 'Editar conductor' });
}
