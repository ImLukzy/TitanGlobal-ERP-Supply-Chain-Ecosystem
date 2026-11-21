import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type { CrearListaRequest, DecisionLista, EditarListaRequest, FiltrosListasMateriales, ListaMaterialDetalle, PaginaListasMateriales } from './tipos';
import { validarCantidad } from './validacion';

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

function leerDetalle(body: string): ListaMaterialDetalle {
  // Al recorrer cadenas JSON completas se evita interpretar el contenido de nombres como una clave.
  const cadenas = /("(?:[^"\\]|\\.)*")(\s*:\s*(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?))?/g;
  const exacto = body.replace(cadenas, (texto: string, clave: string, propiedad?: string, numero?: string) => {
    if (clave === '"cantidad"' && propiedad !== undefined && numero !== undefined) {
      return `${clave}:${JSON.stringify(numero)}`;
    }
    return texto;
  });
  return JSON.parse(exacto) as ListaMaterialDetalle;
}

export function obtenerListaMaterial(id: number): Promise<ListaMaterialDetalle> {
  return apiGet(`${ruta}/${id}`, leerDetalle);
}

function serializarLista(solicitud: CrearListaRequest | EditarListaRequest): string {
  const componentes = solicitud.componentes.map(({ componenteProductoId, cantidad, unidadMedida }) => {
    const decimal = validarCantidad(cantidad);
    if ('error' in decimal) {
      throw new Error(decimal.error);
    }
    return `{"componenteProductoId":${JSON.stringify(componenteProductoId)},"cantidad":${decimal.cantidad},"unidadMedida":${JSON.stringify(unidadMedida.trim())}}`;
  });
  const cabecera = 'productoId' in solicitud
    ? `"productoId":${JSON.stringify(solicitud.productoId)},"version":${JSON.stringify(solicitud.version)},`
    : '';
  return `{${cabecera}"componentes":[${componentes.join(',')}]}`;
}

export function crearListaMaterial(solicitud: CrearListaRequest): Promise<{ id: number }> {
  return apiPost<{ id: number }, CrearListaRequest>(ruta, solicitud, serializarLista);
}

export function editarListaMaterial(id: number, solicitud: EditarListaRequest): Promise<{ id: number }> {
  return apiPut<{ id: number }, EditarListaRequest>(`${ruta}/${id}`, solicitud, serializarLista);
}

export function cambiarEstadoListaMaterial(id: number, accion: DecisionLista): Promise<{ id: number }> {
  return apiPostSinCuerpo(`${ruta}/${id}/${accion}`);
}
