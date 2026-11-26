export type TipoNota = 'Credito' | 'Debito';
export type EstadoNota = 'Borrador' | 'Emitida' | 'Anulada';

export interface NotaFila {
  id: number;
  facturaId: number;
  tipo: TipoNota;
  numero: string;
  moneda: string;
  total: number;
  estado: EstadoNota;
  fechaCreacion: string;
  fechaEmision: string | null;
}

export interface GuardarLineaNota { detalleFacturaId: number; cantidad: number; precioUnitario: number }
export interface GuardarNota { motivo: string; detalles: GuardarLineaNota[] }
export interface CrearNota extends GuardarNota { facturaId: number; tipo: TipoNota }

export interface LineaNota extends GuardarLineaNota {
  id: number;
  porcentajeImpuesto: number;
  baseImponible: number;
  importeImpuesto: number;
  total: number;
}

export interface NotaDetalle extends NotaFila {
  clienteId: number;
  clienteNombre: string;
  facturaTipoComprobante: 'Factura' | 'Boleta';
  facturaSerie: string;
  facturaNumero: number;
  motivo: string;
  baseImponible: number;
  importeImpuesto: number;
  fechaAnulacion: string | null;
  motivoAnulacion: string | null;
  detalles: LineaNota[];
}

export interface FacturaParaNota {
  id: number;
  numeroComprobante: string;
  tipoComprobante: 'Factura' | 'Boleta';
  clienteNombre: string;
  moneda: string;
  estado: string;
}

export interface FacturaConLineas extends FacturaParaNota {
  detalles: { id: number; descripcion: string; cantidad: number; precioUnitario: number; porcentajeImpuesto: number }[];
}
