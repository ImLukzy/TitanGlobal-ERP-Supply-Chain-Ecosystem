import { apiDelete, apiGet, apiPost, apiPostSinCuerpo, apiPut } from '../../api/client';
import type {
  BoletaPago,
  CrearPlanillaRequest,
  FiltrosAfiliaciones,
  FiltrosBonos,
  FiltrosDetalles,
  FiltrosPlanillas,
  GuardarAfiliacionRequest,
  GuardarBonoRequest,
  IdResponse,
  PagarPlanillaRequest,
  PaginaAfiliaciones,
  PaginaBonos,
  PaginaPlanillaEmpleados,
  PaginaPlanillas,
  ParametrosPlanilla,
  PlanillaCreada,
  PlanillaDetalle,
  ResultadoCalculoPlanilla,
  ResumenPlanillas,
} from './tipos';

// Los filtros vacíos no se envían: «Todos» omite el parámetro.
export function listarPlanillas(filtros: FiltrosPlanillas): Promise<PaginaPlanillas> {
  const consulta = new URLSearchParams();
  if (filtros.anio !== null) {
    consulta.set('anio', String(filtros.anio));
  }
  if (filtros.tipo !== '') {
    consulta.set('tipo', filtros.tipo);
  }
  if (filtros.estado !== '') {
    consulta.set('estado', filtros.estado);
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaPlanillas>(`/planillas?${consulta.toString()}`);
}

export function obtenerResumenPlanillas(): Promise<ResumenPlanillas> {
  return apiGet<ResumenPlanillas>('/planillas/resumen');
}

export function obtenerParametrosPlanilla(): Promise<ParametrosPlanilla> {
  return apiGet<ParametrosPlanilla>('/planillas/parametros');
}

export function obtenerPlanilla(id: number): Promise<PlanillaDetalle> {
  return apiGet<PlanillaDetalle>(`/planillas/${id}`);
}

export function listarDetallesPlanilla(id: number, filtros: FiltrosDetalles): Promise<PaginaPlanillaEmpleados> {
  const consulta = new URLSearchParams();
  const termino = filtros.busqueda.trim();
  if (termino !== '') {
    consulta.set('busqueda', termino);
  }
  if (filtros.soloIncidencias) {
    consulta.set('soloIncidencias', 'true');
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaPlanillaEmpleados>(`/planillas/${id}/detalles?${consulta.toString()}`);
}

export function obtenerBoleta(detalleId: number): Promise<BoletaPago> {
  return apiGet<BoletaPago>(`/planillas/boletas/${detalleId}`);
}

export function crearPlanilla(request: CrearPlanillaRequest): Promise<PlanillaCreada> {
  return apiPost<PlanillaCreada>('/planillas', request);
}

export function calcularPlanilla(id: number): Promise<ResultadoCalculoPlanilla> {
  return apiPostSinCuerpo<ResultadoCalculoPlanilla>(`/planillas/${id}/calculo`);
}

export function aprobarPlanilla(id: number): Promise<IdResponse> {
  return apiPostSinCuerpo<IdResponse>(`/planillas/${id}/aprobacion`);
}

export function pagarPlanilla(id: number, request: PagarPlanillaRequest): Promise<IdResponse> {
  return apiPost<IdResponse>(`/planillas/${id}/pago`, request);
}

export function listarBonos(filtros: FiltrosBonos): Promise<PaginaBonos> {
  const consulta = new URLSearchParams();
  if (filtros.anio !== null) {
    consulta.set('anio', String(filtros.anio));
  }
  if (filtros.mes !== null) {
    consulta.set('mes', String(filtros.mes));
  }
  if (filtros.empleadoId !== null) {
    consulta.set('empleadoId', String(filtros.empleadoId));
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaBonos>(`/bonos-planilla?${consulta.toString()}`);
}

export function crearBono(request: GuardarBonoRequest): Promise<IdResponse> {
  return apiPost<IdResponse>('/bonos-planilla', request);
}

export function eliminarBono(id: number): Promise<void> {
  return apiDelete(`/bonos-planilla/${id}`);
}

export function listarAfiliaciones(filtros: FiltrosAfiliaciones): Promise<PaginaAfiliaciones> {
  const consulta = new URLSearchParams();
  if (filtros.empleadoId !== null) {
    consulta.set('empleadoId', String(filtros.empleadoId));
  }
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  return apiGet<PaginaAfiliaciones>(`/afiliaciones-pension?${consulta.toString()}`);
}

export function guardarAfiliacion(empleadoId: number, request: GuardarAfiliacionRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/afiliaciones-pension/${empleadoId}`, request);
}
