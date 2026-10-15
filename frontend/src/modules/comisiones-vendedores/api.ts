import { apiDelete, apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type { AsignarVendedorRequest, ComisionesMes, FiltrosOrden, FiltrosVendedor, GuardarVendedorRequest, Liquidacion, OrdenVendedor, Pagina, VendedorDetalle, VendedorListado, VendedorResponse } from './tipos';

function consulta(filtros: object): string {
  const parametros = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== undefined && valor !== '') {
      parametros.set(clave, String(valor));
    }
  }
  return parametros.size === 0 ? '' : `?${parametros.toString()}`;
}

export function listarVendedores(filtros: FiltrosVendedor): Promise<Pagina<VendedorListado>> {
  return apiGet<Pagina<VendedorListado>>(`/comisiones-vendedores/vendedores${consulta(filtros)}`);
}

export function obtenerVendedor(id: number): Promise<VendedorDetalle> {
  return apiGet<VendedorDetalle>(`/comisiones-vendedores/vendedores/${id}`);
}

export function crearVendedor(request: GuardarVendedorRequest): Promise<VendedorResponse> {
  return apiPost<VendedorResponse>('/comisiones-vendedores/vendedores', request);
}

export function actualizarVendedor(id: number, request: GuardarVendedorRequest): Promise<VendedorResponse> {
  return apiPut<VendedorResponse>(`/comisiones-vendedores/vendedores/${id}`, request);
}

export function eliminarVendedor(id: number): Promise<void> {
  return apiDelete(`/comisiones-vendedores/vendedores/${id}`);
}

export function listarOrdenes(filtros: FiltrosOrden): Promise<Pagina<OrdenVendedor>> {
  return apiGet<Pagina<OrdenVendedor>>(`/comisiones-vendedores/ordenes${consulta({ vendedorId: filtros.vendedorId, sinAsignar: filtros.sinAsignar, anio: filtros.anio, mes: filtros.mes, pagina: filtros.pagina, tamanoPagina: filtros.tamanoPagina })}`);
}

export function asignarVendedor(ordenCompraId: number, request: AsignarVendedorRequest): Promise<OrdenVendedor> {
  return apiPut<OrdenVendedor>(`/comisiones-vendedores/ordenes/${ordenCompraId}/vendedor`, request);
}

export function quitarVendedor(ordenCompraId: number): Promise<void> {
  return apiDelete(`/comisiones-vendedores/ordenes/${ordenCompraId}/vendedor`);
}

export function obtenerComisiones(anio: number, mes: number): Promise<ComisionesMes> {
  return apiGet<ComisionesMes>(`/comisiones-vendedores/comisiones${consulta({ anio, mes })}`);
}

export function liquidarMes(anio: number, mes: number): Promise<ComisionesMes> {
  return apiPost<ComisionesMes>('/comisiones-vendedores/liquidaciones', { anio, mes });
}

export function pagarLiquidacion(id: number): Promise<Liquidacion> {
  return apiPostSinCuerpo<Liquidacion>(`/comisiones-vendedores/liquidaciones/${id}/pagar`);
}
