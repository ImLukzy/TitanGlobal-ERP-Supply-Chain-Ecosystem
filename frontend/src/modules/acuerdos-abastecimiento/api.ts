import { apiDelete, apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type {
  AcuerdoDetalle,
  AcuerdoResponse,
  ActualizarAcuerdoRequest,
  CrearAcuerdoRequest,
  CrearLineaRequest,
  FiltrosAcuerdo,
  PaginaAcuerdos,
  ProductoOpcion,
  ProveedorOpcion,
  RegistrarConsumoRequest,
  ResumenAcuerdos,
} from './tipos';

const base = '/acuerdos-abastecimiento';

export function listarAcuerdos(filtros: FiltrosAcuerdo): Promise<PaginaAcuerdos> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  return apiGet<PaginaAcuerdos>(`${base}${query === '' ? '' : `?${query}`}`);
}

export function obtenerAcuerdo(id: number): Promise<AcuerdoDetalle> {
  return apiGet<AcuerdoDetalle>(`${base}/${id}`);
}

export function crearAcuerdo(request: CrearAcuerdoRequest): Promise<AcuerdoResponse> {
  return apiPost<AcuerdoResponse>(base, request);
}

export function actualizarAcuerdo(id: number, request: ActualizarAcuerdoRequest): Promise<AcuerdoResponse> {
  return apiPut<AcuerdoResponse>(`${base}/${id}`, request);
}

export function eliminarAcuerdo(id: number): Promise<void> {
  return apiDelete(`${base}/${id}`);
}

export function crearLineaAcuerdo(id: number, request: CrearLineaRequest): Promise<AcuerdoResponse> {
  return apiPost<AcuerdoResponse>(`${base}/${id}/lineas`, request);
}

export function eliminarLineaAcuerdo(id: number, lineaId: number): Promise<void> {
  return apiDelete(`${base}/${id}/lineas/${lineaId}`);
}

export function registrarConsumo(id: number, lineaId: number, request: RegistrarConsumoRequest): Promise<AcuerdoResponse> {
  return apiPost<AcuerdoResponse>(`${base}/${id}/lineas/${lineaId}/consumos`, request);
}

export function suspenderAcuerdo(id: number): Promise<AcuerdoResponse> {
  return apiPostSinCuerpo<AcuerdoResponse>(`${base}/${id}/suspender`);
}

export function reactivarAcuerdo(id: number): Promise<AcuerdoResponse> {
  return apiPostSinCuerpo<AcuerdoResponse>(`${base}/${id}/reactivar`);
}

export function cancelarAcuerdo(id: number): Promise<AcuerdoResponse> {
  return apiPostSinCuerpo<AcuerdoResponse>(`${base}/${id}/cancelar`);
}

export function obtenerResumenAcuerdos(): Promise<ResumenAcuerdos> {
  return apiGet<ResumenAcuerdos>(`${base}/resumen`);
}

export async function listarProveedoresActivosAcuerdo(): Promise<ProveedorOpcion[]> {
  const pagina = await apiGet<{ items: ProveedorOpcion[] }>('/proveedores?activo=true&tamanoPagina=100');
  return pagina.items;
}

export async function listarProductosAcuerdo(): Promise<ProductoOpcion[]> {
  const pagina = await apiGet<{ items: ProductoOpcion[] }>('/productos?tamanoPagina=100');
  return pagina.items;
}
