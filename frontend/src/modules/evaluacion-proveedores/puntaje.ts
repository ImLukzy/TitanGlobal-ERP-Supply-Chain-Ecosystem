import type { CalificacionEvaluacion } from './tipos';

export const calificaciones: CalificacionEvaluacion[] = ['Excelente', 'Bueno', 'Regular', 'Deficiente'];

export const formateadorPuntaje = new Intl.NumberFormat('es-ES', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

/** Misma fórmula que la API: 40 % cumplimiento, 30 % calidad, 30 % plazos. */
export function calcularPuntajeFinal(cumplimiento: number, calidad: number, plazos: number): number {
  return (cumplimiento * 40 + calidad * 30 + plazos * 30) / 100;
}

export function calificarPuntaje(puntaje: number): CalificacionEvaluacion {
  if (puntaje >= 90) {
    return 'Excelente';
  }
  if (puntaje >= 75) {
    return 'Bueno';
  }
  if (puntaje >= 60) {
    return 'Regular';
  }
  return 'Deficiente';
}
