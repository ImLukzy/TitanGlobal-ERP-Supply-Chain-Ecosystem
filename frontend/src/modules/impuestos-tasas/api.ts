import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type { CalcularImpuesto, GuardarImpuesto, GuardarTasa, ImpuestoDetalle, ImpuestoListado, ResultadoImpuesto } from './tipos';

const ruta = '/impuestos';

export function listarImpuestos() {
  return apiGet<ImpuestoListado[]>(ruta);
}

export function obtenerImpuesto(id: number) {
  return apiGet<ImpuestoDetalle>(ruta + '/' + id);
}

export function crearImpuesto(datos: GuardarImpuesto) {
  return apiPost<{ id: number }>(ruta, datos);
}

export function actualizarImpuesto(id: number, datos: GuardarImpuesto) {
  return apiPut<void>(ruta + '/' + id, datos);
}

export function desactivarImpuesto(id: number) {
  return apiDelete(ruta + '/' + id);
}

export function registrarTasa(id: number, datos: GuardarTasa) {
  return apiPost<{ id: number; impuestoId: number }>(ruta + '/' + id + '/tasas', datos);
}

export function calcularImpuesto(datos: CalcularImpuesto) {
  return apiPost<ResultadoImpuesto>(ruta + '/calcular', datos);
}
