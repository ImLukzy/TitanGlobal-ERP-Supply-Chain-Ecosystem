import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type {
  AnularOrdenRequest,
  FiltrosOrdenAbastecimiento,
  GenerarDesdeSolicitudRequest,
  GuardarOrdenAbastecimientoRequest,
  OrdenAbastecimientoDetalle,
  OrdenAbastecimientoResponse,
  PaginaOrdenesAbastecimiento,
  ProductoOrdenOpcion,
  ProveedorOrdenOpcion,
  ResumenOrdenesAbastecimiento,
} from './tipos';

export function listarOrdenesAbastecimiento(filtros: FiltrosOrdenAbastecimiento): Promise<PaginaOrdenesAbastecimiento> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  const separador = query === '' ? '' : `?${query}`;
  return apiGet<PaginaOrdenesAbastecimiento>(`/ordenes-abastecimiento${separador}`);
}

export function obtenerOrdenAbastecimiento(id: number): Promise<OrdenAbastecimientoDetalle> {
  return apiGet<OrdenAbastecimientoDetalle>(`/ordenes-abastecimiento/${id}`);
}

export function crearOrdenAbastecimiento(request: GuardarOrdenAbastecimientoRequest): Promise<OrdenAbastecimientoResponse> {
  return apiPost<OrdenAbastecimientoResponse>('/ordenes-abastecimiento', request);
}

export function generarOrdenDesdeSolicitud(id: number, request: GenerarDesdeSolicitudRequest): Promise<OrdenAbastecimientoResponse> {
  return apiPost<OrdenAbastecimientoResponse>(`/ordenes-abastecimiento/desde-solicitud/${id}`, request);
}

export function actualizarOrdenAbastecimiento(id: number, request: GuardarOrdenAbastecimientoRequest): Promise<OrdenAbastecimientoResponse> {
  return apiPut<OrdenAbastecimientoResponse>(`/ordenes-abastecimiento/${id}`, request);
}

export function recibirOrdenAbastecimiento(id: number): Promise<OrdenAbastecimientoDetalle> {
  return apiPostSinCuerpo<OrdenAbastecimientoDetalle>(`/ordenes-abastecimiento/${id}/recibir`);
}

export function anularOrdenAbastecimiento(id: number, request: AnularOrdenRequest): Promise<OrdenAbastecimientoDetalle> {
  return apiPost<OrdenAbastecimientoDetalle>(`/ordenes-abastecimiento/${id}/anular`, request);
}

export function obtenerResumenOrdenesAbastecimiento(): Promise<ResumenOrdenesAbastecimiento> {
  return apiGet<ResumenOrdenesAbastecimiento>('/ordenes-abastecimiento/resumen');
}

export async function listarProveedoresActivosOrden(): Promise<ProveedorOrdenOpcion[]> {
  const pagina = await apiGet<{ items: ProveedorOrdenOpcion[] }>('/proveedores?activo=true&tamanoPagina=100');
  return pagina.items;
}

export async function listarProductosOrden(): Promise<ProductoOrdenOpcion[]> {
  const pagina = await apiGet<{ items: ProductoOrdenOpcion[] }>('/productos?tamanoPagina=100');
  return pagina.items;
}
