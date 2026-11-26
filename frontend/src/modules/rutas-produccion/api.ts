import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import { listarCentrosTrabajo } from '../centros-trabajo/api';
import { listarProductos } from '../productos/api';
import type {
  CrearRutaRequest,
  DecisionRuta,
  EditarRutaRequest,
  FiltrosRutasProduccion,
  IdResponse,
  OpcionCatalogo,
  PaginaRutasProduccion,
  RutaProduccionDetalle,
} from './tipos';

export interface PaginaCatalogo {
  items: OpcionCatalogo[];
  total: number;
  tamanoPagina: number;
}

const tamanoCatalogo = 10;

export async function buscarProductos(busqueda: string, pagina: number): Promise<PaginaCatalogo> {
  const datos = await listarProductos({ busqueda, pagina, tamanoPagina: tamanoCatalogo, categoria: '', precioMin: '', precioMax: '' });
  return { items: datos.items.map(({ id, nombre }) => ({ id, nombre })), total: datos.total, tamanoPagina: datos.tamanoPagina };
}

export async function buscarCentrosTrabajo(busqueda: string, pagina: number): Promise<PaginaCatalogo> {
  const datos = await listarCentrosTrabajo({ busqueda, pagina, tamanoPagina: tamanoCatalogo });
  return { items: datos.items.map(({ id, nombre, activo }) => ({ id, nombre, activo })), total: datos.total, tamanoPagina: datos.tamanoPagina };
}

const ruta = '/rutas-produccion';

export function listarRutasProduccion(filtros: FiltrosRutasProduccion): Promise<PaginaRutasProduccion> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  return apiGet<PaginaRutasProduccion>(`${ruta}${query === '' ? '' : `?${query}`}`);
}

export function obtenerRutaProduccion(id: number): Promise<RutaProduccionDetalle> {
  return apiGet<RutaProduccionDetalle>(`${ruta}/${id}`);
}

export function crearRutaProduccion(solicitud: CrearRutaRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(ruta, solicitud);
}

export function editarRutaProduccion(id: number, solicitud: EditarRutaRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`${ruta}/${id}`, solicitud);
}

export function cambiarEstadoRutaProduccion(id: number, accion: DecisionRuta): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`${ruta}/${id}/${accion}`);
}
