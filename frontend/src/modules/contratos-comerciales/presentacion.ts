import type { CondicionPago } from './tipos';

export function nombreCondicionPago(condicion: CondicionPago): string {
  const nombres: Record<CondicionPago, string> = {
    Contado: 'Contado',
    Credito30: 'Crédito 30 días',
    Credito60: 'Crédito 60 días',
    Credito90: 'Crédito 90 días',
  };
  return nombres[condicion];
}
