import { apiGet, apiPost } from '../../api/client';
import type {
  AdendaContratoRequest,
  AnularContratoLaboralRequest,
  ContratoLaboralCreado,
  ContratoLaboralDetalle,
  FiltrosContratosLaborales,
  GuardarContratoLaboralRequest,
  IdResponse,
  PaginaContratosLaborales,
  RenovarContratoLaboralRequest,
} from './tipos';

// Los filtros vacíos no se envían: «Todos» y «Todas» omiten el parámetro.
export function listarContratosLaborales(filtros: FiltrosContratosLaborales): Promise<PaginaContratosLaborales> {
  const consulta = new URLSearchParams();
  const termino = filtros.busqueda.trim();
  if (termino !== '') {
    consulta.set('busqueda', termino);
  }
  if (filtros.estado !== '') {
    consulta.set('estado', filtros.estado);
  }
  if (filtros.modalidad !== '') {
    consulta.set('modalidad', filtros.modalidad);
  }
  if (filtros.empleadoId !== null) {
    consulta.set('empleadoId', String(filtros.empleadoId));
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaContratosLaborales>(`/contratos-laborales?${consulta.toString()}`);
}

export function obtenerContratoLaboral(id: number): Promise<ContratoLaboralDetalle> {
  return apiGet<ContratoLaboralDetalle>(`/contratos-laborales/${id}`);
}

export function crearContratoLaboral(request: GuardarContratoLaboralRequest): Promise<ContratoLaboralCreado> {
  return apiPost<ContratoLaboralCreado>('/contratos-laborales', request);
}

export function renovarContratoLaboral(id: number, request: RenovarContratoLaboralRequest): Promise<ContratoLaboralCreado> {
  return apiPost<ContratoLaboralCreado>(`/contratos-laborales/${id}/renovacion`, request);
}

export function agregarAdendaContrato(id: number, request: AdendaContratoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/contratos-laborales/${id}/adendas`, request);
}

export function anularContratoLaboral(id: number, request: AnularContratoLaboralRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/contratos-laborales/${id}/anulacion`, request);
}
