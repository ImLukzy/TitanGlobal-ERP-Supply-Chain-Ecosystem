const desfasePeruMs = 5 * 60 * 60 * 1000;

// 2026-11-02 → 02/11/2026, sin pasar por Date para que el día no cambie con la zona horaria.
export function formatearDia(dia: string): string {
  const [anio, mes, diaMes] = dia.split('-');
  return `${diaMes}/${mes}/${anio}`;
}

// Instante UTC de la API mostrado en hora de Perú (UTC-5).
export function formatearInstantePeru(instante: string): string {
  const local = new Date(Date.parse(instante) - desfasePeruMs).toISOString();
  return `${formatearDia(local.slice(0, 10))} ${local.slice(11, 16)}`;
}

export function formatearCantidad(valor: number): string {
  return valor.toLocaleString('es-PE', { maximumFractionDigits: 3 });
}

export function formatearPorcentaje(valor: number | null): string {
  return valor === null ? '—' : `${valor.toFixed(2)} %`;
}
