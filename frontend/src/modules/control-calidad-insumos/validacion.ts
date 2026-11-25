import type {
  AnularInspeccionRequest,
  DictaminarInspeccionRequest,
  GuardarInspeccionRequest,
  OpcionCatalogo,
  ResultadoDictamen,
} from './tipos';

export const mensajes = {
  producto: 'El identificador del producto debe ser mayor que 0.',
  proveedor: 'El identificador del proveedor debe ser mayor que 0.',
  loteObligatorio: 'El lote es obligatorio.',
  loteLargo: 'El lote no puede superar 50 caracteres.',
  fechaObligatoria: 'La fecha de inspección es obligatoria.',
  fechaFutura: 'La fecha de inspección no puede ser posterior a hoy.',
  recibidaRango: 'La cantidad recibida debe ser mayor que 0 y como máximo 1000000000.',
  recibidaDecimales: 'La cantidad recibida admite como máximo 3 decimales.',
  muestraRango: 'La cantidad de la muestra debe ser mayor que 0.',
  muestraDecimales: 'La cantidad de la muestra admite como máximo 3 decimales.',
  muestraMayor: 'La muestra no puede superar la cantidad recibida.',
  unidadObligatoria: 'La unidad de medida es obligatoria.',
  unidadLarga: 'La unidad de medida no puede superar 20 caracteres.',
  toleranciaRango: 'La tolerancia de defectos debe estar entre 0 y 100.',
  toleranciaDecimales: 'La tolerancia de defectos admite como máximo 2 decimales.',
  inspectorObligatorio: 'El inspector es obligatorio.',
  inspectorLargo: 'El inspector no puede superar 100 caracteres.',
  resultado: 'El resultado debe ser Aprobada o Rechazada.',
  defectuosaNegativa: 'La cantidad defectuosa no puede ser negativa.',
  defectuosaDecimales: 'La cantidad defectuosa admite como máximo 3 decimales.',
  defectuosaMayor: 'La cantidad defectuosa no puede superar la muestra.',
  observaciones: 'Las observaciones no pueden superar 500 caracteres.',
  motivoObligatorio: 'El motivo de anulación es obligatorio.',
  motivoLargo: 'El motivo de anulación no puede superar 250 caracteres.',
  rangoFechas: 'La fecha desde no puede ser posterior a la fecha hasta.',
} as const;

const desfasePeruMs = 5 * 60 * 60 * 1000;

// «Hoy» en hora de Perú (UTC-5, sin horario de verano), igual que el backend. No usa la zona del navegador
// ni toISOString() del instante actual, que da el día UTC: a partir de las 19:00 en Perú ya sería mañana.
export function hoyPeru(ahora: number = Date.now()): string {
  return new Date(ahora - desfasePeruMs).toISOString().slice(0, 10);
}

type Numero = { valor: number } | { invalido: true } | { decimales: number; valor: number };

// Lee un número escrito por el usuario (punto o coma decimal) y cuenta sus decimales significativos.
function leerNumero(entrada: string): Numero {
  const partes = /^(-?)(\d+)(?:[.,](\d+))?$/.exec(entrada.trim());
  if (partes === null) {
    return { invalido: true };
  }
  const decimales = (partes[3] ?? '').replace(/0+$/, '');
  const valor = Number(`${partes[1]}${partes[2]}${decimales === '' ? '' : `.${decimales}`}`);
  return { decimales: decimales.length, valor };
}

function validarCantidad(
  entrada: string,
  admite: (valor: number) => boolean,
  maximoDecimales: number,
  errorRango: string,
  errorDecimales: string,
): { valor: number } | { error: string } {
  const numero = leerNumero(entrada);
  if ('invalido' in numero || !admite(numero.valor)) {
    return { error: errorRango };
  }
  if ('decimales' in numero && numero.decimales > maximoDecimales) {
    return { error: errorDecimales };
  }
  return { valor: numero.valor };
}

function texto(entrada: string, maximo: number, obligatorio: string, largo: string): { valor: string } | { error: string } {
  const valor = entrada.trim();
  if (valor === '') {
    return { error: obligatorio };
  }
  return valor.length > maximo ? { error: largo } : { valor };
}

export interface FormularioInspeccion {
  producto: OpcionCatalogo | null;
  proveedor: OpcionCatalogo | null;
  lote: string;
  fechaInspeccion: string;
  cantidadRecibida: string;
  cantidadMuestra: string;
  unidadMedida: string;
  tolerancia: string;
  inspector: string;
}

type Errores = Partial<Record<string, string>>;

export function validarInspeccion(
  formulario: FormularioInspeccion,
  hoy: string,
): { errores: Errores; solicitud?: GuardarInspeccionRequest } {
  const errores: Errores = {};
  if (formulario.producto === null) errores.producto = mensajes.producto;
  if (formulario.proveedor === null) errores.proveedor = mensajes.proveedor;
  const lote = texto(formulario.lote, 50, mensajes.loteObligatorio, mensajes.loteLargo);
  if ('error' in lote) errores.lote = lote.error;
  if (formulario.fechaInspeccion === '') {
    errores.fechaInspeccion = mensajes.fechaObligatoria;
  } else if (formulario.fechaInspeccion > hoy) {
    errores.fechaInspeccion = mensajes.fechaFutura;
  }
  const recibida = validarCantidad(formulario.cantidadRecibida, (valor) => valor > 0 && valor <= 1000000000, 3, mensajes.recibidaRango, mensajes.recibidaDecimales);
  if ('error' in recibida) errores.cantidadRecibida = recibida.error;
  const muestra = validarCantidad(formulario.cantidadMuestra, (valor) => valor > 0, 3, mensajes.muestraRango, mensajes.muestraDecimales);
  if ('error' in muestra) {
    errores.cantidadMuestra = muestra.error;
  } else if ('valor' in recibida && muestra.valor > recibida.valor) {
    errores.cantidadMuestra = mensajes.muestraMayor;
  }
  const unidad = texto(formulario.unidadMedida, 20, mensajes.unidadObligatoria, mensajes.unidadLarga);
  if ('error' in unidad) errores.unidadMedida = unidad.error;
  const tolerancia = validarCantidad(formulario.tolerancia, (valor) => valor >= 0 && valor <= 100, 2, mensajes.toleranciaRango, mensajes.toleranciaDecimales);
  if ('error' in tolerancia) errores.tolerancia = tolerancia.error;
  const inspector = texto(formulario.inspector, 100, mensajes.inspectorObligatorio, mensajes.inspectorLargo);
  if ('error' in inspector) errores.inspector = inspector.error;

  if (Object.keys(errores).length > 0 || formulario.producto === null || formulario.proveedor === null
    || !('valor' in lote) || !('valor' in recibida) || !('valor' in muestra) || !('valor' in unidad)
    || !('valor' in tolerancia) || !('valor' in inspector)) {
    return { errores };
  }
  return {
    errores,
    solicitud: {
      productoId: formulario.producto.id,
      proveedorId: formulario.proveedor.id,
      lote: lote.valor.toUpperCase(),
      fechaInspeccion: formulario.fechaInspeccion,
      cantidadRecibida: recibida.valor,
      cantidadMuestra: muestra.valor,
      unidadMedida: unidad.valor,
      toleranciaDefectosPorcentaje: tolerancia.valor,
      inspector: inspector.valor,
    },
  };
}

// Porcentaje de defectos con 2 decimales, con el mismo redondeo que el backend (la mitad se aleja del cero).
export function porcentajeDefectos(defectuosa: number, muestra: number): number {
  return Math.round((defectuosa / muestra) * 10000 + Number.EPSILON) / 100;
}

export function leerDefectuosa(entrada: string): number | null {
  const numero = leerNumero(entrada);
  return 'invalido' in numero || numero.valor < 0 ? null : numero.valor;
}

export interface FormularioDictamen {
  resultado: ResultadoDictamen | '';
  cantidadDefectuosa: string;
  observaciones: string;
}

export function validarDictamen(
  formulario: FormularioDictamen,
  muestra: number,
): { errores: Errores; solicitud?: DictaminarInspeccionRequest } {
  const errores: Errores = {};
  if (formulario.resultado === '') errores.resultado = mensajes.resultado;
  const defectuosa = validarCantidad(formulario.cantidadDefectuosa, (valor) => valor >= 0, 3, mensajes.defectuosaNegativa, mensajes.defectuosaDecimales);
  if ('error' in defectuosa) {
    errores.cantidadDefectuosa = defectuosa.error;
  } else if (defectuosa.valor > muestra) {
    errores.cantidadDefectuosa = mensajes.defectuosaMayor;
  }
  const observaciones = formulario.observaciones.trim();
  if (observaciones.length > 500) errores.observaciones = mensajes.observaciones;
  if (Object.keys(errores).length > 0 || formulario.resultado === '' || !('valor' in defectuosa)) {
    return { errores };
  }
  const solicitud: DictaminarInspeccionRequest = { resultado: formulario.resultado, cantidadDefectuosa: defectuosa.valor };
  if (observaciones !== '') solicitud.observaciones = observaciones;
  return { errores, solicitud };
}

export function validarAnulacion(motivo: string): { error?: string; solicitud?: AnularInspeccionRequest } {
  const resultado = texto(motivo, 250, mensajes.motivoObligatorio, mensajes.motivoLargo);
  return 'error' in resultado ? { error: resultado.error } : { solicitud: { motivo: resultado.valor } };
}
