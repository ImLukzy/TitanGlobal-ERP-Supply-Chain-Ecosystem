export type ModalidadContrato = 'Indeterminado' | 'PlazoFijo' | 'Formativo';

export type EstadoContrato = 'Vigente' | 'Renovado' | 'Vencido' | 'Cerrado' | 'Anulado';

export interface FiltrosContratosLaborales {
  busqueda: string;
  estado: '' | EstadoContrato;
  modalidad: '' | ModalidadContrato;
  empleadoId: number | null;
  pagina: number;
  tamanoPagina: number;
}

export interface ContratoLaboralListado {
  id: number;
  numero: string;
  empleadoId: number;
  empleadoLegajo: string;
  empleadoNombre: string;
  modalidad: ModalidadContrato;
  fechaInicio: string;
  fechaFin: string | null;
  sueldoBasico: number;
  cargo: string;
  estado: EstadoContrato;
}

export interface AdendaContrato {
  id: number;
  fechaVigencia: string;
  cargo: string;
  sueldoBasico: number;
}

export interface ContratoLaboralDetalle extends ContratoLaboralListado {
  contratoAnteriorId: number | null;
  contratoAnteriorNumero: string | null;
  fechaCierre: string | null;
  motivoCierre: string | null;
  cargoVigente: string;
  sueldoVigente: number;
  adendas: AdendaContrato[];
}

export interface PaginaContratosLaborales {
  items: ContratoLaboralListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ResumenContratosLaborales {
  vigentes: number;
  vencidos: number;
  total: number;
}

export interface GuardarContratoLaboralRequest {
  empleadoId: number;
  modalidad: ModalidadContrato;
  fechaInicio: string;
  fechaFin: string | null;
  sueldoBasico: number;
  cargo: string;
}

export interface RenovarContratoLaboralRequest {
  modalidad: ModalidadContrato;
  fechaFin: string | null;
  sueldoBasico: number;
  cargo: string;
}

export interface AdendaContratoRequest {
  fechaVigencia: string;
  cargo?: string;
  sueldoBasico?: number;
}

export interface AnularContratoLaboralRequest {
  motivo: string;
}

export interface ContratoLaboralCreado {
  id: number;
  numero: string;
}

export interface IdResponse {
  id: number;
}
