import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type { CrearListaRequest, DecisionLista, EditarListaRequest, FiltrosListasMateriales, ListaMaterialDetalle, PaginaListasMateriales } from './tipos';

const ruta = '/listas-materiales';

export function listarListasMateriales(filtros: FiltrosListasMateriales): Promise<PaginaListasMateriales> {
  const consulta = new URLSearchParams();
  if (filtros.productoId !== undefined) {
    consulta.set('productoId', String(filtros.productoId));
  }
  if (filtros.estado !== '') {
    consulta.set('estado', filtros.estado);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet(`${ruta}?${consulta.toString()}`);
}

export function obtenerListaMaterial(id: number): Promise<ListaMaterialDetalle> {
  return apiGet(`${ruta}/${id}`);
}

function limpiarComponentes(solicitud: EditarListaRequest): EditarListaRequest['componentes'] {
  return solicitud.componentes.map(({ componenteProductoId, cantidad, unidadMedida }) => ({
    componenteProductoId,
    cantidad,
    unidadMedida: unidadMedida.trim(),
  }));
}

export function crearListaMaterial(solicitud: CrearListaRequest): Promise<{ id: number }> {
  return apiPost<{ id: number }>(ruta, {
    productoId: solicitud.productoId,
    version: solicitud.version,
    componentes: limpiarComponentes(solicitud),
  });
}

export function editarListaMaterial(id: number, solicitud: EditarListaRequest): Promise<{ id: number }> {
  return apiPut<{ id: number }>(`${ruta}/${id}`, { componentes: limpiarComponentes(solicitud) });
}

export function cambiarEstadoListaMaterial(id: number, accion: DecisionLista): Promise<{ id: number }> {
  return apiPostSinCuerpo(`${ruta}/${id}/${accion}`);
}
