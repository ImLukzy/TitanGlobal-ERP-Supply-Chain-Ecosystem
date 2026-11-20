import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { CentrosTrabajoPage } from './CentrosTrabajoPage';
import type { CentroTrabajoDetalle, GuardarCentroTrabajoRequest, ResumenCentrosTrabajo } from './tipos';

export const centrosBase: CentroTrabajoDetalle[] = [
  { id: 1, codigo: 'CT-LIM-L01', nombre: 'Línea de envasado 1', tipo: 'Linea', planta: 'Planta Lima', capacidadHorasDia: 16, costoHora: 120, responsable: 'Carlos Ramos', activo: true },
  { id: 2, codigo: 'CT-LIM-L02', nombre: 'Línea de envasado 2', tipo: 'Linea', planta: 'Planta Lima', capacidadHorasDia: 8, costoHora: 110, responsable: 'Carlos Ramos', activo: true },
  { id: 3, codigo: 'CT-LIM-C01', nombre: 'Celda de ensamblaje', tipo: 'Celda', planta: 'Planta Lima', capacidadHorasDia: 8, costoHora: 85.5, responsable: 'Rosa Quispe', activo: true },
  { id: 4, codigo: 'CT-ARE-L01', nombre: 'Línea de mezclado', tipo: 'Linea', planta: 'Planta Arequipa', capacidadHorasDia: 24, costoHora: 150, responsable: 'Jorge Mamani', activo: false },
  { id: 5, codigo: 'CT-ARE-C01', nombre: 'Celda de etiquetado', tipo: 'Celda', planta: 'Planta Arequipa', capacidadHorasDia: 12, costoHora: 60, responsable: null, activo: true },
];

export function resumenDe(filas: CentroTrabajoDetalle[]): ResumenCentrosTrabajo {
  const activos = filas.filter((centro) => centro.activo);
  return {
    totalCentros: filas.length, activos: activos.length, inactivos: filas.length - activos.length,
    capacidadTotalHorasDia: filas.reduce((total, centro) => total + centro.capacidadHorasDia, 0),
    capacidadActivaHorasDia: activos.reduce((total, centro) => total + centro.capacidadHorasDia, 0),
    porTipo: (['Linea', 'Celda'] as const).flatMap((tipo) => {
      const coincidencias = filas.filter((centro) => centro.tipo === tipo);
      return coincidencias.length === 0 ? [] : [{ tipo, totalCentros: coincidencias.length, capacidadHorasDia: coincidencias.reduce((total, centro) => total + centro.capacidadHorasDia, 0) }];
    }),
    porPlanta: [...new Set(filas.map((centro) => centro.planta))].map((planta) => {
      const coincidencias = filas.filter((centro) => centro.planta === planta);
      return { planta, totalCentros: coincidencias.length, activos: coincidencias.filter((centro) => centro.activo).length };
    }).sort((a, b) => b.totalCentros - a.totalCentros || a.planta.localeCompare(b.planta)),
  };
}

interface Opciones {
  filas?: CentroTrabajoDetalle[];
  errorLista?: boolean;
  errorGuardar?: 400 | 409;
  errorEliminar?: boolean;
  errorDetalle?: boolean;
  errorResumen?: boolean;
  errorPlantas?: boolean;
}

export function prepararApiCentros(opciones: Opciones = {}) {
  const filas = structuredClone(opciones.filas ?? centrosBase);
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = new URL(String(input), 'http://localhost');
    const metodo = init.method ?? 'GET';
    const ruta = url.pathname;
    function error(codigo: number, mensaje: string): Response {
      return Response.json({ StatusCode: codigo, Message: mensaje }, { status: codigo });
    }
    if (ruta.endsWith('/plantas')) {
      return opciones.errorPlantas ? error(500, 'No se pudieron cargar las plantas.') : Response.json([...new Set(filas.map((centro) => centro.planta))].sort());
    }
    if (ruta.endsWith('/resumen')) {
      return opciones.errorResumen ? error(500, 'No se pudo cargar el resumen de centros de trabajo.') : Response.json(resumenDe(filas));
    }
    const id = Number(ruta.split('/').at(-1));
    if (metodo === 'POST' || metodo === 'PUT') {
      const request = JSON.parse(String(init.body)) as GuardarCentroTrabajoRequest;
      if (opciones.errorGuardar === 400) { return error(400, 'La planta no puede superar 100 caracteres.'); }
      if (opciones.errorGuardar === 409) { return error(409, `Ya existe un centro de trabajo con el código ${request.codigo}.`); }
      if (metodo === 'POST') {
        const nuevoId = Math.max(0, ...filas.map((centro) => centro.id)) + 1;
        filas.push({ id: nuevoId, ...request, responsable: request.responsable ?? null });
        return Response.json({ id: nuevoId });
      }
      const indice = filas.findIndex((centro) => centro.id === id);
      if (indice < 0) { return error(404, `El centro de trabajo ${id} no existe.`); }
      filas[indice] = { id, ...request, responsable: request.responsable ?? null };
      return Response.json({ id });
    }
    if (metodo === 'DELETE') {
      const indice = filas.findIndex((centro) => centro.id === id);
      if (opciones.errorEliminar || indice < 0) { return error(404, `El centro de trabajo ${id} no existe.`); }
      filas.splice(indice, 1);
      return new Response(null, { status: 204 });
    }
    if (Number.isFinite(id)) {
      const centro = filas.find((fila) => fila.id === id);
      return centro === undefined || opciones.errorDetalle ? error(404, `El centro de trabajo ${id} no existe.`) : Response.json(centro);
    }
    if (opciones.errorLista) { return error(500, 'No se pudo cargar la lista de centros de trabajo.'); }
    const busqueda = (url.searchParams.get('busqueda') ?? '').toLowerCase();
    const planta = (url.searchParams.get('planta') ?? '').toLowerCase();
    const tipo = url.searchParams.get('tipo');
    const activo = url.searchParams.get('activo');
    const pagina = Number(url.searchParams.get('pagina') ?? 1);
    const coincidencias = filas.filter((centro) => (
      (busqueda === '' || `${centro.codigo} ${centro.nombre}`.toLowerCase().includes(busqueda))
      && (planta === '' || centro.planta.toLowerCase() === planta)
      && (tipo === null || centro.tipo === tipo) && (activo === null || String(centro.activo) === activo)
    )).sort((a, b) => a.codigo.localeCompare(b.codigo) || a.id - b.id);
    return Response.json({ items: coincidencias.slice((pagina - 1) * 10, pagina * 10), total: coincidencias.length, pagina, tamanoPagina: 10 });
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, filas };
}

export function renderCentros() {
  return render(<MemoryRouter initialEntries={['/modulos/33-centros-trabajo']}><CentrosTrabajoPage /></MemoryRouter>);
}

export async function esperarCargaCentros(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await screen.findByRole('button', { name: 'CT-ARE-C01' });
  await screen.findByRole('button', { name: 'Editar centro de trabajo' });
  await screen.findByRole('option', { name: 'Planta Lima' });
  await screen.findByText('68.00');
  await waitFor(() => expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/centros-trabajo?'))).toHaveLength(1));
}
