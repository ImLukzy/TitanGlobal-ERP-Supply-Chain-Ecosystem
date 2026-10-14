export interface ClienteResumen {
  id: number;
  nombre: string;
  ruc: string;
  totalOrdenes: number;
}

export interface PaginaClientes {
  items: ClienteResumen[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ClienteDetalle extends ClienteResumen {
  descripcion: string | null;
  montoTotal: number;
  ultimaOrdenFecha: string | null;
}

export interface GuardarClienteRequest {
  nombre: string;
  ruc: string;
  descripcion?: string;
}

export interface ClienteMonto {
  clienteId: number;
  nombre: string;
  montoTotal: number;
  totalOrdenes: number;
}

export interface ResumenClientes {
  totalClientes: number;
  clientesConOrdenes: number;
  clientesSinDescripcion: number;
  topClientesPorMonto: ClienteMonto[];
}

export interface IdResponse {
  id: number;
}
