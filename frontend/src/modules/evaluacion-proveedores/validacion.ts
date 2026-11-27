export interface ValoresEvaluacion {
  proveedorId: number | '';
  periodo: string;
  cumplimiento: string;
  calidad: string;
  plazos: string;
  observaciones: string;
}

export type CampoEvaluacion = 'proveedor' | 'periodo' | 'cumplimiento' | 'calidad' | 'plazos' | 'observaciones';
export type ErroresEvaluacion = Partial<Record<CampoEvaluacion, string>>;

export function mesActual(): string {
  const hoy = new Date();
  return `${hoy.getFullYear()}-${String(hoy.getMonth() + 1).padStart(2, '0')}`;
}

export function puntajeValido(texto: string): boolean {
  if (!/^\d{1,3}$/.test(texto.trim())) {
    return false;
  }
  const valor = Number(texto);
  return valor >= 0 && valor <= 100;
}

function mensajePuntaje(nombre: string): string {
  return `El puntaje de ${nombre} debe estar entre 0 y 100.`;
}

/** Mismo orden y mismos literales que la spec de backend del módulo 12. */
export function validarEvaluacion(valores: ValoresEvaluacion, edicion: boolean): ErroresEvaluacion {
  const errores: ErroresEvaluacion = {};
  if (!edicion) {
    if (valores.proveedorId === '') {
      errores.proveedor = 'El proveedor es obligatorio.';
    }
    if (!/^\d{4}-(0[1-9]|1[0-2])$/.test(valores.periodo)) {
      errores.periodo = 'El periodo debe tener el formato AAAA-MM.';
    } else if (valores.periodo > mesActual()) {
      errores.periodo = 'El periodo no puede ser futuro.';
    }
  }
  if (!puntajeValido(valores.cumplimiento)) {
    errores.cumplimiento = mensajePuntaje('cumplimiento');
  }
  if (!puntajeValido(valores.calidad)) {
    errores.calidad = mensajePuntaje('calidad');
  }
  if (!puntajeValido(valores.plazos)) {
    errores.plazos = mensajePuntaje('plazos');
  }
  if (valores.observaciones.trim().length > 500) {
    errores.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  }
  return errores;
}
