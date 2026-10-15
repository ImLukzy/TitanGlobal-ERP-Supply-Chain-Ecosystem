import { apiDelete, apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type {
  FiltrosSolicitud,
  GuardarSolicitudRequest,
  PaginaSolicitudes,
  ProductoOpcion,
  ProveedorOpcion,
  RechazarSolicitudRequest,
  ResumenSolicitudes,
  SolicitudDetalle,
  SolicitudResponse,
} from './tipos';

export function listarSolicitudes(filtros: FiltrosSolicitud): Promise<PaginaSolicitudes> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  const separador = query === '' ? '' : `?${query}`;
  return apiGet<PaginaSolicitudes>(`/solicitudes-compra${separador}`);
}

export function obtenerSolicitud(id: number): Promise<SolicitudDetalle> {
  return apiGet<SolicitudDetalle>(`/solicitudes-compra/${id}`);
}

export function crearSolicitud(request: GuardarSolicitudRequest): Promise<SolicitudResponse> {
  return apiPost<SolicitudResponse>('/solicitudes-compra', request);
}

export function actualizarSolicitud(id: number, request: GuardarSolicitudRequest): Promise<SolicitudResponse> {
  return apiPut<SolicitudResponse>(`/solicitudes-compra/${id}`, request);
}

export function aprobarSolicitud(id: number): Promise<SolicitudDetalle> {
  return apiPostSinCuerpo<SolicitudDetalle>(`/solicitudes-compra/${id}/aprobar`);
}

export function rechazarSolicitud(id: number, request: RechazarSolicitudRequest): Promise<SolicitudDetalle> {
  return apiPost<SolicitudDetalle>(`/solicitudes-compra/${id}/rechazar`, request);
}

export function eliminarSolicitud(id: number): Promise<void> {
  return apiDelete(`/solicitudes-compra/${id}`);
}

export function obtenerResumenSolicitudes(): Promise<ResumenSolicitudes> {
  return apiGet<ResumenSolicitudes>('/solicitudes-compra/resumen');
}

export async function listarProveedoresActivos(): Promise<ProveedorOpcion[]> {
  const pagina = await apiGet<{ items: ProveedorOpcion[] }>('/proveedores?activo=true&tamanoPagina=100');
  return pagina.items;
}

export async function listarProductosSolicitud(): Promise<ProductoOpcion[]> {
  const pagina = await apiGet<{ items: ProductoOpcion[] }>('/productos?tamanoPagina=100');
  return pagina.items;
}
