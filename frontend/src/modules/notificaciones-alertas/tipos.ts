export type SeveridadAlerta = 'Alta' | 'Media' | 'Baja';

export type FiltroSeveridad = '' | 'alta' | 'media' | 'baja';

export type TipoAlerta =
  | 'ReclamoVencido'
  | 'OrdenAbastecimientoAtrasada'
  | 'ContratoExcedido'
  | 'AlmacenCasiLleno'
  | 'ContratoPorVencer'
  | 'CotizacionVencida'
  | 'SolicitudPendienteAntigua';

export interface Alerta {
  clave: string;
  tipo: TipoAlerta;
  severidad: SeveridadAlerta;
  mensaje: string;
  modulo: string;
  registroId: number;
  ruta: string;
  fechaReferencia: string;
  descartada: boolean;
}

export interface FiltrosAlerta {
  tipo: TipoAlerta | '';
  severidad: FiltroSeveridad;
  incluirDescartadas: boolean;
  pagina: number;
  tamanoPagina: number;
}

export interface PaginaAlertas {
  items: Alerta[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface AlertaPorTipo {
  tipo: TipoAlerta;
  alertas: number;
}

export interface ResumenAlertas {
  total: number;
  altas: number;
  medias: number;
  bajas: number;
  descartadas: number;
  porTipo: AlertaPorTipo[];
}
