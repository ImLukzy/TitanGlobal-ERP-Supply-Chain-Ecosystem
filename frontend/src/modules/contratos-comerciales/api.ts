import { apiDelete, apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type { ClienteOpcion, ContratoDetalle, ContratoResponse, ContratosResumen, FiltrosContrato, GuardarContratoRequest, PaginaContratos } from './tipos';

export function listarContratos(filtros: FiltrosContrato): Promise<PaginaContratos> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined && valor !== false) consulta.set(clave, String(valor));
  }
  return apiGet<PaginaContratos>(`/contratos-comerciales${consulta.size ? `?${consulta}` : ''}`);
}

export function obtenerContrato(id: number): Promise<ContratoDetalle> {
  return apiGet<ContratoDetalle>(`/contratos-comerciales/${id}`);
}

export function crearContrato(request: GuardarContratoRequest): Promise<ContratoResponse> {
  return apiPost<ContratoResponse>('/contratos-comerciales', request);
}

export function actualizarContrato(id: number, request: GuardarContratoRequest): Promise<ContratoResponse> {
  return apiPut<ContratoResponse>(`/contratos-comerciales/${id}`, request);
}

export function activarContrato(id: number): Promise<ContratoDetalle> {
  return apiPostSinCuerpo<ContratoDetalle>(`/contratos-comerciales/${id}/activar`);
}

export function finalizarContrato(id: number): Promise<ContratoDetalle> {
  return apiPostSinCuerpo<ContratoDetalle>(`/contratos-comerciales/${id}/finalizar`);
}

export function cancelarContrato(id: number, motivo: string): Promise<ContratoDetalle> {
  return apiPost<ContratoDetalle>(`/contratos-comerciales/${id}/cancelar`, { motivo });
}

export function eliminarContrato(id: number): Promise<void> {
  return apiDelete(`/contratos-comerciales/${id}`);
}

export function obtenerResumenContratos(): Promise<ContratosResumen> {
  return apiGet<ContratosResumen>('/contratos-comerciales/resumen');
}

export async function listarClientesContrato(): Promise<ClienteOpcion[]> {
  const pagina = await apiGet<{ items: ClienteOpcion[] }>('/clientes?tamanoPagina=100');
  return pagina.items;
}
