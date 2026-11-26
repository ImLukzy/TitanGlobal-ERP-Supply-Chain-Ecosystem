export interface TransferenciaListado {
  id: number;
  productoId: number;
  productoNombre: string;
  almacenOrigenId: number;
  almacenOrigenCodigo: string;
  almacenDestinoId: number;
  almacenDestinoCodigo: string;
  cantidad: number;
  costoUnitario: number;
  valorTotal: number;
  fecha: string;
}

export interface TransferenciaDetalle extends TransferenciaListado {
  observaciones: string | null;
  movimientoSalidaId: number;
  movimientoEntradaId: number;
}

export interface PaginaTransferencias {
  items: TransferenciaListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenTransferencias {
  totalTransferencias: number;
  unidadesTransferidas: number;
  valorTransferido: number;
}

export interface GuardarTransferenciaRequest {
  productoId: number;
  almacenOrigenId: number;
  almacenDestinoId: number;
  cantidad: number;
  observaciones?: string;
}

export interface TransferenciaCreada {
  id: number;
  costoUnitario: number;
}

export interface FiltrosTransferencias {
  productoId?: number;
  almacenOrigenId?: number;
  almacenDestinoId?: number;
  fechaDesde?: string;
  fechaHasta?: string;
  pagina?: number;
  tamanoPagina?: number;
}
