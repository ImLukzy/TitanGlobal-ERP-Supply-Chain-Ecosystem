import type { AcuerdoListado, EstadoAcuerdo } from './tipos';

export const estados: EstadoAcuerdo[] = ['Vigente', 'Suspendido', 'Cancelado'];

export type AvisoVigencia = 'Vencido' | 'Aún no inicia' | null;

/** Ayuda visual: solo un acuerdo vigente puede estar vencido o no iniciado; el backend decide con su 409. */
export function avisoVigencia(acuerdo: Pick<AcuerdoListado, 'estado' | 'fechaInicio' | 'fechaFin'>): AvisoVigencia {
  if (acuerdo.estado !== 'Vigente') {
    return null;
  }
  const ahora = Date.now();
  if (new Date(acuerdo.fechaFin).getTime() < ahora) {
    return 'Vencido';
  }
  if (new Date(acuerdo.fechaInicio).getTime() > ahora) {
    return 'Aún no inicia';
  }
  return null;
}

export function porcentajeConsumo(consumido: number, comprometido: number): number {
  return comprometido <= 0 ? 0 : Math.round((consumido / comprometido) * 100);
}

export function aFechaLocal(iso: string): string {
  const fecha = new Date(iso);
  const dos = (valor: number): string => String(valor).padStart(2, '0');
  return `${fecha.getFullYear()}-${dos(fecha.getMonth() + 1)}-${dos(fecha.getDate())}`;
}

export function formatearFechaCorta(iso: string): string {
  return new Intl.DateTimeFormat('es-PE', { dateStyle: 'short' }).format(new Date(iso));
}
