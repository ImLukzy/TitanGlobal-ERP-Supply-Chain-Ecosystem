import { apiGet, apiPost } from '../../api/client';
import type { FiltrosAlerta, PaginaAlertas, ResumenAlertas } from './tipos';

export function listarAlertas(filtros: FiltrosAlerta): Promise<PaginaAlertas> {
  const consulta = new URLSearchParams();
  consulta.set('pagina', String(filtros.pagina));
  consulta.set('tamanoPagina', String(filtros.tamanoPagina));
  if (filtros.tipo !== '') {
    consulta.set('tipo', filtros.tipo);
  }
  if (filtros.severidad !== '') {
    consulta.set('severidad', filtros.severidad);
  }
  if (filtros.incluirDescartadas) {
    consulta.set('incluirDescartadas', 'true');
  }
  return apiGet<PaginaAlertas>(`/alertas?${consulta.toString()}`);
}

export function obtenerResumenAlertas(): Promise<ResumenAlertas> {
  return apiGet<ResumenAlertas>('/alertas/resumen');
}

export function descartarAlerta(clave: string): Promise<void> {
  return apiPost<void>('/alertas/descartar', { clave });
}

export function restaurarAlerta(clave: string): Promise<void> {
  return apiPost<void>('/alertas/restaurar', { clave });
}
