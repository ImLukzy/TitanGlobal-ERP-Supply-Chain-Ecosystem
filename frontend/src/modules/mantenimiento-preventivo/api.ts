import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import { listarCentrosTrabajo } from '../centros-trabajo/api';
import type {
  CancelarMantenimientoRequest,
  CompletarMantenimientoRequest,
  CrearMantenimientoRequest,
  EditarMantenimientoRequest,
  FiltrosMantenimientos,
  IdResponse,
  MantenimientoCreado,
  MantenimientoDetalle,
  PaginaCentros,
  PaginaMantenimientos,
  ResumenMantenimientos,
} from './tipos';

const ruta = '/mantenimientos-preventivos';
const tamanoCatalogo = 20;

export function listarMantenimientos(filtros: FiltrosMantenimientos): Promise<PaginaMantenimientos> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  return apiGet<PaginaMantenimientos>(`${ruta}${query === '' ? '' : `?${query}`}`);
}

export function obtenerResumenMantenimientos(): Promise<ResumenMantenimientos> {
  return apiGet<ResumenMantenimientos>(`${ruta}/resumen`);
}

export function obtenerMantenimiento(id: number): Promise<MantenimientoDetalle> {
  return apiGet<MantenimientoDetalle>(`${ruta}/${id}`);
}

export function crearMantenimiento(solicitud: CrearMantenimientoRequest): Promise<MantenimientoCreado> {
  return apiPost<MantenimientoCreado>(ruta, solicitud);
}

export function editarMantenimiento(id: number, solicitud: EditarMantenimientoRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`${ruta}/${id}`, solicitud);
}

export function iniciarMantenimiento(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`${ruta}/${id}/iniciar`);
}

export function completarMantenimiento(id: number, solicitud: CompletarMantenimientoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`${ruta}/${id}/completar`, solicitud);
}

export function cancelarMantenimiento(id: number, solicitud: CancelarMantenimientoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`${ruta}/${id}/cancelar`, solicitud);
}

// El formulario solo ofrece centros activos; el filtro del listado también muestra los inactivos.
export async function buscarCentros(busqueda: string, soloActivos: boolean): Promise<PaginaCentros> {
  const datos = await listarCentrosTrabajo({
    busqueda,
    activo: soloActivos ? true : '',
    pagina: 1,
    tamanoPagina: tamanoCatalogo,
  });
  return {
    items: datos.items.map(({ id, codigo, nombre }) => ({ id, codigo, nombre })),
    total: datos.total,
    tamanoPagina: datos.tamanoPagina,
  };
}
