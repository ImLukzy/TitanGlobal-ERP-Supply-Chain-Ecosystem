import { apiGet, apiPost } from '../../api/client';
import type {
  AnularPrestamoRequest,
  CapacidadPrestamo,
  FiltrosPrestamos,
  IdResponse,
  PaginaPrestamos,
  PrestamoCreado,
  PrestamoDetalle,
  RegistrarPrestamoRequest,
  ResumenPrestamos,
} from './tipos';

// Los filtros vacíos no se envían: «Todos» omite el parámetro.
export function listarPrestamos(filtros: FiltrosPrestamos): Promise<PaginaPrestamos> {
  const consulta = new URLSearchParams();
  const termino = filtros.busqueda.trim();
  if (termino !== '') {
    consulta.set('busqueda', termino);
  }
  if (filtros.empleadoId !== null) {
    consulta.set('empleadoId', String(filtros.empleadoId));
  }
  if (filtros.tipo !== '') {
    consulta.set('tipo', filtros.tipo);
  }
  if (filtros.estado !== '') {
    consulta.set('estado', filtros.estado);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaPrestamos>(`/prestamos?${consulta.toString()}`);
}

export function obtenerResumenPrestamos(): Promise<ResumenPrestamos> {
  return apiGet<ResumenPrestamos>('/prestamos/resumen');
}

export function obtenerPrestamo(id: number): Promise<PrestamoDetalle> {
  return apiGet<PrestamoDetalle>(`/prestamos/${id}`);
}

// Sin fecha la API calcula la capacidad a hoy.
export function obtenerCapacidadPrestamo(empleadoId: number, fecha: string): Promise<CapacidadPrestamo> {
  const consulta = fecha === '' ? '' : `?fecha=${encodeURIComponent(fecha)}`;
  return apiGet<CapacidadPrestamo>(`/prestamos/capacidad/${empleadoId}${consulta}`);
}

export function registrarPrestamo(request: RegistrarPrestamoRequest): Promise<PrestamoCreado> {
  return apiPost<PrestamoCreado>('/prestamos', request);
}

export function anularPrestamo(id: number, request: AnularPrestamoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/prestamos/${id}/anulacion`, request);
}
