export type TipoSucursal = 'Filial' | 'Planta' | 'Oficina';
export interface ValoresSucursal {
  codigo: string;
  nombre: string;
  tipo: TipoSucursal;
  pais: string;
  ciudad: string;
  direccion: string;
  contacto: string;
  email: string;
  telefono: string;
  observaciones: string;
  fechaApertura: string;
}
export interface GuardarSucursal extends Omit<
  ValoresSucursal,
  'contacto' | 'email' | 'telefono' | 'observaciones' | 'fechaApertura'
> {
  contacto: string | null;
  email: string | null;
  telefono: string | null;
  observaciones: string | null;
  fechaApertura: string | null;
}
export interface SucursalListado {
  id: number;
  codigo: string;
  nombre: string;
  tipo: TipoSucursal;
  pais: string;
  ciudad: string;
  activa: boolean;
}
export interface SucursalDetalle extends SucursalListado {
  direccion: string;
  contacto: string | null;
  email: string | null;
  telefono: string | null;
  observaciones: string | null;
  fechaApertura: string | null;
  fechaRegistro: string;
  fechaCambioEstado: string | null;
}
export interface PaginaSucursales {
  items: SucursalListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
export interface ResumenSucursales {
  total: number;
  activas: number;
  inactivas: number;
  filiales: number;
  plantas: number;
  oficinas: number;
}
export interface FiltrosSucursales {
  busqueda?: string;
  tipo?: string;
  pais?: string;
  activa?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}
