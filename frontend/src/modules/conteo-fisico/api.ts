import { apiGet, apiPost, apiPut } from '../../api/client';
import type {
  ActualizarTomaInventarioRequest,
  CrearTomaInventarioRequest,
  FiltrosTomasInventario,
  PaginaTomasInventario,
  ResumenTomasInventario,
  TomaInventarioDetalle,
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

export function listarTomasInventario(
  filtros: FiltrosTomasInventario,
): Promise<PaginaTomasInventario> {
  const query = construirQuery({
    almacenId: filtros.almacenId,
    estado: filtros.estado,
    busqueda: filtros.busqueda,
    fechaDesde: filtros.fechaDesde,
    fechaHasta: filtros.fechaHasta,
    pagina: filtros.pagina,
    tamanoPagina: filtros.tamanoPagina,
  });
  return apiGet<PaginaTomasInventario>(`/tomas-inventario${query}`);
}

export function obtenerResumenTomasInventario(): Promise<ResumenTomasInventario> {
  return apiGet<ResumenTomasInventario>('/tomas-inventario/resumen');
}

export function obtenerTomaInventario(id: number): Promise<TomaInventarioDetalle> {
  return apiGet<TomaInventarioDetalle>(`/tomas-inventario/${id}`);
}

export function crearTomaInventario(
  request: CrearTomaInventarioRequest,
): Promise<TomaInventarioDetalle> {
  return apiPost<TomaInventarioDetalle>('/tomas-inventario', request);
}

export function actualizarTomaInventario(
  id: number,
  request: ActualizarTomaInventarioRequest,
): Promise<TomaInventarioDetalle> {
  return apiPut<TomaInventarioDetalle>(`/tomas-inventario/${id}`, request);
}

export function cerrarTomaInventario(id: number): Promise<TomaInventarioDetalle> {
  return apiPost<TomaInventarioDetalle>(`/tomas-inventario/${id}/cerrar`, {});
}

export function anularTomaInventario(id: number): Promise<TomaInventarioDetalle> {
  return apiPost<TomaInventarioDetalle>(`/tomas-inventario/${id}/anular`, {});
}
