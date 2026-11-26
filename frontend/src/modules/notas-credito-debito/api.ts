import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import { conFiltros, todasLasPaginas, type PaginaApi } from '../../lib/accionesFinanzas';
import type { CrearNota, FacturaConLineas, FacturaParaNota, GuardarNota, NotaDetalle, NotaFila } from './tipos';

const ruta = '/notas-credito-debito';

export function listarNotas(facturaId: string, tipo: string, estado: string, pagina: number) {
  return apiGet<PaginaApi<NotaFila>>(conFiltros(ruta, { facturaId, tipo, estado, pagina }));
}

export function obtenerNota(id: number) {
  return apiGet<NotaDetalle>(ruta + '/' + id);
}

export function crearNota(datos: CrearNota) {
  return apiPost<NotaDetalle>(ruta, datos);
}

export function editarNota(id: number, datos: GuardarNota) {
  return apiPut<NotaDetalle>(ruta + '/' + id, datos);
}

export function emitirNota(id: number) {
  return apiPostSinCuerpo<NotaDetalle>(ruta + '/' + id + '/emitir');
}

export function anularNota(id: number, motivo: string) {
  return apiPost<NotaDetalle>(ruta + '/' + id + '/anular', { motivo });
}

export async function facturasElegibles() {
  const facturas = await todasLasPaginas(pagina => apiGet<PaginaApi<FacturaParaNota>>(
    conFiltros('/facturas', { estado: 'Emitida', pagina, tamanoPagina: 100 }),
  ));
  return facturas.filter(factura => factura.estado === 'Emitida' && ['PEN', 'USD'].includes(factura.moneda));
}

export function facturaParaNota(id: number) {
  return apiGet<FacturaConLineas>('/facturas/' + id);
}
