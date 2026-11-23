import type { OpcionCatalogo, OperacionRutaRequest } from './tipos';

const maximoVersion = 2147483647;
const maximoOperaciones = 100;
const maximoNombre = 120;

export const mensajes = {
  producto: 'El identificador del producto debe ser mayor que 0.',
  version: 'La versión debe ser mayor que 0.',
  operaciones: 'La ruta de producción debe tener entre 1 y 100 operaciones.',
  nombreObligatorio: 'El nombre de la operación es obligatorio.',
  nombreLargo: 'El nombre de la operación no puede superar 120 caracteres.',
  centro: 'El identificador del centro de trabajo debe ser mayor que 0.',
  preparacionRango: 'Los minutos de preparación deben estar entre 0 y 99999999.99.',
  preparacionDecimales: 'Los minutos de preparación admiten como máximo 2 decimales.',
  unidadRango: 'Los minutos por unidad deben ser mayores que 0 y no superar 99999999.99.',
  unidadDecimales: 'Los minutos por unidad admiten como máximo 2 decimales.',
} as const;

export interface LineaOperacion {
  clave: number;
  nombre: string;
  centro: OpcionCatalogo | null;
  minutosPreparacion: string;
  minutosPorUnidad: string;
}

export interface FormularioRuta {
  producto: OpcionCatalogo | null;
  version: string;
  lineas: LineaOperacion[];
}

export type ResultadoValidacion =
  | { errores: Record<string, string>; operaciones?: undefined }
  | { errores: Record<string, never>; operaciones: OperacionRutaRequest[] };

export function versionValida(version: string): boolean {
  return /^\d+$/.test(version.trim()) && Number(version) >= 1 && Number(version) <= maximoVersion;
}

// Valida un decimal de numeric(10,2): hasta 8 dígitos enteros y 2 decimales significativos.
export function validarMinutos(
  entrada: string,
  admiteCero: boolean,
  errorRango: string,
  errorDecimales: string,
): { valor: number } | { error: string } {
  const valor = /^\+?(?:(\d+)(?:[.,](\d*))?|[.,](\d+))$/.exec(entrada.trim());
  if (valor === null) {
    return { error: errorRango };
  }
  const entero = (valor[1] ?? '0').replace(/^0+(?=\d)/, '');
  const decimales = (valor[2] ?? valor[3] ?? '').replace(/0+$/, '');
  const esCero = entero === '0' && decimales === '';
  if (entero.length > 8 || (esCero && !admiteCero)) {
    return { error: errorRango };
  }
  if (decimales.length > 2) {
    return { error: errorDecimales };
  }
  return { valor: Number(decimales === '' ? entero : `${entero}.${decimales}`) };
}

// La secuencia se asigna por la posición de cada línea (1, 2, 3…), así que siempre es consecutiva.
export function validarFormularioRuta(formulario: FormularioRuta, esAlta: boolean): ResultadoValidacion {
  const errores: Record<string, string> = {};
  if (esAlta && formulario.producto === null) {
    errores.producto = mensajes.producto;
  }
  if (esAlta && !versionValida(formulario.version)) {
    errores.version = mensajes.version;
  }
  if (formulario.lineas.length < 1 || formulario.lineas.length > maximoOperaciones) {
    errores.operaciones = mensajes.operaciones;
  }

  const operaciones: OperacionRutaRequest[] = [];
  formulario.lineas.forEach((linea, indice) => {
    const nombre = linea.nombre.trim();
    if (nombre === '') {
      errores[`${linea.clave}-nombre`] = mensajes.nombreObligatorio;
    } else if (nombre.length > maximoNombre) {
      errores[`${linea.clave}-nombre`] = mensajes.nombreLargo;
    }
    if (linea.centro === null) {
      errores[`${linea.clave}-centro`] = mensajes.centro;
    }
    const preparacion = validarMinutos(linea.minutosPreparacion, true, mensajes.preparacionRango, mensajes.preparacionDecimales);
    if ('error' in preparacion) {
      errores[`${linea.clave}-preparacion`] = preparacion.error;
    }
    const porUnidad = validarMinutos(linea.minutosPorUnidad, false, mensajes.unidadRango, mensajes.unidadDecimales);
    if ('error' in porUnidad) {
      errores[`${linea.clave}-unidad`] = porUnidad.error;
    }
    if (linea.centro !== null && 'valor' in preparacion && 'valor' in porUnidad) {
      operaciones.push({
        secuencia: indice + 1,
        nombre,
        centroTrabajoId: linea.centro.id,
        minutosPreparacion: preparacion.valor,
        minutosPorUnidad: porUnidad.valor,
      });
    }
  });

  if (Object.keys(errores).length > 0) {
    return { errores };
  }
  return { errores: {}, operaciones };
}
