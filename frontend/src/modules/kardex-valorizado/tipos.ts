export type TipoMovimientoKardex = 'Entrada' | 'Salida';

export type MetodoValorizacion = 'Promedio' | 'PEPS';

export interface MovimientoListado {
  id: number;
  productoId: number;
  productoNombre: string;
  almacenId: number;
  almacenCodigo: string;
  loteId: number | null;
  fecha: string;
  tipo: TipoMovimientoKardex;
  cantidad: number;
  costoUnitario: number | null;
  referencia: string | null;
}

export interface PaginaMovimientos {
  items: MovimientoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface GuardarMovimientoRequest {
  productoId: number;
  almacenId: number;
  loteId?: number;
  fecha?: string;
  tipo: TipoMovimientoKardex;
  cantidad: number;
  costoUnitario?: number;
  referencia?: string;
}

export interface LineaKardex {
  movimientoId: number;
  fecha: string;
  tipo: TipoMovimientoKardex;
  cantidad: number;
  costoUnitario: number;
  valorMovimiento: number;
  saldoCantidad: number;
  saldoValor: number;
  costoPromedio: number;
}

export interface KardexValorizado {
  productoId: number;
  productoNombre: string;
  almacenId: number;
  almacenCodigo: string;
  metodo: MetodoValorizacion;
  saldoCantidad: number;
  saldoValor: number;
  lineas: LineaKardex[];
}

export interface SaldoKardex {
  productoId: number;
  productoNombre: string;
  almacenId: number;
  almacenCodigo: string;
  entradas: number;
  salidas: number;
  saldo: number;
}

export interface ResumenKardex {
  totalMovimientos: number;
  totalEntradas: number;
  totalSalidas: number;
  unidadesEntradas: number;
  unidadesSalidas: number;
}

export interface FiltrosMovimientos {
  productoId?: number;
  almacenId?: number;
  tipo?: TipoMovimientoKardex | '';
  fechaDesde?: string;
  fechaHasta?: string;
  pagina?: number;
  tamanoPagina?: number;
}

export interface IdMovimientoResponse {
  id: number;
}
