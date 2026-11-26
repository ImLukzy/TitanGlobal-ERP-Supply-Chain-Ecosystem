import { apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import { listarClientes } from '../clientes/api';
import { listarProductos } from '../productos/api';
import type { FacturaDetalle, FiltrosFactura, GuardarFactura, PaginaFacturas, ResumenFacturas } from './tipos';

const ruta = '/facturas';

export function fechaInicioPeru(dia: string) {
  return dia + 'T05:00:00Z';
}

function fechaFinPeru(dia: string) {
  return new Date(Date.parse(fechaInicioPeru(dia)) + 86_400_000).toISOString().slice(0, 10)
    + 'T04:59:59.9999999Z';
}

function parametrosFecha(desde: string, hasta: string) {
  const params = new URLSearchParams();
  if (desde) params.set('fechaDesde', fechaInicioPeru(desde));
  if (hasta) params.set('fechaHasta', fechaFinPeru(hasta));
  return params;
}

export function listarFacturas(filtros: FiltrosFactura, pagina: number) {
  const params = parametrosFecha(filtros.fechaDesde, filtros.fechaHasta);
  const { busqueda, clienteId, tipoComprobante, estado, moneda } = filtros;
  for (const [clave, valor] of Object.entries({ busqueda, clienteId, tipoComprobante, estado, moneda })) {
    if (valor.trim()) params.set(clave, valor.trim());
  }
  params.set('pagina', String(pagina));
  return apiGet<PaginaFacturas>(ruta + '?' + params);
}

export function resumenFacturas(moneda: string, desde: string, hasta: string) {
  const params = parametrosFecha(desde, hasta);
  params.set('moneda', moneda || 'PEN');
  return apiGet<ResumenFacturas>(ruta + '/resumen?' + params);
}

export function obtenerFactura(id: number) {
  return apiGet<FacturaDetalle>(ruta + '/' + id);
}

export function guardarFactura(datos: GuardarFactura, id?: number) {
  return id === undefined ? apiPost<FacturaDetalle>(ruta, datos) : apiPut<FacturaDetalle>(ruta + '/' + id, datos);
}

export function emitirFactura(id: number) {
  return apiPostSinCuerpo<FacturaDetalle>(ruta + '/' + id + '/emitir');
}

export function anularFactura(id: number, motivo: string) {
  return apiPost<FacturaDetalle>(ruta + '/' + id + '/anular', { motivo });
}

async function leerCatalogo<T>(cargar: (pagina: number) => Promise<{ items: T[]; total: number }>) {
  const primera = await cargar(1);
  const items = [...primera.items];
  for (let pagina = 2; items.length < primera.total; pagina++) {
    const siguiente = await cargar(pagina);
    if (siguiente.items.length === 0) break;
    items.push(...siguiente.items);
  }
  return items;
}

export async function cargarCatalogosFactura() {
  const [clientes, productos, impuestos] = await Promise.all([
    leerCatalogo(pagina => listarClientes('', pagina, 100)),
    leerCatalogo(pagina => listarProductos({ busqueda: '', categoria: '', precioMin: '', precioMax: '', pagina, tamanoPagina: 100 })),
    apiGet<{ id: number; codigo: string; nombre: string; activo: boolean }[]>('/impuestos'),
  ]);
  return { clientes, productos, impuestos: impuestos.filter(tasa => tasa.activo && tasa.codigo === 'IGV') };
}
