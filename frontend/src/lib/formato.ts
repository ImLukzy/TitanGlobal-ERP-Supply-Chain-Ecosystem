export const formateadorMoneda = new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' });

export const formateadorFecha = new Intl.DateTimeFormat('es-PE', {
  day: 'numeric',
  month: 'numeric',
  year: 'numeric',
  timeZone: 'UTC',
});

export function formatearFecha(fecha: string): string {
  return formateadorFecha.format(new Date(fecha));
}
