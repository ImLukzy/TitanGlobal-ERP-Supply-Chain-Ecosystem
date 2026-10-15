import { apiDelete, apiGet, apiPost, apiPut } from "../../api/client";
import type {
  FiltrosListas,
  GuardarLista,
  ListaDetalle,
  PaginaListas,
  PrecioResuelto,
  ResumenListas,
} from "./tipos";

function query(
  parametros: Record<string, string | number | boolean | undefined>,
): string {
  const valores = new URLSearchParams();
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor !== undefined && valor !== "") valores.set(clave, String(valor));
  }
  const cadena = valores.toString();
  return cadena ? `?${cadena}` : "";
}

export function listarListas(filtros: FiltrosListas): Promise<PaginaListas> {
  return apiGet(`/listas-precios${query({ ...filtros })}`);
}
export function obtenerLista(id: number): Promise<ListaDetalle> {
  return apiGet(`/listas-precios/${id}`);
}
export function crearLista(request: GuardarLista): Promise<{ id: number }> {
  return apiPost("/listas-precios", request);
}
export function actualizarLista(
  id: number,
  request: GuardarLista,
): Promise<{ id: number }> {
  return apiPut(`/listas-precios/${id}`, request);
}
export function eliminarLista(id: number): Promise<void> {
  return apiDelete(`/listas-precios/${id}`);
}
export function resolverPrecio(
  productoId: number,
  clienteId: number,
  fecha: string,
): Promise<PrecioResuelto> {
  return apiGet(
    `/listas-precios/precio${query({ productoId, clienteId, fecha })}`,
  );
}
export function obtenerResumenListas(): Promise<ResumenListas> {
  return apiGet("/listas-precios/resumen");
}
