export const estadosRuta = ['Borrador', 'Activa', 'Retirada'] as const;

export type EstadoRuta = (typeof estadosRuta)[number];
export type DecisionRuta = 'activar' | 'retirar';

export interface OpcionCatalogo {
  id: number;
  nombre: string;
  activo?: boolean;
}

interface CabeceraRutaProduccion {
  id: number;
  productoId: number;
  productoNombre: string;
  version: number;
  estado: EstadoRuta;
}

export interface RutaProduccionFila extends CabeceraRutaProduccion {
  totalOperaciones: number;
}

export interface OperacionRutaRequest {
  secuencia: number;
  nombre: string;
  centroTrabajoId: number;
  minutosPreparacion: number;
  minutosPorUnidad: number;
}

export interface OperacionRutaProduccion extends OperacionRutaRequest {
  centroTrabajoNombre: string;
}

export interface RutaProduccionDetalle extends CabeceraRutaProduccion {
  operaciones: OperacionRutaProduccion[];
}

export interface PaginaRutasProduccion {
  items: RutaProduccionFila[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltrosRutasProduccion {
  productoId?: number;
  estado: EstadoRuta | '';
  pagina: number;
  tamanoPagina: number;
}

export interface EditarRutaRequest {
  operaciones: OperacionRutaRequest[];
}

export interface CrearRutaRequest extends EditarRutaRequest {
  productoId: number;
  version: number;
}

export interface IdResponse {
  id: number;
}
