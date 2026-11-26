import { apiGet, apiPost, apiPut } from '../../api/client';
import type {
  ActualizarLoteRequest,
  CambiarEstadoSerieRequest,
  FiltrosLotes,
  FiltrosSeries,
  GuardarLoteRequest,
  GuardarSerieRequest,
  IdLoteSerieResponse,
  LoteDetalle,
  PaginaLotes,
  PaginaSeries,
  ResumenLotes,
} from './tipos';

function construirQuery(parametros: Record<string, string | number | boolean | undefined>): string {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const texto = consulta.toString();
  return texto === '' ? '' : `?${texto}`;
}

export function listarLotes(filtros: FiltrosLotes): Promise<PaginaLotes> {
  const query = construirQuery({
    productoId: filtros.productoId,
    estado: filtros.estado,
    vencidos: filtros.vencidos,
    busqueda: filtros.busqueda,
    pagina: filtros.pagina,
    tamanoPagina: filtros.tamanoPagina,
  });
  return apiGet<PaginaLotes>(`/lotes-series/lotes${query}`);
}

export function obtenerResumenLotes(): Promise<ResumenLotes> {
  return apiGet<ResumenLotes>('/lotes-series/lotes/resumen');
}

export function obtenerLote(id: number): Promise<LoteDetalle> {
  return apiGet<LoteDetalle>(`/lotes-series/lotes/${id}`);
}

export function crearLote(request: GuardarLoteRequest): Promise<IdLoteSerieResponse> {
  return apiPost<IdLoteSerieResponse>('/lotes-series/lotes', request);
}

export function actualizarLote(
  id: number,
  request: ActualizarLoteRequest,
): Promise<IdLoteSerieResponse> {
  return apiPut<IdLoteSerieResponse>(`/lotes-series/lotes/${id}`, request);
}

export function listarSeries(filtros: FiltrosSeries): Promise<PaginaSeries> {
  const query = construirQuery({
    productoId: filtros.productoId,
    loteId: filtros.loteId,
    estado: filtros.estado,
    busqueda: filtros.busqueda,
    pagina: filtros.pagina,
    tamanoPagina: filtros.tamanoPagina,
  });
  return apiGet<PaginaSeries>(`/lotes-series/series${query}`);
}

export function crearSerie(request: GuardarSerieRequest): Promise<IdLoteSerieResponse> {
  return apiPost<IdLoteSerieResponse>('/lotes-series/series', request);
}

export function cambiarEstadoSerie(
  id: number,
  request: CambiarEstadoSerieRequest,
): Promise<IdLoteSerieResponse> {
  return apiPut<IdLoteSerieResponse>(`/lotes-series/series/${id}/estado`, request);
}
