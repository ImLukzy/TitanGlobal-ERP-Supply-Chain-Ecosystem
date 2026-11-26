import { apiGet, apiPost } from '../../api/client';
import type {
  FiltrosTransferencias,
  GuardarTransferenciaRequest,
  PaginaTransferencias,
  ResumenTransferencias,
  TransferenciaCreada,
  TransferenciaDetalle,
} from './tipos';

function construirQuery(parametros: Record<string, string | number | undefined>): string {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const texto = consulta.toString();
  return texto === '' ? '' : `?${texto}`;
}

export function listarTransferencias(
  filtros: FiltrosTransferencias,
): Promise<PaginaTransferencias> {
  const query = construirQuery({
    productoId: filtros.productoId,
    almacenOrigenId: filtros.almacenOrigenId,
    almacenDestinoId: filtros.almacenDestinoId,
    fechaDesde: filtros.fechaDesde,
    fechaHasta: filtros.fechaHasta,
    pagina: filtros.pagina,
    tamanoPagina: filtros.tamanoPagina,
  });
  return apiGet<PaginaTransferencias>(`/transferencias-almacen${query}`);
}

export function obtenerResumenTransferencias(): Promise<ResumenTransferencias> {
  return apiGet<ResumenTransferencias>('/transferencias-almacen/resumen');
}

export function obtenerTransferencia(id: number): Promise<TransferenciaDetalle> {
  return apiGet<TransferenciaDetalle>(`/transferencias-almacen/${id}`);
}

export function registrarTransferencia(
  request: GuardarTransferenciaRequest,
): Promise<TransferenciaCreada> {
  return apiPost<TransferenciaCreada>('/transferencias-almacen', request);
}
