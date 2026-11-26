import { apiGet, apiPost } from '../../api/client';
import type {
  FiltrosMovimientos,
  GuardarMovimientoRequest,
  IdMovimientoResponse,
  KardexValorizado,
  MetodoValorizacion,
  PaginaMovimientos,
  ResumenKardex,
  SaldoKardex,
} from './tipos';

function construirQuery(parametros: Record<string, string | number | undefined>): string {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const texto = consulta.toString();
  return texto === '' ? '' : `?${texto}`;
}

export function listarMovimientos(filtros: FiltrosMovimientos): Promise<PaginaMovimientos> {
  const query = construirQuery({
    productoId: filtros.productoId,
    almacenId: filtros.almacenId,
    tipo: filtros.tipo,
    fechaDesde: filtros.fechaDesde,
    fechaHasta: filtros.fechaHasta,
    pagina: filtros.pagina,
    tamanoPagina: filtros.tamanoPagina,
  });
  return apiGet<PaginaMovimientos>(`/kardex-valorizado/movimientos${query}`);
}

export function registrarMovimiento(
  request: GuardarMovimientoRequest,
): Promise<IdMovimientoResponse> {
  return apiPost<IdMovimientoResponse>('/kardex-valorizado/movimientos', request);
}

export function consultarKardex(
  productoId: number,
  almacenId: number,
  metodo: MetodoValorizacion = 'Promedio',
): Promise<KardexValorizado> {
  const query = construirQuery({ productoId, almacenId, metodo });
  return apiGet<KardexValorizado>(`/kardex-valorizado/kardex${query}`);
}

export function listarSaldosKardex(almacenId?: number): Promise<SaldoKardex[]> {
  const query = construirQuery({ almacenId });
  return apiGet<SaldoKardex[]>(`/kardex-valorizado/saldos${query}`);
}

export function obtenerResumenKardex(): Promise<ResumenKardex> {
  return apiGet<ResumenKardex>('/kardex-valorizado/resumen');
}
