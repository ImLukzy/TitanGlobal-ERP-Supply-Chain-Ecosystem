export type EstadoAcuerdo = 'Vigente' | 'Suspendido' | 'Cancelado';

export interface AcuerdoListado {
  id: number;
  numero: string;
  proveedorNombre: string;
  estado: EstadoAcuerdo;
  fechaInicio: string;
  fechaFin: string;
  montoMaximo: number;
  montoComprometido: number;
  montoConsumido: number;
}

export interface PaginaAcuerdos {
  items: AcuerdoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface LineaAcuerdo {
  id: number;
  productoId: number;
  productoNombre: string;
  precioAcordado: number;
  cantidadComprometida: number;
  cantidadConsumida: number;
  saldo: number;
}

export interface AcuerdoDetalle extends AcuerdoListado {
  proveedorId: number;
  descripcion: string | null;
  fechaRegistro: string;
  lineas: LineaAcuerdo[];
}

export interface CrearAcuerdoRequest {
  numero: string;
  proveedorId: number;
  descripcion?: string;
  fechaInicio: string;
  fechaFin: string;
  montoMaximo: number;
}

export interface ActualizarAcuerdoRequest {
  descripcion?: string;
  fechaInicio: string;
  fechaFin: string;
  montoMaximo: number;
}

export interface CrearLineaRequest {
  productoId: number;
  precioAcordado: number;
  cantidadComprometida: number;
}

export interface RegistrarConsumoRequest {
  cantidad: number;
}

export interface AcuerdoResponse {
  id: number;
}

export interface ConteoEstado {
  estado: EstadoAcuerdo;
  total: number;
}

export interface ResumenAcuerdos {
  totalAcuerdos: number;
  montoMaximoVigente: number;
  montoConsumido: number;
  porEstado: ConteoEstado[];
}

export interface FiltrosAcuerdo {
  busqueda?: string;
  estado?: EstadoAcuerdo | '';
  proveedorId?: number | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface ProveedorOpcion {
  id: number;
  razonSocial: string;
}

export interface ProductoOpcion {
  id: number;
  nombre: string;
}
