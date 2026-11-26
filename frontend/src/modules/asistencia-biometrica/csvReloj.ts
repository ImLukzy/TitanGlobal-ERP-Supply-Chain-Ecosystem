import type { RegistroReloj } from './tipos';

export const CABECERA_CSV = 'numeroRegistro,legajo,tipo,instante';
export const MAXIMO_REGISTROS = 1000;
const CODIGO_BOM = 0xfeff;
export const MENSAJE_CABECERA = `El archivo debe tener la cabecera ${CABECERA_CSV}.`;
export const MENSAJE_CANTIDAD = 'La importación debe tener entre 1 y 1000 registros.';

export type ResultadoCsv =
  | { valido: true; registros: RegistroReloj[] }
  | { valido: false; error: string };

function quitarComillas(celda: string): string {
  const limpia = celda.trim();
  if (limpia.length >= 2 && limpia.startsWith('"') && limpia.endsWith('"')) {
    return limpia.slice(1, -1).trim();
  }
  return limpia;
}

// Un instante ilegible se envía vacío: el servidor rechaza solo ese registro con su motivo.
function aInstante(valor: string): string | null {
  if (valor === '' || Number.isNaN(Date.parse(valor))) {
    return null;
  }
  return valor;
}

// Analiza el CSV del reloj: cabecera exacta, filas vacías ignoradas y entre 1 y 1000 registros.
export function analizarCsvReloj(texto: string): ResultadoCsv {
  const sinBom = texto.charCodeAt(0) === CODIGO_BOM ? texto.slice(1) : texto;
  const lineas = sinBom.split(/\r?\n/).filter((linea) => linea.trim() !== '');
  const cabecera = lineas[0];
  if (cabecera === undefined || cabecera.split(',').map(quitarComillas).join(',') !== CABECERA_CSV) {
    return { valido: false, error: MENSAJE_CABECERA };
  }
  const filas = lineas.slice(1);
  if (filas.length < 1 || filas.length > MAXIMO_REGISTROS) {
    return { valido: false, error: MENSAJE_CANTIDAD };
  }
  const registros: RegistroReloj[] = filas.map((linea) => {
    const celdas = linea.split(',').map(quitarComillas);
    return {
      numeroRegistro: celdas[0] ?? '',
      legajo: celdas[1] ?? '',
      tipo: celdas[2] ?? '',
      instante: aInstante(celdas[3] ?? ''),
    };
  });
  return { valido: true, registros };
}
