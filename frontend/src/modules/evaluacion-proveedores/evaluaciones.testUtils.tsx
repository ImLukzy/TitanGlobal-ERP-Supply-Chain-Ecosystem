import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router';
import { vi } from 'vitest';
import type { EvaluacionDetalle, ResumenEvaluaciones } from './tipos';

export const proveedores = [
  { id: 1, razonSocial: 'Andes Suministros S.A.C.' },
  { id: 2, razonSocial: 'Cobre Austral SpA' },
];

export const evaluaciones: EvaluacionDetalle[] = [
  {
    id: 1,
    proveedorId: 1,
    proveedorNombre: 'Andes Suministros S.A.C.',
    periodo: '2026-09',
    puntajeCumplimiento: 90,
    puntajeCalidad: 85,
    puntajePlazos: 80,
    puntajeFinal: 85.5,
    calificacion: 'Bueno',
    observaciones: 'Entregas completas, un retraso menor.',
    fechaRegistro: '2026-09-30T12:00:00Z',
  },
  {
    id: 2,
    proveedorId: 1,
    proveedorNombre: 'Andes Suministros S.A.C.',
    periodo: '2026-10',
    puntajeCumplimiento: 95,
    puntajeCalidad: 92,
    puntajePlazos: 90,
    puntajeFinal: 92.6,
    calificacion: 'Excelente',
    observaciones: null,
    fechaRegistro: '2026-10-31T12:00:00Z',
  },
  {
    id: 3,
    proveedorId: 2,
    proveedorNombre: 'Cobre Austral SpA',
    periodo: '2026-10',
    puntajeCumplimiento: 70,
    puntajeCalidad: 65,
    puntajePlazos: 60,
    puntajeFinal: 65.5,
    calificacion: 'Regular',
    observaciones: 'Varios lotes fuera de especificación.',
    fechaRegistro: '2026-10-31T12:00:00Z',
  },
];

export const resumen: ResumenEvaluaciones = {
  totalEvaluaciones: 6,
  proveedoresEvaluados: 4,
  promedioGeneral: 78.72,
  porCalificacion: [
    { calificacion: 'Excelente', total: 2 },
    { calificacion: 'Bueno', total: 2 },
    { calificacion: 'Regular', total: 1 },
    { calificacion: 'Deficiente', total: 1 },
  ],
  ranking: [
    { proveedorId: 6, proveedorNombre: 'Pacífico Logística', evaluaciones: 1, promedio: 95.5, ultimaCalificacion: 'Excelente' },
    { proveedorId: 1, proveedorNombre: 'Andes Suministros S.A.C.', evaluaciones: 2, promedio: 89.05, ultimaCalificacion: 'Excelente' },
    { proveedorId: 2, proveedorNombre: 'Cobre Austral SpA', evaluaciones: 1, promedio: 65.5, ultimaCalificacion: 'Regular' },
  ],
};

interface ApiOpciones {
  vacio?: boolean;
  errorLista?: boolean;
  errorCrear?: boolean;
  errorBaja?: boolean;
  totalFilas?: number;
}

function paraListado(evaluacion: EvaluacionDetalle) {
  return {
    id: evaluacion.id,
    proveedorId: evaluacion.proveedorId,
    proveedorNombre: evaluacion.proveedorNombre,
    periodo: evaluacion.periodo,
    puntajeFinal: evaluacion.puntajeFinal,
    calificacion: evaluacion.calificacion,
  };
}

export function prepararApi(opciones: ApiOpciones = {}) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const metodo = init.method ?? 'GET';
    if (url.endsWith('/evaluaciones-proveedores/resumen')) {
      return Response.json(resumen);
    }
    if (url.endsWith('/proveedores?tamanoPagina=100')) {
      return Response.json({ items: proveedores });
    }
    if (metodo === 'POST' && url.endsWith('/evaluaciones-proveedores')) {
      if (opciones.errorCrear) {
        return Response.json({ StatusCode: 409, Message: 'El proveedor 1 ya tiene una evaluación del periodo 2026-10.' }, { status: 409 });
      }
      return Response.json({ id: 5 });
    }
    if (metodo === 'PUT') {
      return Response.json({ id: Number(url.split('/').at(-1)) });
    }
    if (metodo === 'DELETE') {
      if (opciones.errorBaja) {
        return Response.json({ StatusCode: 404, Message: 'La evaluación 2 no existe.' }, { status: 404 });
      }
      return new Response(null, { status: 204 });
    }
    const detalle = /\/evaluaciones-proveedores\/(\d+)$/.exec(url);
    if (detalle !== null) {
      const encontrada = evaluaciones.find((evaluacion) => evaluacion.id === Number(detalle[1]));
      return encontrada === undefined
        ? Response.json({ StatusCode: 404, Message: 'La evaluación no existe.' }, { status: 404 })
        : Response.json(encontrada);
    }
    if (url.includes('/evaluaciones-proveedores?')) {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'No se pudieron cargar las evaluaciones.' }, { status: 500 });
      }
      if (opciones.vacio) {
        return Response.json({ items: [], total: 0, pagina: 1, tamanoPagina: 10 });
      }
      const pagina = Number(new URL(url, 'http://local').searchParams.get('pagina') ?? '1');
      return Response.json({
        items: evaluaciones.map(paraListado),
        total: opciones.totalFilas ?? evaluaciones.length,
        pagina,
        tamanoPagina: 10,
      });
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock };
}

export function renderEnRouter(elemento: ReactElement, initialEntries = ['/modulos/12-evaluacion-proveedores']) {
  return render(<MemoryRouter initialEntries={initialEntries}>{elemento}</MemoryRouter>);
}

export async function esperarCargaInicial(): Promise<void> {
  await screen.findByRole('button', { name: 'Ver evaluación de Andes Suministros S.A.C. del periodo 2026-10' }, { timeout: 5000 });
  await screen.findByText('78,72', {}, { timeout: 5000 });
  await screen.findByRole('option', { name: 'Cobre Austral SpA' }, { timeout: 5000 });
}

export function cambiarValor(etiqueta: string, valor: string): void {
  fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } });
}

export function llamadasLista(fetchMock: ReturnType<typeof prepararApi>['fetchMock']): string[] {
  return fetchMock.mock.calls.map((call) => String(call[0])).filter((url) => url.includes('/evaluaciones-proveedores?'));
}
