export const tiposImpuesto = ['IGV', 'IVA', 'RETENCION', 'PERCEPCION', 'DETRACCION'] as const;
export type TipoImpuesto = typeof tiposImpuesto[number];

export interface GuardarImpuesto {
  codigo: string;
  nombre: string;
  tipo: string;
  descripcion: string | null;
  pais: string;
  activo: boolean;
}

export interface ImpuestoListado extends Omit<GuardarImpuesto, 'descripcion'> {
  id: number;
  porcentajeVigente: number | null;
  fechaInicioVigencia: string | null;
}

export interface TasaImpuesto {
  id: number;
  porcentaje: number;
  fechaInicio: string;
  fechaFin: string | null;
  activo: boolean;
  observacion: string | null;
}

export type GuardarTasa = Omit<TasaImpuesto, 'id' | 'activo'>;

export interface ImpuestoDetalle extends GuardarImpuesto {
  id: number;
  tasas: TasaImpuesto[];
}

export interface CalcularImpuesto {
  impuestoId: number;
  baseImponible: number;
  fechaOperacion: string;
}

export interface ResultadoImpuesto extends CalcularImpuesto {
  codigo: string;
  nombre: string;
  porcentajeAplicado: number;
  importeImpuesto: number;
  total: number;
}
