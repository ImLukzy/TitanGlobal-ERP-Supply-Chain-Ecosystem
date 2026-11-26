import { listarExamenes } from './api';
import { useListado } from './presentacion';
import type { FiltrosExamenes } from './tipos';

export function useExamenesData(filtros: FiltrosExamenes) {
  return useListado(listarExamenes, filtros, 'No se pudo cargar la lista de exámenes médicos.');
}
