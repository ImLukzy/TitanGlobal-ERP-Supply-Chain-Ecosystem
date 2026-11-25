import type { EstadoLicitacion, TipoLicitacion } from './tipos';

export const estados: EstadoLicitacion[] = ['Abierta', 'Cerrada', 'Adjudicada', 'Cancelada'];

export const etiquetasTipo: Record<TipoLicitacion, string> = {
  ConcursoPrecios: 'Concurso de precios',
  SubastaInversa: 'Subasta inversa',
};

/** Ayuda visual: solo una licitación abierta puede tener el plazo vencido; el backend decide con su 409. */
export function plazoVencido(estado: EstadoLicitacion, fechaCierre: string): boolean {
  return estado === 'Abierta' && new Date(fechaCierre).getTime() < Date.now();
}

export function aFechaHoraLocal(iso: string): string {
  const fecha = new Date(iso);
  const dos = (valor: number): string => String(valor).padStart(2, '0');
  return `${fecha.getFullYear()}-${dos(fecha.getMonth() + 1)}-${dos(fecha.getDate())}T${dos(fecha.getHours())}:${dos(fecha.getMinutes())}`;
}

export function formatearFechaHora(iso: string): string {
  return new Intl.DateTimeFormat('es-PE', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(iso));
}
