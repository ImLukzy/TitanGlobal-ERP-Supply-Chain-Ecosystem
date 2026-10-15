import type { AccionAuditoria } from './tipos';

const formateadorFechaHora = new Intl.DateTimeFormat('es-PE', {
  day: 'numeric',
  month: 'numeric',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  timeZone: 'UTC',
});

export function formatearFechaHoraUtc(fecha: string): string {
  return formateadorFechaHora.format(new Date(fecha));
}

export function textoAccion(accion: AccionAuditoria): string {
  if (accion === 'Modificacion') {
    return 'Modificación';
  }
  return accion;
}

export function esRangoInvalido(fechaDesde: string, fechaHasta: string): boolean {
  return fechaDesde !== '' && fechaHasta !== '' && fechaDesde > fechaHasta;
}
