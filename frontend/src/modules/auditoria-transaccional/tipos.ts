export type AccionAuditoria = 'Alta' | 'Modificacion' | 'Baja';

export interface RegistroListado {
  id: number;
  fecha: string;
  entidad: string;
  entidadId: string;
  accion: AccionAuditoria;
  usuario: string;
  ruta: string | null;
  camposCambiados: number;
}

export interface CambioCampo {
  campo: string;
  antes: string | null;
  despues: string | null;
}

export interface RegistroDetalle extends RegistroListado {
  cambios: CambioCampo[];
}

export interface PaginaAuditoria {
  items: RegistroListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenAuditoria {
  total: number;
  altas: number;
  modificaciones: number;
  bajas: number;
  porEntidad: EntidadAuditoria[];
  ultimos7Dias: DiaAuditoria[];
}

export interface EntidadAuditoria {
  entidad: string;
  registros: number;
}

export interface DiaAuditoria {
  fecha: string;
  registros: number;
}

export interface FiltrosAuditoria {
  entidad: string;
  entidadId: string;
  accion: '' | AccionAuditoria;
  usuario: string;
  fechaDesde: string;
  fechaHasta: string;
  pagina: number;
  tamanoPagina: number;
}
