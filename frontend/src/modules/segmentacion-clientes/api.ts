import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  ClienteClasificado,
  FiltrosClasificacion,
  FiltrosSegmentos,
  GuardarSegmentoRequest,
  IdResponse,
  Pagina,
  ResumenSegmentacion,
  SegmentoDetalle,
  SegmentoListado,
} from './tipos';

function crearQuery(parametros: Record<string, string | number | boolean | undefined>): string {
  const query = new URLSearchParams();
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor !== '' && valor !== undefined) {
      query.set(clave, String(valor));
    }
  }
  const serializada = query.toString();
  return serializada === '' ? '' : `?${serializada}`;
}

export function listarSegmentos(filtros: FiltrosSegmentos): Promise<Pagina<SegmentoListado>> {
  const query = crearQuery({ ...filtros });
  return apiGet<Pagina<SegmentoListado>>(`/segmentos-clientes${query}`);
}

export function obtenerSegmento(id: number): Promise<SegmentoDetalle> {
  return apiGet<SegmentoDetalle>(`/segmentos-clientes/${id}`);
}

export function crearSegmento(request: GuardarSegmentoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/segmentos-clientes', request);
}

export function actualizarSegmento(id: number, request: GuardarSegmentoRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/segmentos-clientes/${id}`, request);
}

export function eliminarSegmento(id: number): Promise<void> {
  return apiDelete(`/segmentos-clientes/${id}`);
}

export function listarClientesSegmento(id: number, pagina: number, tamanoPagina: number): Promise<Pagina<ClienteClasificado>> {
  const query = crearQuery({ pagina, tamanoPagina });
  return apiGet<Pagina<ClienteClasificado>>(`/segmentos-clientes/${id}/clientes${query}`);
}

export function listarClasificacion(filtros: FiltrosClasificacion): Promise<Pagina<ClienteClasificado>> {
  const query = crearQuery({ ...filtros });
  return apiGet<Pagina<ClienteClasificado>>(`/segmentos-clientes/clasificacion${query}`);
}

export function obtenerResumenSegmentacion(): Promise<ResumenSegmentacion> {
  return apiGet<ResumenSegmentacion>('/segmentos-clientes/resumen');
}
