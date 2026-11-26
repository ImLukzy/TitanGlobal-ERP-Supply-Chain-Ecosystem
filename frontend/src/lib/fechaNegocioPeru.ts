const fechaPeru = new Intl.DateTimeFormat('es-PE', {
  timeZone: 'America/Lima',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

export function diaDePeru(instante: Date = new Date()): string {
  const partes = fechaPeru.formatToParts(instante);
  const anio = partes.find(parte => parte.type === 'year')?.value;
  const mes = partes.find(parte => parte.type === 'month')?.value;
  const dia = partes.find(parte => parte.type === 'day')?.value;
  return [anio, mes, dia].join('-');
}
