import { apiGet, apiPost, apiPut } from '../../api/client';
import { listarProductos } from '../productos/api';
import { listarProveedores } from '../proveedores/api';
import type {
  AnularInspeccionRequest,
  DictaminarInspeccionRequest,
  FiltrosInspeccionesInsumos,
  GuardarInspeccionRequest,
  IdResponse,
  InspeccionCreada,
  InspeccionInsumoDetalle,
  PaginaCatalogo,
  PaginaInspeccionesInsumos,
  ResumenInspeccionesInsumos,
} from './tipos';

const ruta = '/inspecciones-insumos';
const tamanoCatalogo = 20;

export function listarInspeccionesInsumos(filtros: FiltrosInspeccionesInsumos): Promise<PaginaInspeccionesInsumos> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  return apiGet<PaginaInspeccionesInsumos>(`${ruta}${query === '' ? '' : `?${query}`}`);
}

export function obtenerResumenInspeccionesInsumos(): Promise<ResumenInspeccionesInsumos> {
  return apiGet<ResumenInspeccionesInsumos>(`${ruta}/resumen`);
}

export function obtenerInspeccionInsumo(id: number): Promise<InspeccionInsumoDetalle> {
  return apiGet<InspeccionInsumoDetalle>(`${ruta}/${id}`);
}

export function crearInspeccionInsumo(solicitud: GuardarInspeccionRequest): Promise<InspeccionCreada> {
  return apiPost<InspeccionCreada>(ruta, solicitud);
}

export function editarInspeccionInsumo(id: number, solicitud: GuardarInspeccionRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`${ruta}/${id}`, solicitud);
}

export function dictaminarInspeccionInsumo(id: number, solicitud: DictaminarInspeccionRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`${ruta}/${id}/dictaminar`, solicitud);
}

export function anularInspeccionInsumo(id: number, solicitud: AnularInspeccionRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`${ruta}/${id}/anular`, solicitud);
}

export async function buscarProductos(busqueda: string, pagina: number): Promise<PaginaCatalogo> {
  const datos = await listarProductos({ busqueda, pagina, tamanoPagina: tamanoCatalogo, categoria: '', precioMin: '', precioMax: '' });
  return { items: datos.items.map(({ id, nombre }) => ({ id, nombre })), total: datos.total, tamanoPagina: datos.tamanoPagina };
}

export async function buscarProveedoresActivos(busqueda: string, pagina: number): Promise<PaginaCatalogo> {
  const datos = await listarProveedores({ busqueda, activo: true, pagina, tamanoPagina: tamanoCatalogo });
  return {
    items: datos.items.map(({ id, razonSocial }) => ({ id, nombre: razonSocial })),
    total: datos.total,
    tamanoPagina: datos.tamanoPagina,
  };
}
