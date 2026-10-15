import type { EstadoDevolucion, MotivoDevolucion } from './tipos';

export function nombreMotivo(motivo: MotivoDevolucion): string {
  const nombres: Record<MotivoDevolucion, string> = { Defecto: 'Defecto', ErrorPedido: 'Error de pedido', Arrepentimiento: 'Arrepentimiento', Otro: 'Otro' };
  return nombres[motivo];
}

export function claseEstado(estado: EstadoDevolucion): string {
  return `devolucion-status is-${estado.toLowerCase()}`;
}
