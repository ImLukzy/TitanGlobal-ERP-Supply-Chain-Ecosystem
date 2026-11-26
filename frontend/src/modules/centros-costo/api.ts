import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import { conFiltros, todasLasPaginas, type PaginaApi } from '../../lib/accionesFinanzas';
import { listarEmpleados } from '../empleados/api';
import type { CentroDetalle, CentroFila, GuardarCentro } from './tipos';

const ruta = '/centros-costo';

export function listarCentros(activo: string, centroCostoPadreId: string, pagina: number, tamanoPagina = 20) {
  return apiGet<PaginaApi<CentroFila>>(conFiltros(ruta, { activo, centroCostoPadreId, pagina, tamanoPagina }));
}

export function catalogoCentros() {
  return todasLasPaginas(pagina => listarCentros('', '', pagina, 100));
}

export function responsables() {
  return todasLasPaginas(async pagina => {
    const datos = await listarEmpleados({ estado: '', busqueda: '', pagina, tamanoPagina: 100 });
    return { ...datos, totalRegistros: datos.total, totalPaginas: Math.ceil(datos.total / datos.tamanoPagina) };
  });
}

export function obtenerCentro(id: number) {
  return apiGet<CentroDetalle>(ruta + '/' + id);
}

export function guardarCentro(datos: GuardarCentro, id?: number) {
  return id === undefined ? apiPost<CentroDetalle>(ruta, datos) : apiPut<CentroDetalle>(ruta + '/' + id, datos);
}

export function cambiarVigencia(id: number, activar: boolean) {
  return apiPostSinCuerpo<CentroDetalle>(ruta + '/' + id + (activar ? '/reactivar' : '/desactivar'));
}
