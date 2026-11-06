import { obtenerResumenClientes } from '../modules/clientes/api';
import type { ResumenClientes } from '../modules/clientes/tipos';
import { obtenerResumenContratos } from '../modules/contratos-comerciales/api';
import type { ContratosResumen } from '../modules/contratos-comerciales/tipos';
import { obtenerResumenCotizaciones } from '../modules/cotizaciones/api';
import type { ResumenCotizaciones } from '../modules/cotizaciones/tipos';
import { obtenerResumenAlertas } from '../modules/notificaciones-alertas/api';
import type { ResumenAlertas } from '../modules/notificaciones-alertas/tipos';
import { obtenerResumenOrdenesAbastecimiento } from '../modules/ordenes-abastecimiento/api';
import type { ResumenOrdenesAbastecimiento } from '../modules/ordenes-abastecimiento/tipos';
import { obtenerResumenProductos } from '../modules/productos/api';
import type { ResumenProductos } from '../modules/productos/tipos';
import { obtenerResumenReclamos } from '../modules/reclamos-postventa/api';
import type { ResumenReclamos } from '../modules/reclamos-postventa/tipos';
import { obtenerResumenSolicitudes } from '../modules/solicitudes-compra/api';
import type { ResumenSolicitudes } from '../modules/solicitudes-compra/tipos';
import { formateadorMoneda, formatearCantidad } from '../lib/formato';

const MAXIMO_FILAS = 5;

export interface DatosPanel {
  clientes: ResumenClientes | null;
  productos: ResumenProductos | null;
  contratos: ContratosResumen | null;
  cotizaciones: ResumenCotizaciones | null;
  abastecimiento: ResumenOrdenesAbastecimiento | null;
  solicitudes: ResumenSolicitudes | null;
  reclamos: ResumenReclamos | null;
  alertas: ResumenAlertas | null;
}

export interface Indicador {
  etiqueta: string;
  valor: string | null;
  detalle: string;
  slug: string;
}

export interface PuntoAtencion {
  etiqueta: string;
  cantidad: number;
  nivel: 'critico' | 'aviso';
  slug: string;
}

export interface FilaBarra {
  id: string;
  etiqueta: string;
  valor: number;
  texto: string;
}

export interface Proceso {
  nombre: string;
  slug: string;
  completado: number;
  enCurso: number;
  descartado: number;
}

function valido<T extends object>(resultado: PromiseSettledResult<T>, campos: (keyof T)[]): T | null {
  if (resultado.status === 'rejected') {
    return null;
  }
  const datos: T | null | undefined = resultado.value;
  if (datos === null || datos === undefined) {
    return null;
  }
  return campos.every((campo) => datos[campo] !== undefined && datos[campo] !== null) ? datos : null;
}

export async function cargarPanel(): Promise<DatosPanel> {
  const [clientes, productos, contratos, cotizaciones, abastecimiento, solicitudes, reclamos, alertas] = await Promise.allSettled([
    obtenerResumenClientes(),
    obtenerResumenProductos(),
    obtenerResumenContratos(),
    obtenerResumenCotizaciones(),
    obtenerResumenOrdenesAbastecimiento(),
    obtenerResumenSolicitudes(),
    obtenerResumenReclamos(),
    obtenerResumenAlertas(),
  ]);
  return {
    clientes: valido(clientes, ['totalClientes', 'clientesConOrdenes', 'topClientesPorMonto']),
    productos: valido(productos, ['totalProductos', 'precioPromedio', 'porCategoria', 'masVendidos']),
    contratos: valido(contratos, ['vigentes', 'montoComprometidoVigente', 'porcentajeConsumoVigente']),
    cotizaciones: valido(cotizaciones, ['emitidas', 'aceptadas', 'rechazadas', 'vencidas']),
    abastecimiento: valido(abastecimiento, ['emitidas', 'recibidas', 'anuladas', 'atrasadas', 'montoPorRecibir']),
    solicitudes: valido(solicitudes, ['pendientes', 'aprobadas', 'rechazadas']),
    reclamos: valido(reclamos, ['abiertos', 'enProceso', 'resueltos', 'cerrados', 'rechazados', 'vencidos']),
    alertas: valido(alertas, ['altas']),
  };
}

export function panelSinDatos(datos: DatosPanel): boolean {
  return Object.values(datos).every((valor) => valor === null);
}

export function panelIncompleto(datos: DatosPanel): boolean {
  return Object.values(datos).some((valor) => valor === null);
}

export function construirIndicadores(datos: DatosPanel): Indicador[] {
  const { clientes, productos, contratos, abastecimiento } = datos;
  return [
    {
      etiqueta: 'Clientes',
      valor: clientes === null ? null : String(clientes.totalClientes),
      detalle: clientes === null ? '' : `${clientes.clientesConOrdenes} con órdenes de compra`,
      slug: '01-clientes',
    },
    {
      etiqueta: 'Productos en catálogo',
      valor: productos === null ? null : String(productos.totalProductos),
      detalle: productos === null ? '' : `Precio promedio ${formateadorMoneda.format(productos.precioPromedio)}`,
      slug: '21-productos',
    },
    {
      etiqueta: 'Contratos vigentes',
      valor: contratos === null ? null : formateadorMoneda.format(contratos.montoComprometidoVigente),
      detalle: contratos === null
        ? ''
        : `${formatearCantidad(contratos.vigentes, 'contrato', 'contratos')}, ${Math.round(contratos.porcentajeConsumoVigente)} % consumido`,
      slug: '05-contratos-comerciales',
    },
    {
      etiqueta: 'Compras por recibir',
      valor: abastecimiento === null ? null : formateadorMoneda.format(abastecimiento.montoPorRecibir),
      detalle: abastecimiento === null ? '' : `${formatearCantidad(abastecimiento.emitidas, 'orden emitida', 'órdenes emitidas')}`,
      slug: '15-ordenes-abastecimiento',
    },
  ];
}

export function construirAtencion(datos: DatosPanel): PuntoAtencion[] {
  const { alertas, abastecimiento, reclamos, cotizaciones, solicitudes } = datos;
  const puntos: PuntoAtencion[] = [
    { etiqueta: 'Alertas de prioridad alta', cantidad: alertas?.altas ?? 0, nivel: 'critico', slug: '78-notificaciones-alertas' },
    { etiqueta: 'Órdenes de abastecimiento atrasadas', cantidad: abastecimiento?.atrasadas ?? 0, nivel: 'critico', slug: '15-ordenes-abastecimiento' },
    { etiqueta: 'Reclamos vencidos', cantidad: reclamos?.vencidos ?? 0, nivel: 'critico', slug: '09-reclamos-postventa' },
    { etiqueta: 'Cotizaciones vencidas', cantidad: cotizaciones?.vencidas ?? 0, nivel: 'aviso', slug: '03-cotizaciones' },
    { etiqueta: 'Solicitudes de compra por aprobar', cantidad: solicitudes?.pendientes ?? 0, nivel: 'aviso', slug: '13-solicitudes-compra' },
  ];
  return puntos.filter((punto) => punto.cantidad > 0);
}

export function construirProcesos(datos: DatosPanel): Proceso[] {
  const { cotizaciones, solicitudes, abastecimiento, reclamos } = datos;
  const procesos: Proceso[] = [];
  if (cotizaciones !== null) {
    procesos.push({ nombre: 'Cotizaciones', slug: '03-cotizaciones', completado: cotizaciones.aceptadas, enCurso: cotizaciones.emitidas, descartado: cotizaciones.rechazadas });
  }
  if (solicitudes !== null) {
    procesos.push({ nombre: 'Solicitudes de compra', slug: '13-solicitudes-compra', completado: solicitudes.aprobadas, enCurso: solicitudes.pendientes, descartado: solicitudes.rechazadas });
  }
  if (abastecimiento !== null) {
    procesos.push({ nombre: 'Órdenes de abastecimiento', slug: '15-ordenes-abastecimiento', completado: abastecimiento.recibidas, enCurso: abastecimiento.emitidas, descartado: abastecimiento.anuladas });
  }
  if (reclamos !== null) {
    procesos.push({ nombre: 'Reclamos', slug: '09-reclamos-postventa', completado: reclamos.resueltos + reclamos.cerrados, enCurso: reclamos.abiertos + reclamos.enProceso, descartado: reclamos.rechazados });
  }
  return procesos;
}

function ordenarYRecortar(filas: FilaBarra[]): FilaBarra[] {
  return [...filas].sort((a, b) => b.valor - a.valor).slice(0, MAXIMO_FILAS);
}

export function barrasClientes(clientes: ResumenClientes): FilaBarra[] {
  return ordenarYRecortar(clientes.topClientesPorMonto.map((cliente) => ({
    id: String(cliente.clienteId),
    etiqueta: cliente.nombre,
    valor: cliente.montoTotal,
    texto: formateadorMoneda.format(cliente.montoTotal),
  })));
}

export function barrasVendidos(productos: ResumenProductos): FilaBarra[] {
  return ordenarYRecortar(productos.masVendidos.map((producto) => ({
    id: String(producto.productoId),
    etiqueta: producto.nombre,
    valor: producto.montoVendido,
    texto: formateadorMoneda.format(producto.montoVendido),
  })));
}

export function barrasCategorias(productos: ResumenProductos): FilaBarra[] {
  return ordenarYRecortar(productos.porCategoria.map((categoria) => ({
    id: categoria.categoria,
    etiqueta: categoria.categoria,
    valor: categoria.totalProductos,
    texto: formatearCantidad(categoria.totalProductos, 'producto', 'productos'),
  })));
}
