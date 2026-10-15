import type { Alerta, TipoAlerta } from './tipos';

const etiquetasTipo: Record<TipoAlerta, string> = {
  ReclamoVencido: 'Reclamo vencido',
  OrdenAbastecimientoAtrasada: 'Orden de abastecimiento atrasada',
  ContratoExcedido: 'Contrato excedido',
  AlmacenCasiLleno: 'Almacén casi lleno',
  ContratoPorVencer: 'Contrato por vencer',
  CotizacionVencida: 'Cotización vencida',
  SolicitudPendienteAntigua: 'Solicitud pendiente antigua',
};

export function nombreTipo(tipo: TipoAlerta): string {
  return etiquetasTipo[tipo];
}

export function claseSeveridad(severidad: string): string {
  return `alerta-severity is-${severidad.toLowerCase()}`;
}

export function rutaAlerta(alerta: Alerta): string {
  const claveParametro = parametroPorTipo[alerta.tipo];
  const ruta = new URL(alerta.ruta, window.location.origin);
  if (!ruta.searchParams.has(claveParametro)) {
    ruta.searchParams.set(claveParametro, String(alerta.registroId));
  }
  return `${ruta.pathname}${ruta.search}${ruta.hash}`;
}

const parametroPorTipo: Record<TipoAlerta, string> = {
  ReclamoVencido: 'reclamoId',
  OrdenAbastecimientoAtrasada: 'ordenId',
  ContratoExcedido: 'contratoId',
  AlmacenCasiLleno: 'almacenId',
  ContratoPorVencer: 'contratoId',
  CotizacionVencida: 'cotizacionId',
  SolicitudPendienteAntigua: 'solicitudId',
};
