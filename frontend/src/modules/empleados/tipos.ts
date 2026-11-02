export type TipoDocumento = 'DNI' | 'CE' | 'Pasaporte';

export type EstadoEmpleado = 'Activo' | 'Cesado';

export interface FiltrosEmpleados {
  busqueda: string;
  estado: '' | EstadoEmpleado;
  pagina: number;
  tamanoPagina: number;
}

export interface EmpleadoListado {
  id: number;
  legajo: string;
  tipoDocumento: TipoDocumento;
  numeroDocumento: string;
  nombres: string;
  apellidos: string;
  cargo: string;
  area: string;
  fechaIngreso: string;
  estado: EstadoEmpleado;
}

export interface EmpleadoDetalle extends EmpleadoListado {
  fechaNacimiento: string;
  email: string | null;
  telefono: string | null;
  fechaCese: string | null;
  motivoCese: string | null;
}

export interface PaginaEmpleados {
  items: EmpleadoListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenEmpleados {
  activos: number;
  cesados: number;
  total: number;
}

export interface GuardarEmpleadoRequest {
  tipoDocumento: TipoDocumento;
  numeroDocumento: string;
  nombres: string;
  apellidos: string;
  fechaNacimiento: string;
  fechaIngreso: string;
  cargo: string;
  area: string;
  email: string | null;
  telefono: string | null;
}

export interface CeseEmpleadoRequest {
  fechaCese: string;
  motivo: string;
}

export interface IdResponse {
  id: number;
}
