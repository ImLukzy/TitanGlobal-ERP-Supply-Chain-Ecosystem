import { obtenerCurso, listarCursos } from './api';
import { useListado, usePorId } from './presentacion';
import type { FiltrosCursos } from './tipos';

export function useCursosData(filtros: FiltrosCursos) {
  return useListado(listarCursos, filtros, 'No se pudo cargar la lista de cursos.');
}

export function useCursoDetalle(id: number | null, revision: number) {
  return usePorId(id, obtenerCurso, revision, 'No se pudo cargar el curso.');
}
