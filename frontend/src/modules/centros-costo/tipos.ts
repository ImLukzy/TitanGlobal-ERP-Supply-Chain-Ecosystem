export interface GuardarCentro {
  codigo: string;
  nombre: string;
  descripcion: string | null;
  responsableEmpleadoId: number | null;
  centroCostoPadreId: number | null;
}

export interface CentroFila extends Omit<GuardarCentro, 'descripcion'> {
  id: number;
  activo: boolean;
}

export interface CentroDetalle extends CentroFila {
  descripcion: string | null;
  nivel: number;
  fechaRegistro: string;
  fechaDesactivacion: string | null;
}
